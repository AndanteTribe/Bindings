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
        compilation.WithAnalyzers([new BindingsUsageAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();

    internal static CSharpCompilation CreateCompilation(string source, string? stubs = null, string path = "User.cs") =>
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
        Assert.Equal(new[] { "BND006" }, new BindingsUsageAnalyzer().SupportedDiagnostics.Select(static descriptor => descriptor.Id));
    }

    [Theory]
    [InlineData("UnityEngine.SerializeField")]
    [InlineData("UnityEngine.SerializeReference")]
    public async Task SerializedViewModelFieldsDoNotReportUsageDiagnostics(string attribute)
    {
        var compilation = CreateCompilation($$"""
            [Bindings.ViewModel] public class SampleViewModel { }
            public class Host { [{{attribute}}] private SampleViewModel _viewModel; }
            """);

        Assert.Empty(await AnalyzeAsync(compilation));
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
        Assert.All(diagnostics, static diagnostic => Assert.Equal("BND006", diagnostic.Id));
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

        Assert.DoesNotContain(compilation.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
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

    [Fact]
    public async Task PartialSchemaPropertiesWorkWithoutDirectAccessDiagnostics()
    {
        var compilation = CreateCompilation($$"""
            [Bindings.ViewModel] public partial class SampleViewModel
            {
                [Bindings.Schema("TMPro.TMP_Text.text")] public partial int Count { get; set; }
            }
            public partial class SampleViewModel { public void Update() { Count++; } }
            """);
        var driver = CSharpGeneratorDriver.Create(new ViewModelGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        Assert.DoesNotContain(output.GetDiagnostics(), static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var diagnostics = await AnalyzeAsync(output);
        Assert.Empty(diagnostics);
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Equal(2, driver.GetRunResult().GeneratedTrees.Length);
    }

    [Theory]
    [InlineData("// <auto-generated/>\n", "User.cs")]
    [InlineData("", "User.g.cs")]
    public async Task GeneratedCodeIsIgnored(string prefix, string path)
    {
        var compilation = CreateCompilation(prefix + """
            [Bindings.ViewModel] public class Model
            {
                [Bindings.Schema("x")] private int _count;
                public int Read() => _count;
            }
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