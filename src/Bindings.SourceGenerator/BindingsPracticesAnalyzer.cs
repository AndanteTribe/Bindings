using System.Collections.Generic;
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
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static compilationContext =>
        {
            var viewInterface = compilationContext.Compilation.GetTypeByMetadataName(TypeNames.ViewAbstraction);
            var viewModelInterface = compilationContext.Compilation.GetTypeByMetadataName(TypeNames.ViewModelAbstraction);
            var viewModelAttribute = compilationContext.Compilation.GetTypeByMetadataName(TypeNames.ViewModelAttribute);
            var publisherInterface = compilationContext.Compilation.GetTypeByMetadataName(TypeNames.PublisherAbstraction);

            compilationContext.RegisterSymbolStartAction(symbolContext =>
            {
                var owner = (INamedTypeSymbol)symbolContext.Symbol;
                if (owner.HasAttribute(viewModelAttribute))
                {
                    // Collect helper bodies and call sites independently; callback order is unspecified.
                    var helpers = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                    var calls = new List<(IInvocationOperation Invocation, IMethodSymbol Helper)>();
                    symbolContext.RegisterOperationAction(
                        operationContext => CollectRebindCalls(
                            operationContext,
                            owner,
                            viewModelAttribute,
                            publisherInterface,
                            helpers,
                            calls),
                        OperationKind.Invocation);
                    symbolContext.RegisterSymbolEndAction(
                        symbolEndContext => ReportRebindCalls(symbolEndContext, owner, helpers, calls));
                }

            }, SymbolKind.NamedType);

            compilationContext.RegisterOperationAction(operationContext =>
            {
                var constructedType = operationContext.Operation.Type;
                if (constructedType == null || !constructedType.ImplementsInterface(viewModelInterface))
                {
                    return;
                }

                var viewType = operationContext.ContainingSymbol.GetContainingTypeImplementingInterface(viewInterface);
                if (viewType != null)
                {
                    operationContext.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.Bndp002,
                            operationContext.Operation.Syntax.GetLocation(),
                            viewType.ToDisplayString(),
                            constructedType.ToDisplayString()));
                }
            }, OperationKind.ObjectCreation, OperationKind.TypeParameterObjectCreation);
        });
    }

    private static void CollectRebindCalls(
        OperationAnalysisContext context,
        INamedTypeSymbol owner,
        INamedTypeSymbol? viewModelAttribute,
        INamedTypeSymbol? publisherInterface,
        HashSet<ISymbol> helpers,
        List<(IInvocationOperation Invocation, IMethodSymbol Helper)> calls)
    {
        if (!SymbolEqualityComparer.Default.Equals(context.ContainingSymbol.ContainingType, owner))
        {
            return;
        }

        var invocation = (IInvocationOperation)context.Operation;
        if (context.ContainingSymbol is IMethodSymbol method
            && invocation.IsEquivalentRebindHelperBody(method, publisherInterface, context.CancellationToken))
        {
            lock (helpers)
            {
                helpers.Add(method);
            }
        }

        if (invocation.GetSelfRebindHelper(context.ContainingSymbol, viewModelAttribute, publisherInterface) is { } helper)
        {
            lock (calls)
            {
                calls.Add((invocation, helper));
            }
        }
    }

    private static void ReportRebindCalls(
        SymbolAnalysisContext context,
        INamedTypeSymbol owner,
        HashSet<ISymbol> helpers,
        List<(IInvocationOperation Invocation, IMethodSymbol Helper)> calls)
    {
        foreach (var (invocation, helper) in calls)
        {
            if (helpers.Contains(helper))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.Bndp001,
                        invocation.Syntax.GetLocation(),
                        owner.ToDisplayString()));
            }
        }
    }
}