namespace Bindings
{
    /// <summary>
    /// Shared identifier-conversion helpers used by both <see cref="ViewModelTemplate"/>
    /// and <see cref="ViewTemplate"/>.
    /// </summary>
    internal static class TemplateHelpers
    {
        /// <summary>
        /// Normalizes a field name following the CommunityToolkit ObservableProperty convention:
        /// 1. Trim all leading '_' characters.
        /// 2. If the result starts with "m_", remove that prefix.
        /// </summary>
        internal static string NormalizeFieldIdentifier(string fieldName)
        {
            var s = fieldName.TrimStart('_');
            if (s.StartsWith("m_", System.StringComparison.Ordinal))
                s = s.Substring(2);
            return s;
        }

        /// <summary>
        /// Returns the ViewModel property name derived from a field name (first letter uppercased).
        /// e.g. _count → Count, m_Count → Count, _interactable → Interactable
        /// </summary>
        internal static string ToPropertyName(string fieldName)
        {
            var s = NormalizeFieldIdentifier(fieldName);
            if (s.Length == 0) return fieldName;
            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// Returns the constructor parameter name derived from a field name (first letter lowercased).
        /// e.g. _model → model, m_Model → model
        /// </summary>
        internal static string ToParamName(string fieldName)
        {
            var s = NormalizeFieldIdentifier(fieldName);
            if (s.Length == 0) return fieldName;
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}
