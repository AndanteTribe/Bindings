using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Bindings.Tests;

/// <summary>
/// Test class for ViewModelGenerator.
/// Verifies that generated code matches the specification for each scenario.
/// </summary>
public class ViewModelGeneratorTests
{
    // -------------------------------------------------------------------------
    // Stub definitions for testing (substitutes for Unity-dependent types)
    // -------------------------------------------------------------------------

    private const string AttributeStubs = @"
using System;
using System.Runtime.CompilerServices;

namespace Bindings
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class ViewModelAttribute : Attribute
    {
        public ViewModelAttribute(bool requireBindImplementation = false) { }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RequiredAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SchemaAttribute : Attribute
    {
        public readonly string BindingPath;
        public readonly int Id;
        public readonly string Format;
        public readonly string Tooltip;

        public SchemaAttribute(string bindingPath, int id = -1, string format = """", string tooltip = """")
        {
            BindingPath = bindingPath;
            Id = id;
            Format = format;
            Tooltip = tooltip;
        }

        public SchemaAttribute(object bindingPath, int id = -1, string format = """", string tooltip = """", [CallerArgumentExpression(""bindingPath"")]string path = """")
        {
            BindingPath = path;
            Id = id;
            Format = format;
            Tooltip = tooltip;
        }
    }

    public interface IViewModel { }

    public interface IView
    {
        System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken cancellationToken);
    }

    public interface IView<in T> : IView where T : IViewModel
    {
        void Initialize(T viewModel);
    }

    public interface IMvvmPublisher
    {
        void PublishRebindMessage<T>() where T : IViewModel;
    }

    public interface IMvvmSubscriber<in T>
    {
        void OnReceivedMessage(T message);
    }

    public sealed class DebugBindMessage { public void BindTo(object view) { } }
}

namespace Bindings
{
    public static class TextMeshProExtensions
    {
        public static void SetValue(object text, object value) { }
        public static void SetValue(object text, object value, string format) { }
    }
}
";

    // Stubs for Unity and third-party types referenced in the generated View code.
    private const string RuntimeStubs = @"
namespace UnityEngine
{
    public sealed class SerializeField : System.Attribute { }
    public sealed class NonSerialized : System.Attribute { }
    public sealed class Tooltip : System.Attribute { public Tooltip(string tip) { } }
}
namespace TMPro
{
    public class TMP_Text { public string text { get; set; } = string.Empty; }
}
namespace UnityEngine.UI
{
    public class Button { public ButtonClickedEvent onClick { get; } = new(); }
    public class ButtonClickedEvent
    {
        public void RemoveAllListeners() { }
        public void AddListener(System.Action call) { }
    }
    public class Image { public float fillAmount { get; set; } }
}
";

    private static (string? ViewModelSource, string? ViewSource) RunGenerator(string userCode)
    {
        var generator = new ViewModelGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[]
            {
                CSharpSyntaxTree.ParseText(AttributeStubs),
                CSharpSyntaxTree.ParseText(userCode),
            },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // ViewModel file: contains "ViewModel" in the filename and ends with ".g.cs"
        // View file: contains "View" but NOT "ViewModel" in the filename, ends with ".g.cs"
        var vmSource = runResult.GeneratedTrees
            .FirstOrDefault(t => System.IO.Path.GetFileName(t.FilePath).Contains("ViewModel") && t.FilePath.EndsWith(".g.cs"))
            ?.GetText().ToString();

        var viewSource = runResult.GeneratedTrees
            .FirstOrDefault(t =>
            {
                var name = System.IO.Path.GetFileName(t.FilePath);
                return name.Contains("View") && !name.Contains("ViewModel") && t.FilePath.EndsWith(".g.cs");
            })
            ?.GetText().ToString();

        return (vmSource, viewSource);
    }

    /// <summary>
    /// Runs the generator on <paramref name="userCode"/> and then compiles the user code +
    /// all generated files together. Returns any error-level diagnostics from the final
    /// compilation so tests can assert there are no compiler errors.
    /// </summary>
    private static Diagnostic[] RunGeneratorAndCompile(string userCode)
    {
        var generator = new ViewModelGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var seedCompilation = CSharpCompilation.Create(
            "SeedAssembly",
            new[]
            {
                CSharpSyntaxTree.ParseText(AttributeStubs),
                CSharpSyntaxTree.ParseText(RuntimeStubs),
                CSharpSyntaxTree.ParseText(userCode),
            },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var runResult = driver.RunGenerators(seedCompilation).GetRunResult();

        // Compile user code + generated code together
        var allTrees = new[]
            {
                CSharpSyntaxTree.ParseText(AttributeStubs),
                CSharpSyntaxTree.ParseText(RuntimeStubs),
                CSharpSyntaxTree.ParseText(userCode),
            }
            .Concat(runResult.GeneratedTrees)
            .ToArray();

        var finalCompilation = CSharpCompilation.Create(
            "FinalAssembly",
            allTrees,
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.ValueTask).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return finalCompilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();
    }

    // -------------------------------------------------------------------------
    // Scenario 1: multiple same-type methods with id=-1 (default) produce separate fields
    // -------------------------------------------------------------------------

    [Fact]
    public void CountViewModel1MultipleDefaultIdMethodsProduceSeparateFields()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModel1
    {
        [Bindings.Required]
        private readonly object _model;

        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Increment() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Decrement() { }
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        // id=-1 with multiple Button entries splits into _button1, _button2 (Case A)
        Assert.Contains("_button1", viewSource);
        Assert.Contains("_button2", viewSource);
        Assert.DoesNotContain("_button0", viewSource);
        // TMP_Text has only one entry so uses _text (no suffix)
        Assert.Contains("_text ", viewSource);
        Assert.DoesNotContain("_text0", viewSource);
        // Increment / Decrement each AddListener on their respective button
        Assert.Contains("_button1.onClick.RemoveAllListeners", viewSource);
        Assert.Contains("_button1.onClick.AddListener(_viewModel.Increment)", viewSource);
        Assert.Contains("_button2.onClick.RemoveAllListeners", viewSource);
        Assert.Contains("_button2.onClick.AddListener(_viewModel.Decrement)", viewSource);
    }

    // -------------------------------------------------------------------------
    // Scenario 6: field and method with the same explicit id≥0 share one View field
    // -------------------------------------------------------------------------

    [Fact]
    public void CountViewModel6SameExplicitIdMethodsShareField()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModel6
    {
        [Bindings.Required]
        private readonly object _model;

        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", id: 1)]
        public void Increment() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", id: 1)]
        public void Decrement() { }
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        // 2 entries with id=1 share the same _button1 field (Case B)
        Assert.Contains("_button1", viewSource);
        Assert.DoesNotContain("_button2", viewSource);
        // Both methods AddListener on the same field
        Assert.Contains("_button1.onClick.RemoveAllListeners", viewSource);
        Assert.Contains("_button1.onClick.AddListener(_viewModel.Increment)", viewSource);
        Assert.Contains("_button1.onClick.AddListener(_viewModel.Decrement)", viewSource);
    }

    // -------------------------------------------------------------------------
    // Scenario: readonly struct does not generate a set accessor
    // -------------------------------------------------------------------------

    [Fact]
    public void ReadOnlyStructDoesNotGenerateSetAccessor()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public readonly partial struct CountViewModelReadOnly
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private readonly int _count;
    }
}";

        var (vmSource, _) = RunGenerator(userCode);

        Assert.NotNull(vmSource);
        // get accessor is generated
        Assert.Contains("get => _count;", vmSource);
        // set accessor is NOT generated (readonly struct)
        Assert.DoesNotContain("set", vmSource);
    }

    // -------------------------------------------------------------------------
    // Scenario: single id=-1 entry produces a field without suffix
    // -------------------------------------------------------------------------

    [Fact]
    public void SingleDefaultIdEntryProducesFieldWithoutSuffix()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelSingle
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Increment() { }
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        // One entry per type → no numeric suffix
        Assert.Contains("_text ", viewSource);
        Assert.Contains("_button ", viewSource);
        Assert.DoesNotContain("_text0", viewSource);
        Assert.DoesNotContain("_button0", viewSource);
        Assert.DoesNotContain("_button1", viewSource);
    }

    // -------------------------------------------------------------------------
    // Tooltip: single [Schema] with tooltip → [Tooltip] attribute on View field
    // -------------------------------------------------------------------------

    [Fact]
    public void TooltipSingleSchemaEmitsTooltipAttribute()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelTooltip
    {
        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", tooltip: ""Increment the count"")]
        public void Increment() { }
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        Assert.Contains("[global::UnityEngine.Tooltip(\"Increment the count\")]", viewSource);
        Assert.Contains("[global::UnityEngine.SerializeField]", viewSource);
    }

    // -------------------------------------------------------------------------
    // Tooltip: no tooltip specified → no [Tooltip] attribute on View field
    // -------------------------------------------------------------------------

    [Fact]
    public void TooltipNoTooltipDoesNotEmitTooltipAttribute()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelNoTooltip
    {
        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Increment() { }
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        Assert.DoesNotContain("UnityEngine.Tooltip", viewSource);
    }

    // -------------------------------------------------------------------------
    // Tooltip BND003: same View field with conflicting tooltips → diagnostic error
    // -------------------------------------------------------------------------

    [Fact]
    public void TooltipConflictingTooltipsReportsBND003()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelConflictTooltip
    {
        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", id: 1, tooltip: ""Increment"")]
        public void Increment() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", id: 1, tooltip: ""Decrement"")]
        public void Decrement() { }
    }
}";

        var generator = new ViewModelGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[]
            {
                CSharpSyntaxTree.ParseText(AttributeStubs),
                CSharpSyntaxTree.ParseText(userCode),
            },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // BND003 should be reported
        var bnd003 = runResult.Diagnostics.FirstOrDefault(d => d.Id == "BND003");
        Assert.NotNull(bnd003);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, bnd003.Severity);
        Assert.Contains("_button1", bnd003.GetMessage());

        // View field should still be generated with the first tooltip
        var viewSource = runResult.GeneratedTrees
            .FirstOrDefault(t =>
            {
                var name = System.IO.Path.GetFileName(t.FilePath);
                return name.Contains("View") && !name.Contains("ViewModel") && t.FilePath.EndsWith(".g.cs");
            })
            ?.GetText().ToString();
        Assert.NotNull(viewSource);
        Assert.Contains("[global::UnityEngine.Tooltip(\"Increment\")]", viewSource);
    }

    // -------------------------------------------------------------------------
    // [System.Serializable] already applied: do not add it again
    // -------------------------------------------------------------------------

    [Fact]
    public void AlreadySerializableDoesNotAddSerializableAgain()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    [System.Serializable]
    public partial class CountViewModelSerializable
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;
    }
}";

        var (vmSource, _) = RunGenerator(userCode);

        Assert.NotNull(vmSource);
        // [Serializable] is already applied so the generator must not add it again
        Assert.DoesNotContain("[global::System.Serializable]", vmSource);
    }

    // -------------------------------------------------------------------------
    // BND001: [ViewModel] class name does not contain "ViewModel" → error diagnostic, no View generated
    // -------------------------------------------------------------------------

    [Fact]
    public void BND001TypeNameWithoutViewModelReportsErrorAndSkipsViewGeneration()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountModel
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;
    }
}";

        var generator = new ViewModelGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[]
            {
                CSharpSyntaxTree.ParseText(AttributeStubs),
                CSharpSyntaxTree.ParseText(userCode),
            },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // BND001 must be reported as an Error
        var bnd001 = runResult.Diagnostics.FirstOrDefault(d => d.Id == "BND001");
        Assert.NotNull(bnd001);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, bnd001.Severity);
        Assert.Contains("CountModel", bnd001.GetMessage());

        // No View should be generated for this type
        var viewSource = runResult.GeneratedTrees
            .FirstOrDefault(t =>
            {
                var name = System.IO.Path.GetFileName(t.FilePath);
                return name.Contains("View") && !name.Contains("ViewModel") && t.FilePath.EndsWith(".g.cs");
            });
        Assert.Null(viewSource);

        // ViewModel partial is still generated
        var vmSource = runResult.GeneratedTrees
            .FirstOrDefault(t => System.IO.Path.GetFileName(t.FilePath).Contains("CountModel") && t.FilePath.EndsWith(".g.cs"));
        Assert.NotNull(vmSource);
    }

    // -------------------------------------------------------------------------
    // BND002: [Schema] id < -1 → error diagnostic, id treated as -1 (auto-number)
    // -------------------------------------------------------------------------

    [Fact]
    public void BND002NegativeSchemaIdReportsError()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModel
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"", id: -2)]
        private int _count;
    }
}";

        var generator = new ViewModelGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[]
            {
                CSharpSyntaxTree.ParseText(AttributeStubs),
                CSharpSyntaxTree.ParseText(userCode),
            },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        // BND002 must be reported as an Error
        var bnd002 = runResult.Diagnostics.FirstOrDefault(d => d.Id == "BND002");
        Assert.NotNull(bnd002);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, bnd002.Severity);
        Assert.Contains("-2", bnd002.GetMessage());
    }

    // -------------------------------------------------------------------------
    // RequireBindImplementation=true → no BindAsync emitted in View
    // -------------------------------------------------------------------------

    [Fact]
    public void RequireBindImplementationTrueOmitsBindAsync()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel(requireBindImplementation: true)]
    public partial class CountViewModelRequireBind
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        Assert.DoesNotContain("BindAsync", viewSource);
        // BindAll is still emitted
        Assert.Contains("BindAll", viewSource);
    }

    // -------------------------------------------------------------------------
    // RequireBindImplementation=false (default) → BindAsync IS emitted
    // -------------------------------------------------------------------------

    [Fact]
    public void RequireBindImplementationFalseEmitsBindAsync()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelDefaultBind
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        Assert.Contains("BindAsync", viewSource);
    }

    // -------------------------------------------------------------------------
    // Non-readonly struct generates set accessor
    // -------------------------------------------------------------------------

    [Fact]
    public void RegularStructGeneratesSetAccessor()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial struct CountViewModelStruct
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;
    }
}";

        var (vmSource, _) = RunGenerator(userCode);

        Assert.NotNull(vmSource);
        Assert.Contains("get => _count;", vmSource);
        Assert.Contains("set", vmSource);
        // Generated as struct, not class
        Assert.Contains("public partial struct CountViewModelStruct", vmSource);
    }

    // -------------------------------------------------------------------------
    // Non-TMPro field binding uses direct assignment (_field.member = _viewModel.Prop)
    // -------------------------------------------------------------------------

    [Fact]
    public void NonTmproFieldBindingUsesDirectAssignment()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelDirect
    {
        [Bindings.Schema(""UnityEngine.UI.Image.fillAmount"")]
        private float _progress;
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        // Direct assignment, not SetValue
        Assert.Contains("_image.fillAmount = _viewModel.Progress;", viewSource);
        Assert.DoesNotContain("SetValue", viewSource);
    }

    // -------------------------------------------------------------------------
    // TMPro field binding with format → SetValue with format argument
    // -------------------------------------------------------------------------

    [Fact]
    public void TmproFieldBindingWithFormatUsesSetValueWithFormat()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelFormat
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"", format: ""N0"")]
        private int _count;
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        Assert.Contains("SetValue(_text, _viewModel.Count, \"N0\");", viewSource);
    }

    // -------------------------------------------------------------------------
    // Case C: mixed id=-1 and id≥0 entries for the same type part
    // -------------------------------------------------------------------------

    [Fact]
    public void CaseCMixedIdAssignsCorrectFieldNumbers()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelCaseC
    {
        // id=2 → _button2; id=-1 (unset) → _button1 (first available, skipping 2)
        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", id: 2)]
        public void Submit() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Cancel() { }
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        // id=2 → _button2
        Assert.Contains("_button2", viewSource);
        // id=-1 unset → first available integer not taken by explicit ids (1)
        Assert.Contains("_button1", viewSource);
        Assert.DoesNotContain("_button3", viewSource);
    }

    // -------------------------------------------------------------------------
    // Global namespace (no namespace declaration)
    // -------------------------------------------------------------------------

    [Fact]
    public void GlobalNamespaceViewModelGeneratesWithoutNamespace()
    {
        const string userCode = @"
[Bindings.ViewModel]
public partial class GlobalViewModel
{
    [Bindings.Schema(""TMPro.TMP_Text.text"")]
    private int _count;
}";

        var (vmSource, viewSource) = RunGenerator(userCode);

        Assert.NotNull(vmSource);
        Assert.NotNull(viewSource);
        // No namespace block — type appears at the top level
        Assert.DoesNotContain("namespace", vmSource);
        Assert.DoesNotContain("namespace", viewSource);
        // But the fully-qualified IViewModel reference must still have global:: prefix
        Assert.Contains("global::Bindings.IViewModel", vmSource);
    }

    // -------------------------------------------------------------------------
    // [Required] on property (not field) is collected for the constructor
    // -------------------------------------------------------------------------

    [Fact]
    public void ModelOnPropertyIsIncludedInConstructor()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelPropModel
    {
        [Bindings.Required]
        public object Model { get; }

        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;
    }
}";

        var (vmSource, _) = RunGenerator(userCode);

        Assert.NotNull(vmSource);
        // [Required] on property → constructor param named after the property
        Assert.Contains("model,", vmSource);
    }

    // -------------------------------------------------------------------------
    // BND002 on a method schema with id < -1
    // -------------------------------------------------------------------------

    [Fact]
    public void BND002NegativeSchemaIdOnMethodReportsError()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelMethodBnd002
    {
        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", id: -3)]
        public void Increment() { }
    }
}";

        var generator = new ViewModelGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[]
            {
                CSharpSyntaxTree.ParseText(AttributeStubs),
                CSharpSyntaxTree.ParseText(userCode),
            },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var bnd002 = runResult.Diagnostics.FirstOrDefault(d => d.Id == "BND002");
        Assert.NotNull(bnd002);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Error, bnd002.Severity);
        Assert.Contains("-3", bnd002.GetMessage());
    }

    // -------------------------------------------------------------------------
    // Field and method with same explicit id share one View field (Case B combined)
    // -------------------------------------------------------------------------

    [Fact]
    public void FieldAndMethodWithSameExplicitIdShareViewField()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelFieldMethodShared
    {
        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", id: 1)]
        private bool _interactable;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", id: 1)]
        public void OnClick() { }
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        // Both field and method share _button1
        Assert.Contains("_button1", viewSource);
        Assert.DoesNotContain("_button2", viewSource);
        Assert.Contains("_button1.onClick.AddListener(_viewModel.OnClick)", viewSource);
    }

    // -------------------------------------------------------------------------
    // Inner class: [ViewModel] nested in one outer class
    // -------------------------------------------------------------------------

    [Fact]
    public void InnerViewModelWrapsInContainingClass()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    public partial class Outer
    {
        [Bindings.ViewModel]
        public partial class CountViewModel1
        {
            [Bindings.Schema(""TMPro.TMP_Text.text"")]
            private int _count;
        }
    }
}";

        var (vmSource, viewSource) = RunGenerator(userCode);

        Assert.NotNull(vmSource);
        Assert.NotNull(viewSource);

        // ViewModel: wrapped in namespace + outer class + ViewModel class (all unindented)
        Assert.Contains("namespace Bindings.Sample", vmSource);
        Assert.Contains("public partial class Outer", vmSource);
        Assert.Contains("public partial class CountViewModel1 : global::Bindings.IViewModel", vmSource);

        // ViewModel body uses fixed 4-space indent
        Assert.Contains("    private readonly global::Bindings.IMvvmPublisher _publisher;", vmSource);
        Assert.Contains("    public int Count", vmSource);

        // Fully-qualified name includes the containing type
        Assert.Contains("global::Bindings.Sample.Outer.CountViewModel1", vmSource);

        // View: also wrapped in outer class
        Assert.Contains("public partial class Outer", viewSource);
        Assert.Contains("public sealed partial class CountView1", viewSource);
        Assert.Contains("global::Bindings.Sample.Outer.CountViewModel1", viewSource);
    }

    // -------------------------------------------------------------------------
    // Inner class: [ViewModel] nested in two outer classes
    // -------------------------------------------------------------------------

    [Fact]
    public void DoublyNestedViewModelWrapsInBothContainingClasses()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    public partial class Outer
    {
        public partial class Middle
        {
            [Bindings.ViewModel]
            public partial class CountViewModel1
            {
                [Bindings.Schema(""TMPro.TMP_Text.text"")]
                private int _count;
            }
        }
    }
}";

        var (vmSource, viewSource) = RunGenerator(userCode);

        Assert.NotNull(vmSource);
        Assert.NotNull(viewSource);

        // Both containing classes are present (unindented)
        Assert.Contains("public partial class Outer", vmSource);
        Assert.Contains("public partial class Middle", vmSource);
        Assert.Contains("public partial class CountViewModel1 : global::Bindings.IViewModel", vmSource);

        // Fully-qualified name includes both containing types
        Assert.Contains("global::Bindings.Sample.Outer.Middle.CountViewModel1", vmSource);

        // View also wraps both containing classes
        Assert.Contains("public partial class Outer", viewSource);
        Assert.Contains("public partial class Middle", viewSource);
        Assert.Contains("global::Bindings.Sample.Outer.Middle.CountViewModel1", viewSource);
    }

    // -------------------------------------------------------------------------
    // Inner class without namespace
    // -------------------------------------------------------------------------

    [Fact]
    public void InnerViewModelInGlobalNamespaceWrapsInContainingClass()
    {
        const string userCode = @"
public partial class Outer
{
    [Bindings.ViewModel]
    public partial class CountViewModel1
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;
    }
}";

        var (vmSource, viewSource) = RunGenerator(userCode);

        Assert.NotNull(vmSource);
        Assert.NotNull(viewSource);

        // No namespace block
        Assert.DoesNotContain("namespace", vmSource);

        // Outer class wrapping present
        Assert.Contains("public partial class Outer", vmSource);
        Assert.Contains("public partial class CountViewModel1 : global::Bindings.IViewModel", vmSource);

        // Fully-qualified name with global:: but no namespace
        Assert.Contains("global::Outer.CountViewModel1", vmSource);
    }

    // -------------------------------------------------------------------------
    // Fixed indentation: namespace-present types use the same indent as no-namespace types
    // -------------------------------------------------------------------------

    [Fact]
    public void FixedIndentationClassBodyAlwaysUsesFourSpaces()
    {
        // With namespace
        const string withNs = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModelIndent
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;
    }
}";

        // Without namespace
        const string withoutNs = @"
[Bindings.ViewModel]
public partial class CountViewModelIndentGlobal
{
    [Bindings.Schema(""TMPro.TMP_Text.text"")]
    private int _count;
}";

        var (vmSourceWithNs, _) = RunGenerator(withNs);
        var (vmSourceNoNs, _) = RunGenerator(withoutNs);

        Assert.NotNull(vmSourceWithNs);
        Assert.NotNull(vmSourceNoNs);

        // With namespace: class at column 0 (no leading spaces)
        Assert.Contains("public partial class CountViewModelIndent : global::Bindings.IViewModel", vmSourceWithNs);
        Assert.DoesNotContain("    public partial class CountViewModelIndent", vmSourceWithNs);

        // Without namespace: class also at column 0
        Assert.Contains("public partial class CountViewModelIndentGlobal : global::Bindings.IViewModel", vmSourceNoNs);

        // Both have 4-space indented members
        Assert.Contains("    private readonly global::Bindings.IMvvmPublisher _publisher;", vmSourceWithNs);
        Assert.Contains("    private readonly global::Bindings.IMvvmPublisher _publisher;", vmSourceNoNs);
    }

    // -------------------------------------------------------------------------
    // End-to-end compilation: inner class ViewModel generates compilable code
    // -------------------------------------------------------------------------

    [Fact]
    public void InnerViewModelGeneratedCodeCompiles()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    public partial class Outer
    {
        [Bindings.ViewModel]
        public partial class CountViewModel1
        {
            [Bindings.Schema(""TMPro.TMP_Text.text"")]
            private int _count;
        }
    }
}";

        var errors = RunGeneratorAndCompile(userCode);
        Assert.Empty(errors);
    }

    // -------------------------------------------------------------------------
    // End-to-end compilation: doubly-nested inner class ViewModel
    // -------------------------------------------------------------------------

    [Fact]
    public void DoublyNestedViewModelGeneratedCodeCompiles()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    public partial class Outer
    {
        public partial class Middle
        {
            [Bindings.ViewModel]
            public partial class CountViewModel1
            {
                [Bindings.Schema(""TMPro.TMP_Text.text"")]
                private int _count;
            }
        }
    }
}";

        var errors = RunGeneratorAndCompile(userCode);
        Assert.Empty(errors);
    }

    // -------------------------------------------------------------------------
    // End-to-end compilation: top-level (non-nested) ViewModel still compiles
    // -------------------------------------------------------------------------

    [Fact]
    public void TopLevelViewModelGeneratedCodeCompiles()
    {
        const string userCode = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModel1
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;
    }
}";

        var errors = RunGeneratorAndCompile(userCode);
        Assert.Empty(errors);
    }

    // -------------------------------------------------------------------------
    // GetBindingPath: object overload — expression contains "Resolver." prefix
    // -------------------------------------------------------------------------

    [Fact]
    public void GetBindingPath_ObjectOverloadWithResolverPrefix_ExtractsPath()
    {
        // Using a static readonly (non-const) field forces the object overload.
        // CallerArgumentExpression captures "Resolver.TMPro_TMP_Text_text", so
        // GetBindingPath strips the "Resolver." prefix and returns "TMPro_TMP_Text_text".
        const string userCode = @"
namespace Bindings.Sample
{
    public static class Resolver
    {
        public static readonly int TMPro_TMP_Text_text = 0;
    }

    [Bindings.ViewModel]
    public partial class CountViewModelObjectPath1
    {
        [Bindings.Schema(Resolver.TMPro_TMP_Text_text)]
        private int _count;
    }
}";

        var (_, viewSource) = RunGenerator(userCode);

        Assert.NotNull(viewSource);
        // Binding path is extracted as "TMPro_TMP_Text_text" → type part "TMPro_TMP_Text", member "text"
        Assert.Contains("_text", viewSource);
    }

    // -------------------------------------------------------------------------
    // GetBindingPath: object overload — expression has no "Resolver." prefix
    // -------------------------------------------------------------------------

    [Fact]
    public void GetBindingPath_ObjectOverloadWithoutResolverPrefix_UsesExpressionAsPath()
    {
        // A static readonly field whose name has no "Resolver." → path = rawExpr directly.
        const string userCode = @"
namespace Bindings.Sample
{
    public static class MyPaths
    {
        public static readonly int SomeValue = 0;
    }

    [Bindings.ViewModel]
    public partial class CountViewModelObjectRaw1
    {
        [Bindings.Schema(MyPaths.SomeValue)]
        private int _count;
    }
}";

        var (vmSource, _) = RunGenerator(userCode);

        // Generator runs without crashing; ViewModel source is produced
        Assert.NotNull(vmSource);
    }
}
