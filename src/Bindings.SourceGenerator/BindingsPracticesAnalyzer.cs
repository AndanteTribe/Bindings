using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Bindings;

/// <summary>
/// Reports Bindings best-practice diagnostics independently of source generation.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BindingsPracticesAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.Bndp001, DiagnosticDescriptors.Bndp002);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(static context =>
        {
            var invocation = (IInvocationOperation)context.Operation;
            if (invocation.GetEquivalentRebindHelper(context.ContainingSymbol, context.Compilation, context.CancellationToken) != null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.Bndp001, invocation.Syntax.GetLocation(), context.ContainingSymbol.ContainingType.ToDisplayString()));
            }
        }, OperationKind.Invocation);

        context.RegisterCompilationStartAction(static context =>
        {
            var viewInterface = context.Compilation.GetTypeByMetadataName(TypeNames.ViewAbstraction);
            var viewModelInterface = context.Compilation.GetTypeByMetadataName(TypeNames.ViewModelAbstraction);

            context.RegisterOperationAction(context =>
            {
                var constructedType = context.Operation.Type;
                if (constructedType == null || !constructedType.ImplementsInterface(viewModelInterface))
                {
                    return;
                }

                var viewType = context.ContainingSymbol.GetContainingTypeImplementingInterface(viewInterface);
                if (viewType != null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.Bndp002,
                        context.Operation.Syntax.GetLocation(), viewType.ToDisplayString(), constructedType.ToDisplayString()));
                }
            }, OperationKind.ObjectCreation, OperationKind.TypeParameterObjectCreation);
        });
    }
}