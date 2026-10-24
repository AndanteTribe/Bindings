using System.Resources;

namespace Bindings;

/// <summary>
/// Provides the embedded, culture-aware diagnostic strings without retaining compilation data.
/// </summary>
internal static class DiagnosticResources
{
    public static ResourceManager ResourceManager { get; } = new("Bindings.DiagnosticResources", typeof(DiagnosticResources).Assembly);
}