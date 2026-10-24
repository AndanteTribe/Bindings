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
    /// Returns the field or property accessed directly through an instance-reference operation.
    /// Static accesses and accesses through locals, parameters, or other expressions do not match.
    /// </summary>
    public static ISymbol? GetReferencedInstanceMember(this IOperation? operation) => operation switch
    {
        IFieldReferenceOperation { Instance: IInstanceReferenceOperation } field => field.Field,
        IPropertyReferenceOperation { Instance: IInstanceReferenceOperation } property => property.Property,
        _ => null,
    };

    /// <summary>
    /// Returns an unambiguous self-rebind helper candidate. Its body must be checked separately.
    /// </summary>
    public static IMethodSymbol? GetSelfRebindHelper(
        this IInvocationOperation invocation,
        ISymbol? enclosingSymbol,
        INamedTypeSymbol? viewModelAttribute,
        INamedTypeSymbol? publisherType)
    {
        if (invocation.TargetMethod.Name != RebindHelperName || invocation.TargetMethod.Arity != 1)
        {
            return null;
        }

        var type = enclosingSymbol?.ContainingType;
        if (type == null
            || !type.HasAttribute(viewModelAttribute)
            || invocation.Syntax is not InvocationExpressionSyntax syntax
            || syntax.ContainsDirectives
            || !IsSelfRebind(invocation, type, publisherType))
        {
            return null;
        }

        // Ignore helpers that are missing or ambiguous.
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

        if (SymbolEqualityComparer.Default.Equals(enclosingSymbol, helper))
        {
            return null;
        }

        return helper;
    }

    private static bool IsSelfRebind(
        IInvocationOperation invocation,
        INamedTypeSymbol type,
        INamedTypeSymbol? publisherType)
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

    /// <summary>
    /// Checks a resolved invocation against the helper's complete, single-call body.
    /// The caller supplies operations from analysis callbacks or a document semantic model.
    /// </summary>
    public static bool IsEquivalentRebindHelperBody(
        this IInvocationOperation invocation,
        IMethodSymbol helper,
        INamedTypeSymbol? publisherType,
        CancellationToken cancellationToken)
    {
        if (helper is not { Name: RebindHelperName, IsStatic: false, Arity: 0, Parameters.Length: 0, ReturnsVoid: true }
            || !IsSelfRebind(invocation, helper.ContainingType, publisherType))
        {
            return false;
        }

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
                && expression.SyntaxTree == invocation.Syntax.SyntaxTree
                && expression.Span == invocation.Syntax.Span)
            {
                return true;
            }
        }

        return false;
    }
}