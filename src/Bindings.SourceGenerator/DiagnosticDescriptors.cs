using System;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Bindings;

/// <summary>
/// Central registry of all <see cref="DiagnosticDescriptor"/> instances reported by Bindings.
/// </summary>
internal static class DiagnosticDescriptors
{
    private const string Category = "Bindings";
    private const string PracticesCategory = "Bindings.Practices";

    private static class Documentation
    {
        private const string RepositoryBlobUrl = "https://github.com/AndanteTribe/Bindings/blob/";
        private const string DiagnosticsPath = "/docs/diagnostics/";

        public static readonly string BaseUrl = RepositoryBlobUrl + "main" + DiagnosticsPath;

        static Documentation()
        {
            var informationalVersion = typeof(DiagnosticDescriptors).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (informationalVersion == null)
            {
                return;
            }

            var separator = informationalVersion.LastIndexOf('+');
            if (separator < 0 || informationalVersion.Length - separator - 1 != 40)
            {
                return;
            }

            for (var i = separator + 1; i < informationalVersion.Length; i++)
            {
                if (informationalVersion[i] is not (>= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F'))
                {
                    return;
                }
            }

            var buffer = (Span<char>)stackalloc char[RepositoryBlobUrl.Length + 40 + DiagnosticsPath.Length];
            RepositoryBlobUrl.AsSpan().CopyTo(buffer);
            informationalVersion.AsSpan(separator + 1, 40).CopyTo(buffer.Slice(RepositoryBlobUrl.Length));
            DiagnosticsPath.AsSpan().CopyTo(buffer.Slice(RepositoryBlobUrl.Length + 40));
            BaseUrl = buffer.ToString();
        }
    }

    /// <summary>
    /// BND001: emitted when a [ViewModel] class/struct name does not end with "ViewModel".
    /// Neither ViewModel nor View source is generated for the annotated type.
    /// </summary>
    public static readonly DiagnosticDescriptor Bnd001 = new(
            id: "BND001",
            title: new LocalizableResourceString("Bnd001Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
            messageFormat: new LocalizableResourceString("Bnd001Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
            category: Category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: Documentation.BaseUrl + "BND001.md",
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
            helpLinkUri: Documentation.BaseUrl + "BND002.md",
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
            helpLinkUri: Documentation.BaseUrl + "BND003.md",
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
            helpLinkUri: Documentation.BaseUrl + "BND004.md",
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
            helpLinkUri: Documentation.BaseUrl + "BND005.md",
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
            helpLinkUri: Documentation.BaseUrl + "BND006.md",
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
            helpLinkUri: Documentation.BaseUrl + "BND007.md",
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
            helpLinkUri: Documentation.BaseUrl + "BNDP001.md",
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
            helpLinkUri: Documentation.BaseUrl + "BNDP002.md",
            customTags: [PracticesCategory]);

    /// <summary>
    /// BNDP003: rejects child ViewModels that are only stored or exposed.
    /// </summary>
    public static readonly DiagnosticDescriptor Bndp003 = new(
            id: "BNDP003",
            title: new LocalizableResourceString("Bndp003Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
            messageFormat: new LocalizableResourceString("Bndp003Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
            category: PracticesCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: Documentation.BaseUrl + "BNDP003.md",
            customTags: [PracticesCategory]);
    /// <summary>
    /// BNDP004: rejects methods that only copy parameters to publicly writable Schema properties.
    /// </summary>
    public static readonly DiagnosticDescriptor Bndp004 = new(
            id: "BNDP004",
            title: new LocalizableResourceString("Bndp004Title", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
            messageFormat: new LocalizableResourceString("Bndp004Message", DiagnosticResources.ResourceManager, typeof(DiagnosticResources)),
            category: PracticesCategory,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            helpLinkUri: Documentation.BaseUrl + "BNDP004.md",
            customTags: [PracticesCategory]);
}