#nullable enable

using System;

namespace Bindings
{
    /// <summary>
    /// Adds the annotated ViewModel field or property to the source-generated constructor and assigns the corresponding
    /// argument to that member.
    /// </summary>
    /// <remarks>
    /// Constructor parameters are emitted in member declaration order, before the generated
    /// <see cref="IMvvmPublisher"/> parameter.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RequiredAttribute : Attribute
    {
    }
}
