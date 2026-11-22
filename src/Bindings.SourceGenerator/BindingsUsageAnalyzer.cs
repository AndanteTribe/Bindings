using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Bindings;

/// <summary>
/// Validates Bindings usage independently of source generation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BindingsUsageAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.Bnd006);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static context =>
        {
            var view = context.Compilation.GetTypeByMetadataName(TypeNames.ViewAbstraction);
            var monoBehaviour = context.Compilation.GetTypeByMetadataName(TypeNames.MonoBehaviour);

            context.RegisterSymbolAction(context =>
            {
                var type = (INamedTypeSymbol)context.Symbol;
                if (type.TypeKind == TypeKind.Class && type.InheritsFrom(monoBehaviour) && type.ImplementsInterface(view))
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.Bnd006, type.Locations[0], type.ToDisplayString()));
                }
            }, SymbolKind.NamedType);

        });
    }
}