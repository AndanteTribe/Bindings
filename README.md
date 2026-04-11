# Bindings
[![dotnet-test](https://github.com/AndanteTribe/Bindings/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/AndanteTribe/Bindings/actions/workflows/dotnet-test.yml)
[![Releases](https://img.shields.io/github/release/AndanteTribe/Bindings.svg)](https://github.com/AndanteTribe/Bindings/releases)
[![openupm](https://img.shields.io/npm/v/jp.andantetribe.bindings?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/jp.andantetribe.bindings/)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/Bindings.svg)](./LICENSE)

English | [日本語](README_JA.md)

## Overview
**Bindings** is an MVVM-based automated UI binding framework for Unity built on uGUI.

It provides a Roslyn Source Generator that automatically generates ViewModel and View partial classes from annotated C# code, eliminating boilerplate and keeping UI bindings in sync with your data models.

Bind requests triggered by `PublishRebindMessage()` are **batched** internally and executed all at once just before the Canvas renders (`Canvas.preWillRenderCanvases`), avoiding redundant updates within a single frame.

## Requirements
- Unity 6000.0 or later
- Unity uGUI 2.0.0 or later

## Installation
Open `Window > Package Manager`, select `[+] > Add package from git URL`, and enter the following URL:

```
https://github.com/AndanteTribe/Bindings.git?path=src/Bindings.Unity/Packages/jp.andantetribe.bindings
```

## Quick Start

### 1. Define a ViewModel

Annotate a `partial` class with `[ViewModel]`. Use `[Required]` to declare constructor parameters and `[Schema]` to declare UI bindings.

```csharp
using Bindings;
using UnityEngine;

namespace MyApp
{
    [ViewModel]
    public partial class CounterViewModel
    {
        [Required]
        private readonly CounterModel _model;

        [SerializeField]
        [Schema(PathResolver.TMPro.TMP_Text.text)]
        private int _count;

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Increment()
        {
            _count += 1;
            PublishRebindMessage();
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            _count -= 1;
            PublishRebindMessage();
        }

        partial void OnPostBind()
        {
            _model.Count = _count;
        }
    }
}
```

The source generator automatically produces:
- `CounterViewModel.g.cs` — a partial ViewModel class with properties, constructor, and publisher wiring.
- `CounterView.g.cs` — a sealed partial View class with serialized UI component fields and binding logic.

### Generated Code

**`CounterViewModel.g.cs`**

```csharp
#nullable enable

namespace MyApp
{
    [global::System.Serializable]
    public partial class CounterViewModel : global::Bindings.IViewModel
    {
        private readonly global::Bindings.IMvvmPublisher _publisher;

        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                PublishRebindMessage();
            }
        }

        public CounterViewModel(global::MyApp.CounterModel model, global::Bindings.IMvvmPublisher publisher)
        {
            _model = model;
            _publisher = publisher;
        }

        public void NotifyCompletedBind() => OnPostBind();

        partial void OnPostBind();

        [global::System.Runtime.CompilerServices.MethodImpl(
            global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void PublishRebindMessage()
        {
            _publisher.PublishRebindMessage<CounterViewModel>();
        }
    }
}
```

**`CounterView.g.cs`**

```csharp
#nullable enable

namespace MyApp
{
    [global::System.Serializable]
    public sealed partial class CounterView : global::Bindings.IView<global::MyApp.CounterViewModel>
    {
        [global::System.NonSerialized]
        private global::MyApp.CounterViewModel _viewModel = null!;

        [global::UnityEngine.SerializeField]
        private global::TMPro.TMP_Text _text = null!;

        [global::UnityEngine.SerializeField]
        private global::UnityEngine.UI.Button _button1 = null!;

        [global::UnityEngine.SerializeField]
        private global::UnityEngine.UI.Button _button2 = null!;

        void global::Bindings.IView<global::MyApp.CounterViewModel>.Initialize(
            global::MyApp.CounterViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        global::System.Threading.Tasks.ValueTask global::Bindings.IView.BindAsync(
            global::System.Threading.CancellationToken _)
        {
            BindAll();
            return default;
        }

        private void BindAll()
        {
            global::Bindings.TextMeshProExtensions.SetValue(_text, _viewModel.Count);
            _button1.onClick.RemoveAllListeners();
            _button1.onClick.AddListener(_viewModel.Increment);
            _button2.onClick.RemoveAllListeners();
            _button2.onClick.AddListener(_viewModel.Decrement);
            OnPostBind();
            _viewModel.NotifyCompletedBind();
        }

        partial void OnPostBind();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD || !DISABLE_DEBUGTOOLKIT
    public sealed partial class CounterView : global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>
    {
        void global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>.OnReceivedMessage(
            global::Bindings.DebugBindMessage message)
        {
            message.BindTo(this);
            global::Bindings.TextMeshProExtensions.SetValue(_text, _viewModel.Count);
            OnPostBind();
            _viewModel.NotifyCompletedBind();
        }
    }
#endif
}
```

### 2. Set up the Binder in the Scene

1. Add a `Binder` component to a GameObject in your scene.
2. In the Inspector, assign the generated `CounterView` instance to the **Views** list.
3. Call `binder.Initialize(viewModel)` to associate the ViewModel, then call `binder.Run()` (or enable **Run On Start**) to bind the UI.

#### Using the Binder Inspector

The `Binder` component comes with a custom Unity Inspector that makes it easy to register Views without writing any setup code.

**Step 1 — Add a View: namespace selection**

Click the **Add View** button. A dropdown will appear — select the namespace that contains the View you want to register.

![Add View – namespace selection](https://github.com/user-attachments/assets/a9c4f6a7-9c4e-44b9-af94-5e830600e57e)

**Step 2 — Add a View: View selection**

After choosing a namespace, a list of all View types in that namespace is shown. Click the View you want to add.

![Add View – View selection](https://github.com/user-attachments/assets/f79c5a2b-2cac-46b2-889f-0cd3cd05f283)

**Step 3 — Assign UI components**

The selected View is now registered in the **Views** list. Each `[SerializeField]` declared in the View is shown as a slot in the Inspector — drag the corresponding UI GameObject onto each slot. To remove a View, click the **−** button in the top-right corner of its entry.

![Assign UI components](https://github.com/user-attachments/assets/fff8c683-2c58-47b3-b964-29e040337fc9)

**Step 4 — Preview**

Click the **Preview** button below **Add View** to reveal the ViewModel associated with each registered View. Fill in values directly in the Inspector and click **Invoke** to apply the bindings and verify that the UI updates as expected.

![Preview and Invoke](https://github.com/user-attachments/assets/2d34fa59-19ec-4e8b-8c82-10608e63b287)

```csharp
public class GameEntry : MonoBehaviour
{
    [SerializeField] private Binder _binder;

    private void Start()
    {
        var model = new CounterModel();
        var publisher = _binder; // Binder implements IMvvmPublisher
        var viewModel = new CounterViewModel(model, publisher);
        _binder.Initialize(viewModel);
    }
}
```

#### VContainer Support

When [VContainer](https://github.com/hadashiA/VContainer) is used as the DI container, `Binder` can receive ViewModels via injection automatically.

Add `ENABLE_VCONTAINER` to **Edit > Project Settings > Player > Scripting Define Symbols**, then register your ViewModels and the `Binder` in the container. VContainer will call `Initialize(IReadOnlyList<IViewModel>)` automatically, so no manual `Initialize` call is needed.

```csharp
public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private Binder _binder;

    protected override void Configure(IContainerBuilder builder)
    {
        var model = new CounterModel();
        builder.RegisterInstance(model);
        builder.Register<CounterViewModel>(Lifetime.Scoped)
               .AsImplementedInterfaces()
               .AsSelf();
        builder.RegisterComponent(_binder);
    }
}
```

## Attributes

### `[ViewModel]`

Applied to a `partial` class or struct to mark it as a ViewModel and trigger source generation.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `requireBindImplementation` | `bool` | `false` | When `true`, skips auto-generating `BindAsync`. The user must implement it manually on the partial View class. |

### `[Required]`

Applied to a field or property to include it as a parameter in the generated constructor. The generated constructor will accept a parameter of the field's type and assign it.

### `[Schema(bindingPath, id, format, tooltip)]`

Applied to a field or method to bind it to a UI component. Can be applied multiple times to the same member.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `bindingPath` | `string` | required | The UI component member to bind, expressed via `PathResolver` (e.g., `PathResolver.TMPro.TMP_Text.text`). |
| `id` | `int` | `-1` | Groups multiple `[Schema]` entries onto the **same** View component field. Use `id >= 0` for explicit grouping; `-1` means auto-numbered. Values less than `-1` trigger `BND002`. |
| `format` | `string` | `""` | Format string applied when binding to `TMPro.TMP_Text.text` (e.g., `"N0"`). Ignored for other binding paths. |
| `tooltip` | `string` | `""` | Tooltip text shown in the Unity Inspector on the generated View component field. |

## Messaging with `IMvvmSubscriber<T>`

In addition to automatic UI rebinding, a ViewModel can send arbitrary messages to Views that have opted in. This allows the ViewModel to push events (e.g., showing a dialog, playing an animation) without coupling it to the View type.

**How it works:**

1. Define a message type (typically a `readonly struct`).
2. Call `_publisher.Publish(new MyMessage(...))` from the ViewModel.
3. Implement `IMvvmSubscriber<MyMessage>` on the partial View class to handle the message.

```csharp
// 1. Define the message
public readonly struct ShowDialogMessage
{
    public readonly string Text;
    public ShowDialogMessage(string text) => Text = text;
}

// 2. Publish from the ViewModel
[ViewModel]
public partial class MyViewModel
{
    [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
    public void OnConfirm()
    {
        _publisher.Publish(new ShowDialogMessage("Are you sure?"));
    }
}

// 3. Receive in the View (partial class alongside generated code)
public sealed partial class MyView : IMvvmSubscriber<ShowDialogMessage>
{
    void IMvvmSubscriber<ShowDialogMessage>.OnReceivedMessage(ShowDialogMessage message)
    {
        // show dialog with message.Text
    }
}
```

`Binder` delivers the message to every View in its list that implements `IMvvmSubscriber<T>` for the given `T`.

## Analyzer Diagnostics

| ID | Level | Condition |
|----|-------|-----------|
| `BND001` | Error | `[ViewModel]` class name does not contain `"ViewModel"`. |
| `BND002` | Error | `[Schema]` `id` is less than `-1`. |
| `BND003` | Warning | Multiple `[Schema]` entries assign conflicting tooltip values to the same View field. |

## License
This library is released under the MIT license.
