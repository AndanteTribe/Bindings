using Microsoft.CodeAnalysis;

namespace Bindings;

/// <summary>
/// Central registry of all <see cref="DiagnosticDescriptor"/> instances reported by Bindings.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "Bindings";
    private const string PracticesCategory = "Bindings.Practices";
    private const string HelpLink = "https://github.com/AndanteTribe/Bindings/blob/main/README.md#analyzer-diagnostics";
    private const string PracticesHelpLink = "https://github.com/AndanteTribe/Bindings/blob/main/README.md#best-practice-diagnostics";

    /// <summary>
    /// BND001: emitted when a [ViewModel] class/struct name does not contain "ViewModel".
    /// Neither ViewModel nor View source is generated for the annotated type.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd001 = new(
        id: "BND001",
        title: new LocalizableResourceString("Bnd001Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        messageFormat: new LocalizableResourceString("Bnd001Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink,
        customTags: [Category]);

    /// <summary>
    /// BND002: emitted when a [Schema] id value is less than -1. Only -1 (unset) or 0+ are valid.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd002 = new(
        id: "BND002",
        title: new LocalizableResourceString("Bnd002Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        messageFormat: new LocalizableResourceString("Bnd002Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink,
        customTags: [Category]);

    /// <summary>
    /// BND003: emitted when multiple [Schema] entries that share the same View component field
    /// specify different non-empty tooltip strings. Only the first tooltip encountered is used.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd003 = new(
        id: "BND003",
        title: new LocalizableResourceString("Bnd003Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        messageFormat: new LocalizableResourceString("Bnd003Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink,
        customTags: [Category]);

    /// <summary>
    /// BND004: emitted when a ViewModel or one of its containing types has an accessibility
    /// that cannot be represented in generated source. No source is generated for that ViewModel.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd004 = new(
        id: "BND004",
        title: new LocalizableResourceString("Bnd004Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        messageFormat: new LocalizableResourceString("Bnd004Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink,
        customTags: [Category]);

    /// <summary>
    /// BND005: emitted when a field whose type is annotated with [ViewModel] is marked with
    /// Unity's [SerializeField] or [SerializeReference].
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd005 = new(
        id: "BND005",
        title: new LocalizableResourceString("Bnd005Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        messageFormat: new LocalizableResourceString("Bnd005Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink,
        customTags: [Category]);

    /// <summary>
    /// BND006: emitted when an IView implementation derives from MonoBehaviour.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd006 = new(
        id: "BND006",
        title: new LocalizableResourceString("Bnd006Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        messageFormat: new LocalizableResourceString("Bnd006Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink,
        customTags: [Category]);

    /// <summary>
    /// BND007: emitted when user code directly accesses a [Schema] field in a ViewModel.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd007 = new(
        id: "BND007",
        title: new LocalizableResourceString("Bnd007Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        messageFormat: new LocalizableResourceString("Bnd007Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: HelpLink,
        customTags: [Category]);

    /// <summary>
    /// BNDP001: requires the generated helper for equivalent self-rebind requests by default.
    /// </summary>
    public static readonly DiagnosticDescriptor Bndp001 = new(
        id: "BNDP001",
        title: new LocalizableResourceString("Bndp001Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        messageFormat: new LocalizableResourceString("Bndp001Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        category: PracticesCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: PracticesHelpLink,
        customTags: [PracticesCategory]);

    /// <summary>
    /// BNDP002: requires Views to receive ViewModels instead of constructing them.
    /// </summary>
    public static readonly DiagnosticDescriptor Bndp002 = new(
        id: "BNDP002",
        title: new LocalizableResourceString("Bndp002Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        messageFormat: new LocalizableResourceString("Bndp002Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
        category: PracticesCategory,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        helpLinkUri: PracticesHelpLink,
        customTags: [PracticesCategory]);

}