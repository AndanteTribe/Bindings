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
- Unity 6000.7 or later
- Unity uGUI 2.7.0 or later

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
            Count += 1;
        }

        [Schema(PathResolver.UnityEngine.UI.Button.onClick)]
        public void Decrement()
        {
            Count -= 1;
        }

        partial void OnPostBind()
        {
            _model.Count = Count;
        }
    }
}
```

The source generator automatically produces:
- `MyApp.CounterViewModel.g.cs` — a partial ViewModel class with properties, constructor, and publisher wiring.
- `MyApp.CounterView.g.cs` — a sealed partial View class with serialized UI component fields and binding logic.

### Generated Code

**`MyApp.CounterViewModel.g.cs`**

```csharp
#nullable enable

namespace MyApp
{
#if UNITY_EDITOR
    [global::System.Serializable]
#endif
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
            OnPostConstruct();
        }

        public void NotifyCompletedBind() => OnPostBind();

        partial void OnPostBind();

        partial void OnPostConstruct();

        [global::System.Runtime.CompilerServices.MethodImpl(
            global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private void PublishRebindMessage()
        {
            _publisher.PublishRebindMessage<CounterViewModel>();
        }
    }
}
```

**`MyApp.CounterView.g.cs`**

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
        private global::TMPro.TMP_Text _countText = null!;

        [global::UnityEngine.SerializeField]
        private global::UnityEngine.UI.Button _incrementButton = null!;

        [global::UnityEngine.SerializeField]
        private global::UnityEngine.UI.Button _decrementButton = null!;

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
            global::Bindings.TextMeshProExtensions.SetValue(_countText, _viewModel.Count);
            _incrementButton.onClick.RemoveAllListeners();
            _incrementButton.onClick.AddListener(_viewModel.Increment);
            _decrementButton.onClick.RemoveAllListeners();
            _decrementButton.onClick.AddListener(_viewModel.Decrement);
            OnPostBind();
            _viewModel.NotifyCompletedBind();
        }

        partial void OnPostBind();
    }

#if UNITY_EDITOR || !DISABLE_DEBUGTOOLKIT
    public sealed partial class CounterView : global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>
    {
        void global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>.OnReceivedMessage(
            global::Bindings.DebugBindMessage message)
        {
            message.BindTo(this);
            global::Bindings.TextMeshProExtensions.SetValue(_countText, _viewModel.Count);
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

## Attributes

### `[ViewModel]`

Applied to a `partial` class or struct to mark it as a ViewModel and trigger source generation.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `requireBindImplementation` | `bool` | `false` | When `true`, skips auto-generating `BindAsync`. See [Binding and Serialization](#binding-and-serialization). |

### `[Required]`

Applied to a field or property to include it as a parameter in the generated constructor. The constructor accepts each member's type and assigns it. Required members appear in declaration order, followed by `IMvvmPublisher publisher`; without Required members, the constructor accepts only `publisher`.

The generated constructor assigns `[Required]` members, then the internal `IMvvmPublisher`. For ViewModel classes, it then calls `partial void OnPostConstruct();`. Implement this hook in your partial class for one-time initialization that needs the publisher, including Schema property assignments. Structs do not generate this hook.

### `[Schema(bindingPath, id, format, tooltip)]`

Applied to a field or method to bind it to a UI component. Can be applied multiple times to the same member.

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `bindingPath` | `string` | required | Fully qualified component member path, supplied as a string or via `PathResolver` (e.g., `PathResolver.TMPro.TMP_Text.text`). |
| `id` | `int` | `-1` | Entries with the **same component type and non-negative ID** share a View field. Each `-1` entry is independent. Values less than `-1` trigger [BND002](docs/diagnostics/BND002.md). |
| `format` | `string` | `""` | Format string applied when binding to `TMPro.TMP_Text.text` (e.g., `"N0"`). Ignored for other binding paths. |
| `tooltip` | `string` | `""` | Tooltip text shown in the Unity Inspector on the generated View component field. |

#### GameObject active state binding

Specify `UnityEngine.GameObject.activeSelf` (or `PathResolver.UnityEngine.GameObject.activeSelf`) as the `bindingPath` to have the generated View update the active state through `GameObject.SetActive(bool)`.

Example:

```csharp
[Schema(PathResolver.UnityEngine.GameObject.activeSelf)]
private bool _isVisible;
```

The generated binding will call `_isVisibleGameObject.SetActive(_viewModel.IsVisible);` (or the equivalent generated helper) when applying the binding.

#### RectTransform calculated size binding

Use `PathResolver.UnityEngine.RectTransform.rect.size` to bind a `Vector2` to the calculated size of a `RectTransform` while preserving its current anchors:

```csharp
[Schema(PathResolver.UnityEngine.RectTransform.rect.size)]
private Vector2 _size;
```

The generated View calls `RectTransform.SetSizeWithCurrentAnchors` once for the horizontal axis with `Size.x` and once for the vertical axis with `Size.y`. This binding is opt-in; `PathResolver.UnityEngine.RectTransform.sizeDelta` continues to assign `RectTransform.sizeDelta` directly.

## Generated-Code Conventions

### Types and Names

- A ViewModel is a partial class or struct. Its name must end with `ViewModel`, optionally followed by ASCII digits (`0`–`9`). Only that final `ViewModel` is replaced with `View`, preserving the digits: `CounterViewModel2` becomes `CounterView2`. See [BND001](docs/diagnostics/BND001.md).
- The generated View is a sealed partial class implementing `IView<T>`, with the same accessibility and containing-type chain as its ViewModel. All containing types must also be partial. See [BND004](docs/diagnostics/BND004.md) for supported accessibilities.
- Generated file names include the namespace and containing types: `MyApp.Screen.CounterViewModel.g.cs` and `MyApp.Screen.CounterView.g.cs`, for example. Generated type references use fully qualified names to avoid user-name collisions.
- For field-derived identifiers, remove all leading `_` characters, then one leading `m_`. Uppercase the first remaining character for a Schema property, or lowercase it for a Required constructor parameter. The result must be a valid, non-empty identifier.

| Member name | Schema property | Required parameter |
| --- | --- | --- |
| `_count`, `__count`, `m_count`, `_m_count` | `Count` | `count` |
| `m_Service` | `Service` | `service` |
| `myService` | `MyService` | `myService` |

Each Schema field produces one public property, even with multiple Schema attributes. For classes and ordinary structs, its setter assigns the field and requests a rebind, including when the value is unchanged. Use the property rather than directly accessing the field; see [BND007](docs/diagnostics/BND007.md). For readonly structs, only a getter is generated.

### Component Fields and Sharing

Generated component fields combine the normalized member name and component name: `_count` bound to `TMP_Text` becomes `_countText`, and `Increment` bound to `Button` becomes `_incrementButton`. Member names use the lowercase-first normalization above. The component suffix uses the class-name segment after its last underscore (`TMP_Text` → `Text`), falling back to the class name when that segment is empty.

- Entries with the same component type and the same explicit `id >= 0` share one field. Its name uses the first collected member; fields are collected before methods, each in declaration order. The ID itself is not appended to the name.
- Every omitted ID or `id = -1` creates an independent component. Distinct names have no numeric suffix; colliding names receive `1`, `2`, and so on in encounter order, skipping names already used.
- Repeating the same Button schema twice on `Submit` therefore produces `_submitButton1` and `_submitButton2`. Two different methods `Increment` and `Decrement` produce `_incrementButton` and `_decrementButton`.
- `RectTransform.rect.size` and `RectTransform.sizeDelta` can share a RectTransform using the same explicit ID.
- Component fields are serialized and emitted in first-reference order, processing Schema fields before Schema methods. A shared component is declared once.
- A supplied tooltip becomes a Unity `Tooltip` attribute. The first non-empty tooltip wins; different non-empty values on a shared field produce [BND003](docs/diagnostics/BND003.md).

### Binding and Serialization

Binding applies field schemas first, then method event schemas. TMP text uses `TextMeshProExtensions.SetValue`; `activeSelf` and `rect.size` use the dedicated APIs described above. Other field bindings use direct assignment. For each component/event pair, `RemoveAllListeners()` is called once, followed by `AddListener` for each bound method. Finally, the View's `OnPostBind` runs, then the ViewModel's `OnPostBind` through `NotifyCompletedBind`.

With `requireBindImplementation: true`, implement `IView.BindAsync` on the partial View and call its generated `BindAll()` when ready. The default implementation calls `BindAll()` synchronously and returns a completed `ValueTask`.

The debug subscriber is compiled under `UNITY_EDITOR || !DISABLE_DEBUGTOOLKIT`. It applies only field bindings, followed by the same completion hooks; it does not re-register event listeners.

Views always receive `[Serializable]` for the Binder's `[SerializeReference]` storage; their ViewModel reference is non-serialized. ViewModels receive `[Serializable]` only under `UNITY_EDITOR`, unless the user already supplied it. This supports inspection, not Unity persistence of generated runtime state; construct ViewModels at runtime as explained in [BND005](docs/diagnostics/BND005.md).

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

Bindings detects incorrect usage at compile time and reports what needs to be fixed.

Each ID links to its detailed cause, examples, exceptions, Code Fix availability, and source-generation behavior. IDE diagnostic help opens the English page at the build-source commit recorded in the analyzer assembly; each page links to Japanese.

| ID | Level | Condition |
|----|-------|-----------|
| [`BND001`](docs/diagnostics/BND001.md) | Error | A type annotated with `[ViewModel]` has a name that does not end with `ViewModel`, optionally followed by ASCII digits (`0`–`9`). |
| [`BND002`](docs/diagnostics/BND002.md) | Error | `[Schema]` `id` is less than `-1`. |
| [`BND003`](docs/diagnostics/BND003.md) | Error | Multiple `[Schema]` entries assign conflicting tooltip values to the same View field. |
| [`BND004`](docs/diagnostics/BND004.md) | Error | A `[ViewModel]` type or one of its containing types has an accessibility that cannot be represented in generated source. |
| [`BND005`](docs/diagnostics/BND005.md) | Warning | A field of a `[ViewModel]` type is marked with `[SerializeField]` or `[SerializeReference]`. Construct or assign ViewModels at runtime instead of relying on Unity deserialization. |
| [`BND006`](docs/diagnostics/BND006.md) | Error | A class implementing `IView` derives from `MonoBehaviour`. Implement Views as plain C# classes instead. |
| [`BND007`](docs/diagnostics/BND007.md) | Error | Code directly reads or writes a `[Schema]` field in a `[ViewModel]` type. Use its generated property instead. |

### Best-practice diagnostics

`BNDP` rules describe design and coding practices recommended by the author of Bindings. They are errors by default; adjust their severity to suit your project's policy.

| ID | Level | Recommendation |
|----|-------|----------------|
| [`BNDP001`](docs/diagnostics/BNDP001.md) | Error | For self-rebind requests, use the generated `PublishRebindMessage()` instead of an equivalent `_publisher.PublishRebindMessage<T>()` call. |
| [`BNDP002`](docs/diagnostics/BNDP002.md) | Error | Construct ViewModels outside the View and pass them in, rather than constructing them inside the View. |
| [`BNDP003`](docs/diagnostics/BNDP003.md) | Error | If a child ViewModel is only stored or exposed without being used in the parent's logic, move ownership outside the parent. |
| [`BNDP004`](docs/diagnostics/BNDP004.md) | Error | Assign public Schema properties directly instead of writing methods that only copy arguments unchanged to those properties on the current instance. |

### Configuring Severity

Configure diagnostic severity with `.editorconfig` (recommended) or a code analysis rule set file (`.ruleset`). Rule set files must be explicitly supplied to the compiler. For example, to make `BNDP001` an IDE suggestion with `.editorconfig`:

```ini
[*.cs]
dotnet_diagnostic.BNDP001.severity = suggestion
```

Use `none` to disable a diagnostic. To disable all best-practice diagnostics, set `dotnet_analyzer_diagnostic.category-Bindings.Practices.severity = none`.

Diagnostic messages use Japanese for the `ja` / `ja-JP` UI cultures and fall back to English for other cultures. Source-generation errors BND001 and BND004 stop only the affected ViewModel's generation; usage and best-practice diagnostics do not suppress source generation. Detailed behavior is documented on each rule's page.

## Single-Axis Virtual Scrolling

Use `SingleAxisVirtualScrollViewModel` together with `SingleAxisVirtualScrollView` to display a vertical or horizontal list of fixed-size items. Only enough item Binders for the visible area are created and reused, making this suitable for large lists.

- **`SingleAxisVirtualScrollViewModel`** receives the items in display order and lets you request a scroll position from code.
- **`SingleAxisVirtualScrollView`** uses a `ScrollRect` and an item prefab to reuse item Binders as the visible range changes.

Both are built-in types; you do not need to define a `[ViewModel]` class for the scroll itself. Each item can use ordinary ViewModels and Views.

VContainer registration is also supported. See [VContainer Support: Registering a Virtual Scroll](#registering-a-virtual-scroll) for details.

### 1. Configure the Item Prefab and Scroll Area

1. Create an item Binder prefab and register Views matching the item ViewModels in its **Views** list. Give the prefab's `RectTransform` a positive, fixed size along the scroll axis.
2. Create a scroll area under the scene's Canvas using Unity's standard **UI > Scroll View** menu. The root **Scroll View** has a `ScrollRect` component and contains **Viewport > Content**.
3. Select **Scroll View**. On its `ScrollRect` component, assign the child **Viewport**'s `RectTransform` to **Viewport**, and the nested **Content**'s `RectTransform` to **Content**. The standard menu normally assigns these references automatically. Enable exactly one direction on this `ScrollRect`: **Vertical** or **Horizontal**.
4. Add `SingleAxisVirtualScrollView` to the **Views** list of the outer Binder that manages the list.
5. In the Binder Inspector, assign the item Binder prefab to that `SingleAxisVirtualScrollView` entry's **Item Prefab**, and the scene's **Scroll View** `ScrollRect` component to its **Scroll Rect** field. Do not add a `ScrollRect` to the item prefab.

#### Settings

| Field | Description |
| --- | --- |
| **Item Prefab** | Item Binder prefab with Views registered for the item ViewModels. |
| **Spacing** | Gap between items along the scroll axis. |
| **Lane Count** | Number of columns for vertical scrolling or rows for horizontal scrolling. The default of 1 displays a single list. |
| **Lane Spacing** | Gap between columns or rows perpendicular to the scroll axis. |
| **Scroll Rect** | The scene's Scroll View `ScrollRect`, with Viewport and Content assigned and exactly one scroll direction enabled. |

#### Layout Notes

- The View sizes Content along the scroll axis and positions items automatically. Avoid `LayoutGroup` or `ContentSizeFitter` components that overwrite those values.
- For a grid, give the item prefab a positive fixed size on both axes and ensure Content is wide/tall enough for all lanes. The grid is centered perpendicular to the scroll axis.
- Items fill left-to-right before advancing down for vertical scrolling, or top-to-bottom before advancing right for horizontal scrolling.
- Items retain the same lane positions even when the final row/column is incomplete.
- Layout sizes and the lane count are assumed to remain fixed after binding. Bind again if you change them.

### 2. Create Items and Connect the ViewModel

Create items in display order and pass them to `SingleAxisVirtualScrollViewModel`. Each item needs its own publisher: pass it to the item ViewModel's constructor and call `Register` on the returned container. Do not pass the outer Binder directly to item ViewModels.

```csharp
// models is a CounterModel[] in display order; binder is the outer Binder.
var publishers = new ScrollItemMvvmPublisher[models.Length];
for (var i = 0; i < models.Length; i++)
{
    var publisher = ScrollItemMvvmPublisher.Create(out var container);
    container.Register(new CounterViewModel(models[i], publisher));
    publishers[i] = publisher;
}

