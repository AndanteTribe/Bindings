using System;
using System.Collections.Generic;
using System.Text;

namespace Bindings.GeneratorCore
{
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
        public string TypeKeyword => Context.IsStruct ? "struct" : "class";

        // Indentation helpers (one extra level when inside a namespace block)
        public string I1 => HasNamespace ? "    " : "";
        public string I2 => I1 + "    ";
        public string I3 => I2 + "    ";
        public string I4 => I3 + "    ";

        /// <summary>
        /// The fully-qualified type name of the ViewModel with the global:: prefix.
        /// </summary>
        public string ViewModelFullName => string.IsNullOrEmpty(Context.Namespace) ? "global::" + Context.ClassName : "global::" + Context.Namespace + "." + Context.ClassName;

        /// <summary>
        /// Comma-separated constructor parameter list: [Required] fields first, then publisher.
        /// </summary>
        public string ConstructorParamList
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var (typeFullName, fieldName) in Context.Models.AsSpan())
                {
                    sb.Append(typeFullName).Append(' ').Append(ToParamName(fieldName)).Append(", ");
                }
                sb.Append("global::Bindings.IMvvmPublisher publisher");
                return sb.ToString();
            }
        }

        /// <summary>
        /// Normalizes a field identifier following the CommunityToolkit ObservableProperty convention.
        /// Strips leading underscores and m_ prefix.
        /// </summary>
        private static string NormalizeFieldIdentifier(string fieldName) =>
            TemplateHelpers.NormalizeFieldIdentifier(fieldName).ToString();

        /// <summary>
        /// Returns the ViewModel property name derived from a field name (first letter uppercased).
        /// e.g. _count → Count, m_Count → Count
        /// </summary>
        public static string ToPropertyName(string fieldName) =>
            TemplateHelpers.ToPropertyName(fieldName);

        /// <summary>
        /// Returns the constructor parameter name derived from a field name (first letter lowercased).
        /// e.g. _model → model, m_Model → model
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

        // Indentation helpers (one extra level when inside a namespace block)
        public string I1 => HasNamespace ? "    " : "";
        public string I2 => I1 + "    ";
        public string I3 => I2 + "    ";

        /// <summary>
        /// The fully-qualified type name of the ViewModel with the global:: prefix.
        /// </summary>
        public string ViewModelFullName => string.IsNullOrEmpty(Context.Namespace) ? "global::" + Context.ClassName : "global::" + Context.Namespace + "." + Context.ClassName;

        /// <summary>
        /// The View class name derived from the ViewModel class name.
        /// </summary>
        public string ViewClassName => Context.ClassName.Replace("ViewModel", "View");

        /// <summary>
        /// Initializer expression for the _viewModel field.
        /// Classes use " = null!"; structs are value types with no initializer needed.
        /// </summary>
        public string VmFieldInit => Context.IsStruct ? "" : " = null!";

        /// <summary>
        /// Returns the ViewModel property name derived from a field name (first letter uppercased).
        /// </summary>
        public static string ToPropertyName(string fieldName) =>
            TemplateHelpers.ToPropertyName(fieldName);

        /// <summary>
        /// Splits a binding path at the last '.' and returns (TypePart, MemberName).
        /// e.g. "TMPro.TMP_Text.text" → ("TMPro.TMP_Text", "text")
        /// </summary>
        public static (string TypePart, string MemberName) SplitBindingPath(string path)
        {
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
            for (var i = 0; i < Context.SchemaFields.Length; i++)
            {
                var (fieldName, _, bindingPath, _, format, _) = Context.SchemaFields[i];
                var viewField = FieldAssignments[i];
                var propName = ToPropertyName(fieldName);

                if (bindingPath == "TMPro.TMP_Text.text")
                {
                    sb.Append(indent);
                    sb.Append("global::Bindings.TextMeshProExtensions.SetValue(").Append(viewField).Append(", _viewModel.").Append(propName);
                    if (string.IsNullOrEmpty(format))
                    {
                        sb.Append(");");
                    }
                    else
                    {
                        sb.Append(", \"").Append(format).Append("\");");
                    }
                }
                else
                {
                    var (_, memberName) = SplitBindingPath(bindingPath);
                    sb.Append(indent).Append(viewField).Append('.').Append(memberName).Append(" = _viewModel.").Append(propName).Append(";");
                }
                sb.AppendLine();
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
            var groupOrder = new List<(string ViewField, string MemberName)>();
            var groups = new Dictionary<(string, string), List<string>>();

            for (var i = 0; i < Context.SchemaMethods.Length; i++)
            {
                var (methodName, bindingPath, _, _) = Context.SchemaMethods[i];
                var viewField = MethodAssignments[i];
                var (_, memberName) = SplitBindingPath(bindingPath);
                var key = (viewField, memberName);

                if (!groups.ContainsKey(key))
                {
                    groups[key] = new List<string>();
                    groupOrder.Add(key);
                }
                groups[key].Add(methodName);
            }

            var sb = new StringBuilder();
            foreach (var key in groupOrder)
            {
                var (viewField, memberName) = key;
                sb.Append(indent).Append(viewField).Append('.').Append(memberName).Append(".RemoveAllListeners();");
                sb.AppendLine();
                foreach (var methodName in groups[key])
                {
                    sb.Append(indent).Append(viewField).Append('.').Append(memberName).Append(".AddListener(_viewModel.").Append(methodName).Append(");");
                    sb.AppendLine();
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
}
