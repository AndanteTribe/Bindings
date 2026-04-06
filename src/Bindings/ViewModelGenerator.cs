using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bindings;

/// <summary>
/// [ViewModel] アノテーションが付与されたクラスを解析し、ViewModel と View の partial クラスを生成する
/// Roslyn IIncrementalGenerator.
/// </summary>
[Generator]
public sealed class ViewModelGenerator : IIncrementalGenerator
{
    private const string ViewModelAttributeFullName = "Bindings.ViewModelAttribute";
    private const string ModelAttributeFullName = "Bindings.ModelAttribute";
    private const string SchemaAttributeFullName = "Bindings.SchemaAttribute";
    private const string SerializableAttributeFullName = "System.SerializableAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // [ViewModel] が付与されたクラスを効率的に抽出する
        var viewModelClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ViewModelAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => CollectGenerationContext(ctx, ct));

        // 収集したメタデータをコメントアウトした形式で出力（検証用）
        context.RegisterSourceOutput(
            viewModelClasses,
            static (ctx, data) => EmitCommentedDump(ctx, data));
    }

    /// <summary>
    /// [ViewModel] クラスのシンボルから必要なメタデータをすべて収集する.
    /// </summary>
    private static GenerationContext CollectGenerationContext(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;

        // 1. [ViewModel] 属性の引数 (requireBindImplementation)
        var viewModelAttr = ctx.Attributes[0];
        var requireBind = viewModelAttr.ConstructorArguments.Length > 0
                          && viewModelAttr.ConstructorArguments[0].Value is true;

        // 2. クラス自体に [System.Serializable] が既に付与されているか確認
        var alreadySerializable = false;
        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == SerializableAttributeFullName)
            {
                alreadySerializable = true;
                break;
            }
        }

        // 3. 名前空間（グローバル名前空間の場合は空文字列）
        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        // 4. メンバーを走査して [Model] / [Schema] 情報を収集
        var models = new List<(string TypeFullName, string FieldName)>();
        var schemaFields = new List<(string FieldName, string FieldTypeName, string BindingPath, int Id, string Format)>();
        var schemaMethods = new List<(string MethodName, string BindingPath, int Id)>();

        foreach (var member in classSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            switch (member)
            {
                case IFieldSymbol field:
                {
                    foreach (var attr in field.GetAttributes())
                    {
                        var attrName = attr.AttributeClass?.ToDisplayString();
                        if (attrName == ModelAttributeFullName)
                        {
                            models.Add((
                                TypeFullName: field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                FieldName: field.Name));
                        }
                        else if (attrName == SchemaAttributeFullName)
                        {
                            schemaFields.Add((
                                FieldName: field.Name,
                                FieldTypeName: field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                BindingPath: GetBindingPath(attr),
                                Id: GetSchemaId(attr),
                                Format: GetSchemaFormat(attr)));
                        }
                    }
                    break;
                }
                case IPropertySymbol prop:
                {
                    foreach (var attr in prop.GetAttributes())
                    {
                        var attrName = attr.AttributeClass?.ToDisplayString();
                        if (attrName == ModelAttributeFullName)
                        {
                            models.Add((
                                TypeFullName: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                FieldName: prop.Name));
                        }
                        else if (attrName == SchemaAttributeFullName)
                        {
                            schemaFields.Add((
                                FieldName: prop.Name,
                                FieldTypeName: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                BindingPath: GetBindingPath(attr),
                                Id: GetSchemaId(attr),
                                Format: GetSchemaFormat(attr)));
                        }
                    }
                    break;
                }
                case IMethodSymbol method
                    when method.MethodKind == MethodKind.Ordinary && !method.IsImplicitlyDeclared:
                {
                    foreach (var attr in method.GetAttributes())
                    {
                        if (attr.AttributeClass?.ToDisplayString() == SchemaAttributeFullName)
                        {
                            schemaMethods.Add((
                                MethodName: method.Name,
                                BindingPath: GetBindingPath(attr),
                                Id: GetSchemaId(attr)));
                        }
                    }
                    break;
                }
            }
        }

        return new GenerationContext(
            className: classSymbol.Name,
            @namespace: ns,
            requireBindImplementation: requireBind,
            alreadySerializable: alreadySerializable,
            models: models.ToArray(),
            schemaFields: schemaFields.ToArray(),
            schemaMethods: schemaMethods.ToArray());
    }

    /// <summary>
    /// SchemaAttribute の ConstructorArguments から bindingPath 文字列を取得する.
    ///
    /// string オーバーロード: ConstructorArguments[0] が直接バインディングパス文字列.
    /// object オーバーロード: CallerArgumentExpression により ConstructorArguments[3] に
    ///   生の式文字列（例: "PathResolver.TMPro.TMP_Text.text"）が入る. "Resolver." 以降を取り出す.
    ///
    /// PathResolver の定数 (const string) を使う場合は string オーバーロードが呼ばれるため、
    /// 通常は ConstructorArguments[0] の分岐で取得できる.
    /// </summary>
    private static string GetBindingPath(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length == 0) return string.Empty;

        // string オーバーロード: 第1引数がバインディングパス文字列
        var firstArg = attr.ConstructorArguments[0];
        if (firstArg.Kind == TypedConstantKind.Primitive && firstArg.Value is string path)
            return path;

        // object オーバーロード: CallerArgumentExpression が第4引数 (index 3) に入る
        if (attr.ConstructorArguments.Length > 3 && attr.ConstructorArguments[3].Value is string rawExpr)
        {
            const string keyword = "Resolver.";
            var idx = rawExpr.IndexOf(keyword, StringComparison.Ordinal);
            return idx >= 0 ? rawExpr.Substring(idx + keyword.Length) : rawExpr;
        }

        return string.Empty;
    }

    /// <summary>
    /// SchemaAttribute の ConstructorArguments から id を取得する.
    /// 指定がない場合は -1 (未指定センチネル) を返す.
    /// </summary>
    private static int GetSchemaId(AttributeData attr)
        => attr.ConstructorArguments.Length > 1 && attr.ConstructorArguments[1].Value is int id
            ? id
            : -1;

    /// <summary>
    /// SchemaAttribute の ConstructorArguments から format 文字列を取得する.
    /// 指定がない場合は空文字列を返す.
    /// </summary>
    private static string GetSchemaFormat(AttributeData attr)
        => attr.ConstructorArguments.Length > 2 && attr.ConstructorArguments[2].Value is string fmt
            ? fmt
            : string.Empty;

    /// <summary>
    /// 収集したメタデータをコメントアウトした形式で .g.cs ファイルとして出力する.
    /// SourceGenerator が Roslyn API から必要な情報をすべて取得できることを確認するための検証用出力.
    /// </summary>
    private static void EmitCommentedDump(SourceProductionContext ctx, GenerationContext data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("// *** Collected metadata (commented-out dump for verification) ***");
        sb.AppendLine($"// Namespace          : {data.Namespace}");
        sb.AppendLine($"// ClassName          : {data.ClassName}");
        sb.AppendLine($"// RequireBind        : {data.RequireBindImplementation}");
        sb.AppendLine($"// AlreadySerializable: {data.AlreadySerializable}");

        sb.AppendLine($"// Models ({data.Models.Length}):");
        foreach (var (typeFullName, fieldName) in data.Models)
            sb.AppendLine($"//   TypeFullName={typeFullName}  FieldName={fieldName}");

        sb.AppendLine($"// SchemaFields ({data.SchemaFields.Length}):");
        foreach (var (fieldName, fieldTypeName, bindingPath, id, format) in data.SchemaFields)
            sb.AppendLine($"//   FieldName={fieldName}  FieldType={fieldTypeName}  BindingPath={bindingPath}  Id={id}  Format={format}");

        sb.AppendLine($"// SchemaMethods ({data.SchemaMethods.Length}):");
        foreach (var (methodName, bindingPath, id) in data.SchemaMethods)
            sb.AppendLine($"//   MethodName={methodName}  BindingPath={bindingPath}  Id={id}");

        ctx.AddSource($"{data.ClassName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }
}
