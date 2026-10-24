using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Bindings.Tests;

public class ViewModelConstructionAnalyzerTests
{
    private const string Model = """
        public class TestViewModel : Bindings.IViewModel
        {
            public TestViewModel() { }
            public TestViewModel(int value) { }
        }
        """;

    private const string BindMethod = "public System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken cancellationToken) => default;";

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(Compilation compilation) =>
        compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();

    private static CSharpCompilation CreateCompilation(string source, string? stubs = null, string path = "User.cs") =>
        BindingsUsageAnalyzerTests.CreateCompilation(source, stubs, path);

    private static string ViewWith(string member) => Model + " public class TestView : Bindings.IView { " + BindMethod + member + " }";

    [Theory]
    [InlineData("public void Build() { var model = new TestViewModel(); }", "new TestViewModel()")]
    [InlineData("public void Build() { var model = new TestViewModel(1); }", "new TestViewModel(1)")]
    [InlineData("public void Build() { TestViewModel model = new(); }", "new()")]
    [InlineData("public void Build() { TestViewModel model = new(1); }", "new(1)")]
    [InlineData("private TestViewModel _model = new TestViewModel();", "new TestViewModel()")]
    [InlineData("private static TestViewModel s_model = new();", "new()")]
    [InlineData("public TestViewModel Model { get; } = new();", "new()")]
    [InlineData("public TestView() { var model = new TestViewModel(); }", "new TestViewModel()")]
    [InlineData("public TestView() : this(new TestViewModel()) { } public TestView(TestViewModel model) { }", "new TestViewModel()")]
    [InlineData("public TestViewModel Model => new TestViewModel();", "new TestViewModel()")]
    [InlineData("public static TestViewModel Build() => new TestViewModel();", "new TestViewModel()")]
    [InlineData("public void Build() { System.Func<TestViewModel> factory = () => new TestViewModel(); }", "new TestViewModel()")]
    [InlineData("public void Build() { TestViewModel Local() => new TestViewModel(); }", "new TestViewModel()")]
    [InlineData("public void Build() { var models = new[] { new TestViewModel() }; }", "new TestViewModel()")]
    public async Task ReportsConstructorCallsThroughoutAView(string member, string expectedLocation)
    {
        var compilation = CreateCompilation(ViewWith(member));
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var diagnostic = Assert.Single(await AnalyzeAsync(compilation));
        Assert.Equal("BNDP002", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("View 'TestView'", diagnostic.GetMessage());
        Assert.Contains("ViewModel 'TestViewModel'", diagnostic.GetMessage());
        Assert.Contains("outside the View", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Equal(expectedLocation, diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Theory]
    [InlineData("public class TestView : Bindings.IView<TestViewModel> { public void Initialize(TestViewModel model) { }", "TestView")]
    [InlineData("public interface ICustomView : Bindings.IView { } public class TestView : ICustomView {", "TestView")]
    [InlineData("public class ViewBase : Bindings.IView { public System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken token) => default; } public class TestView : ViewBase {", "TestView")]
    public async Task IncludesGenericAndInheritedViewInterfaces(string declaration, string expectedViewName)
    {
        var compilation = CreateCompilation(Model + declaration + BindMethod + " public void Build() { var model = new TestViewModel(); } }");
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var diagnostic = Assert.Single(await AnalyzeAsync(compilation));
        Assert.Equal("BNDP002", diagnostic.Id);
        Assert.Contains("View '" + expectedViewName + "'", diagnostic.GetMessage());
    }

    [Fact]
    public async Task IncludesViewOwnedNestedHelpers()
    {
        var source = ViewWith("private class Factory { public TestViewModel Build() => new TestViewModel(); }");
        Assert.Equal("BNDP002", Assert.Single(await AnalyzeAsync(CreateCompilation(source))).Id);
    }

    [Fact]
    public async Task IncludesStructViewImplementations()
    {
        var source = Model + "public struct TestView : Bindings.IView { " + BindMethod + " public void Build() { var model = new TestViewModel(); } }";
        Assert.Equal("BNDP002", Assert.Single(await AnalyzeAsync(CreateCompilation(source))).Id);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public async Task IncludesViewModelClassesAndStructs(string kind)
    {
        var source = "public " + kind + " Model : Bindings.IViewModel { } public class TestView : Bindings.IView { " + BindMethod + " public void Build() { var model = new Model(); } }";
        Assert.Equal("BNDP002", Assert.Single(await AnalyzeAsync(CreateCompilation(source))).Id);
    }

    [Fact]
    public async Task IncludesInheritedViewModelInterfacesAndGenericTypes()
    {
        const string models = "public class BaseModel : Bindings.IViewModel { } public class Model<T> : BaseModel { }";
        var source = models + " public class TestView : Bindings.IView { " + BindMethod + " public void Build() { var model = new Model<int>(); } }";
        var diagnostic = Assert.Single(await AnalyzeAsync(CreateCompilation(source)));
        Assert.Equal("BNDP002", diagnostic.Id);
        Assert.Contains("Model<int>", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("Bindings.IViewModel", "", "")]
    [InlineData("TestViewModel", "", "")]
    [InlineData("U", ", U", " where U : Bindings.IViewModel")]
    public async Task IncludesTypeParameterConstructionWhenConstraintsIdentifyAViewModel(string constraint, string extraTypeParameter, string extraConstraint)
    {
        var source = ViewWith("public T Build<T" + extraTypeParameter + ">() where T : " + constraint + ", new()" + extraConstraint + " => new T();");
        var compilation = CreateCompilation(source);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var diagnostic = Assert.Single(await AnalyzeAsync(compilation));
        Assert.Equal("BNDP002", diagnostic.Id);
        Assert.Contains("ViewModel 'T'", diagnostic.GetMessage());
    }

    [Fact]
    public async Task IncludesStructConstrainedTypeParameterConstruction()
    {
        var compilation = CreateCompilation(ViewWith("public T Build<T>() where T : struct, Bindings.IViewModel => new T();"));
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal("BNDP002", Assert.Single(await AnalyzeAsync(compilation)).Id);
    }

    [Fact]
    public async Task RecognizesInterfaceAliases()
    {
        var compilation = CreateCompilation("""
            using VM = Model;
            using ViewInterface = Bindings.IView;
            public class Model : Bindings.IViewModel { }
            public class TestView : ViewInterface
            {
                public System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken token) => default;
                public void Build() { var model = new VM(); }
            }
            """);
        var diagnostic = Assert.Single(await AnalyzeAsync(compilation));
        Assert.Equal("BNDP002", diagnostic.Id);
        Assert.Contains("ViewModel 'Model'", diagnostic.GetMessage());
    }

    [Fact]
    public async Task AttributeAloneDoesNotIdentifyAViewModel()
    {
        var compilation = CreateCompilation("""
            [Bindings.ViewModel] public partial class TestViewModel { }
            public class TestView : Bindings.IView
            {
                public System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken token) => default;
                public void Build() { var model = new TestViewModel(); }
            }
            """);
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Empty(await AnalyzeAsync(compilation));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DetectsViewAndViewModelInterfacesAfterGenerationWithoutStoppingGeneration(bool alreadyImplementsViewModel)
    {
        var compilation = CreateCompilation($$"""
            [Bindings.ViewModel] public partial class CountViewModel {{(alreadyImplementsViewModel ? ": Bindings.IViewModel" : "")}} { }
            public partial class CountView
            {
                public void Build(Bindings.IMvvmPublisher publisher) { var model = new CountViewModel(publisher); }
            }
            """);
        var driver = CSharpGeneratorDriver.Create(new ViewModelGenerator()).RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Equal(2, driver.GetRunResult().GeneratedTrees.Length);
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var diagnostic = Assert.Single(await AnalyzeAsync(output));
        Assert.Equal("BNDP002", diagnostic.Id);
        Assert.Equal("User.cs", diagnostic.Location.SourceTree!.FilePath);
    }

    [Theory]
    [InlineData("public void Accept(TestViewModel model) { }")]
    [InlineData("public void Build() { var text = new object(); }")]
    [InlineData("public void Build() { var models = new TestViewModel[1]; }")]
    [InlineData("public void Build() { var models = new System.Collections.Generic.List<TestViewModel>(); }")]
    [InlineData("public T Build<T>() where T : new() => new T();")]
    [InlineData("public void Build() { TestViewModel model = default; }")]
    [InlineData("public TestViewModel Build() => System.Activator.CreateInstance<TestViewModel>();")]
    public async Task DoesNotFlagOtherObjectsReceivingModelsOrNonConstructorFactories(string member)
    {
        Assert.Empty(await AnalyzeAsync(CreateCompilation(ViewWith(member))));
    }

    [Theory]
    [InlineData("public class ViewByNameOnly { public void Build() { var model = new TestViewModel(); } }")]
    [InlineData("public class EntryPoint : UnityEngine.Behaviour { public void Build() { var model = new TestViewModel(); } }")]
    [InlineData("namespace Other { public interface IView { } } public class TestView : Other.IView { public void Build() { var model = new TestViewModel(); } }")]
    [InlineData("public class OtherModel { } public class TestView : Bindings.IView { public System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken token) => default; public void Build() { var model = new OtherModel(); } }")]
    [InlineData("namespace Other { public interface IViewModel { } } public class OtherModel : Other.IViewModel { } public class TestView : Bindings.IView { public System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken token) => default; public void Build() { var model = new OtherModel(); } }")]
    [InlineData("namespace Other { public class ViewModelAttribute : System.Attribute { } } [Other.ViewModel] public class OtherModel { } public class TestView : Bindings.IView { public System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken token) => default; public void Build() { var model = new OtherModel(); } }")]
    public async Task UsesSymbolIdentityRatherThanTypeNames(string source)
    {
        Assert.Empty(await AnalyzeAsync(CreateCompilation(Model + source)));
    }

    [Theory]
    [InlineData("// <auto-generated/>\n", "User.cs")]
    [InlineData("", "User.g.cs")]
    public async Task GeneratedConstructionIsDiagnosed(string prefix, string path)
    {
        Assert.Equal("BNDP002", Assert.Single(await AnalyzeAsync(CreateCompilation(prefix + ViewWith("public void Build() { var model = new TestViewModel(); }"), path: path))).Id);
    }

    [Fact]
    public async Task DiagnosticCanBeSuppressedOrDowngraded()
    {
        var source = ViewWith("public void Build() {\n#pragma warning disable BNDP002\nvar model = new TestViewModel(); }");
        Assert.Empty(await AnalyzeAsync(CreateCompilation(source)));
        var compilation = CreateCompilation(ViewWith("public void Build() { var model = new TestViewModel(); }"));
        compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(
            compilation.Options.SpecificDiagnosticOptions.SetItem("BNDP002", ReportDiagnostic.Info)));
        Assert.Equal(DiagnosticSeverity.Info, Assert.Single(await AnalyzeAsync(compilation)).Severity);
    }

    [Theory]
    [InlineData("")]
    [InlineData("namespace Bindings { public interface IView { } }")]
    [InlineData("namespace Bindings { public class ViewModelAttribute : System.Attribute { } }")]
    public async Task MissingTypesDoNotCauseAnalyzerFailures(string stubs)
    {
        Assert.Empty(await AnalyzeAsync(CreateCompilation("""
            [Bindings.ViewModel] public class Model { }
            public class TestView : Bindings.IView { public void Build() { var model = new Model(); } }
            """, stubs)));
    }

    [Theory]
    [InlineData("public void Build() { var model = new Missing(); }")]
    [InlineData("public T Build<T, U>() where T : U, new() where U : T => new T();")]
    public async Task ErrorSymbolsAndCyclicConstraintsDoNotCauseAnalyzerFailures(string member)
    {
        Assert.Empty(await AnalyzeAsync(CreateCompilation(ViewWith(member))));
    }

    [Fact]
    public async Task ReportsEveryConstructionAndDoesNotOfferAnAutomaticArchitectureChange()
    {
        var source = ViewWith("public void Build() { var first = new TestViewModel(); var second = new TestViewModel(); }");
        using var workspace = new AdhocWorkspace();
        var document = workspace.AddProject("Construction", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) })
            .AddDocument("User.cs", SourceText.From(source));
        document = document.Project.AddDocument("Stubs.cs", SourceText.From(ViewModelGeneratorTests.AttributeStubs)).Project.GetDocument(document.Id)!;
        var diagnostics = await AnalyzeAsync((await document.Project.GetCompilationAsync())!);
        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("BNDP002", diagnostic.Id));
        var provider = new BindingsPracticesCodeFixProvider();
        Assert.DoesNotContain("BNDP002", provider.FixableDiagnosticIds);
        var actions = new List<CodeAction>();
        await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostics[0], (action, _) => actions.Add(action), CancellationToken.None));
        Assert.Empty(actions);
    }
}