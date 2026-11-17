using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Bindings.Tests;

public class ViewModelUiComponentAnalyzerTests
{
    private const string Stubs = """
        namespace Bindings {
            public interface IViewModel { }
            public interface IView { }
            public class ViewModelAttribute : System.Attribute { }
        }
        namespace UnityEngine {
            public class Component { }
            public class Canvas : Component { }
            public class CanvasGroup : Component { }
            public class CanvasRenderer : Component { }
            public class RectTransform : Component { }
            public class Sprite { }
            public class Material { }
            public struct Color { }
        }
        namespace UnityEngine.EventSystems {
            public class UIBehaviour : UnityEngine.Component { }
        }
        namespace UnityEngine.UI {
            public class Button : UnityEngine.EventSystems.UIBehaviour {
                public enum Transition { None }
            }
        }
        namespace App {
            public class Widget : UnityEngine.EventSystems.UIBehaviour { public bool Visible { get; set; } }
            public class GenericWidget<T> : Widget { }
            public class WidgetList { public Widget this[int index] => null; }
            public static class Widgets { public static Widget Get() => null; }
        }
        """;

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, string? stubs = null,
        string path = "User.cs", bool allowCompilerErrors = false)
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation(source, stubs ?? Stubs, path);
        if (!allowCompilerErrors)
        {
            Assert.DoesNotContain(compilation.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        }

        var diagnostics = await compilation.WithAnalyzers(
            [new BindingsPracticesAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "AD0001");
        return diagnostics;
    }

    [Theory]
    [InlineData("UnityEngine.EventSystems.UIBehaviour", "UnityEngine.EventSystems.UIBehaviour")]
    [InlineData("UnityEngine.UI.Button", "UnityEngine.UI.Button")]
    [InlineData("App.Widget", "App.Widget")]
    [InlineData("App.Widget[]", "App.Widget")]
    [InlineData("System.Collections.Generic.List<App.Widget>", "App.Widget")]
    [InlineData("System.Collections.Generic.List<App.Widget[]>", "App.Widget")]
    [InlineData("System.Collections.Generic.List<System.Collections.Generic.List<App.Widget>>", "App.Widget")]
    [InlineData("System.Collections.Generic.List<App.Widget>.Enumerator", "App.Widget")]
    [InlineData("App.GenericWidget<int>", "App.GenericWidget<int>")]
    [InlineData("WidgetList", "App.Widget", "WidgetList")]
    public async Task ReportsComponentTypeOnce(string type, string component, string? location = null)
    {
        var diagnostic = Assert.Single(await AnalyzeAsync(
            $"using WidgetList = System.Collections.Generic.List<App.Widget>; [Bindings.ViewModel] class CounterViewModel {{ public {type} Widget; }}"));
        Assert.Equal("BNDP005", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(
            $"ViewModel 'CounterViewModel' directly uses UI component type '{component}'. Keep UI component access in the View.",
            diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Equal(location ?? component, diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Theory]
    [InlineData("public App.Widget Widget { get; set; }")]
    [InlineData("public App.Widget Get() => null;")]
    [InlineData("public void Set(App.Widget widget) { }")]
    [InlineData("public void Run() { var widget = App.Widgets.Get(); widget.Visible = true; }")]
    [InlineData("public void Run() { App.Widgets.Get().Visible = true; }")]
    [InlineData("public void Run() { System.Action update = () => App.Widgets.Get().Visible = true; }")]
    [InlineData("public void Run<T>() where T : App.Widget { T widget = default; }")]
    [InlineData("public class Helper { public App.Widget Widget; }")]
    public async Task ReportsComponentDependenciesAndUsage(string member)
    {
        var diagnostics = await AnalyzeAsync($"class CounterViewModel : Bindings.IViewModel {{ {member} }}");
        Assert.NotEmpty(diagnostics);
        Assert.All(diagnostics, diagnostic => Assert.Equal("BNDP005", diagnostic.Id));
    }

    [Theory]
    [InlineData("public void Run(App.WidgetList widgets) { widgets[0].Visible = true; }", "widgets[0]")]
    [InlineData("private App.WidgetList widgets; public void Run() { widgets[0].Visible = true; }", "widgets[0]")]
    [InlineData("public bool? Run(App.WidgetList widgets) => widgets?[0].Visible;", "[0]")]
    public async Task ReportsIndexerResult(string member, string location)
    {
        var diagnostic = Assert.Single(await AnalyzeAsync($"class CounterViewModel : Bindings.IViewModel {{ {member} }}"));
        Assert.Equal("BNDP005", diagnostic.Id);
        Assert.Contains("'App.Widget'", diagnostic.GetMessage(CultureInfo.InvariantCulture));
        Assert.Equal(location, diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public async Task AllowsNonComponentIndexerResults()
    {
        Assert.Empty(await AnalyzeAsync("""
            class CounterViewModel : Bindings.IViewModel {
                public int Read(int[] values) => values[0];
                public int? ReadOptional(int[] values) => values?[0];
            }
            """));
    }

    [Theory]
    [InlineData("UnityEngine.Component")]
    [InlineData("UnityEngine.Canvas")]
    [InlineData("UnityEngine.CanvasGroup")]
    [InlineData("UnityEngine.CanvasRenderer")]
    [InlineData("UnityEngine.RectTransform")]
    [InlineData("UnityEngine.Color")]
    [InlineData("UnityEngine.Sprite")]
    [InlineData("UnityEngine.Material[]")]
    [InlineData("UnityEngine.UI.Button.Transition")]
    public async Task AllowsTypesOutsideUiBehaviourHierarchy(string type)
    {
        Assert.Empty(await AnalyzeAsync($"class CounterViewModel : Bindings.IViewModel {{ public {type} Value; }}"));
    }

    [Fact]
    public async Task AllowsComponentAccessOutsideViewModelsAndPathConstantsInsideThem()
    {
        Assert.Empty(await AnalyzeAsync("""
            using App;
            class CounterView : Bindings.IView { public Widget Widget; }
            class Other { public Widget Widget; }
            class CounterViewModel : Bindings.IViewModel {
                public string Path => Paths.Widget.Visible;
                public UnityEngine.UI.Button.Transition Transition => UnityEngine.UI.Button.Transition.None;
                public class NestedView : CounterView {
                    public void Run() { Widget.Visible = true; }
                    public class Helper { public Widget Widget; }
                }
            }
            static class Paths { public static class Widget { public const string Visible = "App.Widget.Visible"; } }
            """));
    }

    [Fact]
    public async Task ATypeNamedUiBehaviourIsNotEnough()
    {
        Assert.Empty(await AnalyzeAsync("""
            namespace Other { public class UIBehaviour { } public class Widget : UIBehaviour { } }
            class CounterViewModel : Bindings.IViewModel { public Other.Widget Widget; }
            """));
    }

    [Theory]
    [InlineData("User.cs")]
    [InlineData("User.g.cs")]
    public async Task ChecksPartialAndInheritedViewModelsIncludingGeneratedCode(string path)
    {
        var diagnostic = Assert.Single(await AnalyzeAsync("""
            // <auto-generated/>
            using Widget = App.Widget;
            class Base : Bindings.IViewModel { }
            partial class CounterViewModel : Base { }
            partial class CounterViewModel { public Widget Value; }
            """, path: path));
        Assert.Equal("BNDP005", diagnostic.Id);
        Assert.Contains("CounterViewModel", diagnostic.GetMessage(CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task MissingAndCyclicTypesDoNotCrash()
    {
        Assert.Empty(await AnalyzeAsync(
            "class CounterViewModel : Bindings.IViewModel { public Missing Value; }", allowCompilerErrors: true));
        Assert.Empty(await AnalyzeAsync(
            "class CounterViewModel<T, U> : Bindings.IViewModel where T : U where U : T { public T Value; }",
            allowCompilerErrors: true));
        Assert.Empty(await AnalyzeAsync(
            "class CounterViewModel : Bindings.IViewModel { public App.Widget Value; }",
            stubs: Stubs.Replace("public class UIBehaviour : UnityEngine.Component { }", ""), allowCompilerErrors: true));
    }

    [Fact]
    public async Task AllowsGeneratedViewInsideOuterViewModel()
    {
        var stubs = ViewModelGeneratorTests.AttributeStubs + ViewModelGeneratorTests.RuntimeStubs.Replace(
            "public class TMP_Text {", "public class TMP_Text : UnityEngine.EventSystems.UIBehaviour {");
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation("""
            namespace UnityEngine.EventSystems { public class UIBehaviour { } }
            [Bindings.ViewModel]
            public partial class OuterViewModel {
                [Bindings.ViewModel]
                public partial class InnerViewModel {
                    [Bindings.Schema("TMPro.TMP_Text.text")] private int _count;
                }
            }
            """, stubs);
        _ = CSharpGeneratorDriver.Create(new ViewModelGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        Assert.Empty(generatorDiagnostics);
        Assert.Equal(4, output.SyntaxTrees.Count() - compilation.SyntaxTrees.Count());
        Assert.DoesNotContain(output.GetDiagnostics(), diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var diagnostics = await output.WithAnalyzers(
            [new BindingsPracticesAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DiagnosticDoesNotStopGeneration()
    {
        var compilation = BindingsUsageAnalyzerTests.CreateCompilation("""
            [Bindings.ViewModel] public partial class CounterViewModel { public App.Widget Widget; }
            namespace UnityEngine.EventSystems { public class UIBehaviour { } }
            namespace App { public class Widget : UnityEngine.EventSystems.UIBehaviour { } }
            """);
        _ = CSharpGeneratorDriver.Create(new ViewModelGenerator())
            .RunGeneratorsAndUpdateCompilation(compilation, out var output, out var generatorDiagnostics);
        Assert.Empty(generatorDiagnostics);
        Assert.Equal(2, output.SyntaxTrees.Count() - compilation.SyntaxTrees.Count());
        var diagnostics = await output.WithAnalyzers(
            [new BindingsPracticesAnalyzer()]).GetAnalyzerDiagnosticsAsync();
        Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "BNDP005");
        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Id == "AD0001");
    }
}