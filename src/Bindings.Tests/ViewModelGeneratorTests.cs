using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Bindings.Tests;

/// <summary>
/// ViewModelGenerator のテストクラス.
/// 各シナリオで生成コードが仕様通りかを検証する.
/// </summary>
public class ViewModelGeneratorTests
{
    // -------------------------------------------------------------------------
    // テスト用のスタブ定義（Unity 依存型の代替）
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

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
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
    // シナリオ 1: id=-1 (デフォルト) の複数の同一型メソッドは別々のフィールドになる
    // -------------------------------------------------------------------------

    [Fact]
    public void CountViewModel1_MultipleDefaultIdMethods_ProduceSeparateFields()
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
        // id=-1 の複数 Button エントリは _button1, _button2 に分かれる（Case A）
        Assert.Contains("_button1", viewSource);
        Assert.Contains("_button2", viewSource);
        Assert.DoesNotContain("_button0", viewSource);
        // TMP_Text は 1 つだけなので _text（連番なし）
        Assert.Contains("_text ", viewSource);
        Assert.DoesNotContain("_text0", viewSource);
        // Increment / Decrement はそれぞれ別の button に AddListener
        Assert.Contains("_button1.onClick.RemoveAllListeners", viewSource);
        Assert.Contains("_button1.onClick.AddListener(_viewModel.Increment)", viewSource);
        Assert.Contains("_button2.onClick.RemoveAllListeners", viewSource);
        Assert.Contains("_button2.onClick.AddListener(_viewModel.Decrement)", viewSource);
    }

    // -------------------------------------------------------------------------
    // シナリオ 6: 同一 id≥0 のフィールドとメソッドは同一フィールドを共有する
    // -------------------------------------------------------------------------

    [Fact]
    public void CountViewModel6_SameExplicitIdMethodsShareField()
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
        // id=1 の 2 エントリは同一フィールド _button1 を共有する（Case B）
        Assert.Contains("_button1", viewSource);
        Assert.DoesNotContain("_button2", viewSource);
        // 同一フィールドに 2 つの AddListener
        Assert.Contains("_button1.onClick.RemoveAllListeners", viewSource);
        Assert.Contains("_button1.onClick.AddListener(_viewModel.Increment)", viewSource);
        Assert.Contains("_button1.onClick.AddListener(_viewModel.Decrement)", viewSource);
    }

    // -------------------------------------------------------------------------
    // シナリオ: readonly struct は set アクセサを生成しない
    // -------------------------------------------------------------------------

    [Fact]
    public void ReadOnlyStruct_DoesNotGenerateSetAccessor()
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
        // get アクセサは生成される
        Assert.Contains("get => _count;", vmSource);
        // set アクセサは生成されない（readonly struct）
        Assert.DoesNotContain("set", vmSource);
    }

    // -------------------------------------------------------------------------
    // シナリオ: id=-1 のシングルエントリは連番なし
    // -------------------------------------------------------------------------

    [Fact]
    public void SingleDefaultIdEntry_ProducesFieldWithoutSuffix()
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
        // 各型が 1 つずつなので連番なし
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
    public void Tooltip_SingleSchema_EmitsTooltipAttribute()
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
    public void Tooltip_NoTooltip_DoesNotEmitTooltipAttribute()
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
    public void Tooltip_ConflictingTooltips_ReportsBND003()
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
    public void AlreadySerializable_DoesNotAddSerializableAgain()
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
}
