using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Bindings.Tests;

public class ViewModelAggregationAnalyzerTests
{
    private const string Child = "public class Child : Bindings.IViewModel { public int Value { get; set; } public void Update() { } } public class ChildComponent { public Child Value { get; set; } } ";

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string members, string owner = "class", bool generate = false)
    {
        Compilation compilation = BindingsUsageAnalyzerTests.CreateCompilation(Child +
            (generate ? "[Bindings.ViewModel] " : "") + "public partial " + owner + " ParentViewModel : Bindings.IViewModel { " + members + " }");
        if (generate)
        {
            var driver = CSharpGeneratorDriver.Create(new ViewModelGenerator())
                .RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _);
            Assert.Empty(driver.GetRunResult().Diagnostics);
        }

        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
    }

    [Theory]
    [InlineData("public Child Child { get; set; }")]
    [InlineData("public Child Child { get; } public ParentViewModel(Child child) => Child = child;")]
    [InlineData("private Child _child; public Child Child => _child;")]
    [InlineData("private Child _child; public Child Child { get { return _child; } set => _child = value; }")]
    [InlineData("private Child _child; public Child Child { get => (_child); set { _child = value; } }")]
    [InlineData("private Child _child; public Child First => _child; public Child Second => First;")]
    [InlineData("private Child _child = new Child(); public string Name => nameof(_child);")]
    [InlineData("public Bindings.IViewModel Child { get; set; }")]
    [InlineData("private Child _child; public Child Child => _child!;")]
    [InlineData("private Child _child; public Bindings.IViewModel Child => (Bindings.IViewModel)_child;")]
    public async Task ReportsStorageAndTransparentExposure(string members)
    {
        var diagnostic = Assert.Single(await Analyze(members));
        Assert.Equal("BNDP003", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("ParentViewModel", diagnostic.GetMessage());
        Assert.Contains("only stores or exposes", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Equal("User.cs", diagnostic.Location.SourceTree!.FilePath);
    }

    [Theory]
    [InlineData("public void Update() => _child.Update();")]
    [InlineData("public int Value => Child.Value;")]
    [InlineData("public void Update() { Child.Value = 42; }")]
    [InlineData("public void Update() { Consume(Child); } private void Consume(Child child) { }")]
    [InlineData("public void Update() { System.Action action = () => Child.Update(); }")]
    [InlineData("public void Update() { void Local() => Child.Update(); Local(); }")]
    [InlineData("public void Update() { var child = Second; child.Update(); } public Child Second => Child;")]
    [InlineData("public ParentViewModel(Child child) { _child = child; _child.Update(); }")]
    [InlineData("public void Update() => Child?.Update();")]
    [InlineData("public void Update() => _child = Create(_child); private Child Create(Child child) => child;")]
    public async Task AcceptsBehavioralReferencesIncludingAliases(string logic)
    {
        Assert.Empty(await Analyze("private Child _child; public Child Child { get => _child; set => _child = value; } " + logic));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public async Task ReportsEachUnusedStorageIndependently(string owner)
    {
        var diagnostics = await Analyze("public Child First { get; set; } public Child Second { get; set; } public void Update() => First.Update();", owner);
        Assert.Contains("Second", Assert.Single(diagnostics).GetMessage());
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task SharedAliasChainsPropagateUseRegardlessOfDeclarationOrder(
        bool reverseOrder,
        bool targetAlreadyUsed)
    {
        const string first = "public Child First => Second; ";
        const string second = "public Child Second => _child; ";
        const string third = "public Child Third => Second; ";
        var aliases = reverseOrder ? third + second + first : first + second + third;
        var extraUse = targetAlreadyUsed ? "Second.Update(); " : "";
        Assert.Empty(await Analyze(
            "private Child _child; " + aliases +
            "public void Update() { First.Update(); Third.Update(); " + extraUse + "}"));
    }

    [Fact]
    public async Task SelfReferencingAliasDoesNotMarkUnrelatedStorageAsUsed()
    {
        var diagnostic = Assert.Single(await Analyze(
            "private Child _child; public Child Self => Self; public void Update() => Self.Update();"));
        Assert.Equal("BNDP003", diagnostic.Id);
        Assert.Contains("_child", diagnostic.GetMessage());
    }

    [Fact]
    public async Task GeneratedRequiredConstructorDoesNotCountAsUse()
    {
        Assert.Equal("BNDP003", Assert.Single(await Analyze("[Bindings.Required] private Child _child; public Child Child => _child;", generate: true)).Id);
    }

    [Fact]
    public async Task GeneratedSchemaPropertyIsTrackedAsAlias()
    {
        Assert.Empty(await Analyze("[Bindings.Schema(\"ChildComponent.Value\")] private Child _child; public void Update() => Child.Update();", generate: true));
    }

    [Theory]
    [InlineData("public static Child Child { get; set; }")]
    [InlineData("public Child[] Children { get; set; }")]
    [InlineData("public Child Child => new Child();")]
    [InlineData("public object Value { get; set; }")]
    public async Task IgnoresNonStorageAndNonViewModelMembers(string members)
    {
        Assert.Empty(await Analyze(members));
    }

    [Fact]
    public async Task OnlyOwningInstanceReferencesCount()
    {
        Assert.Equal("BNDP003", Assert.Single(await Analyze("public Child Child { get; set; } public void Update(ParentViewModel other) => other.Child.Update();")).Id);
    }

    [Fact]
    public async Task SupportsPartialTypesAndGenericConstraints()
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation("""
            public partial class Parent<T> : Bindings.IViewModel where T : Bindings.IViewModel { private T _child; public T Child => _child; }
            public partial class Parent<T> { public void Update() { Consume(Child); } private void Consume(T child) { } }
            """);
        Assert.Empty(await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync());
    }

    [Fact]
    public async Task IgnoresMissingInterfacesButDiagnosesGeneratedOwners()
    {
        var missing = BindingsUsageAnalyzerTests.CreateCompilation("class Parent { private Missing _child; }", stubs: "");
        Assert.Empty(await missing.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync());
        var generated = BindingsUsageAnalyzerTests.CreateCompilation(Child + "class Parent : Bindings.IViewModel { private Child _child; }", path: "Owner.g.cs");
        Assert.Equal("BNDP003", Assert.Single(await generated.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync()).Id);
    }

    [Fact]
    public async Task AbstractContractsAreNotStorage()
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation("""
            public interface IParent : Bindings.IViewModel { Bindings.IViewModel Child { get; } }
            public abstract class Parent : Bindings.IViewModel { public abstract Bindings.IViewModel Child { get; } }
            """);
        Assert.Empty(await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync());
    }

    [Fact]
    public async Task CyclicAliasesAndBrokenSymbolsDoNotCrash()
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation("""
            public class Parent : Bindings.IViewModel {
                private Bindings.IViewModel _unused;
                public Bindings.IViewModel First => Second;
                public Bindings.IViewModel Second => First;
                public Missing Broken { get; set; }
                public void Update() { Consume(First); }
                private void Consume(object value) { }
            }
            """);
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Equal("BNDP003", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task GeneratedReferencesCountAsBehavior()
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(Child + "public partial class Parent : Bindings.IViewModel { public Child Child { get; set; } }")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("public partial class Parent { public void GeneratedUpdate() => Child.Update(); }", path: "Generated.g.cs"));
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData("Helper.g.cs", "")]
    [InlineData("Helper.g.i.cs", "")]
    [InlineData("Helper.generated.cs", "")]
    [InlineData("Helper.designer.cs", "")]
    [InlineData("TemporaryGeneratedFile_Helper.cs", "")]
    [InlineData("Helper.cs", "// <auto-generated/>\n")]
    [InlineData("Helper.cs", "// <autogenerated/>\n")]
    public async Task GeneratedGetterAliasesAndBehavioralUsageAreCollected(string path, string prefix)
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(Child + "public partial class Parent : Bindings.IViewModel { private Child _child; }")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(prefix + "public partial class Parent { public Child Child => _child; public void GeneratedUpdate() => Child.Update(); }", path: path));
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Empty(diagnostics);

        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("public partial class Parent { public void Update() => Child.Update(); }", path: "Usage.cs"));
        Assert.Empty(await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync());
    }

    [Fact]
    public async Task GeneratedCodeAttributeUsageCountsAsBehavior()
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(Child + """
            namespace System.CodeDom.Compiler {
                public sealed class GeneratedCodeAttribute : System.Attribute { public GeneratedCodeAttribute(string tool, string version) { } }
            }
            public class Parent : Bindings.IViewModel {
                public Child Child { get; set; }
                [System.CodeDom.Compiler.GeneratedCode("Test", "1")]
                public void Update() => Child.Update();
            }
            """);
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Empty(diagnostics);
    }

    [Theory]
    [InlineData(GeneratedKind.MarkedGenerated)]
    [InlineData(GeneratedKind.NotGenerated)]
    public async Task GeneratedTreeConfigurationDoesNotExcludeBehavior(GeneratedKind kind)
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(Child + "public partial class Parent : Bindings.IViewModel { public Child Child { get; set; } }")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("public partial class Parent { public void Update() => Child.Update(); }", path: "Usage.g.cs"));
        compilation = compilation.WithOptions(compilation.Options.WithSyntaxTreeOptionsProvider(new GeneratedTreeOptions(kind)));
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Empty(diagnostics);
    }

    private sealed class GeneratedTreeOptions(GeneratedKind kind) : SyntaxTreeOptionsProvider
    {
        public override GeneratedKind IsGenerated(SyntaxTree tree, System.Threading.CancellationToken cancellationToken) =>
            tree.FilePath == "Usage.g.cs" ? kind : GeneratedKind.Unknown;

        public override bool TryGetDiagnosticValue(SyntaxTree tree, string diagnosticId, System.Threading.CancellationToken cancellationToken, out ReportDiagnostic severity)
        {
            severity = ReportDiagnostic.Default;
            return false;
        }

        public override bool TryGetGlobalDiagnosticValue(string diagnosticId, System.Threading.CancellationToken cancellationToken, out ReportDiagnostic severity)
        {
            severity = ReportDiagnostic.Default;
            return false;
        }
    }

    [Theory]
    [InlineData("true", 0)]
    [InlineData("false", 0)]
    [InlineData("invalid", 0)]
    public async Task AnalyzerConfigDoesNotExcludeGeneratedUsage(string setting, int expectedCount)
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(Child + "public partial class Parent : Bindings.IViewModel { public Child Child { get; set; } }")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("public partial class Parent { public void Update() => Child.Update(); }", path: "Usage.g.cs"));
        var options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, new GeneratedCodeConfigProvider(setting));
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer()), options).GetAnalyzerDiagnosticsAsync();
        Assert.Equal(expectedCount, diagnostics.Length);
    }

    private sealed class GeneratedCodeConfigProvider(string setting) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => new GeneratedCodeConfig("");
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new GeneratedCodeConfig(tree.FilePath == "Usage.g.cs" ? setting : "");
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;
    }

    private sealed class GeneratedCodeConfig(string setting) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            value = setting;
            return key == "generated_code" && setting.Length != 0;
        }
    }

    [Fact]
    public async Task SeverityCanBeConfiguredWithoutStoppingGeneration()
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(Child + "[Bindings.ViewModel] public partial class ParentViewModel { public Child Child { get; set; } }");
        CSharpGeneratorDriver.Create(new ViewModelGenerator()).RunGeneratorsAndUpdateCompilation(compilation, out var output, out var diagnostics);
        Assert.Empty(diagnostics);
        Assert.Contains(output.SyntaxTrees, static tree => tree.FilePath.EndsWith(".g.cs"));
        output = output.WithOptions(output.Options.WithSpecificDiagnosticOptions(ImmutableDictionary<string, ReportDiagnostic>.Empty.Add("BNDP003", ReportDiagnostic.Warn)));
        var reported = await output.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Equal(DiagnosticSeverity.Warning, Assert.Single(reported).Severity);
    }
}