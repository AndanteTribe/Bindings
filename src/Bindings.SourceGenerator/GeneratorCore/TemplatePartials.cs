using System.Collections.Generic;
using System.Text;

namespace Bindings.GeneratorCore;

/// <summary>
/// Context fields and helper methods for the <see cref="ViewModelTemplate"/> partial class.
/// </summary>
public partial class ViewModelTemplate(GenerationContext context)
{
    /// <summary>
    /// The generation context for the [ViewModel] type being processed.
    /// </summary>
    public readonly GenerationContext Context = context;

    public bool HasNamespace => !string.IsNullOrEmpty(Context.Namespace);
    public string TypeKeyword => Context.Type == GenerationContext.ViewModelTypeKind.Class ? "class" : "struct";

    /// <summary>
    /// The fully-qualified type name of the ViewModel with the global:: prefix,
    /// including any containing type names for nested types.
    /// </summary>
    public string ViewModelFullName
    {
        get
        {
            var sb = new StringBuilder("global::");
            if (!string.IsNullOrEmpty(Context.Namespace))
            {
                _ = sb.Append(Context.Namespace).Append('.');
            }
            for (var i = 0; i < Context.ContainingTypes.Count; i++)
            {
                _ = sb.Append(Context.ContainingTypes[i].TypeName).Append('.');
            }
            _ = sb.Append(Context.ClassName);
            return sb.ToString();
        }
    }

