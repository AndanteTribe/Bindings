#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Bindings
{
    /// <summary>
    /// Supplies binding metadata for a ViewModel field or method to the Bindings source generator.
    /// </summary>
    /// <remarks>
    /// For a field, the generator creates a ViewModel property and updates the selected Unity component member when
    /// the view binds. For a method, the generator registers the method as a listener on the selected Unity event.
    /// Apply this attribute more than once to declare multiple bindings for the same ViewModel member.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SchemaAttribute : Attribute
    {
        /// <summary>
        /// Gets the fully qualified component member path consumed by the source generator.
        /// </summary>
        public readonly string BindingPath;

        /// <summary>
        /// Gets the component-sharing identifier, or <c>-1</c> when the generator assigns a component automatically.
        /// </summary>
        /// <remarks>
        /// Schema entries with the same component type and a non-negative ID share one generated View field.
        /// </remarks>
        public readonly int Id;

        /// <summary>
        /// Gets the format string used for <c>TMPro.TMP_Text.text</c> field bindings.
        /// </summary>
        /// <remarks>The generator ignores this value for other binding paths and for method bindings.</remarks>
        public readonly string Format;

        /// <summary>
        /// Gets the tooltip applied to the generated serialized component field in the Unity Inspector.
        /// </summary>
        public readonly string Tooltip;

        /// <summary>
        /// Initializes a schema declaration from a binding path string.
        /// </summary>
        /// <param name="bindingPath">
        /// The fully qualified component member path, such as <c>TMPro.TMP_Text.text</c>.
        /// </param>
        /// <param name="id">
        /// A non-negative identifier for sharing a generated component field, or <c>-1</c> for automatic assignment.
        /// </param>
        /// <param name="format">The format string for a <c>TMPro.TMP_Text.text</c> field binding.</param>
        /// <param name="tooltip">The Unity Inspector tooltip for the generated component field.</param>
        public SchemaAttribute(string bindingPath, int id = -1, string format = "", string tooltip = "")
        {
            BindingPath = bindingPath;
            Id = id;
            Format = format;
            Tooltip = tooltip;
        }

        /// <summary>
        /// Initializes a schema declaration by deriving its binding path from the caller's argument expression.
        /// </summary>
        /// <param name="bindingPath">An attribute-compatible expression whose source text identifies the binding path.</param>
        /// <param name="id">
        /// A non-negative identifier for sharing a generated component field, or <c>-1</c> for automatic assignment.
        /// </param>
        /// <param name="format">The format string for a <c>TMPro.TMP_Text.text</c> field binding.</param>
        /// <param name="tooltip">The Unity Inspector tooltip for the generated component field.</param>
        /// <param name="path">
        /// The compiler-captured expression for <paramref name="bindingPath"/>. Callers should not pass this argument.
        /// </param>
        public SchemaAttribute(object bindingPath, int id = -1, string format = "", string tooltip = "", [CallerArgumentExpression("bindingPath")] string path = "")
        {
            const string keyword = "Resolver.";
            BindingPath = path.IndexOf(keyword, StringComparison.Ordinal) is var i && i >= 0 ? path[(i + keyword.Length)..] : path;
            Id = id;
            Format = format;
            Tooltip = tooltip;
        }
    }
}
