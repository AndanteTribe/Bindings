# BNDP003: ViewModels must not merely aggregate other ViewModels

English | [日本語](BNDP003.ja.md)

| Property | Value |
| --- | --- |
| Category | Bindings.Practices |
| Default severity | Error |
| Enabled by default | Yes |
| Code fix | None |

## Message

> ViewModel '{0}' only stores or exposes child ViewModel member '{1}'. If it is not used in the owning ViewModel's logic, move ownership outside this ViewModel.

`{0}` is the owner and `{1}` is the child storage member.

## Cause

A ViewModel stores another ViewModel but does not reference it behaviorally in its own logic. Prefer an external owner when the parent merely groups or forwards child instances.

## Violation

Assuming `ChildViewModel` implements IViewModel:

```csharp
[ViewModel]
public partial class ParentViewModel
{
    [Required]
    private readonly ChildViewModel _child;

    public ChildViewModel Child => _child;
}
```

Constructor injection and the transparent getter do not count as behavioral use.

## Fix

Remove storage that the parent does not need, and let an external composition point own and distribute the instances. For example, independently constructed parent and child ViewModels can be registered with the Binder individually:

```csharp
binder.Initialize(parent);
binder.Initialize(child);
```

If the parent genuinely needs the child in its logic, keeping ownership is valid. Calling the child, reading or changing its state, or passing it to another operation in that logic counts as use.

## Storage and aliases

- Owners and children are identified by the exact `Bindings.IViewModel` interface, inherited implementations, or constrained type parameters. A member declared as IViewModel itself also qualifies.
- Each explicitly declared instance field and auto-property is separate storage. One diagnostic is reported at each unused storage member.
- Expression-bodied or single-return getters that transparently expose storage form aliases, including alias chains and generated Schema properties. Aliases do not receive duplicate diagnostics.
- Static members, indexers, collections, inherited storage, and computed properties that do not transparently expose storage are outside the storage scope.

## What counts as use

References must access the owning instance within that owner's declared logic, including its partial declarations, lambdas, and local functions. No value transformation is required.

The following do **not** count:

- Simple assignment targets, including constructor injection and setters.
- Transparent getter returns and `nameof`.
- Access through another owner instance, external consumers, or nested helper types.

Generated code is analyzed and reported like handwritten code. Generated Required constructor assignments remain non-behavioral, while behavioral references in generated code count. This is reference analysis, not reachability or whole-program data-flow analysis.

## Generation and configuration

Source generation continues. The default Error severity fails compilation unless fixed, suppressed, or reconfigured. There is no automatic Code Fix because moving ownership requires an architectural decision.

[Diagnostic index](../../README.md#best-practice-diagnostics) · [Configuration](../../README.md#configuring-severity)
