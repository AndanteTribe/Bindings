# BNDP004: Do not wrap generated Schema property assignments

English | [日本語](BNDP004.ja.md)

| Property | Value |
| --- | --- |
| Category | Bindings.Practices |
| Default severity | Error |
| Enabled by default | Yes |
| Code fix | None |

## Message

> Method '{0}' only assigns its parameters directly to publicly writable Schema properties. Assign the properties directly instead.

`{0}` is the wrapper method name.

## Cause

A ViewModel method only copies its parameters unchanged to distinct, publicly writable Schema properties on its own instance. Those properties already provide the public update API, including rebind notifications.

## Violation

```csharp
[ViewModel]
public partial class CounterViewModel
{
    [Schema(PathResolver.TMPro.TMP_Text.text)]
    private int _count;

    public void SetCount(int count) => Count = count;
}
```

## Fix

Remove the redundant wrapper and update callers to assign the property:

```csharp
viewModel.Count = count;
```

Methods performing validation, transformation, or other actual behavior are outside this rule's simple-copy condition. Keep meaningful operations where appropriate.

## Method conditions

- The containing type implements the exact `Bindings.IViewModel`, including inherited implementations.
- The method is ordinary, non-static, non-async, returns void, and has at least one parameter. All accessibilities are checked.
- Overrides, explicit and implicit interface implementations (including generic interfaces), and methods with the actual `[Schema]` attribute are excluded. Attributes and interface implementations on either part of a partial method are respected.
- Delegate or method-group use is not an exemption.

## Assignment conditions

- Every parameter is copied exactly once to a different property, with no unused parameters. `ref`, `in`, and `out` parameters are excluded.
- The property belongs to the current instance (`this`, unqualified, or `base`), is public, and has a public non-init setter. Child and other instances are excluded.
- The property corresponds by generated name and type to a Schema field declared in its containing type. Ordinary properties, direct field writes, static properties, indexers, getter-only properties, and restricted setters do not qualify.
- Parameter and property types must match, including nullable reference annotations. Parentheses are allowed. Any explicit or implicit conversion, including an identity cast, prevents reporting.
- The entire body consists of simple assignment expression statements, one per parameter. Expression-bodied methods are supported. Explicit returns, locals, calls, control flow, compound assignments, or repeated parameters/properties prevent reporting.

## Reporting and generation

One diagnostic is reported at the method identifier. Generated code is analyzed and reported like handwritten code. Source generation continues, but the default Error severity fails compilation unless fixed, suppressed, or reconfigured.

There is no automatic Code Fix because removing an API and updating its callers requires an explicit decision.

[Diagnostic index](../../README.md#best-practice-diagnostics) · [Configuration](../../README.md#configuring-severity)
