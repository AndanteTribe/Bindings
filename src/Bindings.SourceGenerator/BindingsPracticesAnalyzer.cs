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

        context.RegisterCompilationStartAction(static context =>
        {
            var viewInterface = context.Compilation.GetTypeByMetadataName(TypeNames.ViewAbstraction);
            var viewModelInterface = context.Compilation.GetTypeByMetadataName(TypeNames.ViewModelAbstraction);
            var viewModelAttribute = context.Compilation.GetTypeByMetadataName(TypeNames.ViewModelAttribute);
            var publisherInterface = context.Compilation.GetTypeByMetadataName(TypeNames.PublisherAbstraction);

            context.RegisterSymbolStartAction(context =>
            {
                var owner = (INamedTypeSymbol)context.Symbol;
                if (owner.HasAttribute(viewModelAttribute))
                {
                    // Collect helper bodies and call sites independently; callback order is unspecified.
                    var helpers = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                    var calls = new List<(IInvocationOperation Invocation, IMethodSymbol Helper)>();
                    context.RegisterOperationAction(
                        context => CollectRebindCalls(context, owner, viewModelAttribute, publisherInterface, helpers, calls),
                        OperationKind.Invocation);
                    context.RegisterSymbolEndAction(context => ReportRebindCalls(context, owner, helpers, calls));
                }

            }, SymbolKind.NamedType);

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
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.Bndp002,
                        context.Operation.Syntax.GetLocation(), viewType.ToDisplayString(), constructedType.ToDisplayString()));
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
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.Bndp001,
                    invocation.Syntax.GetLocation(), owner.ToDisplayString()));
            }
        }
    }
}