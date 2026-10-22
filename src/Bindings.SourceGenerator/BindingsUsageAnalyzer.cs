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
        ImmutableArray.Create(DiagnosticDescriptors.Bnd005, DiagnosticDescriptors.Bnd006);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(static context =>
        {
            var viewModelAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.ViewModelAttribute);
            var serializeField = context.Compilation.GetTypeByMetadataName(TypeNames.SerializeField);
            var serializeReference = context.Compilation.GetTypeByMetadataName(TypeNames.SerializeReference);
            var view = context.Compilation.GetTypeByMetadataName(TypeNames.IView);
            var monoBehaviour = context.Compilation.GetTypeByMetadataName(TypeNames.MonoBehaviour);

            context.RegisterSymbolAction(context =>
            {
                var field = (IFieldSymbol)context.Symbol;
                if (field.Type is not INamedTypeSymbol fieldType || !fieldType.HasAttribute(viewModelAttribute))
                {
                    return;
                }

                foreach (var attribute in field.GetAttributes())
                {
                    var attributeType = attribute.AttributeClass;
                    if (attributeType == null
                        || (!attribute.IsAttribute(serializeField) && !attribute.IsAttribute(serializeReference)))
                    {
                        continue;
                    }

                    var location = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? field.Locations[0];
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.Bnd005, location, field.Name, fieldType.ToDisplayString(), attributeType.Name));
                }
            }, SymbolKind.Field);

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