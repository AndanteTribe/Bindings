using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        ImmutableArray.Create(DiagnosticDescriptors.Bndp001, DiagnosticDescriptors.Bndp002, DiagnosticDescriptors.Bndp003);

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

                if (!owner.IsOrImplementsInterface(viewModelInterface))
                {
                    return;
                }

                // 1. Collect the owner's child ViewModel storage.
                var storage = CollectStorageMembers(owner, viewModelInterface, symbolContext.CancellationToken);
                if (storage.Count == 0)
                {
                    return;
                }

                // 2. Collect getter aliases and behavioral usage, regardless of source origin.
                var aliases = new Dictionary<ISymbol, ISymbol>(SymbolEqualityComparer.Default);
                var used = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
                symbolContext.RegisterOperationAction(
                    operationContext => CollectReferencesAndAliases(operationContext, owner, viewModelInterface, aliases, used),
                    OperationKind.FieldReference,
                    OperationKind.PropertyReference);

                // 3. Resolve alias usage and report unused storage after all operations are analyzed.
                symbolContext.RegisterSymbolEndAction(
                    symbolEndContext => ReportUnusedStorage(symbolEndContext, owner, storage, aliases, used));
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

    private static List<ISymbol> CollectStorageMembers(
        INamedTypeSymbol owner,
        INamedTypeSymbol? viewModelInterface,
        CancellationToken cancellationToken)
    {
        var storage = new List<ISymbol>();
        foreach (var member in owner.GetMembers())
        {
            if (member.IsStatic || member.IsImplicitlyDeclared)
            {
                continue;
            }

            if (member is IFieldSymbol field && field.Type.IsOrImplementsInterface(viewModelInterface))
            {
                storage.Add(field);
            }
            else if (member is IPropertySymbol { IsIndexer: false, IsAbstract: false } property
                && property.Type.IsOrImplementsInterface(viewModelInterface))
            {
                foreach (var reference in property.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax declaration)
                    {
                        continue;
                    }

                    var getter = declaration.AccessorList?.Accessors.FirstOrDefault(static accessor => accessor.Keyword.ValueText == "get");
                    if (getter is { Body: null, ExpressionBody: null })
                    {
                        storage.Add(property);
                    }
                }
            }
        }

        return storage;
    }

    private static void CollectReferencesAndAliases(
        OperationAnalysisContext context,
        INamedTypeSymbol owner,
        INamedTypeSymbol? viewModelInterface,
        Dictionary<ISymbol, ISymbol> aliases,
        HashSet<ISymbol> used)
    {
        var member = context.Operation.GetReferencedInstanceMember();
        if (member == null
            || !SymbolEqualityComparer.Default.Equals(member.ContainingType, owner)
            || !SymbolEqualityComparer.Default.Equals(context.ContainingSymbol.ContainingType, owner))
        {
            return;
        }

        IOperation current = context.Operation;
        while (current.Parent is IConversionOperation or IParenthesizedOperation)
        {
            current = current.Parent;
        }

        if (context.ContainingSymbol is IMethodSymbol { MethodKind: MethodKind.PropertyGet, AssociatedSymbol: IPropertySymbol property }
            && property.Type.IsOrImplementsInterface(viewModelInterface)
            && current.Parent is IReturnOperation { Parent: IBlockOperation { Operations.Length: 1, Parent: IMethodBodyOperation or null } })
        {
            lock (aliases)
            {
                aliases[property] = member;
            }

            return;
        }

        // Storage assignments (including handwritten constructors) are not behavioral use.
        if (current.Parent is ISimpleAssignmentOperation assignment && assignment.Target == current)
        {
            return;
        }

        if (current.Parent is INameOfOperation)
        {
            return;
        }

        lock (used)
        {
            used.Add(member);
        }
    }

    private static void ReportUnusedStorage(
        SymbolAnalysisContext context,
        INamedTypeSymbol owner,
        List<ISymbol> storage,
        Dictionary<ISymbol, ISymbol> aliases,
        HashSet<ISymbol> used)
    {
        // A use of a transparent public property is also a use of its backing storage.
        foreach (var member in aliases.Keys)
        {
            if (!used.Contains(member))
            {
                continue;
            }

            var current = member;
            while (aliases.TryGetValue(current, out var target) && used.Add(target))
            {
                current = target;
            }
        }

        foreach (var member in storage)
        {
            if (!used.Contains(member))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.Bndp003,
                        member.Locations.FirstOrDefault(),
                        owner.ToDisplayString(),
                        member.Name));
            }
        }
    }

}