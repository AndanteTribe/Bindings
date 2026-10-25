using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Operations;

namespace Bindings;

/// <summary>
/// Provides IDE code fixes for Bindings best-practice diagnostics.
/// The IDE discovers this provider independently of compiler diagnostic analyzers.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(BindingsPracticesCodeFixProvider)), Shared]
public sealed class BindingsPracticesCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticDescriptors.Bndp001.Id);

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case "BNDP001":
                    await RegisterRebindHelperFixAsync(context, diagnostic).ConfigureAwait(false);
                    break;
            }
        }
    }

    private static async Task RegisterRebindHelperFixAsync(CodeFixContext context, Diagnostic diagnostic)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var invocation = root?.FindNode(context.Span, getInnermostNodeForTie: true).FirstAncestorOrSelf<InvocationExpressionSyntax>();
        if (model == null
            || invocation == null
            || model.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation)
        {
            return;
        }

        var helper = operation.GetSelfRebindHelper(
            model.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken),
            model.Compilation.GetTypeByMetadataName(TypeNames.ViewModelAttribute),
            model.Compilation.GetTypeByMetadataName(TypeNames.PublisherAbstraction));
        if (helper == null
            || !await HasEquivalentHelperBodyAsync(context.Document, helper, context.CancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var replacement = CreateHelperInvocation(invocation, helper, model);
        if (replacement == null)
        {
            return;
        }

        // Preserve comments in the removed receiver and generic type argument, as well as the argument list.
        replacement = replacement.WithExpression(replacement.Expression.WithLeadingTrivia(invocation.Expression.DescendantTrivia()))
            .WithAdditionalAnnotations(Formatter.Annotation);
        context.RegisterCodeFix(CodeAction.Create(
            "Use PublishRebindMessage()",
            _ => Task.FromResult(context.Document.WithSyntaxRoot(root!.ReplaceNode(invocation, replacement))),
            equivalenceKey: DiagnosticDescriptors.Bndp001.Id), diagnostic);
    }

    private static async Task<bool> HasEquivalentHelperBodyAsync(
        Document document,
        IMethodSymbol helper,
        CancellationToken cancellationToken)
    {
        foreach (var reference in helper.DeclaringSyntaxReferences)
        {
            if (await reference.GetSyntaxAsync(cancellationToken).ConfigureAwait(false) is not MethodDeclarationSyntax declaration)
            {
                continue;
            }

            var expression = declaration.ExpressionBody?.Expression;
            if (declaration.Body is { Statements.Count: 1 } body && body.Statements[0] is ExpressionStatementSyntax statement)
            {
                expression = statement.Expression;
            }

            var helperDocument = document.Project.Solution.GetDocument(declaration.SyntaxTree);
            var model = helperDocument == null ? null : await helperDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (expression != null
                && model?.GetOperation(expression, cancellationToken) is IInvocationOperation invocation
                && invocation.IsEquivalentRebindHelperBody(helper, model.Compilation.GetTypeByMetadataName(TypeNames.PublisherAbstraction), cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static InvocationExpressionSyntax? CreateHelperInvocation(
        InvocationExpressionSyntax invocation,
        IMethodSymbol helper,
        SemanticModel model)
    {
        var replacement = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName(helper.Name), invocation.ArgumentList);
        if (BindsToHelper(model, invocation.SpanStart, replacement, helper))
        {
            return replacement;
        }

        // A local function or delegate can shadow the unqualified helper name.
        replacement = replacement.WithExpression(SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(), SyntaxFactory.IdentifierName(helper.Name)));
        return BindsToHelper(model, invocation.SpanStart, replacement, helper) ? replacement : null;
    }

    private static bool BindsToHelper(
        SemanticModel model,
        int position,
        InvocationExpressionSyntax replacement,
        IMethodSymbol helper) =>
        SymbolEqualityComparer.Default.Equals(
            model.GetSpeculativeSymbolInfo(position, replacement, SpeculativeBindingOption.BindAsExpression).Symbol, helper);
}