#nullable enable

using System;

namespace Bindings
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class ViewModelAttribute : Attribute
    {
        public readonly bool OverrideBindImplement;

        public ViewModelAttribute(bool overrideBindImplement = false)
        {
            OverrideBindImplement = overrideBindImplement;
        }
    }
}