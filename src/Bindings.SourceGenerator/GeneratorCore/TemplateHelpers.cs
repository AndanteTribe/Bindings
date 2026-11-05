using System;

namespace Bindings.GeneratorCore
{
    /// <summary>
    /// Shared identifier-conversion helpers used by both <see cref="ViewModelTemplate"/>
    /// and <see cref="ViewTemplate"/>.
    /// </summary>
    internal static class TemplateHelpers
    {
        /// <summary>
        /// Replaces the trailing ViewModel, optionally followed by ASCII digits, with View.
        /// Returns null when the type name does not follow this convention.
        /// </summary>
        internal static string? GetViewName(string typeName)
        {
            var suffixEnd = typeName.Length;
            while (suffixEnd > 0 && typeName[suffixEnd - 1] is >= '0' and <= '9')
            {
                suffixEnd--;
            }

            if (!typeName.AsSpan(0, suffixEnd).EndsWith("ViewModel".AsSpan(), StringComparison.Ordinal))
            {
                return null;
            }

            return typeName.Remove(suffixEnd - "Model".Length, "Model".Length);
        }

        /// <summary>
        /// Normalizes a field name following the CommunityToolkit ObservableProperty convention:
        /// 1. Trim all leading '_' characters.
        /// 2. If the result starts with "m_", remove that prefix.
        /// </summary>
        internal static ReadOnlySpan<char> NormalizeFieldIdentifier(ReadOnlySpan<char> fieldName)
        {
            var s = fieldName.TrimStart('_');
            if (s.StartsWith("m_"))
            {
                s = s.Slice(2);
            }
            return s;
        }

        /// <summary>
        /// Returns the ViewModel property name derived from a field name (first letter uppercased).
        /// e.g. _count → Count, m_Count → Count, _interactable → Interactable
        /// </summary>
        internal static string ToPropertyName(ReadOnlySpan<char> fieldName)
        {
            var s = NormalizeFieldIdentifier(fieldName);
            if (s.IsEmpty)
            {
                return "";
            }

            var temp = (Span<char>)stackalloc char[s.Length];
            s.CopyTo(temp);
            temp[0] = char.ToUpperInvariant(s[0]);
            return temp.ToString();
        }

        /// <summary>
        /// Returns the constructor parameter name derived from a field name (first letter lowercased).
        /// e.g. _count → count, m_Count → count
        /// </summary>
        internal static string ToParamName(ReadOnlySpan<char> fieldName)
        {
            var s = NormalizeFieldIdentifier(fieldName);
            if (s.IsEmpty)
            {
                return "";
            }

            var temp = (Span<char>)stackalloc char[s.Length];
            s.CopyTo(temp);
            temp[0] = char.ToLowerInvariant(s[0]);
            return temp.ToString();
        }
    }
}