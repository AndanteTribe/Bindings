using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Bindings.Tests;

public class BindingsPracticesAnalyzerTests
{
    private const string Header = "[Bindings.ViewModel] public partial class TestViewModel { ";

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(Compilation compilation) =>
        compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsPracticesAnalyzer())).GetAnalyzerDiagnosticsAsync();

    private static Compilation Generate(string source)
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(source);
        var driver = CSharpGeneratorDriver.Create(new ViewModelGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return output;
    }

    [Fact]
    public void PracticeRulesAreEnabledErrorsInTheirOwnCategory()
    {
        var descriptors = new BindingsPracticesAnalyzer().SupportedDiagnostics;
        Assert.Equal(new[] { "BNDP001", "BNDP002" }, descriptors.Select(descriptor => descriptor.Id));
        Assert.All(descriptors, descriptor =>
        {
            Assert.Equal("Bindings.Practices", descriptor.Category);
            Assert.Equal(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.True(descriptor.IsEnabledByDefault);
        });
        Assert.Same(typeof(ViewModelGenerator).Assembly, typeof(BindingsPracticesCodeFixProvider).Assembly);
    }

    [Theory]
    [InlineData("_publisher.PublishRebindMessage<TestViewModel>();")]
    [InlineData("this._publisher.PublishRebindMessage<global::TestViewModel>();")]
    [InlineData("System.Action action = () => _publisher.PublishRebindMessage<TestViewModel>();")]
    [InlineData("void Rebind() { _publisher.PublishRebindMessage<TestViewModel>(); }")]
    public async Task ReportsEquivalentSelfRebindCalls(string statement)
    {
        var diagnostics = await AnalyzeAsync(Generate(Header + "public void Update() { " + statement + " } }"));
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BNDP001", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("PublishRebindMessage()", diagnostic.GetMessage());
        Assert.Contains("TestViewModel", diagnostic.GetMessage());
        Assert.Contains("PublishRebindMessage<", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
        Assert.Equal("User.cs", diagnostic.Location.SourceTree.FilePath);
    }

    [Theory]
    [InlineData("public void Update() { PublishRebindMessage(); }")]
    [InlineData("public void Update(Bindings.IMvvmPublisher other) { other.PublishRebindMessage<TestViewModel>(); }")]
    [InlineData("public void Update(TestViewModel other) { other._publisher.PublishRebindMessage<TestViewModel>(); }")]
    [InlineData("public void Update() { _publisher.PublishRebindMessage<OtherViewModel>(); }")]
    [InlineData("public void Update() { _publisher?.PublishRebindMessage<TestViewModel>(); }")]
    [InlineData("private Bindings.IMvvmPublisher Other => _publisher; public void Update() { Other.PublishRebindMessage<TestViewModel>(); }")]
    [InlineData("public void Update() { ((Bindings.IMvvmPublisher)_publisher).PublishRebindMessage<TestViewModel>(); }")]
    [InlineData("public static void Update(TestViewModel other) { other._publisher.PublishRebindMessage<TestViewModel>(); }")]
    public async Task IgnoresDifferentReceiversTypesAndExistingHelperCalls(string member)
    {
        var source = Header + member + " } public class OtherViewModel : Bindings.IViewModel { }";
        Assert.Empty(await AnalyzeAsync(Generate(source)));
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public async Task WorksForNestedViewModels(string kind)
    {
        var source = "public partial class Container { [Bindings.ViewModel] public partial " + kind + " NestedViewModel { public void Update() { _publisher.PublishRebindMessage<NestedViewModel>(); } } }";
        Assert.Equal("BNDP001", Assert.Single(await AnalyzeAsync(Generate(source))).Id);
    }

    [Theory]
    [InlineData("", "public void PublishRebindMessage() { }")]
    [InlineData("[Bindings.ViewModel]", "")]
    [InlineData("[Bindings.ViewModel]", "public void PublishRebindMessage() { _publisher.Publish(1); }")]
    [InlineData("[Bindings.ViewModel]", "public void PublishRebindMessage() { _publisher.PublishRebindMessage<TestViewModel>(); _publisher.PublishRebindMessage<TestViewModel>(); }")]
    [InlineData("[Bindings.ViewModel]", "public static void PublishRebindMessage() { }")]
    [InlineData("[Bindings.ViewModel]", "private System.Action PublishRebindMessage;")]
    [InlineData("[Bindings.ViewModel]", "private void PublishRebindMessage<T>() { }")]
    [InlineData("[Bindings.ViewModel]", "private void PublishRebindMessage(int value) { }")]
    [InlineData("[Bindings.ViewModel]", "private int PublishRebindMessage() => 0;")]
    public async Task DoesNotRecommendMissingOrInequivalentHelpers(string attribute, string helper)
    {
        var source = attribute + " public class TestViewModel : Bindings.IViewModel { private Bindings.IMvvmPublisher _publisher; " + helper + " public void Update() { _publisher.PublishRebindMessage<TestViewModel>(); } }";
        Assert.Empty(await AnalyzeAsync(BindingsUsageAnalyzerTests.CreateCompilation(source)));
    }

    [Theory]
    [InlineData("private void PublishRebindMessage() => _publisher.PublishRebindMessage<TestViewModel<T>>();")]
    [InlineData("private void PublishRebindMessage() { _publisher.PublishRebindMessage<TestViewModel<T>>(); }")]
    public async Task SupportsEquivalentHelpersAndDoesNotSuggestRecursion(string helper)
    {
        var source = "[Bindings.ViewModel] public class TestViewModel<T> : Bindings.IViewModel { private Bindings.IMvvmPublisher _publisher; " + helper + " public void Update() { _publisher.PublishRebindMessage<TestViewModel<T>>(); } }";
        Assert.Single(await AnalyzeAsync(BindingsUsageAnalyzerTests.CreateCompilation(source)));
    }

    [Theory]
    [InlineData("// <auto-generated/>\n", "User.cs")]
    [InlineData("", "User.g.cs")]
    public async Task GeneratedCallsAreIgnored(string prefix, string path)
    {
        const string source = "[Bindings.ViewModel] public class TestViewModel : Bindings.IViewModel { private Bindings.IMvvmPublisher _publisher; private void PublishRebindMessage() => _publisher.PublishRebindMessage<TestViewModel>(); public void Update() { _publisher.PublishRebindMessage<TestViewModel>(); } }";
        Assert.Empty(await AnalyzeAsync(BindingsUsageAnalyzerTests.CreateCompilation(prefix + source, path: path)));
    }

    [Fact]
    public async Task RuleCanBeDisabledWithoutAffectingGeneration()
    {
        var source = Header + "public void Update() {\n#pragma warning disable BNDP001\n_publisher.PublishRebindMessage<TestViewModel>(); } }";
        Assert.Empty(await AnalyzeAsync(Generate(source)));
    }

    [Fact]
    public async Task DefaultErrorSeverityCanBeReconfigured()
    {
        var compilation = Generate(Header + "public void Update() { _publisher.PublishRebindMessage<TestViewModel>(); } }");
        compilation = compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(
            compilation.Options.SpecificDiagnosticOptions.SetItem("BNDP001", ReportDiagnostic.Info)));
        Assert.Equal(DiagnosticSeverity.Info, Assert.Single(await AnalyzeAsync(compilation)).Severity);
    }

    [Fact]
    public async Task MissingTypesAndIncompleteCodeDoNotCrashAnalyzer()
    {
        Assert.Empty(await AnalyzeAsync(BindingsUsageAnalyzerTests.CreateCompilation("public class Test { public void Update() { Missing.Call(); } }", stubs: "")));
        Assert.Empty(await AnalyzeAsync(BindingsUsageAnalyzerTests.CreateCompilation(Header + "public void Update() { _publisher.PublishRebindMessage<Missing>(); } }")));
    }

    [Fact]
    public async Task SameNamePublisherMethodsAreNotConfusedWithBindings()
    {
        const string source = """
            public class OtherPublisher { public void PublishRebindMessage<T>() { } }
            [Bindings.ViewModel] public class TestViewModel
            {
                private OtherPublisher _publisher;
                private void PublishRebindMessage() => _publisher.PublishRebindMessage<TestViewModel>();
                public void Update() => _publisher.PublishRebindMessage<TestViewModel>();
            }
            """;
        Assert.Empty(await AnalyzeAsync(BindingsUsageAnalyzerTests.CreateCompilation(source)));
    }

    [Fact]
    public async Task AmbiguousHelpersDoNotProduceDiagnosticsOrAnalyzerFailures()
    {
        const string source = """
            [Bindings.ViewModel] public class TestViewModel : Bindings.IViewModel
            {
                private Bindings.IMvvmPublisher _publisher;
                private void PublishRebindMessage() => _publisher.PublishRebindMessage<TestViewModel>();
                private void PublishRebindMessage() => _publisher.PublishRebindMessage<TestViewModel>();
                public void Update() => _publisher.PublishRebindMessage<TestViewModel>();
            }
            """;
        Assert.Empty(await AnalyzeAsync(BindingsUsageAnalyzerTests.CreateCompilation(source)));
    }

    [Fact]
    public async Task InvocationsContainingDirectivesAreNotRewritten()
    {
        var source = Header + """
            public void Update()
            {
                _publisher.PublishRebindMessage<
            #if EXAMPLE
                    global::TestViewModel
            #else
                    TestViewModel
            #endif
                >();
            }
            }
            """;
        Assert.Empty(await AnalyzeAsync(Generate(source)));
    }

    [Theory]
    [InlineData("_publisher.PublishRebindMessage<TestViewModel>();", "PublishRebindMessage();")]
    [InlineData("this._publisher.PublishRebindMessage<global::TestViewModel>();", "PublishRebindMessage();")]
    [InlineData("void PublishRebindMessage() { } _publisher.PublishRebindMessage<TestViewModel>();", "void PublishRebindMessage() { } this.PublishRebindMessage();")]
    [InlineData("System.Action PublishRebindMessage = () => { }; _publisher.PublishRebindMessage<TestViewModel>();", "System.Action PublishRebindMessage = () => { }; this.PublishRebindMessage();")]
    [InlineData("void PublishRebindMessage(int value) { } _publisher.PublishRebindMessage<TestViewModel>();", "void PublishRebindMessage(int value) { } this.PublishRebindMessage();")]
    [InlineData("// Keep this comment\n_publisher.PublishRebindMessage<TestViewModel>(/* Keep arguments */);", "// Keep this comment\nPublishRebindMessage(/* Keep arguments */);")]
    public async Task CodeFixProducesEquivalentCompilingCall(string statement, string expected)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Header + "public void Update() {\n" + statement + "\n} }");
        var compilation = (await document.Project.GetCompilationAsync())!;
        var diagnostic = Assert.Single(await AnalyzeAsync(compilation));
        var provider = new BindingsPracticesCodeFixProvider();
        Assert.Equal(new[] { "BNDP001" }, provider.FixableDiagnosticIds);
        Assert.Same(WellKnownFixAllProviders.BatchFixer, provider.GetFixAllProvider());
        var actions = new List<CodeAction>();
        await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None));
        var action = Assert.Single(actions);
        Assert.Equal("BNDP001", action.EquivalenceKey);
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var changed = Assert.Single(operations.OfType<ApplyChangesOperation>()).ChangedSolution.GetDocument(document.Id)!;
        var root = (await changed.GetSyntaxRootAsync())!;
        var body = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Single(method => method.Identifier.ValueText == "Update").Body!;
        Assert.Equal(SyntaxFactory.ParseStatement("{\n" + expected + "\n}").NormalizeWhitespace().ToFullString(), body.NormalizeWhitespace().ToFullString());
        Assert.DoesNotContain((await changed.Project.GetCompilationAsync())!.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Empty(await AnalyzeAsync((await changed.Project.GetCompilationAsync())!));
    }

    [Fact]
    public async Task CodeFixRetainsCommentsInsideRemovedReceiverAndTypeArguments()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Header + "public void Update() { _publisher /* receiver */ .PublishRebindMessage</* type */ TestViewModel>(); } }");
        var diagnostic = Assert.Single(await AnalyzeAsync((await document.Project.GetCompilationAsync())!));
        var actions = new List<CodeAction>();
        await new BindingsPracticesCodeFixProvider().RegisterCodeFixesAsync(
            new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None));
        var operations = await Assert.Single(actions).GetOperationsAsync(CancellationToken.None);
        var changed = Assert.Single(operations.OfType<ApplyChangesOperation>()).ChangedSolution.GetDocument(document.Id)!;
        var text = (await changed.GetTextAsync()).ToString();
        Assert.Contains("/* receiver */", text);
        Assert.Contains("/* type */", text);
        Assert.DoesNotContain((await changed.Project.GetCompilationAsync())!.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task FixAllReplacesCallsAcrossDocuments()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Header + "public void Update() { _publisher.PublishRebindMessage<TestViewModel>(); _publisher.PublishRebindMessage<TestViewModel>(); } }");
        var other = document.Project.AddDocument("Other.cs", SourceText.From("public partial class TestViewModel { public void Second() { this._publisher.PublishRebindMessage<TestViewModel>(); } }"));
        document = other.Project.GetDocument(document.Id)!;
        var provider = new BindingsPracticesCodeFixProvider();
        var context = new FixAllContext(document, provider, FixAllScope.Project, "BNDP001",
            provider.FixableDiagnosticIds, new PracticeDiagnosticProvider(), CancellationToken.None);
        var action = await provider.GetFixAllProvider().GetFixAsync(context);
        Assert.NotNull(action);
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var project = Assert.Single(operations.OfType<ApplyChangesOperation>()).ChangedSolution.GetProject(document.Project.Id)!;
        var compilation = (await project.GetCompilationAsync())!;
        Assert.Empty(await AnalyzeAsync(compilation));
        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    private sealed class PracticeDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            return (await AnalyzeAsync((await document.Project.GetCompilationAsync(cancellationToken))!))
                .Where(diagnostic => diagnostic.Location.SourceTree == tree);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult(Enumerable.Empty<Diagnostic>());

        public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            await AnalyzeAsync((await project.GetCompilationAsync(cancellationToken))!);
    }

    [Theory]
    [InlineData("42", "not an invocation")]
    [InlineData("PublishRebindMessage()", "existing helper")]
    public async Task CodeFixIgnoresStaleOrUnrelatedDiagnostics(string expression, string reason)
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Header + "public void Update() { " + (expression == "42" ? "var value = 42" : expression) + "; } }");
        var text = (await document.GetTextAsync()).ToString();
        var span = new TextSpan(text.IndexOf(expression, StringComparison.Ordinal), expression.Length);
        var descriptor = Assert.Single(new BindingsPracticesAnalyzer().SupportedDiagnostics, descriptor => descriptor.Id == "BNDP001");
        var tree = (await document.GetSyntaxTreeAsync())!;
        var diagnostic = Diagnostic.Create(descriptor, Location.Create(tree, span), reason);
        var actions = new List<CodeAction>();
        await new BindingsPracticesCodeFixProvider().RegisterCodeFixesAsync(
            new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None));
        Assert.Empty(actions);
    }

    [Fact]
    public async Task CodeFixIgnoresUnsupportedDiagnosticIds()
    {
        using var workspace = new AdhocWorkspace();
        var document = CreateDocument(workspace, Header + "public void Update() { _publisher.PublishRebindMessage<TestViewModel>(); } }");
        var original = Assert.Single(await AnalyzeAsync((await document.Project.GetCompilationAsync())!));
        var descriptor = new DiagnosticDescriptor("OTHER001", "Other rule", "Other rule", "Other", DiagnosticSeverity.Error, true);
        var diagnostic = Diagnostic.Create(descriptor, original.Location);
        var actions = new List<CodeAction>();
        await new BindingsPracticesCodeFixProvider().RegisterCodeFixesAsync(
            new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None));
        Assert.Empty(actions);
    }

    private static Document CreateDocument(AdhocWorkspace workspace, string source)
    {
        var project = workspace.AddProject("Practices", LanguageNames.CSharp)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithMetadataReferences(new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var document = project.AddDocument("User.cs", SourceText.From(source));
        project = document.Project.AddDocument("Stubs.cs", SourceText.From(ViewModelGeneratorTests.AttributeStubs + ViewModelGeneratorTests.RuntimeStubs)).Project;
        var compilation = project.GetCompilationAsync().GetAwaiter().GetResult()!;
        var driver = CSharpGeneratorDriver.Create(new ViewModelGenerator()).RunGenerators(compilation);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        foreach (var tree in driver.GetRunResult().GeneratedTrees)
        {
            project = project.AddDocument(System.IO.Path.GetFileName(tree.FilePath), tree.GetText()).Project;
        }
        return project.GetDocument(document.Id)!;
    }
}