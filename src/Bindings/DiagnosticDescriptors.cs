using Microsoft.CodeAnalysis;

namespace Bindings;

/// <summary>
/// Central registry of all <see cref="DiagnosticDescriptor"/> instances reported by the ViewModelGenerator.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "Bindings";

    /// <summary>
    /// BND001: emitted when a [ViewModel] class/struct name does not contain "ViewModel".
    /// View class name cannot be derived, so no View is generated.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd001 = new(
        id: "BND001",
        title: "ViewModel type name must contain \"ViewModel\"",
        messageFormat: "Type '{0}' is annotated with [ViewModel] but its name does not contain \"ViewModel\". No View will be generated.",
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
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
