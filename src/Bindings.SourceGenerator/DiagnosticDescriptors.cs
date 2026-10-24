using Microsoft.CodeAnalysis;

namespace Bindings;

/// <summary>
/// Central registry of all <see cref="DiagnosticDescriptor"/> instances reported by Bindings.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "Bindings";
    private const string PracticesCategory = "Bindings.Practices";

    /// <summary>
    /// BND001: emitted when a [ViewModel] class/struct name does not contain "ViewModel".
    /// Neither ViewModel nor View source is generated for the annotated type.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd001 = new(
        id: "BND001",
        title: "ViewModel type name must contain \"ViewModel\"",
        messageFormat: "Type '{0}' is annotated with [ViewModel] but its name does not contain \"ViewModel\". No source will be generated for this ViewModel.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// BND002: emitted when a [Schema] id value is less than -1. Only -1 (unset) or 0+ are valid.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd002 = new(
        id: "BND002",
        title: "Invalid [Schema] id value",
        messageFormat: "[Schema] id value {0} is invalid. Use id >= 0 for explicit grouping, or omit id (defaults to -1) for auto-numbering.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// BND003: emitted when multiple [Schema] entries that share the same View component field
    /// specify different non-empty tooltip strings. Only the first tooltip encountered is used.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd003 = new(
        id: "BND003",
        title: "Conflicting tooltip values for the same View field",
        messageFormat: "View field '{0}' has conflicting tooltip values from multiple [Schema] entries with the same id. Only the first tooltip will be used.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// BND004: emitted when a ViewModel or one of its containing types has an accessibility
    /// that cannot be represented in generated source. No source is generated for that ViewModel.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd004 = new(
        id: "BND004",
        title: "Unsupported type accessibility",
        messageFormat: "Type '{0}' has unsupported accessibility '{1}'. No source will be generated for ViewModel '{2}'.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// BND005: emitted when a field whose type is annotated with [ViewModel] is marked with
    /// Unity's [SerializeField] or [SerializeReference].
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd005 = new(
        id: "BND005",
        title: "Unity deserialization may leave ViewModel state uninitialized",
        messageFormat: "Field '{0}' uses [{2}] with ViewModel type '{1}'. Unity deserialization may leave generated runtime state uninitialized, even when the type is marked [Serializable]. Construct or assign the ViewModel at runtime instead.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// BND006: emitted when an IView implementation derives from MonoBehaviour.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd006 = new(
        id: "BND006",
        title: "View implementations must not derive from MonoBehaviour",
        messageFormat: "Type '{0}' implements Bindings.IView and derives from UnityEngine.MonoBehaviour. View implementations must not derive from MonoBehaviour.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// BND007: emitted when user code directly accesses a [Schema] field in a ViewModel.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd007 = new(
        id: "BND007",
        title: "Schema fields must be accessed through generated properties",
        messageFormat: "Field '{0}' is annotated with [Schema]. Access the generated property '{1}' instead of the field.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// BNDP001: requires the generated helper for equivalent self-rebind requests by default.
    /// </summary>
    public static readonly DiagnosticDescriptor Bndp001 = new(
        id: "BNDP001",
        title: "Use the generated self-rebind helper",
        messageFormat: "Use PublishRebindMessage() instead of directly publishing a rebind message for '{0}'",
        category: PracticesCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// BNDP002: requires Views to receive ViewModels instead of constructing them.
    /// </summary>
    public static readonly DiagnosticDescriptor Bndp002 = new(
        id: "BNDP002",
        title: "Views must not construct ViewModels",
        messageFormat: "View '{0}' constructs ViewModel '{1}'. Construct the ViewModel outside the View and pass it in instead.",
        category: PracticesCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}