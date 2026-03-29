using System;

namespace Bindings
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ModelAttribute : Attribute
    {
    }
}