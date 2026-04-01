#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Bindings
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SchemaAttribute : Attribute
    {
        public string BindingPath { get; }

        public SchemaAttribute(string bindingPath)
        {
            BindingPath = bindingPath;
        }

        public SchemaAttribute(object bindingPath, [CallerArgumentExpression("bindingPath")]string path = "")
        {
            const string keyword = "Resolver.";
            BindingPath = path.IndexOf(keyword, StringComparison.Ordinal) is var i && i >= 0 ? path[(i + keyword.Length)..] : path;
        }
    }
}