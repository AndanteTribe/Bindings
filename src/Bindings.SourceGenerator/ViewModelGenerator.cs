using System;
using System.Collections.Generic;
using System.Linq;
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
    private static readonly SymbolDisplayFormat s_fullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Efficiently find all classes/structs annotated with [ViewModel]
        var viewModelTypes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                TypeNames.ViewModelAttribute,
                predicate: static (node, _) => node is ClassDeclarationSyntax or StructDeclarationSyntax,
                transform: static (ctx, ct) => CollectGenerationContext(ctx, ct));

        // Generate partial ViewModel type and sealed partial View type for each annotated type
        context.RegisterSourceOutput(
            viewModelTypes,
            static (ctx, data) =>
            {
                // Report all diagnostics collected during the transform phase.
                for (var i = 0; i < data.Diagnostics.Count; i++)
                {
                    ctx.ReportDiagnostic(data.Diagnostics[i]);
                }

                if (!data.CanGenerate)
                {
                    return;
                }

                ctx.AddSource(GetHintName(data, data.ClassName), CreateViewModelSource(data));

                var (fieldAssignments, methodAssignments, orderedFields, conflictingTooltipFields) = BuildComponentFieldAssignments(data);
                foreach (var fieldName in conflictingTooltipFields)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors.Bnd003, Location.None, fieldName));
                }

                var viewClassName = TemplateHelpers.GetViewName(data.ClassName)!;
                ctx.AddSource(
                    GetHintName(data, viewClassName),
                    CreateViewSource(data, fieldAssignments, methodAssignments, orderedFields));
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
        var compilation = ctx.SemanticModel.Compilation;
        var requiredAttribute = compilation.GetTypeByMetadataName(TypeNames.RequiredAttribute);
        var schemaAttribute = compilation.GetTypeByMetadataName(TypeNames.SchemaAttribute);
        var viewModelType = typeSymbol switch
        {
            { TypeKind: TypeKind.Struct, IsReadOnly: true } => GenerationContext.ViewModelTypeKind.ReadOnlyStruct,
            { TypeKind: TypeKind.Struct } => GenerationContext.ViewModelTypeKind.Struct,
            _ => GenerationContext.ViewModelTypeKind.Class,
        };
        var typeLocation = ctx.TargetNode.GetLocation();

        // 1. Extract [ViewModel] attribute argument (requireBindImplementation)
        var viewModelAttr = ctx.Attributes[0];
        var requireBind = viewModelAttr.ConstructorArguments.Length > 0 && viewModelAttr.ConstructorArguments[0].Value is true;

        // 2. Namespace (empty string when the type is in the global namespace)
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : typeSymbol.ContainingNamespace.ToDisplayString();

        // 3. Validate accessibility
        var diagnostics = new List<Diagnostic>();
        var (accessibility, canGenerate) = GetAccessibilityKeyword(typeSymbol, typeLocation, typeSymbol.Name, diagnostics);

        // 4. Collect containing types (outermost first)
        var containingTypes = new List<(string Accessibility, string TypeKeyword, string TypeName)>();
        var enclosing = typeSymbol.ContainingType;
        while (enclosing != null)
        {
            var keyword = enclosing.TypeKind == TypeKind.Struct ? "struct" : "class";
            var (enclosingAccessibility, isSupported) = GetAccessibilityKeyword(enclosing, typeLocation, typeSymbol.Name, diagnostics);
            canGenerate = canGenerate && isSupported;
            containingTypes.Insert(0, (enclosingAccessibility, keyword, enclosing.Name));
            enclosing = enclosing.ContainingType;
        }

        // 5. Validate the ViewModel class name
        if (TemplateHelpers.GetViewName(typeSymbol.Name) == null)
        {
            diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.Bnd001, typeLocation, typeSymbol.Name));
            canGenerate = false;
        }

        // 6. Walk members to collect [Required] / [Schema] information
        // [Schema] is only valid on fields and methods; properties are excluded.
        var requiredFields = new List<(string TypeFullName, string FieldName, string TypeOfName)>();
        var schemaFields = new List<(string FieldName, string FieldTypeName, GenerationContext.BindingInfo Binding)>();
        var schemaMethods = new List<(string MethodName, string ActionField, GenerationContext.BindingInfo Binding)>();
        var cachedActions = new List<(string FieldName, string ActionType, string Target)>();
        HashSet<string>? reservedNames = null;

        foreach (var member in typeSymbol.GetMembers())
        {
            ct.ThrowIfCancellationRequested();

            switch (member)
            {
                case IFieldSymbol field:
                {
                    foreach (var attr in field.GetAttributes())
                    {
                        if (attr.IsAttribute(requiredAttribute))
                        {
                            requiredFields.Add((
                                TypeFullName: field.Type.ToDisplayString(s_fullyQualifiedNullableFormat),
                                FieldName: field.Name,
                                TypeOfName: field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                        }
                        else if (attr.IsAttribute(schemaAttribute))
                        {
                            var id = GetSchemaId(attr);
                            // BND002: id < -1 is invalid
                            if (id < -1)
                            {
                                var attrLoc = attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation() ?? typeLocation;
                                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.Bnd002, attrLoc, id.ToString()));
                                id = -1; // treat as unset to avoid further errors
                            }
                            schemaFields.Add((
                                FieldName: field.Name,
                                FieldTypeName: field.Type.ToDisplayString(s_fullyQualifiedNullableFormat),
                                Binding: new GenerationContext.BindingInfo(
                                    bindingPath: GetBindingPath(attr),
                                    id: id,
                                    format: GetSchemaFormat(attr),
                                    tooltip: GetSchemaTooltip(attr))));
                        }
                    }
                    break;
                }
                // [Required] on properties is still supported; [Schema] on properties is excluded.
                case IPropertySymbol prop:
                {
                    foreach (var attr in prop.GetAttributes())
                    {
                        if (attr.IsAttribute(requiredAttribute))
                        {
                            requiredFields.Add((
                                TypeFullName: prop.Type.ToDisplayString(s_fullyQualifiedNullableFormat),
                                FieldName: prop.Name,
                                TypeOfName: prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                        }
                    }
                    break;
                }
                case IMethodSymbol { MethodKind: MethodKind.Ordinary, IsImplicitlyDeclared: false } method:
                {
                    string? actionField = null;
                    foreach (var attr in method.GetAttributes())
                    {
                        if (attr.IsAttribute(schemaAttribute))
                        {
                            var id = GetSchemaId(attr);
                            // BND002: id < -1 is invalid
                            if (id < -1)
                            {
                                var attrLoc = attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation() ?? typeLocation;
                                diagnostics.Add(Diagnostic.Create(DiagnosticDescriptors.Bnd002, attrLoc, id.ToString()));
                                id = -1; // treat as unset to avoid further errors
                            }
                            if (actionField == null)
                            {
                                reservedNames ??= [.. typeSymbol.MemberNames];
                                actionField = "_" + TemplateHelpers.ToParamName(method.Name) + "Action";
                                while (!reservedNames.Add(actionField))
                                {
                                    actionField += "_";
                                }
                                var actionType = "global::UnityEngine.Events.UnityAction";
                                if (method.Parameters.Length > 0)
                                {
                                    actionType += "<" + string.Join(", ", method.Parameters.Select(p => p.Type.ToDisplayString(s_fullyQualifiedNullableFormat))) + ">";
                                }
                                var receiver = method.IsStatic ? typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : "this";
                                cachedActions.Add((actionField, actionType, receiver + ".@" + method.Name));
                            }
                            schemaMethods.Add((
                                MethodName: method.Name,
                                ActionField: actionField,
                                Binding: new GenerationContext.BindingInfo(
                                    bindingPath: GetBindingPath(attr),
                                    id: id,
                                    format: string.Empty,
                                    tooltip: GetSchemaTooltip(attr))));
                        }
                    }
                    break;
                }
            }
        }

        return new GenerationContext(
            className: typeSymbol.Name,
            @namespace: ns,
            canGenerate: canGenerate,
            accessibility: accessibility,
            containingTypes: containingTypes,
            type: viewModelType,
            requireBindImplementation: requireBind,
            requiredFields: requiredFields,
            schemaFields: schemaFields,
            schemaMethods: schemaMethods,
            cachedActions: cachedActions,
            diagnostics: diagnostics,
            generateVContainerInjector: CanGenerateVContainerInjector(compilation, typeSymbol));
    }

    private static bool CanGenerateVContainerInjector(Compilation compilation, INamedTypeSymbol type)
    {
        // Do not replace reflection injection when user-defined injection behavior is present.
        if (type.TypeKind != TypeKind.Class
            || type.IsAbstract
            || type.IsStatic
            || type.IsGenericType
            || type.ContainingType != null
            || type.BaseType?.SpecialType != SpecialType.System_Object)
        {
            return false;
        }

        foreach (var name in new[]
                 {
                     "VContainer.IInjector",
                     "VContainer.IObjectResolver",
                     "VContainer.IInjectParameter",
                     "VContainer.IObjectResolverExtensions",
                     "VContainer.PreserveAttribute"
                 })
        {
            if (compilation.GetTypeByMetadataName(name) == null)
            {
                return false;
            }
        }

        var injectAttribute = compilation.GetTypeByMetadataName(TypeNames.InjectAttribute);
        foreach (var member in type.GetMembers())
        {
            if (member is IMethodSymbol { MethodKind: MethodKind.Constructor, IsImplicitlyDeclared: false })
            {
                return false;
            }

            if (member.HasAttribute(injectAttribute))
            {
                return false;
            }
        }

        return type.GetTypeMembers("__BindingsInjector").Length == 0;
    }

    /// <summary>
    /// Builds a source hint name from the namespace, containing type chain, and generated type name.
    /// </summary>
    private static string GetHintName(GenerationContext context, string typeName)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrEmpty(context.Namespace))
        {
            _ = builder.Append(context.Namespace).Append('.');
        }

        for (var i = 0; i < context.ContainingTypes.Count; i++)
        {
            _ = builder.Append(context.ContainingTypes[i].TypeName).Append('.');
        }

        return builder.Append(typeName).Append(".g.cs").ToString();
    }

    /// <summary>
    /// Returns the C# keyword for a type accessibility and reports a diagnostic when the
    /// accessibility cannot be represented in generated source.
    /// </summary>
    private static (string Keyword, bool IsSupported) GetAccessibilityKeyword(
        INamedTypeSymbol typeSymbol,
        Location fallbackLocation,
        string viewModelName,
        List<Diagnostic> diagnostics)
    {
        var keyword = GetAccessibilityKeyword(typeSymbol.DeclaredAccessibility);
        if (keyword != null)
        {
            return (keyword, true);
        }

        var location = typeSymbol.Locations.Length > 0 ? typeSymbol.Locations[0] : fallbackLocation;
        diagnostics.Add(Diagnostic.Create(
            DiagnosticDescriptors.Bnd004,
            location,
            typeSymbol.ToDisplayString(),
            typeSymbol.DeclaredAccessibility.ToString(),
            viewModelName));
        return (string.Empty, false);
    }

    /// <summary>
    /// Returns the C# keyword for a supported Roslyn accessibility value.
    /// </summary>
    private static string? GetAccessibilityKeyword(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.NotApplicable => null,
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.Protected => "protected",
            Accessibility.Internal => "internal",
            Accessibility.ProtectedOrInternal => "protected internal",
            Accessibility.Public => "public",
            _ => null,
        };
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
        if (attr.ConstructorArguments.Length == 0)
        {
            return string.Empty;
        }

        // String overload: first argument is the binding path string
        var firstArg = attr.ConstructorArguments[0];
        if (firstArg is { Kind: TypedConstantKind.Primitive, Value: string path })
        {
            return path;
        }

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
    private static int GetSchemaId(AttributeData attr) =>
        attr.ConstructorArguments.Length > 1 && attr.ConstructorArguments[1].Value is int id ? id : -1;

    /// <summary>
    /// Retrieves the format string from SchemaAttribute's ConstructorArguments.
    /// Returns an empty string when no format is specified.
    /// </summary>
    private static string GetSchemaFormat(AttributeData attr) =>
        attr.ConstructorArguments.Length > 2 && attr.ConstructorArguments[2].Value is string fmt ? fmt : string.Empty;

    /// <summary>
    /// Retrieves the tooltip string from SchemaAttribute's ConstructorArguments.
    /// Both overloads place tooltip at index 3:
    ///   String overload: (bindingPath=0, id=1, format=2, tooltip=3)
    ///   Object overload: (bindingPath=0, id=1, format=2, tooltip=3, path=4)
    /// Returns an empty string when no tooltip is specified.
    /// </summary>
    private static string GetSchemaTooltip(AttributeData attr) =>
        attr.ConstructorArguments.Length > 3 && attr.ConstructorArguments[3].Value is string tooltip ? tooltip : string.Empty;

    // -------------------------------------------------------------------------
    // Identifier conversion helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Extracts the component type part of a binding path.
    /// e.g. "TMPro.TMP_Text.text" → "TMPro.TMP_Text"
    /// The nested RectTransform.rect.size path is normalized to the RectTransform component type.
    /// </summary>
    private static string GetComponentTypePart(string path)
    {
        if (path == "UnityEngine.RectTransform.rect.size")
        {
            return "UnityEngine.RectTransform";
        }

        var lastDot = path.LastIndexOf('.');
        return lastDot < 0 ? path : path.Substring(0, lastDot);
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
        if (baseName.Length == 0)
        {
            baseName = className;
        }

        return char.ToLowerInvariant(baseName[0]) + baseName.Substring(1);
    }

    // -------------------------------------------------------------------------
    // View component field assignment
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assigns a View component field name to each schema entry (SchemaField / SchemaMethod).
    ///
    /// Each field name combines the ViewModel member name and the component type:
    ///   _{memberBase}{ComponentBase}
    /// Entries with the same type part and explicit id share one component field. Unset ids create
    /// independent component fields. A numeric suffix is added only when multiple independent fields
    /// have the same name candidate.
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
        var fieldCount = data.SchemaFields.Count;
        var methodCount = data.SchemaMethods.Count;

        var fieldAssignments = new string[fieldCount];
        var methodAssignments = new string[methodCount];

        // Collect entries directly into type groups, preserving first appearance order.
        var entriesByType = new Dictionary<string, List<(bool, int, int, string, string)>>();
        var orderedTypeGroups = new List<(string, List<(bool, int, int, string, string)>)>();
        for (var i = 0; i < fieldCount; i++)
        {
            var (fieldName, _, binding) = data.SchemaFields[i];
            AddEntry(binding, false, i, fieldName);
        }

        for (var i = 0; i < methodCount; i++)
        {
            var (methodName, _, binding) = data.SchemaMethods[i];
            AddEntry(binding, true, i, methodName);
        }

        void AddEntry(GenerationContext.BindingInfo binding, bool isMethod, int index, string memberName)
        {
            var typePart = GetComponentTypePart(binding.BindingPath);
            if (!entriesByType.TryGetValue(typePart, out var entries))
            {
                entries = [];
                entriesByType.Add(typePart, entries);
                orderedTypeGroups.Add((typePart, entries));
            }
            entries.Add((isMethod, index, binding.Id, binding.Tooltip, memberName));
        }

        var componentGroups = new List<(
            string TypePart,
            string CandidateName,
            List<(bool IsMethod, int Index, string Tooltip)> Entries)>();

        // Build logical component groups. An explicit id groups entries within the same component type;
        // each unset id represents an independent component.
        foreach (var (typePart, entries) in orderedTypeGroups)
        {
            var componentBase = TypePartToFieldBase(typePart);

            var explicitGroupIndexes = new Dictionary<int, int>();
            foreach (var (isMethod, index, id, tooltip, memberName) in entries)
            {
                if (id >= 0 && explicitGroupIndexes.TryGetValue(id, out var groupIndex))
                {
                    componentGroups[groupIndex].Entries.Add((isMethod, index, tooltip));
                    continue;
                }

                var candidateName = BuildComponentFieldNameCandidate(memberName, componentBase);
                var groupEntries = new List<(bool IsMethod, int Index, string Tooltip)>
                {
                    (isMethod, index, tooltip),
                };
                componentGroups.Add((typePart, candidateName, groupEntries));

                if (id >= 0)
                {
                    explicitGroupIndexes.Add(id, componentGroups.Count - 1);
                }
            }
        }

        var candidateCounts = new Dictionary<string, int>();
        foreach (var (_, candidateName, _) in componentGroups)
        {
            _ = candidateCounts.TryGetValue(candidateName, out var count);
            candidateCounts[candidateName] = count + 1;
        }

        // Retain the parsed type and resolved tooltip for each assigned View field.
        var fieldMetadata = new Dictionary<string, (string, string)>();
        // Track fields that have conflicting tooltip values (HashSet for O(1) lookup)
        var conflictingTooltipFields = new HashSet<string>();
        var candidateNumbers = new Dictionary<string, int>();
        var usedFieldNames = new HashSet<string>();

        foreach (var (typePart, candidateName, entries) in componentGroups)
        {
            var fieldName = candidateName;
            if (candidateCounts[fieldName] > 1 || !usedFieldNames.Add(fieldName))
            {
                _ = candidateNumbers.TryGetValue(candidateName, out var number);
                do
                {
                    number++;
                    fieldName = candidateName + number;
                }
                while (!usedFieldNames.Add(fieldName));

                candidateNumbers[candidateName] = number;
            }

            var resolvedTooltip = string.Empty;
            foreach (var entry in entries)
            {
                Assign(entry, fieldName, fieldAssignments, methodAssignments);
                var tooltip = entry.Tooltip;
                if (string.IsNullOrEmpty(tooltip))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(resolvedTooltip))
                {
                    resolvedTooltip = tooltip;
                }
                else if (resolvedTooltip != tooltip)
                {
                    // HashSet.Add is a no-op when the element already exists
                    _ = conflictingTooltipFields.Add(fieldName);
                }
            }
            fieldMetadata.Add(fieldName, (typePart, resolvedTooltip));
        }

        // Build the ordered field declaration list (deduplicated, initial-appearance order)
        var seenNames = new HashSet<string>();
        var orderedFields = new List<(string TypePart, string FieldName, string Tooltip)>();
        for (var i = 0; i < fieldCount; i++)
        {
            var fn = fieldAssignments[i];
            if (seenNames.Add(fn))
            {
                var (typePart, tooltip) = fieldMetadata[fn];
                orderedFields.Add((typePart, fn, tooltip));
            }
        }
        for (var i = 0; i < methodCount; i++)
        {
            var fn = methodAssignments[i];
            if (seenNames.Add(fn))
            {
                var (typePart, tooltip) = fieldMetadata[fn];
                orderedFields.Add((typePart, fn, tooltip));
            }
        }

        return (fieldAssignments, methodAssignments, orderedFields, conflictingTooltipFields);
    }

    private static string BuildComponentFieldNameCandidate(string memberName, string componentBase)
    {
        var memberBase = TemplateHelpers.ToParamName(memberName);
        return $"_{memberBase}{TemplateHelpers.ToPropertyName(componentBase)}";
    }

    private static void Assign(
        (bool IsMethod, int Index, string Tooltip) entry,
        string fieldName,
        string[] fieldAssignments,
        string[] methodAssignments)
    {
        if (entry.IsMethod)
        {
            methodAssignments[entry.Index] = fieldName;
        }
        else
        {
            fieldAssignments[entry.Index] = fieldName;
        }
    }

    // -------------------------------------------------------------------------
    // ViewModel partial generation → {FullyQualifiedViewModelName}.g.cs
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates the partial class or struct for the ViewModel using the <see cref="ViewModelTemplate"/>.
    ///
    /// Generated contents:
    ///   - IViewModel implementation
    ///   - _publisher field
    ///   - One public property per [Schema] field (in declaration order)
    ///   - Constructor ([Required] fields in declaration order, then publisher)
    ///   - NotifyCompletedBind / OnPostBind / PublishRebindMessage helpers
    /// </summary>
    private static SourceText CreateViewModelSource(GenerationContext context)
    {
        var template = new ViewModelTemplate(context);
        return SourceText.From(template.TransformText(), Encoding.UTF8);
    }

    // -------------------------------------------------------------------------
    // View sealed partial generation → {FullyQualifiedViewName}.g.cs
    // -------------------------------------------------------------------------

    /// <summary>
    /// Generates the sealed partial class for the View using the <see cref="ViewTemplate"/>.
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
    private static SourceText CreateViewSource(
        GenerationContext context,
        string[] fieldAssignments,
        string[] methodAssignments,
        List<(string TypePart, string FieldName, string Tooltip)> orderedFields)
    {
        var template = new ViewTemplate(context, fieldAssignments, methodAssignments, [.. orderedFields]);
        return SourceText.From(template.TransformText(), Encoding.UTF8);
    }
}