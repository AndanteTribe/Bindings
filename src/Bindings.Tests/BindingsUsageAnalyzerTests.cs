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
        Assert.Equal(new[] { "BND005", "BND006", "BND007" }, new BindingsUsageAnalyzer().SupportedDiagnostics.Select(descriptor => descriptor.Id));
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
    [InlineData("var value = _count;", "_count")]
    [InlineData("var value = this._count;", "this._count")]
    [InlineData("_count = 1;", "_count")]
    [InlineData("_count += 1;", "_count")]
    [InlineData("_count++;", "_count")]
    [InlineData("--_count;", "_count")]
    [InlineData("TakeRef(ref _count);", "_count")]
    [InlineData("TakeOut(out _count);", "_count")]
    [InlineData("TakeIn(in _count);", "_count")]
    [InlineData("ref int value = ref _count;", "_count")]
    [InlineData("System.Func<int> read = () => _count;", "_count")]
    [InlineData("int Read() => _count;", "_count")]
    [InlineData("var value = new SampleViewModel()._count;", "new SampleViewModel()._count")]
    [InlineData("var value = new SampleViewModel { _count = 1 };", "_count")]
    public async Task BND007ReportsEachKindOfDirectAccess(string statement, string expectedLocation)
    {
        var compilation = CreateCompilation($$"""
            [Bindings.ViewModel]
            public class SampleViewModel
            {
                [Bindings.Schema("TMPro.TMP_Text.text")] private int _count;
                public void Update() { {{statement}} }
                private static void TakeRef(ref int value) { }
                private static void TakeOut(out int value) { value = 0; }
                private static void TakeIn(in int value) { }
            }
            """);

        Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var diagnostic = Assert.Single(await AnalyzeAsync(compilation));
        Assert.Equal("BND007", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Field '_count' is annotated with [Schema]", diagnostic.GetMessage());
        Assert.Contains("generated property 'Count'", diagnostic.GetMessage());
        Assert.Equal(expectedLocation, diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Theory]
    [InlineData("_count", "Count")]
    [InlineData("m_count", "Count")]
    [InlineData("__m_count", "Count")]
    [InlineData("count", "Count")]
    public async Task BND007UsesGeneratorPropertyNaming(string fieldName, string propertyName)
    {
        var diagnostics = await AnalyzeAsync(CreateCompilation($$"""
            using Schema = Bindings.SchemaAttribute;
            [Bindings.ViewModel] public class SampleViewModel<T>
            {
                [Schema("x"), Schema("y")] private T {{fieldName}};
                public T Read() => {{fieldName}};
            }
            """));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BND007", diagnostic.Id);
        Assert.Contains($"generated property '{propertyName}'", diagnostic.GetMessage());
    }

    [Theory]
    [InlineData("public SampleViewModel() { _count = 1; }", 1)]
    [InlineData("public int Value { get => _count; set => _count = value; }", 2)]
    [InlineData("public void Update() { _count = _count + 1; }", 2)]
    [InlineData("public class Nested { public int Read(SampleViewModel model) => model._count; }", 1)]
    public async Task BND007IncludesConstructorsAccessorsAndNestedTypes(string member, int expectedCount)
    {
        var diagnostics = await AnalyzeAsync(CreateCompilation($$"""
            [Bindings.ViewModel] public class SampleViewModel
            {
                [Bindings.Schema("x")] private int _count;
                {{member}}
            }
            """));

        Assert.Equal(expectedCount, diagnostics.Length);
        Assert.All(diagnostics, diagnostic => Assert.Equal("BND007", diagnostic.Id));
    }

    [Fact]
    public async Task BND007IncludesNonPrivateFieldsAndReferencesOutsideTheViewModel()
    {
        var diagnostics = await AnalyzeAsync(CreateCompilation("""
            [Bindings.ViewModel] public class SampleViewModel<T>
            {
                [Bindings.Schema("x")] public T Value;
            }
            public class Host { public int Read(SampleViewModel<int> model) => model.Value; }
            """));

        Assert.Equal("BND007", Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task BND007IncludesMutableStructMemberWrites()
    {
        var diagnostics = await AnalyzeAsync(CreateCompilation("""
            [Bindings.ViewModel] public struct SampleViewModel
            {
                [Bindings.Schema("x")] private UnityEngine.Vector2 _position;
                public void Update() { _position.x = 1; }
            }
            """));

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BND007", diagnostic.Id);
        Assert.Contains("generated property 'Position'", diagnostic.GetMessage());
    }

    [Fact]
    public async Task BND007ChecksFieldReferencesInsideDeclarationInitializers()
    {
        var diagnostics = await AnalyzeAsync(CreateCompilation("""
            [Bindings.ViewModel] public class SampleViewModel
            {
                [Bindings.Schema("x")] private static int _count = 1;
                private static int _initial = _count;
            }
            """));

        Assert.Equal("BND007", Assert.Single(diagnostics).Id);
    }

    [Theory]
    [InlineData("namespace Bindings { public class SchemaAttribute : System.Attribute { public SchemaAttribute(string path) { } } }")]
    [InlineData("namespace Bindings { public class ViewModelAttribute : System.Attribute { } }")]
    public async Task BND007MissingAttributeTypesDoNotCauseAnalyzerFailures(string stubs)
    {
        Assert.Empty(await AnalyzeAsync(CreateCompilation("""
            [Bindings.ViewModel] public class SampleViewModel
            {
                [Bindings.Schema("x")] private int _count;
                public int Read() => _count;
            }
            """, stubs)));
    }

    [Theory]
    [InlineData("[Bindings.ViewModel] public class SampleViewModel { [Bindings.Schema(\"x\")] private int _count = 1; }")]
    [InlineData("[Bindings.ViewModel] public class SampleViewModel { [Bindings.Schema(\"x\")] private int _count; public string Name() => nameof(_count); }")]
    [InlineData("[Bindings.ViewModel] public class SampleViewModel { [Bindings.Schema(\"x\")] private int _count; public string Name() => nameof(this._count); }")]
    [InlineData("[Bindings.ViewModel] public class SampleViewModel { [Bindings.Schema(\"x\")] private UnityEngine.Vector2 _position; public string Name() => nameof(_position.x); }")]
    [InlineData("[Bindings.ViewModel] public class SampleViewModel { private int _count; public int Read() => _count; }")]
    [InlineData("public class Other { [Bindings.Schema(\"x\")] private int _count; public int Read() => _count; }")]
    [InlineData("[Bindings.ViewModel] public class SampleViewModel { [Bindings.Schema(\"x\")] private int _count; public int Read() { int _count = 0; return _count; } }")]
    [InlineData("[Bindings.ViewModel] public class SampleViewModel { [Bindings.Schema(\"x\")] public void Execute() { } public void Call() => Execute(); }")]
    [InlineData("namespace Other { public class SchemaAttribute : System.Attribute { } } [Bindings.ViewModel] public class SampleViewModel { [Other.Schema] private int _count; public int Read() => _count; }")]
    [InlineData("namespace Other { public class ViewModelAttribute : System.Attribute { } } [Other.ViewModel] public class SampleViewModel { [Bindings.Schema(\"x\")] private int _count; public int Read() => _count; }")]
    [InlineData("[Bindings.ViewModel] public class SampleViewModel { [Missing] private int _count; public int Read() => _count; }")]
    public async Task BND007IgnoresDeclarationsNameofAndUnrelatedSymbols(string source)
    {
        Assert.Empty(await AnalyzeAsync(CreateCompilation(source)));
    }

    [Theory]
    [InlineData("public int Read() => Count; public void Update() { Count++; }", 0)]
    [InlineData("public int Read() => _count;", 1)]
    public async Task BND007WorksWithGeneratedPropertiesAndContinuesGeneration(string member, int expectedCount)
    {
        var compilation = CreateCompilation($$"""
            [Bindings.ViewModel] public partial class SampleViewModel
            {
                [Bindings.Schema("TMPro.TMP_Text.text")] private int _count;
            }
            public partial class SampleViewModel { {{member}} }
            """);
        var driver = CSharpGeneratorDriver.Create(new ViewModelGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out _);

        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var diagnostics = await AnalyzeAsync(output);
        Assert.Equal(expectedCount, diagnostics.Length);
        Assert.All(diagnostics, diagnostic =>
        {
            Assert.Equal("BND007", diagnostic.Id);
            Assert.Equal("User.cs", diagnostic.Location.SourceTree!.FilePath);
        });
        Assert.Empty(driver.GetRunResult().Diagnostics);
        Assert.Equal(2, driver.GetRunResult().GeneratedTrees.Length);
    }

    [Fact]
    public async Task BND007CanBeSuppressedExplicitly()
    {
        Assert.Empty(await AnalyzeAsync(CreateCompilation("""
            [Bindings.ViewModel] public class SampleViewModel
            {
                [Bindings.Schema("x")] private int _count;
            #pragma warning disable BND007
                public int Read() => _count;
            }
            """)));
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