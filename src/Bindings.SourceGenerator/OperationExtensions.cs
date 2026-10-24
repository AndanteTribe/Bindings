using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Bindings;

/// <summary>
/// Shares operation analysis between Bindings diagnostics and code fixes.
/// </summary>
internal static class OperationExtensions
{
    private const string RebindHelperName = "PublishRebindMessage";

    /// <summary>
    /// Returns the equivalent self-rebind helper, without constructing replacement syntax.
    /// </summary>
    public static IMethodSymbol? GetEquivalentRebindHelper(
        this IInvocationOperation invocation,
        ISymbol? enclosingSymbol,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        if (invocation.TargetMethod.Name != RebindHelperName || invocation.TargetMethod.Arity != 1)
        {
            return null;
        }

        var type = enclosingSymbol?.ContainingType;
        var publisherType = compilation.GetTypeByMetadataName(TypeNames.PublisherAbstraction);
        if (type == null || !type.HasAttribute(compilation.GetTypeByMetadataName(TypeNames.ViewModelAttribute)) ||
            invocation.Syntax is not InvocationExpressionSyntax syntax || syntax.ContainsDirectives ||
            !IsSelfRebind(invocation, type, publisherType))
        {
            return null;
        }

        // Ignore helpers that are missing, ambiguous, or have different behavior.
        IMethodSymbol? helper = null;
        foreach (var member in type.GetMembers(RebindHelperName))
        {
            if (member is not IMethodSymbol { IsStatic: false, Arity: 0, Parameters.Length: 0, ReturnsVoid: true } method)
            {
                continue;
            }

            if (helper != null)
            {
                return null;
            }

            helper = method;
        }

        if (helper == null
            || SymbolEqualityComparer.Default.Equals(enclosingSymbol, helper)
            || !HasEquivalentBody(helper, type, publisherType, compilation, cancellationToken))
        {
            return null;
        }

        return helper;
    }

    private static bool IsSelfRebind(IInvocationOperation invocation, INamedTypeSymbol type, INamedTypeSymbol? publisherType)
    {
        return publisherType != null
               && invocation is { TargetMethod: { Name: RebindHelperName, Arity: 1, Parameters.Length: 0, ReturnsVoid: true }, Arguments.Length: 0 }
               && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, publisherType)
               && SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.TypeArguments[0], type)
               && invocation.Instance is IFieldReferenceOperation
               {
                   Field: { Name: "_publisher", IsStatic: false } field,
                   Instance: IInstanceReferenceOperation
                   {
                       ReferenceKind: InstanceReferenceKind.ContainingTypeInstance
                   },
               }
               && SymbolEqualityComparer.Default.Equals(field.Type, publisherType)
               && SymbolEqualityComparer.Default.Equals(field.ContainingType, type);
    }

    private static bool HasEquivalentBody(
        IMethodSymbol helper, INamedTypeSymbol type, INamedTypeSymbol? publisherType,
        Compilation compilation, CancellationToken cancellationToken)
    {
        foreach (var reference in helper.DeclaringSyntaxReferences)
        {
            if (reference.GetSyntax(cancellationToken) is not MethodDeclarationSyntax declaration)
            {
                continue;
            }

            var expression = declaration.ExpressionBody?.Expression;
            if (declaration.Body is { Statements.Count: 1 } body && body.Statements[0] is ExpressionStatementSyntax statement)
            {
                expression = statement.Expression;
            }

            if (expression != null
                && compilation.GetSemanticModel(declaration.SyntaxTree).GetOperation(expression, cancellationToken) is IInvocationOperation operation
                && IsSelfRebind(operation, type, publisherType))
            {
                return true;
            }
        }

        return false;
    }
}