    /// <summary>
    /// Comma-separated constructor parameter list: [Required] fields first, then publisher.
    /// </summary>
    public string ConstructorParamList
    {
        get
        {
            var sb = new StringBuilder();
            for (var i = 0; i < Context.RequiredFields.Count; i++)
            {
                var (typeFullName, fieldName, _) = Context.RequiredFields[i];
                _ = sb.Append(typeFullName).Append(' ').Append(ToParamName(fieldName)).Append(", ");
            }
            _ = sb.Append("global::Bindings.IMvvmPublisher publisher");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Schema fields used to generate ViewModel properties, deduplicated by source field name.
    /// Multiple Schema attributes on one field still remain in <see cref="GenerationContext.SchemaFields"/>
    /// so the View can generate every declared binding.
    /// </summary>
    public IEnumerable<(string FieldName, string FieldTypeName, GenerationContext.BindingInfo Binding)> SchemaPropertyFields
    {
        get
        {
            var fieldNames = new HashSet<string>();
            for (var i = 0; i < Context.SchemaFields.Count; i++)
            {
                var schemaField = Context.SchemaFields[i];
                if (fieldNames.Add(schemaField.FieldName))
                {
                    yield return schemaField;
                }
            }
        }
    }

    /// <summary>
    /// Returns the ViewModel property name derived from a field name (first letter uppercased).
    /// e.g. _count → Count, m_Count → Count
    /// </summary>
    public static string ToPropertyName(string fieldName) =>
        TemplateHelpers.ToPropertyName(fieldName);

    /// <summary>
    /// Returns the constructor parameter name derived from a field name (first letter lowercased).
    /// e.g. _count → count, m_Count → count
    /// </summary>
    public static string ToParamName(string fieldName) =>
        TemplateHelpers.ToParamName(fieldName);
}

/// <summary>
/// Context fields and helper methods for the <see cref="ViewTemplate"/> partial class.
/// </summary>
public partial class ViewTemplate(
    GenerationContext context,
    string[] fieldAssignments,
    string[] methodAssignments,
    (string TypePart, string FieldName, string Tooltip)[] orderedFields)
{
    /// <summary>
    /// The generation context for the [ViewModel] type being processed.
    /// </summary>
    public readonly GenerationContext Context = context;

    /// <summary>
    /// View component field name assigned to each SchemaFields[i] entry.
    /// </summary>
    public readonly string[] FieldAssignments = fieldAssignments;

    /// <summary>
    /// View component field name assigned to each SchemaMethods[i] entry.
    /// </summary>
    public readonly string[] MethodAssignments = methodAssignments;

    /// <summary>
    /// Ordered (deduplicated) list of View component fields to declare, with type and tooltip.
    /// </summary>
    public readonly (string TypePart, string FieldName, string Tooltip)[] OrderedFields = orderedFields;

    public bool HasNamespace => !string.IsNullOrEmpty(Context.Namespace);

    /// <summary>
    /// The fully-qualified type name of the ViewModel with the global:: prefix,
    /// including any containing type names for nested types.
    /// </summary>
    public string ViewModelFullName
    {
        get
        {
            var sb = new StringBuilder("global::");
            if (!string.IsNullOrEmpty(Context.Namespace))
            {
                _ = sb.Append(Context.Namespace).Append('.');
            }
            for (var i = 0; i < Context.ContainingTypes.Count; i++)
            {
                _ = sb.Append(Context.ContainingTypes[i].TypeName).Append('.');
            }
            _ = sb.Append(Context.ClassName);
            return sb.ToString();
        }
    }

    /// <summary>
    /// The View class name derived from the ViewModel class name.
    /// </summary>
    public string ViewClassName => TemplateHelpers.GetViewName(Context.ClassName)!;

    /// <summary>
    /// Initializer expression for the _viewModel field.
    /// Classes use " = null!"; structs are value types with no initializer needed.
    /// </summary>
    public string VmFieldInit => Context.Type == GenerationContext.ViewModelTypeKind.Class ? " = null!" : "";

    /// <summary>
    /// Returns the ViewModel property name derived from a field name (first letter uppercased).
    /// </summary>
    public static string ToPropertyName(string fieldName) =>
        TemplateHelpers.ToPropertyName(fieldName);

    /// <summary>
    /// Splits a binding path and returns (TypePart, MemberName).
    /// e.g. "TMPro.TMP_Text.text" → ("TMPro.TMP_Text", "text")
    /// The nested RectTransform.rect.size path is normalized to the RectTransform component type.
    /// </summary>
    public static (string TypePart, string MemberName) SplitBindingPath(string path)
    {
        if (path == "UnityEngine.RectTransform.rect.size")
        {
            return ("UnityEngine.RectTransform", "rect.size");
        }

        var lastDot = path.LastIndexOf('.');
        if (lastDot < 0)
        {
            return (path, "");
        }

        return (path.Substring(0, lastDot), path.Substring(lastDot + 1));
    }

    /// <summary>
    /// Generates field binding statements for all [Schema] fields.
    /// Each statement is prefixed with <paramref name="indent"/>.
    /// </summary>
    public string WriteFieldBindings(string indent)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < Context.SchemaFields.Count; i++)
        {
            var (fieldName, _, binding) = Context.SchemaFields[i];
            var viewField = FieldAssignments[i];
            var propName = ToPropertyName(fieldName);

            switch (binding.BindingPath)
            {
                case "TMPro.TMP_Text.text":
                    _ = sb.Append(indent);
                    _ = sb.Append("global::Bindings.TextMeshProExtensions.SetValue(").Append(viewField).Append(", _viewModel.").Append(propName);
                    if (!string.IsNullOrEmpty(binding.Format))
                    {
                        _ = sb.Append(", \"").Append(binding.Format).Append('"');
                    }
                    _ = sb.Append(");");
                    break;
                case "UnityEngine.GameObject.activeSelf":
                    _ = sb.Append(indent);
                    _ = sb.Append(viewField).Append(".SetActive(_viewModel.").Append(propName).Append(");");
                    break;
                case "UnityEngine.RectTransform.rect.size":
                    _ = sb.Append(indent);
                    _ = sb.Append(viewField).Append(".SetSizeWithCurrentAnchors(global::UnityEngine.RectTransform.Axis.Horizontal, _viewModel.").Append(propName).Append(".x);");
                    _ = sb.AppendLine();
                    _ = sb.Append(indent);
                    _ = sb.Append(viewField).Append(".SetSizeWithCurrentAnchors(global::UnityEngine.RectTransform.Axis.Vertical, _viewModel.").Append(propName).Append(".y);");
                    break;
                default:
                    var (_, memberName) = SplitBindingPath(binding.BindingPath);
                    _ = sb.Append(indent).Append(viewField).Append('.').Append(memberName).Append(" = _viewModel.").Append(propName).Append(";");
                    break;
            }
            _ = sb.AppendLine();
        }
        // Remove trailing newline so the template's surrounding whitespace is consistent
        if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
        {
            sb.Length--;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates method binding statements for all [Schema] methods, grouped by (viewField, memberName).
    /// RemoveAllListeners is called once per group, followed by AddListener for each method.
    /// Each statement is prefixed with <paramref name="indent"/>.
    /// </summary>
    public string WriteMethodBindings(string indent)
    {
        var orderedGroups = new List<(string, string, List<string>)>();
        var groups = new Dictionary<(string, string), List<string>>();

        for (var i = 0; i < Context.SchemaMethods.Count; i++)
        {
            var (methodName, binding) = Context.SchemaMethods[i];
            var viewField = MethodAssignments[i];
            var (_, memberName) = SplitBindingPath(binding.BindingPath);
            var key = (viewField, memberName);

            if (!groups.TryGetValue(key, out var methods))
            {
                methods = [];
                groups.Add(key, methods);
                orderedGroups.Add((viewField, memberName, methods));
            }
            methods.Add(methodName);
        }

        var sb = new StringBuilder();
        foreach (var (viewField, memberName, methods) in orderedGroups)
        {
            _ = sb.Append(indent).Append(viewField).Append('.').Append(memberName).Append(".RemoveAllListeners();");
            _ = sb.AppendLine();
            foreach (var methodName in methods)
            {
                _ = sb.Append(indent).Append(viewField).Append('.').Append(memberName).Append(".AddListener(_viewModel.").Append(methodName).Append(");");
                _ = sb.AppendLine();
            }
        }
        // Remove trailing newline so the template's surrounding whitespace is consistent
        if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
        {
            sb.Length--;
        }

        return sb.ToString();
    }
}