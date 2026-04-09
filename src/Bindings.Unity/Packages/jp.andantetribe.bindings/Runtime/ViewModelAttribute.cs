#nullable enable

using System;

namespace Bindings
{
    /// <summary>
    /// Indicates that the class or struct is a ViewModel.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class ViewModelAttribute : Attribute
    {
        /// <summary>
        /// If true, the source generator will not generate the Bind method and will report an error if it is not implemented by the user.
        /// </summary>
        public readonly bool RequireBindImplementation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewModelAttribute"/> class.
        /// </summary>
        /// <param name="requireBindImplementation">If true, the source generator will not generate the Bind method and will report an error if it is not implemented by the user.</param>
        public ViewModelAttribute(bool requireBindImplementation = false)
        {
            RequireBindImplementation = requireBindImplementation;
        }
    }
}