var scrollViewModel = new SingleAxisVirtualScrollViewModel(publishers, binder);
binder.Initialize(scrollViewModel);
binder.Run();
```

Call `Register` on that item's `container` multiple times when an item has multiple ViewModels. You do not need to register item ViewModels with the outer Binder; they are assigned to item Binders automatically when displayed.

### 3. Scroll from Code

Assign `ScrollRatio` to request a position. Zero is the top/left edge; one is the bottom/right edge.

```csharp
scrollViewModel.ScrollRatio = 0.5f; // Scroll to the middle.
```

Values are clamped to 0–1; `NaN` is not accepted. Reading this property returns the current position reported by the View, including user scrolling. Before the View is bound, it holds the last requested position.

### Items Outside the Visible Area

When an item leaves the visible area, its Binder can be reused for another item, but its ViewModels are retained. Messages are not delivered while the item is hidden. Keep state that should be restored on redisplay in the item ViewModels so binding can restore it.

## VContainer Support

When using [VContainer](https://github.com/hadashiA/VContainer), use `RegisterBinder` to register a Binder and its ViewModels together. `Initialize` is called automatically; call `Binder.Run()` yourself or enable **Run On Start**.

`WithViewModel<T>()` also supports [generated-code optimization](#generated-code-optimization-with-withviewmodel).

### Registering a Binder and ViewModels

```csharp
public class GameLifetimeScope : LifetimeScope
{
    [SerializeField] private Binder _binder;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(new CounterModel());
        using (var registration = builder.RegisterBinder(_binder))
        {
            registration.WithViewModel<CounterViewModel>().AsSelf();
            // Add more ViewModels to this same registration scope as needed.
        }
    }
}
```

Call `WithViewModel<T>()` multiple times on the same scope. The Binder receives only that scope's ViewModels, in addition order, and is supplied to each ViewModel as `IMvvmPublisher`.

- Each call returns VContainer's `RegistrationBuilder`, supporting `AsSelf`, `As<IViewModel>`, `Keyed`, and `WithParameter`. Service interfaces are not added automatically.
- The default ViewModel lifetime is `Lifetime.Scoped`. Change it with `WithViewModel<T>(lifetime)`.
- **End the `using` block before building the container.** Registrations commit when the block ends, including exception unwinding; this is not a rollback transaction. Do not copy, box, default-construct, or dispose the registration scope twice.
- For multiple Binders in one container, `RegisterBinder(binder, keyed: true)` uses the Binder instance as its key (requires VContainer 1.19.0 or later).

### Registering a Virtual Scroll

Use `WithScrollViewModel<TItem>(getItems, configureItem)` to register `SingleAxisVirtualScrollViewModel` and its item ViewModels together. Inspector setup is the same as for [Single-Axis Virtual Scrolling](#single-axis-virtual-scrolling).

```csharp
// In LifetimeScope.Configure; _binder is the outer scroll Binder.
builder.RegisterInstance<CounterModel[]>(models);
using (var registration = builder.RegisterBinder(_binder))
{
    registration.WithScrollViewModel<CounterModel>(
        resolver => resolver.Resolve<CounterModel[]>(),
        (itemRegistration, model) =>
        {
            itemRegistration.WithViewModel<CounterViewModel>()
                .WithParameter(model);
        })
        .AsSelf();
}
```

- **`getItems`** is called with the container when the scroll ViewModel is constructed and returns an `IReadOnlyList<TItem>` in display order.
- **`configureItem`** registers each item's ViewModels. Use `itemRegistration.WithViewModel<T>()` and options such as `WithParameter` to supply item data. You can register multiple ViewModels per item.

A child container and dedicated publisher are created automatically for each item, so you do not need to call `ScrollItemMvvmPublisher.Create` or construct item ViewModels manually. The library commits and disposes the supplied `itemRegistration`; do not add your own `using` or `Dispose` inside the callback.

Item child containers are retained even when their items leave the visible area and are disposed with the owning container. Virtualization applies to the display Binders, not item ViewModel construction. With `AsSelf()`, you can resolve `SingleAxisVirtualScrollViewModel` from the container to control `ScrollRatio`.

### Generated-Code Optimization with WithViewModel

The Bindings Source Generator produces code that calls ViewModel constructors directly. Registrations made with `WithViewModel<T>()` use this generated code to avoid reflective constructor invocation, similarly to `VContainer.SourceGenerator.dll`. VContainer's Source Generator itself is not required.

This optimization applies **only to registrations made with `WithViewModel<T>()`**. It is not automatically applied to VContainer's ordinary `Register<T>()` or other registration paths.

Supported types are top-level, non-generic, concrete classes annotated with `[ViewModel]`, without custom base classes, explicit constructors, or `[Inject]` members. Unsupported types use VContainer's standard construction and injection behavior.

The VContainer APIs must also be available. The generated private injector resolves Required members and the publisher using VContainer's parameters and normalized constructor parameter names, and calls the generated constructor, including `OnPostConstruct`. Registration options such as lifetime, interfaces, keys, and parameters are retained.

If you also use VContainer's Source Generator, it may generate invalid constructor calls because it cannot see the constructor generated by Bindings. In that case, add `[VContainer.InjectIgnore]` to your ViewModel declaration.

## License
This library is released under the MIT license.
