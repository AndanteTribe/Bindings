using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Bindings.Tests;

public class SchemaAssignmentWrapperAnalyzerTests
{
    private const string Header = """
        public partial class TestViewModel : Bindings.IViewModel {
            [Bindings.Schema("Text.text")] private int _count;
            [Bindings.Schema("Text.text")] private string _name;
            public int Count { get; set; }
            public string Name { get; set; }
        """;

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, bool allowCompilerErrors = false)
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(source);
        if (!allowCompilerErrors)
        {
            Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        return await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
    }

    [Theory]
    [InlineData("public void Set(int count) => Count = count;")]
    [InlineData("private void Set(int count) { this.Count = count; }")]
    [InlineData("internal void Set(int count, string name) { Count = count; Name = name; }")]
    [InlineData("public void Set(int count, string name) { Name = (name); Count = count; }")]
    [InlineData("public void Set(int count) => Count = count; public System.Action<int> Handler => Set;")]
    [InlineData("public void Set(int count = 0) => Count = count;")]
    [InlineData("public void Set<T>(int count) => Count = count;")]
    public async Task ReportsOnlyDirectParameterCopies(string member)
    {
        var diagnostic = Assert.Single(await AnalyzeAsync(Header + member + "}"));
        Assert.Equal("BNDP004", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Method 'Set'", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Contains("Assign the properties directly", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Equal("Set", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Theory]
    [InlineData("public void Set(int count) { Count = count; Save(); } private void Save() { }")]
    [InlineData("public void Set(int count) { if (count > 0) Count = count; }")]
    [InlineData("public void Set(int count) { Count = count; return; }")]
    [InlineData("public void Set(int count) { var value = count; Count = value; }")]
    [InlineData("public void Set(int count) => Count = count + 1;")]
    [InlineData("public void Set(int count) => Count = (int)count;")]
    [InlineData("public void Set(int count) => Count = checked((int)count);")]
    [InlineData("public void Set(short count) => Count = count;")]
    [InlineData("public void Set(string? name) => Name = name;")]
    [InlineData("public void Set(int count) => Name = count.ToString();")]
    [InlineData("public void Set(object name) => Name = (string)name;")]
    [InlineData("public void Set(int count) => Count += count;")]
    [InlineData("public int Set(int count) => Count = count;")]
    [InlineData("public async void Set(int count) { Count = count; }")]
    [InlineData("public void Set(int count, int unused) => Count = count;")]
    [InlineData("public void Set() => Count = 1;")]
    [InlineData("public void Set(int count) => Count = 1;")]
    [InlineData("public void Set(ref int count) => Count = count;")]
    [InlineData("public void Set(in int count) => Count = count;")]
    [InlineData("public void Set(out int count) { count = 0; Count = count; }")]
    [InlineData("public void Set(int first, int second) { Count = first; Count = second; }")]
    [InlineData("public void Set(int count) { }")]
    [InlineData("public void Set(int count) { Count = ; }")]
    [InlineData("public void Set(int count) { void Copy() => Count = count; Copy(); }")]
    [InlineData("public void Set(int count) { System.Action copy = () => Count = count; copy(); }")]
    [InlineData("public TestViewModel(int count) { Count = count; }")]
    [InlineData("public void Set(int count) => Other = count; public int Other { get; set; }")]
    [InlineData("public void Set(int count) => _count = count;")]
    [InlineData("[Bindings.Schema(\"Slider.onValueChanged\")] public void Set(int count) => Count = count;")]
    public async Task ExcludesProcessingAndNonWrapperMethods(string member)
    {
        Assert.Empty(await AnalyzeAsync(Header + member + "}", allowCompilerErrors: member.Contains("Count = ;")));
    }

    [Theory]
    [InlineData("private set;")]
    [InlineData("protected set;")]
    [InlineData("internal set;")]
    public async Task ExcludesSettersThatAreNotPublic(string setter)
    {
        var source = Header.Replace("public int Count { get; set; }", "public int Count { get; " + setter + " }");
        Assert.Empty(await AnalyzeAsync(source + "public void Set(int count) => Count = count; }"));
    }

    [Theory]
    [InlineData("public int Count { get; }")]
    [InlineData("public int Count { get; init; }")]
    [InlineData("public Missing Count { get; set; }")]
    public async Task IncompleteOrInvalidAssignmentsDoNotCrash(string property)
    {
        var source = Header.Replace("public int Count { get; set; }", property);
        Assert.Empty(await AnalyzeAsync(source + "public void Set(int count) => Count = count; }", allowCompilerErrors: true));
    }

    [Fact]
    public async Task ExcludesRepeatedArgumentsAcrossDifferentProperties()
    {
        Assert.Empty(await AnalyzeAsync(Header + """
            [Bindings.Schema("Text.text")] private int _second;
            public int Second { get; set; }
            public void Set(int first, int second) { Count = first; Second = first; }
            }
            """));
    }

    [Fact]
    public async Task ExcludesIndexersAndImplicitUserDefinedConversions()
    {
        Assert.Empty(await AnalyzeAsync(Header + "public int this[int index] { get => 0; set { } } public void Set(int count) => this[count] = count; }"));
        Assert.Empty(await AnalyzeAsync(Header + "public void Set(Number count) => Count = count; } public struct Number { public static implicit operator int(Number value) => 0; }"));
    }

    [Fact]
    public async Task InheritedSchemaPropertiesOnTheCurrentInstanceAreIncluded()
    {
        var diagnostic = Assert.Single(await AnalyzeAsync(Header + "} public class Derived : TestViewModel { public void Set(int count) => base.Count = count; }"));
        Assert.Equal("BNDP004", diagnostic.Id);
    }

    [Fact]
    public async Task LookalikeAndMissingMetadataDoNotMatch()
    {
        var lookalike = Header.Replace("[Bindings.Schema(\"Text.text\")]", "[Other.Schema]");
        Assert.Empty(await AnalyzeAsync(lookalike + "public void Set(int count) => Count = count; } namespace Other { public class SchemaAttribute : System.Attribute { } }"));
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation("class Model { public void Set(Missing value) => Count = value; }", stubs: "");
        Assert.Empty(await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync());
    }

    [Theory]
    [InlineData("Generated.g.cs", "")]
    [InlineData("Generated.cs", "// <auto-generated/>\n")]
    public async Task GeneratedWrappersAreAlsoDiagnosed(string path, string prefix)
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(prefix + Header + "public void Set(int count) => Count = count; }", path: path);
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Equal("BNDP004", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task ExcludesChildAndOtherInstanceAssignments()
    {
        Assert.Empty(await AnalyzeAsync(Header + "private TestViewModel _other; public void Set(int count) => _other.Count = count; }"));
    }

    [Fact]
    public async Task ExcludesOrdinaryClassesAndMissingSchemaAttributes()
    {
        Assert.Empty(await AnalyzeAsync(Header.Replace(": Bindings.IViewModel", "") + "public void Set(int count) => Count = count; }"));
        Assert.Empty(await AnalyzeAsync(Header.Replace("[Bindings.Schema(\"Text.text\")]", "") + "public void Set(int count) => Count = count; }"));
    }

    [Fact]
    public async Task ExcludesStaticMethodsAndProperties()
    {
        var source = Header.Replace("public int Count", "public static int Count");
        Assert.Empty(await AnalyzeAsync(source + "public static void Set(int count) => Count = count; }"));
        Assert.Empty(await AnalyzeAsync(source + "public void Set(int count) => Count = count; }"));
    }

    [Theory]
    [InlineData("IContract", "public void Set(int count) => Count = count;", "interface IContract { void Set(int count); }")]
    [InlineData("IContract", "void IContract.Set(int count) => Count = count;", "interface IContract { void Set(int count); }")]
    [InlineData("IContract<int>", "public void Set(int count) => Count = count;", "interface IContract<T> { void Set(T count); }")]
    public async Task ExcludesInterfaceContracts(string contract, string member, string declaration)
    {
        var source = Header.Replace(": Bindings.IViewModel", ": Bindings.IViewModel, " + contract);
        Assert.Empty(await AnalyzeAsync(source + member + "}" + declaration));
    }

    [Fact]
    public async Task DoesNotExcludeUnrelatedOverloadsOfInterfaceMethods()
    {
        var source = Header.Replace(": Bindings.IViewModel", ": Bindings.IViewModel, IContract");
        var diagnostic = Assert.Single(await AnalyzeAsync(source + """
            public void Set(string name) => Name = name;
            public void Set(int count) => Count = count;
            }
            interface IContract { void Set(string name); }
            """));
        Assert.Equal("BNDP004", diagnostic.Id);
    }

    [Fact]
    public async Task ExcludesOverridesAndSchemaPartialMethods()
    {
        var derived = Header.Replace(": Bindings.IViewModel", ": Base, Bindings.IViewModel");
        Assert.Empty(await AnalyzeAsync(derived + "public override void Set(int count) => Count = count; } public class Base { public virtual void Set(int count) { } }"));
        Assert.Empty(await AnalyzeAsync(Header + "[Bindings.Schema(\"Slider.onValueChanged\")] partial void Set(int count); partial void Set(int count) => Count = count; }"));
    }

    [Fact]
    public async Task ExcludesPartialInterfaceImplementations()
    {
        var source = Header.Replace(": Bindings.IViewModel", ": Bindings.IViewModel, IContract");
        Assert.Empty(await AnalyzeAsync(source + "public partial void Set(int count); public partial void Set(int count) => Count = count; } interface IContract { void Set(int count); }"));
    }

    [Fact]
    public async Task ReportsActualGeneratedSchemaPropertiesAndContinuesGeneration()
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation("""
            [Bindings.ViewModel] public partial class SampleViewModel {
                [Bindings.Schema("TMPro.TMP_Text.text")] private int _count;
                public void Set(int count) => Count = count;
            }
            """);
        var driver = CSharpGeneratorDriver.Create(new ViewModelGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        Assert.Empty(generatorDiagnostics);
        Assert.Equal(2, driver.GetRunResult().GeneratedTrees.Length);
        Assert.DoesNotContain(output.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var diagnostics = await output.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Equal("BNDP004", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task SeverityAndSuppressionAreConfigurable()
    {
        Assert.Empty(await AnalyzeAsync(Header + "\n#pragma warning disable BNDP004\npublic void Set(int count) => Count = count; }"));
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(Header + "public void Set(int count) => Count = count; }");
        compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(
            compilation.Options.SpecificDiagnosticOptions.SetItem("BNDP004", ReportDiagnostic.Info)));
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();
        Assert.Equal(DiagnosticSeverity.Info, Assert.Single(diagnostics).Severity);
    }
}