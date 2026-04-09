using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Bindings.GeneratorCore;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bindings;

/// <summary>
/// Roslyn IIncrementalGenerator that analyzes classes and structs annotated with [ViewModel]
/// and emits partial ViewModel and sealed partial View types.
/// </summary>
[Generator]
public sealed class ViewModelGenerator : IIncrementalGenerator
{
    private const string ViewModelAttributeFullName = nameof(Bindings) + ".ViewModelAttribute";
    private const string ModelAttributeFullName = nameof(Bindings) + ".ModelAttribute";
    private const string SchemaAttributeFullName = nameof(Bindings) + ".SchemaAttribute";
    private const string SerializableAttributeFullName = nameof(System) + "." + nameof(System.SerializableAttribute);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Efficiently find all classes/structs annotated with [ViewModel]
        var viewModelTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ViewModelAttributeFullName,
                predicate: static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
                transform: static (ctx, ct) => CollectGenerationContext(ctx, ct));

        // Generate partial ViewModel type and sealed partial View type for each annotated type
        context.RegisterSourceOutput(
            viewModelTypes,
            static (ctx, data) =>
            {
                // Report all diagnostics collected during the transform phase (BND001, BND002)
                foreach (var (descriptor, location, args) in data.Diagnostics)
                    ctx.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));

                EmitViewModelSource(ctx, data);
                EmitViewSource(ctx, data);
            });
    }

    /// <summary>
    /// Collects all required metadata from the symbol of a [ViewModel] class or struct.
    /// </summary>
    private static GenerationContext CollectGenerationContext(
        GeneratorAttributeSyntaxContext ctx,
        CancellationToken ct)
    {
        var typeSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var isStruct = typeSymbol.TypeKind == TypeKind.Struct;
        var typeLocation = ctx.TargetNode.GetLocation();

        // 1. Extract [ViewModel] attribute argument (requireBindImplementation)
        var viewModelAttr = ctx.Attributes[0];
        var requireBind = viewModelAttr.ConstructorArguments.Length > 0
                          && viewModelAttr.ConstructorArguments[0].Value is true;

        // 2. Check whether [System.Serializable] is already applied to the type
        var alreadySerializable = false;
        foreach (var attr in typeSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == SerializableAttributeFullName)
            {
                alreadySerializable = true;
                break;
            }
        }

        // 3. Namespace (empty string when the type is in the global namespace)
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : typeSymbol.ContainingNamespace.ToDisplayString();

        // 4. Collect early diagnostics
        var diagnostics = new List<(DiagnosticDescriptor, Location, string[])>();

        // BND001: class name must contain "ViewModel"
        if (!typeSymbol.Name.Contains("ViewModel"))
        {
            diagnostics.Add((DiagnosticDescriptors.Bnd001, typeLocation, new[] { typeSymbol.Name }));
        }

        // 5. Walk members to collect [Model] / [Schema] information
        // [Schema] is only valid on fields and methods; properties are excluded.
        var models = new List<(string TypeFullName, string FieldName)>();
        var schemaFields = new List<(string FieldName, string FieldTypeName, string BindingPath, int Id, string Format, string Tooltip)>();
        var schemaMethods = new List<(string MethodName, string BindingPath, int Id, string Tooltip)>();

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
                            var id = GetSchemaId(attr);
                            // BND002: id < -1 is invalid
                            if (id < -1)
                            {
                                var attrLoc = attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation()
                                              ?? typeLocation;
                                diagnostics.Add((DiagnosticDescriptors.Bnd002, attrLoc, new[] { id.ToString() }));
                                id = -1; // treat as unset to avoid further errors
                            }
                            schemaFields.Add((
                                FieldName: field.Name,
                                FieldTypeName: field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                BindingPath: GetBindingPath(attr),
                                Id: id,
                                Format: GetSchemaFormat(attr),
                                Tooltip: GetSchemaTooltip(attr)));
                        }
                    }
                    break;
                }
                // [Model] on properties is still supported; [Schema] on properties is excluded.
                case IPropertySymbol prop:
                {
                    foreach (var attr in prop.GetAttributes())
                    {
                        if (attr.AttributeClass?.ToDisplayString() == ModelAttributeFullName)
                        {
                            models.Add((
                                TypeFullName: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                FieldName: prop.Name));
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
                            var id = GetSchemaId(attr);
                            // BND002: id < -1 is invalid
                            if (id < -1)
                            {
                                var attrLoc = attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation()
                                              ?? typeLocation;
                                diagnostics.Add((DiagnosticDescriptors.Bnd002, attrLoc, new[] { id.ToString() }));
                                id = -1; // treat as unset to avoid further errors
                            }
                            schemaMethods.Add((
                                MethodName: method.Name,
                                BindingPath: GetBindingPath(attr),
                                Id: id,
                                Tooltip: GetSchemaTooltip(attr)));
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
            isReadOnly: typeSymbol.IsReadOnly,
            requireBindImplementation: requireBind,
            alreadySerializable: alreadySerializable,
            models: models.ToArray(),
            schemaFields: schemaFields.ToArray(),
            schemaMethods: schemaMethods.ToArray(),
            diagnostics: diagnostics.ToArray());
    }

    /// <summary>
    /// Retrieves the binding path string from SchemaAttribute's ConstructorArguments.
    ///
    /// String overload: ConstructorArguments[0] holds the binding path string directly.
    /// Object overload: CallerArgumentExpression places the raw expression string (e.g.
    ///   "PathResolver.TMPro.TMP_Text.text") in ConstructorArguments[4]; the substring
    ///   after "Resolver." is extracted.
    ///   Signature: (object bindingPath, int id, string format, string tooltip, [CallerArgumentExpression] string path)
    ///
    /// PathResolver constants (const string) always invoke the string overload, so
    /// ConstructorArguments[0] is the common path.
    /// </summary>
    private static string GetBindingPath(AttributeData attr)
    {
        if (attr.ConstructorArguments.Length == 0) return string.Empty;

        // String overload: first argument is the binding path string
        var firstArg = attr.ConstructorArguments[0];
        if (firstArg.Kind == TypedConstantKind.Primitive && firstArg.Value is string path)
            return path;

        // Object overload: CallerArgumentExpression places the raw expression in the fifth parameter (index 4)
        // Signature: (object bindingPath, int id, string format, string tooltip, [CallerArgumentExpression] string path)
        if (attr.ConstructorArguments.Length > 4 && attr.ConstructorArguments[4].Value is string rawExpr)
        {
            const string keyword = "Resolver.";
            var idx = rawExpr.IndexOf(keyword, StringComparison.Ordinal);
            return idx >= 0 ? rawExpr.Substring(idx + keyword.Length) : rawExpr;
        }

        return string.Empty;
    }

    /// <summary>
    /// Retrieves the id value from SchemaAttribute's ConstructorArguments.
    /// Returns -1 (unset sentinel) when no id is specified.
    /// </summary>
    private static int GetSchemaId(AttributeData attr)
        => attr.ConstructorArguments.Length > 1 && attr.ConstructorArguments[1].Value is int id
            ? id
            : -1;

    /// <summary>
    /// Retrieves the format string from SchemaAttribute's ConstructorArguments.
    /// Returns an empty string when no format is specified.
    /// </summary>
    private static string GetSchemaFormat(AttributeData attr)
        => attr.ConstructorArguments.Length > 2 && attr.ConstructorArguments[2].Value is string fmt
            ? fmt
            : string.Empty;

    /// <summary>
    /// Retrieves the tooltip string from SchemaAttribute's ConstructorArguments.
    /// Both overloads place tooltip at index 3:
    ///   String overload: (bindingPath=0, id=1, format=2, tooltip=3)
    ///   Object overload: (bindingPath=0, id=1, format=2, tooltip=3, path=4)
    /// Returns an empty string when no tooltip is specified.
    /// </summary>
    private static string GetSchemaTooltip(AttributeData attr)
        => attr.ConstructorArguments.Length > 3 && attr.ConstructorArguments[3].Value is string tooltip
            ? tooltip
            : string.Empty;

    // -------------------------------------------------------------------------
    // Identifier conversion helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Splits a binding path at the last '.' and returns the type part and member name.
    /// e.g. "TMPro.TMP_Text.text" → ("TMPro.TMP_Text", "text")
    /// </summary>
    private static (string TypePart, string MemberName) SplitBindingPath(string path)
    {
        var lastDot = path.LastIndexOf('.');
        if (lastDot < 0) return (path, string.Empty);
        return (path.Substring(0, lastDot), path.Substring(lastDot + 1));
    }

    /// <summary>
    /// Returns the lowercase-first base name for a View field derived from the type part of a binding path.
    /// e.g. "TMPro.TMP_Text" → "text", "UnityEngine.UI.Button" → "button"
    /// Algorithm: extract class name (text after last '.'), then take the substring after the last '_'
    /// (falls back to the full class name when no '_' exists or when the substring after '_' is empty),
    /// then lowercase the first letter.
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

    // -------------------------------------------------------------------------
    // View component field assignment
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assigns a View component field name to each schema entry (SchemaField / SchemaMethod).
    ///
    /// Assignment rules (entries are grouped by their type part):
    ///   Case A — all entries have id=-1 (unset)
    ///     single entry : _{base} (no number suffix)
    ///     multiple     : _{base}1, _{base}2, ... (numbered from 1 in appearance order)
    ///   Case B — all entries have id>=0 (explicit id)
    ///     each entry   : _{base}{id} (entries with the same id share one field)
    ///   Case C — mixed id=-1 and id>=0
    ///     id>=0  : _{base}{id}
    ///     id=-1  : assigned the smallest positive integer that does not conflict with any explicit id
    ///
    /// Tooltip conflict (BND003): when multiple entries that share the same View field specify
    /// different non-empty tooltip strings, the conflicting field names are returned in ConflictingTooltipFields.
    ///
    /// Return values:
    ///   FieldAssignments[i]       — View field name corresponding to SchemaFields[i]
    ///   MethodAssignments[i]      — View field name corresponding to SchemaMethods[i]
    ///   OrderedFields             — (TypePart, FieldName, Tooltip) list of fields to declare in the View
    ///                               (deduplicated, initial-appearance order; tooltip is the first non-empty value)
    ///   ConflictingTooltipFields  — field names where tooltip conflict (BND003) was detected
    /// </summary>
    private static (
        string[] FieldAssignments,
        string[] MethodAssignments,
        List<(string TypePart, string FieldName, string Tooltip)> OrderedFields,
        HashSet<string> ConflictingTooltipFields)
        BuildComponentFieldAssignments(GenerationContext data)
    {
        var fieldCount = data.SchemaFields.Length;
        var methodCount = data.SchemaMethods.Length;

        var fieldAssignments = new string[fieldCount];
        var methodAssignments = new string[methodCount];

        // Pre-compute the type part for each schema entry
        var fieldTypeParts = new string[fieldCount];
        var methodTypeParts = new string[methodCount];
        for (var i = 0; i < fieldCount; i++)
            (fieldTypeParts[i], _) = SplitBindingPath(data.SchemaFields[i].BindingPath);
        for (var i = 0; i < methodCount; i++)
            (methodTypeParts[i], _) = SplitBindingPath(data.SchemaMethods[i].BindingPath);

        // Build an ordered list of unique type parts (fields first, then methods)
        var typePartSet = new HashSet<string>();
        var typePartOrder = new List<string>();
        for (var i = 0; i < fieldCount; i++)
            if (typePartSet.Add(fieldTypeParts[i])) typePartOrder.Add(fieldTypeParts[i]);
        for (var i = 0; i < methodCount; i++)
            if (typePartSet.Add(methodTypeParts[i])) typePartOrder.Add(methodTypeParts[i]);

        // Track tooltip per assigned View field name (first non-empty value wins)
        var fieldTooltips = new Dictionary<string, string>();
        // Track fields that have conflicting tooltip values (HashSet for O(1) lookup)
        var conflictingTooltipFields = new HashSet<string>();

        // Determine field names for each type part
        foreach (var typePart in typePartOrder)
        {
            var fieldBase = TypePartToFieldBase(typePart);

            // Collect entries belonging to this type part (fields first, then methods, in appearance order)
            var entries = new List<(bool IsMethod, int Index, int Id, string Tooltip)>();
            for (var i = 0; i < fieldCount; i++)
                if (fieldTypeParts[i] == typePart)
                    entries.Add((false, i, data.SchemaFields[i].Id, data.SchemaFields[i].Tooltip));
            for (var i = 0; i < methodCount; i++)
                if (methodTypeParts[i] == typePart)
                    entries.Add((true, i, data.SchemaMethods[i].Id, data.SchemaMethods[i].Tooltip));

            // Determine which cases apply
            var hasExplicit = false;
            var hasUnset = false;
            foreach (var (_, _, id, _) in entries)
            {
                if (id >= 0) hasExplicit = true;
                else hasUnset = true;
            }

            if (!hasExplicit)
            {
                // Case A: all id=-1
                if (entries.Count == 1)
                {
                    // Single entry: no number suffix
                    Assign(entries[0], $"_{fieldBase}", fieldAssignments, methodAssignments);
                }
                else
                {
                    // Multiple entries: number from 1
                    for (var i = 0; i < entries.Count; i++)
                        Assign(entries[i], $"_{fieldBase}{i + 1}", fieldAssignments, methodAssignments);
                }
            }
            else if (!hasUnset)
            {
                // Case B: all id>=0 → _{base}{id} (entries with the same id share one field)
                foreach (var entry in entries)
                    Assign(entry, $"_{fieldBase}{entry.Id}", fieldAssignments, methodAssignments);
            }
            else
            {
                // Case C: mixed — first assign explicit ids, then assign available numbers to unset ids
                var usedIds = new HashSet<int>();
                foreach (var (_, _, id, _) in entries)
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

            // BND003: detect conflicting tooltip values among entries that share the same View field
            foreach (var entry in entries)
            {
                var assignedField = entry.IsMethod
                    ? methodAssignments[entry.Index]
                    : fieldAssignments[entry.Index];
                var tooltip = entry.Tooltip;
                if (string.IsNullOrEmpty(tooltip)) continue;

                if (!fieldTooltips.ContainsKey(assignedField))
                {
                    fieldTooltips[assignedField] = tooltip;
                }
                else if (fieldTooltips[assignedField] != tooltip)
                {
                    // HashSet.Add is a no-op when the element already exists
                    conflictingTooltipFields.Add(assignedField);
                }
            }
        }

        // Build the ordered field declaration list (deduplicated, initial-appearance order)
        var seenNames = new HashSet<string>();
        var orderedFields = new List<(string TypePart, string FieldName, string Tooltip)>();
        for (var i = 0; i < fieldCount; i++)
        {
            var fn = fieldAssignments[i];
            if (seenNames.Add(fn))
            {
                fieldTooltips.TryGetValue(fn, out var tt);
                orderedFields.Add((fieldTypeParts[i], fn, tt ?? string.Empty));
            }
        }
        for (var i = 0; i < methodCount; i++)
        {
            var fn = methodAssignments[i];
            if (seenNames.Add(fn))
            {
                fieldTooltips.TryGetValue(fn, out var tt);
                orderedFields.Add((methodTypeParts[i], fn, tt ?? string.Empty));
            }
        }

        return (fieldAssignments, methodAssignments, orderedFields, conflictingTooltipFields);
    }

    private static void Assign(
        (bool IsMethod, int Index, int Id, string Tooltip) entry,
        string fieldName,
        string[] fieldAssignments,
        string[] methodAssignments)
    {
        if (entry.IsMethod) methodAssignments[entry.Index] = fieldName;
        else fieldAssignments[entry.Index] = fieldName;
    }

    // -------------------------------------------------------------------------
    // ViewModel partial generation → {ClassName}.g.cs
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates the partial class or struct for the ViewModel using the <see cref="ViewModelTemplate"/>.
    ///
    /// Generated contents:
    ///   - [global::System.Serializable] (only when not already applied by the user)
    ///   - IViewModel implementation
    ///   - _publisher field
    ///   - One public property per [Schema] field (in declaration order)
    ///   - Constructor ([Model] fields in declaration order, then publisher)
    ///   - NotifyCompletedBind / OnPostBind / PublishRebindMessage helpers
    /// </summary>
    private static void EmitViewModelSource(SourceProductionContext ctx, GenerationContext context)
    {
        var template = new ViewModelTemplate(context);
        ctx.AddSource($"{context.ClassName}.g.cs", SourceText.From(template.TransformText(), Encoding.UTF8));
    }

    // -------------------------------------------------------------------------
    // View sealed partial generation → {ViewClassName}.g.cs
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates the sealed partial class for the View using the <see cref="ViewTemplate"/>.
    /// Skips generation when the class name does not contain "ViewModel" (BND001 is reported in RegisterSourceOutput).
    ///
    /// Generated contents:
    ///   - [global::System.Serializable] (always emitted)
    ///   - IView&lt;T&gt; implementation (_viewModel field, Initialize)
    ///   - UI component fields derived from schema entries (deduplicated)
    ///   - BindAsync (only when requireBindImplementation=false)
    ///   - BindAll (field bindings → event bindings → OnPostBind → NotifyCompletedBind)
    ///   - partial void OnPostBind
    ///   - Debug subscriber block (#if UNITY_EDITOR || ... )
    /// </summary>
    private static void EmitViewSource(SourceProductionContext ctx, GenerationContext context)
    {
        // BND001 was already reported in RegisterSourceOutput; skip View generation here
        if (!context.ClassName.Contains("ViewModel"))
            return;

        var (fieldAssignments, methodAssignments, orderedFields, conflictingTooltipFields) =
            BuildComponentFieldAssignments(context);

        // BND003: report a warning for each View field that has conflicting tooltip values
        foreach (var fieldName in conflictingTooltipFields)
            ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.Bnd003, Location.None, fieldName));

        var viewClassName = context.ClassName.Replace("ViewModel", "View");
        var template = new ViewTemplate(context, fieldAssignments, methodAssignments, orderedFields.ToArray());
        ctx.AddSource($"{viewClassName}.g.cs", SourceText.From(template.TransformText(), Encoding.UTF8));
    }
}
