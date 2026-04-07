using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bindings;

/// <summary>
/// [ViewModel] アノテーションが付与されたクラスまたは構造体を解析し、ViewModel と View の partial 型を生成する
/// Roslyn IIncrementalGenerator.
/// </summary>
[Generator]
public sealed class ViewModelGenerator : IIncrementalGenerator
{
    private const string ViewModelAttributeFullName = "Bindings.ViewModelAttribute";
    private const string ModelAttributeFullName = "Bindings.ModelAttribute";
    private const string SchemaAttributeFullName = "Bindings.SchemaAttribute";
    private const string SerializableAttributeFullName = nameof(System.SerializableAttribute);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // [ViewModel] が付与されたクラスまたは構造体を効率的に抽出する
        var viewModelTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ViewModelAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
                transform: static (ctx, ct) => CollectGenerationContext(ctx, ct));

        // ViewModel の partial 型と View の sealed partial 型を生成する
        context.RegisterSourceOutput(
            viewModelTypes,
            static (ctx, data) =>
            {
                EmitViewModelSource(ctx, data);
                EmitViewSource(ctx, data);
            });
    }

    /// <summary>
    /// [ViewModel] クラスまたは構造体のシンボルから必要なメタデータをすべて収集する.
    /// </summary>
    private static GenerationContext CollectGenerationContext(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var isStruct = typeSymbol.TypeKind == TypeKind.Struct;

        // 1. [ViewModel] 属性の引数 (requireBindImplementation)
        var viewModelAttr = ctx.Attributes[0];
        var requireBind = viewModelAttr.ConstructorArguments.Length > 0
                          && viewModelAttr.ConstructorArguments[0].Value is true;

        // 2. クラス自体に [System.Serializable] が既に付与されているか確認
        var alreadySerializable = false;
        foreach (var attr in typeSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.Name == SerializableAttributeFullName)
            {
                alreadySerializable = true;
                break;
            }
        }

        // 3. 名前空間（グローバル名前空間の場合は空文字列）
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();

        // 4. メンバーを走査して [Model] / [Schema] 情報を収集
        var models = new List<(string TypeFullName, string FieldName)>();
        var schemaFields = new List<(string FieldName, string FieldTypeName, string BindingPath, int Id, string Format)>();
        var schemaMethods = new List<(string MethodName, string BindingPath, int Id)>();

        foreach (var member in typeSymbol.GetMembers())
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
            className: typeSymbol.Name,
            @namespace: ns,
            isStruct: isStruct,
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

    // -------------------------------------------------------------------------
    // 識別子変換ヘルパー
    // -------------------------------------------------------------------------

    /// <summary>
    /// CommunityToolkit ObservableProperty 規則に従ってフィールド名を正規化する.
    /// 1. 先頭の '_' をすべて除去 (TrimStart)
    /// 2. 結果が 'm_' で始まる場合はその 'm_' を除去
    /// </summary>
    private static string NormalizeFieldIdentifier(string fieldName)
    {
        var s = fieldName.TrimStart('_');
        if (s.StartsWith("m_", StringComparison.Ordinal))
            s = s.Substring(2);
        return s;
    }

    /// <summary>
    /// フィールド名から ViewModel プロパティ名を生成する（正規化後の先頭を大文字化）.
    /// 例: _count → Count, m_Count → Count, _interactable → Interactable
    /// </summary>
    private static string ToPropertyName(string fieldName)
    {
        var s = NormalizeFieldIdentifier(fieldName);
        if (s.Length == 0) return fieldName;
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    /// <summary>
    /// フィールド名からコンストラクタ引数名を生成する（正規化後の先頭を小文字化）.
    /// 例: _model → model, _model2 → model2, m_Model → model
    /// </summary>
    private static string ToParamName(string fieldName)
    {
        var s = NormalizeFieldIdentifier(fieldName);
        if (s.Length == 0) return fieldName;
        return char.ToLowerInvariant(s[0]) + s.Substring(1);
    }

    /// <summary>
    /// バインディングパスを最後の '.' で分割し、型部分とメンバ名を返す.
    /// 例: "TMPro.TMP_Text.text" → ("TMPro.TMP_Text", "text")
    /// </summary>
    private static (string TypePart, string MemberName) SplitBindingPath(string path)
    {
        var lastDot = path.LastIndexOf('.');
        if (lastDot < 0) return (path, string.Empty);
        return (path.Substring(0, lastDot), path.Substring(lastDot + 1));
    }

    /// <summary>
    /// バインディングパスの型部分から View フィールドのベース名（小文字始まり）を返す.
    /// 例: "TMPro.TMP_Text" → "text", "UnityEngine.UI.Button" → "button"
    /// アルゴリズム: クラス名の最後の '_' より後ろ → 先頭を小文字化
    /// </summary>
    private static string TypePartToFieldBase(string typePart)
    {
        var lastDot = typePart.LastIndexOf('.');
        var className = lastDot >= 0 ? typePart.Substring(lastDot + 1) : typePart;
        var lastUnderscore = className.LastIndexOf('_');
        var baseName = lastUnderscore >= 0 ? className.Substring(lastUnderscore + 1) : className;
        if (baseName.Length == 0) baseName = className;
        return char.ToLowerInvariant(baseName[0]) + baseName.Substring(1);
    }

    /// <summary>
    /// ViewModel の global:: 付き完全修飾型名を返す.
    /// </summary>
    private static string GetViewModelFullName(GenerationContext data) =>
        string.IsNullOrEmpty(data.Namespace)
            ? $"global::{data.ClassName}"
            : $"global::{data.Namespace}.{data.ClassName}";

    // -------------------------------------------------------------------------
    // View コンポーネントフィールド割り当て
    // -------------------------------------------------------------------------

    /// <summary>
    /// 各スキーマエントリ（SchemaField / SchemaMethod）に View コンポーネントフィールド名を割り当てる.
    ///
    /// 割り当てルール（同一型部分のスキーマをグループ化して判定）:
    ///   ケース A — 全エントリが id=-1（未指定）
    ///     1つのみ: _{base}（連番なし）
    ///     複数:    _{base}1, _{base}2, ...（出現順に 1 から連番）
    ///   ケース B — 全エントリが id≥0（明示的 id）
    ///     各エントリ: _{base}{id}（同一 id のエントリは同一フィールドを共有）
    ///   ケース C — id=-1 と id≥0 が混在
    ///     id≥0:  _{base}{id}
    ///     id=-1: 明示的 id と衝突しない最小正整数を出現順に割り当て
    ///
    /// 返値:
    ///   FieldAssignments[i]  — SchemaFields[i] に対応する View フィールド名
    ///   MethodAssignments[i] — SchemaMethods[i] に対応する View フィールド名
    ///   OrderedFields        — View に宣言するフィールドの (TypePart, FieldName) リスト（初出順・重複なし）
    /// </summary>
    private static (
        string[] FieldAssignments,
        string[] MethodAssignments,
        List<(string TypePart, string FieldName)> OrderedFields)
        BuildComponentFieldAssignments(GenerationContext data)
    {
        var fieldCount = data.SchemaFields.Length;
        var methodCount = data.SchemaMethods.Length;

        var fieldAssignments = new string[fieldCount];
        var methodAssignments = new string[methodCount];

        // 各スキーマエントリの型部分を事前計算
        var fieldTypeParts = new string[fieldCount];
        var methodTypeParts = new string[methodCount];
        for (var i = 0; i < fieldCount; i++)
            (fieldTypeParts[i], _) = SplitBindingPath(data.SchemaFields[i].BindingPath);
        for (var i = 0; i < methodCount; i++)
            (methodTypeParts[i], _) = SplitBindingPath(data.SchemaMethods[i].BindingPath);

        // 初出順で型部分のリストを作成（フィールド → メソッドの順）
        var typePartSet = new HashSet<string>();
        var typePartOrder = new List<string>();
        for (var i = 0; i < fieldCount; i++)
            if (typePartSet.Add(fieldTypeParts[i])) typePartOrder.Add(fieldTypeParts[i]);
        for (var i = 0; i < methodCount; i++)
            if (typePartSet.Add(methodTypeParts[i])) typePartOrder.Add(methodTypeParts[i]);

        // 型部分ごとにフィールド名を決定
        foreach (var typePart in typePartOrder)
        {
            var fieldBase = TypePartToFieldBase(typePart);

            // この型部分に属するエントリを収集（フィールド → メソッドの出現順）
            var entries = new List<(bool IsMethod, int Index, int Id)>();
            for (var i = 0; i < fieldCount; i++)
                if (fieldTypeParts[i] == typePart)
                    entries.Add((false, i, data.SchemaFields[i].Id));
            for (var i = 0; i < methodCount; i++)
                if (methodTypeParts[i] == typePart)
                    entries.Add((true, i, data.SchemaMethods[i].Id));

            // ケース判定
            var hasExplicit = false;
            var hasUnset = false;
            foreach (var (_, _, id) in entries)
            {
                if (id >= 0) hasExplicit = true;
                else hasUnset = true;
            }

            if (!hasExplicit)
            {
                // ケース A: 全 id=-1
                if (entries.Count == 1)
                {
                    // 1つのみ → 連番なし
                    Assign(entries[0], $"_{fieldBase}", fieldAssignments, methodAssignments);
                }
                else
                {
                    // 複数 → 1 から連番
                    for (var i = 0; i < entries.Count; i++)
                        Assign(entries[i], $"_{fieldBase}{i + 1}", fieldAssignments, methodAssignments);
                }
            }
            else if (!hasUnset)
            {
                // ケース B: 全 id≥0 → _{base}{id}（同一 id はフィールドを共有）
                foreach (var entry in entries)
                    Assign(entry, $"_{fieldBase}{entry.Id}", fieldAssignments, methodAssignments);
            }
            else
            {
                // ケース C: 混在 — まず明示的 id を割り当て、次に未指定 id に空き番を割り当て
                var usedIds = new HashSet<int>();
                foreach (var (_, _, id) in entries)
                    if (id >= 0) usedIds.Add(id);

                foreach (var entry in entries)
                    if (entry.Id >= 0)
                        Assign(entry, $"_{fieldBase}{entry.Id}", fieldAssignments, methodAssignments);

                var nextNum = 1;
                foreach (var entry in entries)
                {
                    if (entry.Id == -1)
                    {
                        while (usedIds.Contains(nextNum)) nextNum++;
                        usedIds.Add(nextNum);
                        Assign(entry, $"_{fieldBase}{nextNum}", fieldAssignments, methodAssignments);
                        nextNum++;
                    }
                }
            }
        }

        // 宣言フィールドリストを構築（初出順・重複なし）
        var seenNames = new HashSet<string>();
        var orderedFields = new List<(string TypePart, string FieldName)>();
        for (var i = 0; i < fieldCount; i++)
            if (seenNames.Add(fieldAssignments[i]))
                orderedFields.Add((fieldTypeParts[i], fieldAssignments[i]));
        for (var i = 0; i < methodCount; i++)
            if (seenNames.Add(methodAssignments[i]))
                orderedFields.Add((methodTypeParts[i], methodAssignments[i]));

        return (fieldAssignments, methodAssignments, orderedFields);
    }

    private static void Assign(
        (bool IsMethod, int Index, int Id) entry,
        string fieldName,
        string[] fieldAssignments,
        string[] methodAssignments)
    {
        if (entry.IsMethod) methodAssignments[entry.Index] = fieldName;
        else fieldAssignments[entry.Index] = fieldName;
    }

    // -------------------------------------------------------------------------
    // ViewModel partial 生成 → {ClassName}.g.cs
    // -------------------------------------------------------------------------

    /// <summary>
    /// ViewModel の partial クラスまたは構造体を生成する.
    ///
    /// 生成内容:
    ///   - [global::System.Serializable]（ユーザーが未付与の場合のみ）
    ///   - IViewModel 実装
    ///   - _publisher フィールド
    ///   - [Schema] フィールドごとの公開プロパティ（宣言順）
    ///   - コンストラクタ（[Model] フィールド順 → publisher）
    ///   - NotifyCompletedBind / OnPostBind / PublishRebindMessage
    /// </summary>
    private static void EmitViewModelSource(SourceProductionContext ctx, GenerationContext data)
    {
        var typeKw = data.IsStruct ? "struct" : "class";
        var hasNs = !string.IsNullOrEmpty(data.Namespace);
        // インデント（名前空間ブロックがある場合は 1 段深くなる）
        var i1 = hasNs ? "    " : "";        // クラスレベル
        var i2 = i1 + "    ";               // メンバーレベル
        var i3 = i2 + "    ";               // メソッドボディ
        var i4 = i3 + "    ";               // ネストされたブロック

        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (hasNs)
        {
            sb.AppendLine($"namespace {data.Namespace}");
            sb.AppendLine("{");
        }

        // [Serializable] はユーザーが未付与の場合のみ生成
        if (!data.AlreadySerializable)
            sb.AppendLine($"{i1}[global::System.Serializable]");

        sb.AppendLine($"{i1}public partial {typeKw} {data.ClassName} : global::Bindings.IViewModel");
        sb.AppendLine($"{i1}{{");

        // _publisher フィールド
        sb.AppendLine($"{i2}private readonly global::Bindings.IMvvmPublisher _publisher;");

        // [Schema] フィールドごとの公開プロパティ（宣言順）
        foreach (var (fieldName, fieldTypeName, _, _, _) in data.SchemaFields)
        {
            var propName = ToPropertyName(fieldName);
            sb.AppendLine();
            sb.AppendLine($"{i2}public {fieldTypeName} {propName}");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}get => {fieldName};");
            sb.AppendLine($"{i3}set");
            sb.AppendLine($"{i3}{{");
            sb.AppendLine($"{i4}{fieldName} = value;");
            sb.AppendLine($"{i4}PublishRebindMessage();");
            sb.AppendLine($"{i3}}}");
            sb.AppendLine($"{i2}}}");
        }

        // コンストラクタ: [Model] フィールド順 → publisher
        sb.AppendLine();
        var ctorParams = new List<string>();
        var ctorBodyLines = new List<string>();
        foreach (var (typeFullName, fieldName) in data.Models)
        {
            var paramName = ToParamName(fieldName);
            ctorParams.Add($"{typeFullName} {paramName}");
            ctorBodyLines.Add($"{i3}{fieldName} = {paramName};");
        }
        ctorParams.Add("global::Bindings.IMvvmPublisher publisher");

        sb.AppendLine($"{i2}public {data.ClassName}({string.Join(", ", ctorParams)})");
        sb.AppendLine($"{i2}{{");
        foreach (var line in ctorBodyLines)
            sb.AppendLine(line);
        sb.AppendLine($"{i3}_publisher = publisher;");
        sb.AppendLine($"{i2}}}");

        // ヘルパーメソッド
        sb.AppendLine();
        sb.AppendLine($"{i2}public void NotifyCompletedBind() => OnPostBind();");
        sb.AppendLine();
        sb.AppendLine($"{i2}partial void OnPostBind();");
        sb.AppendLine();
        sb.AppendLine($"{i2}[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        sb.AppendLine($"{i2}private void PublishRebindMessage()");
        sb.AppendLine($"{i2}{{");
        // global:: 付き完全修飾型名を使用（ユーザー定義型との衝突を避けるため）
        sb.AppendLine($"{i3}_publisher.PublishRebindMessage<{GetViewModelFullName(data)}>();");
        sb.AppendLine($"{i2}}}");

        sb.AppendLine($"{i1}}}");
        if (hasNs) sb.AppendLine("}");

        ctx.AddSource($"{data.ClassName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    // -------------------------------------------------------------------------
    // View sealed partial 生成 → {ViewClassName}.g.cs
    // -------------------------------------------------------------------------

    /// <summary>
    /// View の sealed partial クラスを生成する.
    /// クラス名に "ViewModel" が含まれない場合（BND001）は生成をスキップする.
    ///
    /// 生成内容:
    ///   - [global::System.Serializable]（常に付与）
    ///   - IView&lt;T&gt; 実装（_viewModel フィールド・Initialize）
    ///   - UI コンポーネントフィールド（スキーマから導出・重複排除）
    ///   - BindAsync（requireBindImplementation=false の場合のみ）
    ///   - BindAll（フィールドバインド → イベントバインド → OnPostBind → NotifyCompletedBind）
    ///   - partial void OnPostBind
    ///   - デバッグ用サブスクライバ（#if UNITY_EDITOR || ... ブロック）
    /// </summary>
    private static void EmitViewSource(SourceProductionContext ctx, GenerationContext data)
    {
        // BND001: クラス名に "ViewModel" が含まれない場合はスキップ（View クラス名を導出できない）
        if (!data.ClassName.Contains("ViewModel"))
            return;

        var viewClassName = data.ClassName.Replace("ViewModel", "View");
        var vmFullName = GetViewModelFullName(data);
        var hasNs = !string.IsNullOrEmpty(data.Namespace);
        var i1 = hasNs ? "    " : "";
        var i2 = i1 + "    ";
        var i3 = i2 + "    ";

        var (fieldAssignments, methodAssignments, orderedFields) = BuildComponentFieldAssignments(data);

        var sb = new StringBuilder();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();

        if (hasNs)
        {
            sb.AppendLine($"namespace {data.Namespace}");
            sb.AppendLine("{");
        }

        // View クラス宣言（常に [Serializable] を付与）
        sb.AppendLine($"{i1}[global::System.Serializable]");
        sb.AppendLine($"{i1}public sealed partial class {viewClassName} : global::Bindings.IView<{vmFullName}>");
        sb.AppendLine($"{i1}{{");

        // _viewModel フィールド（クラスは null! で初期化、構造体は値型のため初期化子なし）
        var vmFieldInit = data.IsStruct ? string.Empty : " = null!";
        sb.AppendLine($"{i2}[global::System.NonSerialized]");
        sb.AppendLine($"{i2}private {vmFullName} _viewModel{vmFieldInit};");

        // UI コンポーネントフィールド（初出順・重複なし）
        foreach (var (typePart, fieldName) in orderedFields)
        {
            sb.AppendLine();
            sb.AppendLine($"{i2}[global::UnityEngine.SerializeField]");
            sb.AppendLine($"{i2}private global::{typePart} {fieldName} = null!;");
        }

        // Initialize（明示的インターフェース実装）
        sb.AppendLine();
        sb.AppendLine($"{i2}void global::Bindings.IView<{vmFullName}>.Initialize({vmFullName} viewModel)");
        sb.AppendLine($"{i2}{{");
        sb.AppendLine($"{i3}_viewModel = viewModel;");
        sb.AppendLine($"{i2}}}");

        // BindAsync（requireBindImplementation=false の場合のみ生成）
        if (!data.RequireBindImplementation)
        {
            sb.AppendLine();
            sb.AppendLine($"{i2}global::System.Threading.Tasks.ValueTask global::Bindings.IView.BindAsync(global::System.Threading.CancellationToken _)");
            sb.AppendLine($"{i2}{{");
            sb.AppendLine($"{i3}BindAll();");
            sb.AppendLine($"{i3}return default;");
            sb.AppendLine($"{i2}}}");
        }

        // BindAll: フィールドバインド（宣言順）→ イベントバインド（宣言順）
        sb.AppendLine();
        sb.AppendLine($"{i2}private void BindAll()");
        sb.AppendLine($"{i2}{{");
        AppendFieldBindings(sb, i3, data.SchemaFields, fieldAssignments);
        AppendMethodBindings(sb, i3, data.SchemaMethods, methodAssignments);
        sb.AppendLine($"{i3}OnPostBind();");
        sb.AppendLine($"{i3}_viewModel.NotifyCompletedBind();");
        sb.AppendLine($"{i2}}}");

        sb.AppendLine();
        sb.AppendLine($"{i2}partial void OnPostBind();");
        sb.AppendLine($"{i1}}}"); // end View class

        // デバッグ用サブスクライバブロック（フィールドバインドのみ再実行）
        sb.AppendLine();
        sb.AppendLine("#if UNITY_EDITOR || DEVELOPMENT_BUILD || !DISABLE_DEBUGTOOLKIT");
        sb.AppendLine($"{i1}public sealed partial class {viewClassName} : global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>");
        sb.AppendLine($"{i1}{{");
        sb.AppendLine($"{i2}void global::Bindings.IMvvmSubscriber<global::Bindings.DebugBindMessage>.OnReceivedMessage(global::Bindings.DebugBindMessage message)");
        sb.AppendLine($"{i2}{{");
        sb.AppendLine($"{i3}message.BindTo(this);");
        AppendFieldBindings(sb, i3, data.SchemaFields, fieldAssignments); // フィールドバインドのみ
        sb.AppendLine($"{i3}OnPostBind();");
        sb.AppendLine($"{i3}_viewModel.NotifyCompletedBind();");
        sb.AppendLine($"{i2}}}");
        sb.AppendLine($"{i1}}}");
        sb.AppendLine("#endif");

        if (hasNs) sb.AppendLine("}");

        ctx.AddSource($"{viewClassName}.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    // -------------------------------------------------------------------------
    // BindAll 内コード生成ヘルパー
    // -------------------------------------------------------------------------

    /// <summary>
    /// [Schema] フィールドのバインドコードを生成する.
    ///
    /// バインドルール:
    ///   - "TMPro.TMP_Text.text" かつ format なし → SetValue(_field, _viewModel.Prop)
    ///   - "TMPro.TMP_Text.text" かつ format あり → SetValue(_field, _viewModel.Prop, "fmt")
    ///   - それ以外すべて                         → _field.member = _viewModel.Prop
    /// </summary>
    private static void AppendFieldBindings(
        StringBuilder sb,
        string indent,
        (string FieldName, string FieldTypeName, string BindingPath, int Id, string Format)[] schemaFields,
        string[] fieldAssignments)
    {
        for (var i = 0; i < schemaFields.Length; i++)
        {
            var (fieldName, _, bindingPath, _, format) = schemaFields[i];
            var viewField = fieldAssignments[i];
            var propName = ToPropertyName(fieldName);

            if (bindingPath == "TMPro.TMP_Text.text")
            {
                // TMPro.TMP_Text.text のみ SetValue 拡張メソッドを使用
                if (string.IsNullOrEmpty(format))
                    sb.AppendLine($"{indent}global::Bindings.TextMeshProExtensions.SetValue({viewField}, _viewModel.{propName});");
                else
                    sb.AppendLine($"{indent}global::Bindings.TextMeshProExtensions.SetValue({viewField}, _viewModel.{propName}, \"{format}\");");
            }
            else
            {
                // その他すべては直接代入
                var (_, memberName) = SplitBindingPath(bindingPath);
                sb.AppendLine($"{indent}{viewField}.{memberName} = _viewModel.{propName};");
            }
        }
    }

    /// <summary>
    /// [Schema] メソッドのイベントバインドコードを生成する.
    /// 同一 (フィールド名, イベント名) グループの先頭で RemoveAllListeners を1回だけ呼ぶ.
    /// グループの順序は初出順に従う.
    /// </summary>
    private static void AppendMethodBindings(
        StringBuilder sb,
        string indent,
        (string MethodName, string BindingPath, int Id)[] schemaMethods,
        string[] methodAssignments)
    {
        // (viewFieldName, memberName) をキーにグループ化（初出順）
        var groupOrder = new List<(string ViewField, string MemberName)>();
        var groups = new Dictionary<(string, string), List<string>>();

        for (var i = 0; i < schemaMethods.Length; i++)
        {
            var (methodName, bindingPath, _) = schemaMethods[i];
            var viewField = methodAssignments[i];
            var (_, memberName) = SplitBindingPath(bindingPath);
            var key = (viewField, memberName);

            if (!groups.ContainsKey(key))
            {
                groups[key] = new List<string>();
                groupOrder.Add(key);
            }
            groups[key].Add(methodName);
        }

        foreach (var key in groupOrder)
        {
            var (viewField, memberName) = key;
            // RemoveAllListeners は同一グループで1回のみ
            sb.AppendLine($"{indent}{viewField}.{memberName}.RemoveAllListeners();");
            foreach (var methodName in groups[key])
                sb.AppendLine($"{indent}{viewField}.{memberName}.AddListener(_viewModel.{methodName});");
        }
    }
}
