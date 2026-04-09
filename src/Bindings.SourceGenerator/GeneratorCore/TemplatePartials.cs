using System.Collections.Generic;
using System.Text;

namespace Bindings.GeneratorCore
{
    /// <summary>
    /// Context fields and helper methods for the <see cref="ViewModelTemplate"/> partial class.
    /// </summary>
    public partial class ViewModelTemplate
    {
        /// <summary>
        /// The generation context for the [ViewModel] type being processed.
        /// </summary>
        public readonly GenerationContext Context;

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
        /// Comma-separated constructor parameter list: [Model] fields first, then publisher.
        /// </summary>
        public string ConstructorParamList
        {
            get
            {
                var parts = new List<string>();
                foreach (var (typeFullName, fieldName) in Context.Models)
                {
                    parts.Add($"{typeFullName} {ToParamName(fieldName)}");
                }

                parts.Add("global::Bindings.IMvvmPublisher publisher");
                return string.Join(", ", parts);
            }
        }

        public ViewModelTemplate(GenerationContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Normalizes a field identifier following the CommunityToolkit ObservableProperty convention.
        /// Strips leading underscores and m_ prefix.
        /// </summary>
        private static string NormalizeFieldIdentifier(string fieldName) =>
            TemplateHelpers.NormalizeFieldIdentifier(fieldName);

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
    public partial class ViewTemplate
    {
        /// <summary>
        /// The generation context for the [ViewModel] type being processed.
        /// </summary>
        public readonly GenerationContext Context;

        /// <summary>
        /// View component field name assigned to each SchemaFields[i] entry.
        /// </summary>
        public readonly string[] FieldAssignments;

        /// <summary>
        /// View component field name assigned to each SchemaMethods[i] entry.
        /// </summary>
        public readonly string[] MethodAssignments;

        /// <summary>
        /// Ordered (deduplicated) list of View component fields to declare, with type and tooltip.
        /// </summary>
        public readonly (string TypePart, string FieldName, string Tooltip)[] OrderedFields;

        public bool HasNamespace => !string.IsNullOrEmpty(Context.Namespace);

        // Indentation helpers (one extra level when inside a namespace block)
        public string I1 => HasNamespace ? "    " : "";
        public string I2 => I1 + "    ";
        public string I3 => I2 + "    ";

        /// <summary>
        /// The fully-qualified type name of the ViewModel with the global:: prefix.
        /// </summary>
        public string ViewModelFullName =>
            string.IsNullOrEmpty(Context.Namespace)
                ? $"global::{Context.ClassName}"
                : $"global::{Context.Namespace}.{Context.ClassName}";

        /// <summary>
        /// The View class name derived from the ViewModel class name.
        /// </summary>
        public string ViewClassName => Context.ClassName.Replace("ViewModel", "View");

        /// <summary>
        /// Initializer expression for the _viewModel field.
        /// Classes use " = null!"; structs are value types with no initializer needed.
        /// </summary>
        public string VmFieldInit => Context.IsStruct ? string.Empty : " = null!";

        public ViewTemplate(GenerationContext context, string[] fieldAssignments, string[] methodAssignments, (string TypePart, string FieldName, string Tooltip)[] orderedFields)
        {
            Context = context;
            FieldAssignments = fieldAssignments;
            MethodAssignments = methodAssignments;
            OrderedFields = orderedFields;
        }

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
            if (lastDot < 0) return (path, string.Empty);
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
                    if (string.IsNullOrEmpty(format))
                        sb.Append($"{indent}global::Bindings.TextMeshProExtensions.SetValue({viewField}, _viewModel.{propName});\n");
                    else
                        sb.Append($"{indent}global::Bindings.TextMeshProExtensions.SetValue({viewField}, _viewModel.{propName}, \"{format}\");\n");
                }
                else
                {
                    var (_, memberName) = SplitBindingPath(bindingPath);
                    sb.Append($"{indent}{viewField}.{memberName} = _viewModel.{propName};\n");
                }
            }
            // Remove trailing newline so the template's surrounding whitespace is consistent
            if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
                sb.Length--;
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
                sb.Append($"{indent}{viewField}.{memberName}.RemoveAllListeners();\n");
                foreach (var methodName in groups[key])
                    sb.Append($"{indent}{viewField}.{memberName}.AddListener(_viewModel.{methodName});\n");
            }
            // Remove trailing newline so the template's surrounding whitespace is consistent
            if (sb.Length > 0 && sb[sb.Length - 1] == '\n')
                sb.Length--;
            return sb.ToString();
        }
    }
}
