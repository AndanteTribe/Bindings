#nullable enable

using System;

namespace Bindings
{
    /// <summary>
    /// Marks a partial class or struct as input for the Bindings source generator.
    /// </summary>
    /// <remarks>
    /// The annotated type name must contain <c>ViewModel</c>. The generator augments that type with
    /// <see cref="IViewModel"/> and generates a corresponding serializable View type whose name replaces
    /// <c>ViewModel</c> with <c>View</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class ViewModelAttribute : Attribute
    {
        /// <summary>
        /// Indicates whether the generated View omits its default <see cref="IView.BindAsync"/> implementation.
        /// </summary>
        /// <remarks>
        /// When <see langword="true"/>, implement <see cref="IView.BindAsync"/> in another part of the generated
        /// View class.
        /// </remarks>
        public readonly bool RequireBindImplementation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelAttribute"/> class.
        /// </summary>
        /// <param name="requireBindImplementation">
        /// <see langword="true"/> to require a user-written <see cref="IView.BindAsync"/> implementation;
        /// otherwise, <see langword="false"/> to generate the default binding implementation.
        /// </param>
        public ViewModelAttribute(bool requireBindImplementation = false)
        {
            RequireBindImplementation = requireBindImplementation;
        }
    }
}
