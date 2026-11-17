# BNDP005: ViewModels must not use UI components

English | [日本語](BNDP005.ja.md)

| Property | Value |
| --- | --- |
| Category | Bindings.Practices |
| Default severity | Error |
| Enabled by default | Yes |
| Code fix | None |

## Message

> ViewModel '{0}' directly uses UI component type '{1}'. Keep UI component access in the View.

## Cause

A ViewModel directly uses `UnityEngine.EventSystems.UIBehaviour` or a derived type.

## Violation

```csharp
[ViewModel]
public partial class CounterViewModel
{
    [Required] private readonly UnityEngine.UI.Text _label;
    public void Reset() => _label.text = "0";
}
```

## Fix

Expose data through Schema members and place custom component operations in the View.

```csharp
[ViewModel]
public partial class CounterViewModel
{
    [Schema(PathResolver.UnityEngine.UI.Text.text)]
    private int _count;
    public void Reset() => Count = 0;
}
```

## Scope and exceptions

- Checks types annotated with `[ViewModel]` or implementing `IViewModel`, including inherited implementations and lexically nested helper types.
- Checks referenced names, call results, and indexer results using their resolved types. This covers member declarations and component access, including inferred types, arrays, generic type arguments, and type-parameter constraints.
- Generated code is checked under the same conditions as handwritten code.
- UI access inside nested `IView` implementations is allowed.
- Runtime types are not inferred through reflection or data flow. An object referenced only as `object` or an unrelated interface is not diagnosed.

## Generation

Source generation continues.

[Diagnostic index](../../README.md#best-practice-diagnostics) · [Configuration](../../README.md#configuring-severity)
