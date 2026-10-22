using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Bindings.Tests;

public class BindingsUsageAnalyzerTests
{
    private const string MonoBehaviourStub = "namespace UnityEngine { public class MonoBehaviour : Behaviour { } }";

    internal static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(Compilation compilation) =>
        compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new BindingsUsageAnalyzer()))
            .GetAnalyzerDiagnosticsAsync();

    private static CSharpCompilation CreateCompilation(string source, string? stubs = null, string path = "User.cs") =>
        CSharpCompilation.Create("AnalyzerTests",
            new[]
            {
                CSharpSyntaxTree.ParseText(stubs ?? ViewModelGeneratorTests.AttributeStubs + ViewModelGeneratorTests.RuntimeStubs + MonoBehaviourStub, path: "Stubs.cs"),
                CSharpSyntaxTree.ParseText(source, path: path),
            },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    [Fact]
    public void SupportedDiagnosticsAreUsageRulesOnly()
    {
        Assert.Equal(new[] { "BND005", "BND006" }, new BindingsUsageAnalyzer().SupportedDiagnostics.Select(descriptor => descriptor.Id));
    }

    [Theory]
    [InlineData("UnityEngine.SerializeField")]
    [InlineData("UnityEngine.SerializeReference")]
    public async Task BND005ReportsWithoutRunningGenerator(string attribute)
    {
        var compilation = CreateCompilation($$"""
            [Bindings.ViewModel] public class SampleViewModel { }
            public class Host { [{{attribute}}] private SampleViewModel _viewModel; }
            """);

        var diagnostic = Assert.Single(await AnalyzeAsync(compilation));
        Assert.Equal("BND005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("_viewModel", diagnostic.GetMessage());
        Assert.Contains("SampleViewModel", diagnostic.GetMessage());
        Assert.Equal(attribute, diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public async Task BND005ReportsEachAttributeAndField()
    {
        var diagnostics = await AnalyzeAsync(CreateCompilation("""
            [Bindings.ViewModel] public struct SampleViewModel { }
            public class Host
            {
                [UnityEngine.SerializeField, UnityEngine.SerializeReference]
                private SampleViewModel _first, _second;
            }
            """));

        Assert.Equal(4, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("BND005", diagnostic.Id));
    }

    [Fact]
    public async Task BND005RecognizesAliasesAndConstructedGenericTypes()
    {
        var diagnostics = await AnalyzeAsync(CreateCompilation("""
            using Serialized = UnityEngine.SerializeField;
            [Bindings.ViewModel] public class GenericViewModel<T> { }
            public class Host { [Serialized] private GenericViewModel<int> _value; }
            """));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BND005", diagnostic.Id);
        Assert.Contains("GenericViewModel<int>", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("[System.Serializable, Bindings.ViewModel] public class SampleViewModel { } public class Host { private SampleViewModel _value; }")]
    [InlineData("[Bindings.ViewModel] public class SampleViewModel { } public class Host { [UnityEngine.SerializeField] private SampleViewModel[] _value; }")]
    [InlineData("public class Host<T> { [UnityEngine.SerializeReference] private T _value; }")]
    [InlineData("public class Host { [UnityEngine.SerializeField] private Missing _value; }")]
    [InlineData("namespace Other { public class ViewModelAttribute : System.Attribute { } } [Other.ViewModel] public class Sample { } public class Host { [UnityEngine.SerializeField] private Sample _value; }")]
    [InlineData("namespace Other { public class SerializeField : System.Attribute { } } [Bindings.ViewModel] public class SampleViewModel { } public class Host { [Other.SerializeField] private SampleViewModel _value; }")]
    [InlineData("[Bindings.ViewModel] public class SampleViewModel { } public class Host { [UnityEngine.SerializeField] public SampleViewModel Value { get; set; } }")]
    public async Task BND005IgnoresUnrelatedOrUnsupportedTargets(string source)
    {
        Assert.Empty(await AnalyzeAsync(CreateCompilation(source)));
    }

    [Theory]
    [InlineData("public abstract class BadView : UnityEngine.MonoBehaviour, Bindings.IView { }", true)]
    [InlineData("public class Model : Bindings.IViewModel { } public abstract class BadView : UnityEngine.MonoBehaviour, Bindings.IView<Model> { }", true)]
    [InlineData("public class ComponentBase : UnityEngine.MonoBehaviour { } public interface ICustomView : Bindings.IView { } public abstract class BadView : ComponentBase, ICustomView { }", true)]
    [InlineData("public abstract class ViewBase : Bindings.IView { } public abstract class BadView : ViewBase { }", false)]
    [InlineData("public abstract class BadView : UnityEngine.MonoBehaviour { }", false)]
    [InlineData("public abstract class BadView : Bindings.IView { }", false)]
    [InlineData("public struct BadView : Bindings.IView { }", false)]
    [InlineData("namespace Other { public interface IView { } } public class BadView : UnityEngine.MonoBehaviour, Other.IView { }", false)]
    [InlineData("namespace Other { public class MonoBehaviour { } } public abstract class BadView : Other.MonoBehaviour, Bindings.IView { }", false)]
    [InlineData("public abstract class BadView : Missing, Bindings.IView { }", false)]
    public async Task BND006ChecksInterfaceAndBaseTypeIdentity(string source, bool expected)
    {
        var diagnostics = await AnalyzeAsync(CreateCompilation(source));
        if (!expected)
        {
            Assert.Empty(diagnostics);
            return;
        }

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BND006", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("BadView", diagnostic.GetMessage());
        Assert.Contains("Bindings.IView", diagnostic.GetMessage());
        Assert.Contains("UnityEngine.MonoBehaviour", diagnostic.GetMessage());
        Assert.Equal("BadView", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public async Task BND006ChecksInheritedInterfaceOnEachDerivedClass()
    {
        var diagnostics = await AnalyzeAsync(CreateCompilation("""
            public abstract class BaseView : UnityEngine.MonoBehaviour, Bindings.IView { }
            public abstract class DerivedView : BaseView { }
            """));

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("BND006", diagnostic.Id));
    }

    [Fact]
    public async Task BND006ReportsOnOtherwiseValidNestedGenericImplementation()
    {
        var compilation = CreateCompilation("""
            public class Container
            {
                public class ComponentBase<T> : UnityEngine.MonoBehaviour { }
                public class BadView<T> : ComponentBase<T>, Bindings.IView
                {
                    public System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken cancellationToken) => default;
                }
            }
            """);

        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var diagnostic = Assert.Single(await AnalyzeAsync(compilation));
        Assert.Equal("BND006", diagnostic.Id);
        Assert.Contains("Container.BadView<T>", diagnostic.GetMessage());
    }

    [Fact]
    public async Task BND006ReportsOnceForPartialType()
    {
        var diagnostics = await AnalyzeAsync(CreateCompilation("""
            public abstract partial class BadView : UnityEngine.MonoBehaviour { }
            public abstract partial class BadView : Bindings.IView { }
            """));

        Assert.Equal("BND006", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task BND006DetectsInterfaceAddedByGeneratorAndContinuesGeneration()
    {
        var compilation = CreateCompilation("""
            [Bindings.ViewModel] public partial class SampleViewModel { }
            public sealed partial class SampleView : UnityEngine.MonoBehaviour { }
            """);
        var driver = CSharpGeneratorDriver.Create(new ViewModelGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        var diagnostic = Assert.Single(await AnalyzeAsync(output));
        Assert.Equal("BND006", diagnostic.Id);
        Assert.Equal("User.cs", diagnostic.Location.SourceTree!.FilePath);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Equal(2, driver.GetRunResult().GeneratedTrees.Length);
    }

    [Theory]
    [InlineData("// <auto-generated/>\n", "User.cs")]
    [InlineData("", "User.g.cs")]
    public async Task GeneratedCodeIsIgnored(string prefix, string path)
    {
        var compilation = CreateCompilation(prefix + """
            [Bindings.ViewModel] public class Model { }
            public abstract class BadView : UnityEngine.MonoBehaviour, Bindings.IView
            {
                [UnityEngine.SerializeField] private Model _model;
            }
            """, path: path);
        Assert.Empty(await AnalyzeAsync(compilation));
    }

    [Theory]
    [InlineData("")]
    [InlineData("namespace Bindings { public interface IView { } }")]
    [InlineData("namespace UnityEngine { public class MonoBehaviour { } }")]
    [InlineData("namespace Bindings { public class ViewModelAttribute : System.Attribute { } }")]
    public async Task MissingFrameworkTypesDoNotCauseAnalyzerFailures(string stubs)
    {
        Assert.Empty(await AnalyzeAsync(CreateCompilation("""
            public abstract class Test : UnityEngine.MonoBehaviour, Bindings.IView
            {
                [UnityEngine.SerializeField] private Missing _value;
            }
            """, stubs)));
    }
}