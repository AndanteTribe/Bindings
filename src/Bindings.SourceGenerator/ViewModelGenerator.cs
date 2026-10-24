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
                foreach (var (descriptor, location, args) in data.Diagnostics)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(descriptor, location, args));
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

                var viewClassName = data.ClassName.Replace("ViewModel", "View");
                ctx.AddSource(GetHintName(data, viewClassName), CreateViewSource(data, fieldAssignments, methodAssignments, orderedFields));
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
        var isStruct = typeSymbol.TypeKind == TypeKind.Struct;
        var typeLocation = ctx.TargetNode.GetLocation();

        // 1. Extract [ViewModel] attribute argument (requireBindImplementation)
        var viewModelAttr = ctx.Attributes[0];
        var requireBind = viewModelAttr.ConstructorArguments.Length > 0 && viewModelAttr.ConstructorArguments[0].Value is true;

        // 2. Check whether [System.Serializable] is already applied to the type
        var alreadySerializable = typeSymbol.HasAttribute(compilation.GetTypeByMetadataName(TypeNames.SerializableAttribute));

        // 3. Namespace (empty string when the type is in the global namespace)
        var ns = typeSymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : typeSymbol.ContainingNamespace.ToDisplayString();

        // 4. Validate accessibility
        var diagnostics = new List<(DiagnosticDescriptor, Location, string[])>();
        var (accessibility, canGenerate) = GetAccessibilityKeyword(typeSymbol, typeLocation, typeSymbol.Name, diagnostics);

        // 5. Collect containing types (outermost first)
        var containingTypesList = new List<(string Accessibility, string TypeKeyword, string TypeName)>();
        var enclosing = typeSymbol.ContainingType;
        while (enclosing != null)
        {
            var keyword = enclosing.TypeKind == TypeKind.Struct ? "struct" : "class";
            var (enclosingAccessibility, isSupported) = GetAccessibilityKeyword(enclosing, typeLocation, typeSymbol.Name, diagnostics);
            canGenerate = canGenerate && isSupported;
            containingTypesList.Insert(0, (enclosingAccessibility, keyword, enclosing.Name));
            enclosing = enclosing.ContainingType;
        }
        var containingTypes = containingTypesList.ToArray();

        // 6. Validate the ViewModel class name
        if (!typeSymbol.Name.Contains("ViewModel"))
        {
            diagnostics.Add((DiagnosticDescriptors.Bnd001, typeLocation, new[] { typeSymbol.Name }));
            canGenerate = false;
        }

        // 7. Walk members to collect [Required] / [Schema] information
        // [Schema] is only valid on fields and methods; properties are excluded.
        var requiredFields = new List<(string TypeFullName, string FieldName, string TypeOfName)>();
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
                                diagnostics.Add((DiagnosticDescriptors.Bnd002, attrLoc, new[] { id.ToString() }));
                                id = -1; // treat as unset to avoid further errors
                            }
                            schemaFields.Add((
                                FieldName: field.Name,
                                FieldTypeName: field.Type.ToDisplayString(s_fullyQualifiedNullableFormat),
                                BindingPath: GetBindingPath(attr),
                                Id: id,
                                Format: GetSchemaFormat(attr),
                                Tooltip: GetSchemaTooltip(attr)));
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
                case IMethodSymbol method
                    when method.MethodKind == MethodKind.Ordinary && !method.IsImplicitlyDeclared:
                {
                    foreach (var attr in method.GetAttributes())
                    {
                        if (attr.IsAttribute(schemaAttribute))
                        {
                            var id = GetSchemaId(attr);
                            // BND002: id < -1 is invalid
                            if (id < -1)
                            {
                                var attrLoc = attr.ApplicationSyntaxReference?.GetSyntax(ct).GetLocation() ?? typeLocation;
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
            canGenerate: canGenerate,
            accessibility: accessibility,
            containingTypes: containingTypes,
            isStruct: isStruct,
            isReadOnly: typeSymbol.IsReadOnly,
            requireBindImplementation: requireBind,
            alreadySerializable: alreadySerializable,
            requiredFields: requiredFields.ToArray(),
            schemaFields: schemaFields.ToArray(),
            schemaMethods: schemaMethods.ToArray(),
            diagnostics: diagnostics.ToArray(),
            generateVContainerInjector: CanGenerateVContainerInjector(compilation, typeSymbol));
    }

    private static bool CanGenerateVContainerInjector(Compilation compilation, INamedTypeSymbol type)
    {
        // Do not replace reflection injection when user-defined injection behavior is present.
        if (type.TypeKind != TypeKind.Class || type.IsAbstract || type.IsStatic || type.IsGenericType ||
            type.ContainingType != null || type.BaseType?.SpecialType != SpecialType.System_Object)
        {
            return false;
        }

        foreach (var name in new[] { "VContainer.IInjector", "VContainer.IObjectResolver", "VContainer.IInjectParameter",
                     "VContainer.IObjectResolverExtensions", "VContainer.PreserveAttribute" })
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
            builder.Append(context.Namespace).Append('.');
        }

        for (var i = 0; i < context.ContainingTypes.Length; i++)
        {
            builder.Append(context.ContainingTypes[i].TypeName).Append('.');
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
        List<(DiagnosticDescriptor, Location, string[])> diagnostics)
    {
        var keyword = GetAccessibilityKeyword(typeSymbol.DeclaredAccessibility);
        if (keyword != null)
        {
            return (keyword, true);
        }

        var location = typeSymbol.Locations.Length > 0 ? typeSymbol.Locations[0] : fallbackLocation;
        diagnostics.Add((
            DiagnosticDescriptors.Bnd004,
            location,
            new[]
            {
                typeSymbol.ToDisplayString(),
                typeSymbol.DeclaredAccessibility.ToString(),
                viewModelName,
            }));
        return (string.Empty, false);
    }

    /// <summary>
    /// Returns the C# keyword for a supported Roslyn accessibility value.
    /// </summary>
    private static string? GetAccessibilityKeyword(Accessibility accessibility)
    {
        switch (accessibility)
        {
            case Accessibility.NotApplicable:
                return null;
            case Accessibility.Private:
                return "private";
            case Accessibility.ProtectedAndInternal:
                return "private protected";
            case Accessibility.Protected:
                return "protected";
            case Accessibility.Internal:
                return "internal";
            case Accessibility.ProtectedOrInternal:
                return "protected internal";
            case Accessibility.Public:
                return "public";
            default:
                return null;
        }
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
        if (firstArg.Kind == TypedConstantKind.Primitive && firstArg.Value is string path)
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
    /// Splits a binding path and returns the component type part and member path.
    /// e.g. "TMPro.TMP_Text.text" → ("TMPro.TMP_Text", "text")
    /// The nested RectTransform.rect.size path is normalized to the RectTransform component type.
    /// </summary>
    private static (string TypePart, string MemberName) SplitBindingPath(string path)
    {
        if (path == "UnityEngine.RectTransform.rect.size")
        {
            return ("UnityEngine.RectTransform", "rect.size");
        }

        var lastDot = path.LastIndexOf('.');
        if (lastDot < 0)
        {
            return (path, string.Empty);
        }

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
        var fieldCount = data.SchemaFields.Length;
        var methodCount = data.SchemaMethods.Length;

        var fieldAssignments = new string[fieldCount];
        var methodAssignments = new string[methodCount];

        // Pre-compute the type part for each schema entry
        var fieldTypeParts = new string[fieldCount];
        var methodTypeParts = new string[methodCount];
        for (var i = 0; i < fieldCount; i++)
        {
            (fieldTypeParts[i], _) = SplitBindingPath(data.SchemaFields[i].BindingPath);
        }

        for (var i = 0; i < methodCount; i++)
        {
            (methodTypeParts[i], _) = SplitBindingPath(data.SchemaMethods[i].BindingPath);
        }

        // Build an ordered list of unique type parts (fields first, then methods)
        var typePartSet = new HashSet<string>();
        var typePartOrder = new List<string>();
        for (var i = 0; i < fieldCount; i++)
        {
            if (typePartSet.Add(fieldTypeParts[i]))
            {
                typePartOrder.Add(fieldTypeParts[i]);
            }
        }

        for (var i = 0; i < methodCount; i++)
        {
            if (typePartSet.Add(methodTypeParts[i]))
            {
                typePartOrder.Add(methodTypeParts[i]);
            }
        }

        var componentGroups = new List<(
            string CandidateName,
            List<(bool IsMethod, int Index, string Tooltip)> Entries)>();

        // Build logical component groups. An explicit id groups entries within the same component type;
        // each unset id represents an independent component.
        foreach (var typePart in typePartOrder)
        {
            var componentBase = TypePartToFieldBase(typePart);

            // Collect entries belonging to this type part (fields first, then methods, in appearance order)
            var entries = new List<(bool IsMethod, int Index, int Id, string Tooltip, string MemberName)>();
            for (var i = 0; i < fieldCount; i++)
            {
                if (fieldTypeParts[i] == typePart)
                {
                    entries.Add((
                        false,
                        i,
                        data.SchemaFields[i].Id,
                        data.SchemaFields[i].Tooltip,
                        data.SchemaFields[i].FieldName));
                }
            }

            for (var i = 0; i < methodCount; i++)
            {
                if (methodTypeParts[i] == typePart)
                {
                    entries.Add((
                        true,
                        i,
                        data.SchemaMethods[i].Id,
                        data.SchemaMethods[i].Tooltip,
                        data.SchemaMethods[i].MethodName));
                }
            }

            var explicitGroupIndexes = new Dictionary<int, int>();
            foreach (var entry in entries)
            {
                if (entry.Id >= 0 && explicitGroupIndexes.TryGetValue(entry.Id, out var groupIndex))
                {
                    componentGroups[groupIndex].Entries.Add((entry.IsMethod, entry.Index, entry.Tooltip));
                    continue;
                }

                var candidateName = BuildComponentFieldNameCandidate(entry.MemberName, componentBase);
                var groupEntries = new List<(bool IsMethod, int Index, string Tooltip)>
                {
                    (entry.IsMethod, entry.Index, entry.Tooltip),
                };
                componentGroups.Add((candidateName, groupEntries));

                if (entry.Id >= 0)
                {
                    explicitGroupIndexes.Add(entry.Id, componentGroups.Count - 1);
                }
            }
        }

        var candidateCounts = new Dictionary<string, int>();
        foreach (var group in componentGroups)
        {
            candidateCounts.TryGetValue(group.CandidateName, out var count);
            candidateCounts[group.CandidateName] = count + 1;
        }

        // Track tooltip per assigned View field name (first non-empty value wins)
        var fieldTooltips = new Dictionary<string, string>();
        // Track fields that have conflicting tooltip values (HashSet for O(1) lookup)
        var conflictingTooltipFields = new HashSet<string>();
        var candidateNumbers = new Dictionary<string, int>();
        var usedFieldNames = new HashSet<string>();

        foreach (var group in componentGroups)
        {
            var fieldName = group.CandidateName;
            if (candidateCounts[fieldName] > 1 || !usedFieldNames.Add(fieldName))
            {
                candidateNumbers.TryGetValue(group.CandidateName, out var number);
                do
                {
                    number++;
                    fieldName = group.CandidateName + number;
                }
                while (!usedFieldNames.Add(fieldName));

                candidateNumbers[group.CandidateName] = number;
            }

            foreach (var entry in group.Entries)
            {
                Assign(entry, fieldName, fieldAssignments, methodAssignments);
                var tooltip = entry.Tooltip;
                if (string.IsNullOrEmpty(tooltip))
                {
                    continue;
                }

                if (!fieldTooltips.ContainsKey(fieldName))
                {
                    fieldTooltips[fieldName] = tooltip;
                }
                else if (fieldTooltips[fieldName] != tooltip)
                {
                    // HashSet.Add is a no-op when the element already exists
                    conflictingTooltipFields.Add(fieldName);
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
    ///   - [global::System.Serializable] in UNITY_EDITOR (only when not already applied by the user)
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
        var template = new ViewTemplate(context, fieldAssignments, methodAssignments, orderedFields.ToArray());
        return SourceText.From(template.TransformText(), Encoding.UTF8);
    }
}