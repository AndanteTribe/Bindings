using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Bindings.Tests;

/// <summary>
/// Tests for <see cref="ViewModelGenerator"/> using the sample ViewModel scenarios
/// defined in Bindings.Sample/CountViewModel.cs.
/// </summary>
public class ViewModelGeneratorTests
{
    // ──────────────────────────────────────────────────────────
    //  Attribute stubs (the Unity runtime isn't available here)
    // ──────────────────────────────────────────────────────────

    private const string AttributeStubs = @"
using System;
using System.Runtime.CompilerServices;

namespace Bindings
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class ViewModelAttribute : Attribute
    {
        public readonly bool RequireBindImplementation;
        public ViewModelAttribute(bool requireBindImplementation = false)
        {
            RequireBindImplementation = requireBindImplementation;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ModelAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SchemaAttribute : Attribute
    {
        public readonly string BindingPath;
        public readonly int Id;
        public readonly string Format;

        public SchemaAttribute(string bindingPath, int id = 0, string format = """")
        {
            BindingPath = bindingPath;
            Id = id;
            Format = format;
        }
    }
}

namespace UnityEngine { public sealed class SerializeFieldAttribute : Attribute { } }
";

    // ──────────────────────────────────────────────────────────
    //  Helper: run the generator and return all generated source texts
    // ──────────────────────────────────────────────────────────

    private static (string viewModelSource, string viewSource) RunGenerator(string userCode)
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
            });

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var vmTree = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith(".ViewModel.g.cs"));
        var viewTree = runResult.GeneratedTrees.FirstOrDefault(t => t.FilePath.EndsWith(".g.cs") && !t.FilePath.EndsWith(".ViewModel.g.cs"));

        return (vmTree?.GetText().ToString() ?? "", viewTree?.GetText().ToString() ?? "");
    }

    // ──────────────────────────────────────────────────────────
    //  Scenario 1 — simple
    // ──────────────────────────────────────────────────────────

    private const string Scenario1Input = @"
namespace Bindings.Sample
{
    public sealed class CountModel { public int Count { get; set; } }

    [Bindings.ViewModel]
    public partial class CountViewModel1
    {
        [Bindings.Model]
        private readonly CountModel _model;

        [UnityEngine.SerializeField]
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Increment() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Decrement() { }

        partial void OnPostBind() { }
    }
}";

    [Fact]
    public void Scenario1_GeneratesSerializableAttribute()
    {
        var (vm, _) = RunGenerator(Scenario1Input);
        Assert.Contains("[global::System.Serializable]", vm);
    }

    [Fact]
    public void Scenario1_GeneratesIViewModelInterface()
    {
        var (vm, _) = RunGenerator(Scenario1Input);
        Assert.Contains("public partial class CountViewModel1 : global::Bindings.IViewModel", vm);
    }

    [Fact]
    public void Scenario1_GeneratesPublisherField()
    {
        var (vm, _) = RunGenerator(Scenario1Input);
        Assert.Contains("private readonly global::Bindings.IMvvmPublisher _publisher;", vm);
    }

    [Fact]
    public void Scenario1_GeneratesCountProperty()
    {
        var (vm, _) = RunGenerator(Scenario1Input);
        Assert.Contains("public int Count", vm);
        Assert.Contains("get => _count;", vm);
        Assert.Contains("_count = value;", vm);
        Assert.Contains("PublishRebindMessage();", vm);
    }

    [Fact]
    public void Scenario1_GeneratesConstructorWithModelAndPublisher()
    {
        var (vm, _) = RunGenerator(Scenario1Input);
        Assert.Contains("global::Bindings.Sample.CountModel model, global::Bindings.IMvvmPublisher publisher", vm);
        Assert.Contains("_model = model;", vm);
        Assert.Contains("_publisher = publisher;", vm);
    }

    [Fact]
    public void Scenario1_GeneratesNotifyCompletedBind()
    {
        var (vm, _) = RunGenerator(Scenario1Input);
        Assert.Contains("public void NotifyCompletedBind() => OnPostBind();", vm);
    }

    [Fact]
    public void Scenario1_GeneratesPublishRebindMessage()
    {
        var (vm, _) = RunGenerator(Scenario1Input);
        Assert.Contains("_publisher.PublishRebindMessage<CountViewModel1>();", vm);
    }

    [Fact]
    public void Scenario1_View_GeneratesTwoButtonFields()
    {
        var (_, view) = RunGenerator(Scenario1Input);
        Assert.Contains("private global::UnityEngine.UI.Button _button1 = null!;", view);
        Assert.Contains("private global::UnityEngine.UI.Button _button2 = null!;", view);
    }

    [Fact]
    public void Scenario1_View_GeneratesTextMeshProField()
    {
        var (_, view) = RunGenerator(Scenario1Input);
        Assert.Contains("private global::TMPro.TMP_Text _text = null!;", view);
    }

    [Fact]
    public void Scenario1_View_GeneratesBindAll()
    {
        var (_, view) = RunGenerator(Scenario1Input);
        Assert.Contains("global::Bindings.TextMeshProExtensions.SetValue(_text, _viewModel.Count);", view);
        Assert.Contains("_button1.onClick.RemoveAllListeners();", view);
        Assert.Contains("_button1.onClick.AddListener(_viewModel.Increment);", view);
        Assert.Contains("_button2.onClick.RemoveAllListeners();", view);
        Assert.Contains("_button2.onClick.AddListener(_viewModel.Decrement);", view);
    }

    [Fact]
    public void Scenario1_View_GeneratesBindAsync()
    {
        var (_, view) = RunGenerator(Scenario1Input);
        Assert.Contains("global::Bindings.IView.BindAsync(global::System.Threading.CancellationToken _)", view);
        Assert.Contains("BindAll();", view);
    }

    [Fact]
    public void Scenario1_View_GeneratesDebugSection()
    {
        var (_, view) = RunGenerator(Scenario1Input);
        Assert.Contains("UNITY_EDITOR || DEVELOPMENT_BUILD || !DISABLE_DEBUGTOOLKIT", view);
        Assert.Contains("IMvvmSubscriber<global::Bindings.DebugBindMessage>", view);
        Assert.Contains("message.BindTo(this);", view);
        // Debug section should repeat data bindings but NOT event bindings
        Assert.Contains("global::Bindings.TextMeshProExtensions.SetValue(_text, _viewModel.Count);", view);
    }

    // ──────────────────────────────────────────────────────────
    //  Scenario 2 — requireBindImplementation: true
    // ──────────────────────────────────────────────────────────

    private const string Scenario2Input = @"
namespace Bindings.Sample
{
    public sealed class CountModel { public int Count { get; set; } }

    [Bindings.ViewModel(requireBindImplementation: true)]
    public partial class CountViewModel2
    {
        [Bindings.Model]
        private readonly CountModel _model;

        [UnityEngine.SerializeField]
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Increment() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Decrement() { }
    }
}";

    [Fact]
    public void Scenario2_View_DoesNotGenerateBindAsync()
    {
        var (_, view) = RunGenerator(Scenario2Input);
        // BindAsync should NOT be present when requireBindImplementation: true
        Assert.DoesNotContain("global::Bindings.IView.BindAsync", view);
    }

    [Fact]
    public void Scenario2_View_DoesGenerateBindAll()
    {
        var (_, view) = RunGenerator(Scenario2Input);
        Assert.Contains("private void BindAll()", view);
    }

    // ──────────────────────────────────────────────────────────
    //  Scenario 3 — already [Serializable]
    // ──────────────────────────────────────────────────────────

    private const string Scenario3Input = @"
namespace Bindings.Sample
{
    public sealed class CountModel { public int Count { get; set; } }

    [Bindings.ViewModel]
    [System.Serializable]
    public partial class CountViewModel3
    {
        [Bindings.Model]
        private readonly CountModel _model;

        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Increment() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Decrement() { }
    }
}";

    [Fact]
    public void Scenario3_ViewModel_DoesNotAddSerializable()
    {
        var (vm, _) = RunGenerator(Scenario3Input);
        // Should NOT add [global::System.Serializable] since the user already has [System.Serializable]
        Assert.DoesNotContain("[global::System.Serializable]", vm);
    }

    // ──────────────────────────────────────────────────────────
    //  Scenario 4 — no model
    // ──────────────────────────────────────────────────────────

    private const string Scenario4Input = @"
namespace Bindings.Sample
{
    [Bindings.ViewModel]
    public partial class CountViewModel4
    {
        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Increment() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Decrement() { }
    }
}";

    [Fact]
    public void Scenario4_ViewModel_ConstructorOnlyPublisher()
    {
        var (vm, _) = RunGenerator(Scenario4Input);
        Assert.Contains("public CountViewModel4(global::Bindings.IMvvmPublisher publisher)", vm);
        Assert.Contains("_publisher = publisher;", vm);
    }

    // ──────────────────────────────────────────────────────────
    //  Scenario 5 — multi models
    // ──────────────────────────────────────────────────────────

    private const string Scenario5Input = @"
namespace Bindings.Sample
{
    public sealed class CountModel { public int Count { get; set; } }

    [Bindings.ViewModel]
    public partial class CountViewModel5
    {
        [Bindings.Model]
        private readonly CountModel _model;

        [Bindings.Model]
        private readonly CountModel _model2;

        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Increment() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Decrement() { }
    }
}";

    [Fact]
    public void Scenario5_ViewModel_ConstructorHasTwoModels()
    {
        var (vm, _) = RunGenerator(Scenario5Input);
        Assert.Contains("global::Bindings.Sample.CountModel model, global::Bindings.Sample.CountModel model2, global::Bindings.IMvvmPublisher publisher", vm);
        Assert.Contains("_model = model;", vm);
        Assert.Contains("_model2 = model2;", vm);
    }

    // ──────────────────────────────────────────────────────────
    //  Scenario 6 — same id pair (two methods share one Button)
    // ──────────────────────────────────────────────────────────

    private const string Scenario6Input = @"
namespace Bindings.Sample
{
    public sealed class CountModel { public int Count { get; set; } }

    [Bindings.ViewModel]
    public partial class CountViewModel6
    {
        [Bindings.Model]
        private readonly CountModel _model;

        [Bindings.Schema(""TMPro.TMP_Text.text"")]
        private int _count;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", id: 1)]
        public void Increment() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"", id: 1)]
        public void Decrement() { }
    }
}";

    [Fact]
    public void Scenario6_View_SharedButtonHasId1Suffix()
    {
        var (_, view) = RunGenerator(Scenario6Input);
        Assert.Contains("private global::UnityEngine.UI.Button _button1 = null!;", view);
        // There should NOT be a _button2
        Assert.DoesNotContain("_button2", view);
    }

    [Fact]
    public void Scenario6_View_BothMethodsShareOneButton()
    {
        var (_, view) = RunGenerator(Scenario6Input);
        // RemoveAllListeners called once
        Assert.Contains("_button1.onClick.RemoveAllListeners();", view);
        Assert.Contains("_button1.onClick.AddListener(_viewModel.Increment);", view);
        Assert.Contains("_button1.onClick.AddListener(_viewModel.Decrement);", view);
    }

    // ──────────────────────────────────────────────────────────
    //  Scenario 7 — format and non-text field (Toggle)
    // ──────────────────────────────────────────────────────────

    private const string Scenario7Input = @"
namespace Bindings.Sample
{
    public sealed class CountModel { public int Count { get; set; } }

    [Bindings.ViewModel]
    public partial class CountViewModel7
    {
        [Bindings.Model]
        private readonly CountModel _model;

        [Bindings.Schema(""TMPro.TMP_Text.text"", format: ""N0"")]
        private int _count;

        [Bindings.Schema(""UnityEngine.UI.Toggle.interactable"")]
        private bool _interactable;

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Increment() { }

        [Bindings.Schema(""UnityEngine.UI.Button.onClick"")]
        public void Decrement() { }
    }
}";

    [Fact]
    public void Scenario7_View_GeneratesToggleField()
    {
        var (_, view) = RunGenerator(Scenario7Input);
        Assert.Contains("private global::UnityEngine.UI.Toggle _toggle = null!;", view);
    }

    [Fact]
    public void Scenario7_View_GeneratesFormattedSetValue()
    {
        var (_, view) = RunGenerator(Scenario7Input);
        Assert.Contains("global::Bindings.TextMeshProExtensions.SetValue(_text, _viewModel.Count, \"N0\");", view);
    }

    [Fact]
    public void Scenario7_View_GeneratesDirectPropertyBinding()
    {
        var (_, view) = RunGenerator(Scenario7Input);
        Assert.Contains("_toggle.interactable = _viewModel.Interactable;", view);
    }

    [Fact]
    public void Scenario7_View_DebugSectionIncludesDataBindingsOnly()
    {
        var (_, view) = RunGenerator(Scenario7Input);
        // Debug section should have text and toggle bindings but NOT button event bindings
        var debugIdx = view.IndexOf("UNITY_EDITOR");
        Assert.True(debugIdx >= 0, "Debug section not found");
        var debugSection = view.Substring(debugIdx);
        Assert.Contains("SetValue(_text, _viewModel.Count, \"N0\")", debugSection);
        Assert.Contains("_toggle.interactable = _viewModel.Interactable;", debugSection);
        Assert.DoesNotContain("RemoveAllListeners", debugSection);
    }

    [Fact]
    public void Scenario7_ViewModel_GeneratesBothProperties()
    {
        var (vm, _) = RunGenerator(Scenario7Input);
        Assert.Contains("public int Count", vm);
        Assert.Contains("public bool Interactable", vm);
    }

    // ──────────────────────────────────────────────────────────
    //  File naming
    // ──────────────────────────────────────────────────────────

    [Fact]
    public void GeneratedFiles_HaveExpectedNames()
    {
        var generator = new ViewModelGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[]
            {
                CSharpSyntaxTree.ParseText(AttributeStubs),
                CSharpSyntaxTree.ParseText(Scenario1Input),
            },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var runResult = driver.RunGenerators(compilation).GetRunResult();
        var fileNames = runResult.GeneratedTrees.Select(t => System.IO.Path.GetFileName(t.FilePath)).ToArray();

        Assert.Contains("CountViewModel1.ViewModel.g.cs", fileNames);
        Assert.Contains("CountView1.g.cs", fileNames);
    }
}
