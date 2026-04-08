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
    public sealed class ModelAttribute : Attribute { }

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

    public interface IView<T> { }

    public interface IView
    {
        System.Threading.Tasks.ValueTask BindAsync(System.Threading.CancellationToken cancellationToken);
    }

    public interface IMvvmPublisher
    {
        void PublishRebindMessage<T>() where T : IViewModel;
    }

    public interface IMvvmSubscriber<T> { }

    public sealed class DebugBindMessage { }
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
        [Bindings.Model]
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
        [Bindings.Model]
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
    // Tooltip BND003: same View field with conflicting tooltips → diagnostic warning
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
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, bnd003.Severity);
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
}
