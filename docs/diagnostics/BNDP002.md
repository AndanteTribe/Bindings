# BNDP002: Views must not construct ViewModels

English | [日本語](BNDP002.ja.md)

| Property | Value |
| --- | --- |
| Category | Bindings.Practices |
| Default severity | Error |
| Enabled by default | Yes |
| Code fix | None |

## Message

> View '{0}' constructs ViewModel '{1}'. Construct the ViewModel outside the View and pass it in instead.

`{0}` is the owning View type and `{1}` is the constructed ViewModel type.

## Cause

Code owned by a View constructs a ViewModel. Keep construction and dependency ownership outside the View and pass the instance in through initialization.

## Violation

For a generated `CounterView` and a ViewModel requiring only a publisher:

```csharp
public sealed partial class CounterView
{
    private CounterViewModel Create(IMvvmPublisher publisher)
        => new CounterViewModel(publisher);
}
```

## Fix

Move construction to an external composition point, such as a bootstrap component or a VContainer registration:

```csharp
// In the external bootstrap, not in the View:
var viewModel = new CounterViewModel(binder);
binder.Initialize(viewModel);
binder.Run();
```

There is no automatic Code Fix because selecting the owner and injection strategy requires an architectural decision.

## Scope and exceptions

- A View implements the exact `Bindings.IView` interface, including `IView<T>` and inherited implementations. Lexically nested helper types are also View-owned; the nearest enclosing View is named in the diagnostic.
- A ViewModel implements the exact `Bindings.IViewModel` interface, including inherited implementations. `[ViewModel]` alone is not the criterion: successful generation adds the interface before analyzer execution.
- Explicit and target-typed `new` expressions are included. `new T()` is included when interface/base constraints, including transitive type-parameter constraints, establish IViewModel membership.
- Initializers, constructors and constructor initializers, static methods, accessors, lambdas, and local functions are included. Generated code follows the same rules.
- Ordinary object creation, array allocation, collection allocation without constructing ViewModel elements, `default`, and construction outside Views are excluded.
- Factory calls and reflection are outside the detection scope.
- One diagnostic is reported at each complete construction expression.

## Generation and configuration

Source generation continues. This author-recommended practice is an Error by default and fails compilation unless fixed, suppressed, or reconfigured.

[Diagnostic index](../../README.md#best-practice-diagnostics) · [Configuration](../../README.md#configuring-severity)
