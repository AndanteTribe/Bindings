#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Bindings
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SchemaAttribute : Attribute
    {
        public readonly string BindingPath;

        public readonly string Format;

        public SchemaAttribute(string bindingPath, string format = "")
        {
            BindingPath = bindingPath;
            Format = format;
        }

        public SchemaAttribute(object bindingPath, string format = "", [CallerArgumentExpression("bindingPath")]string path = "")
        {
            const string keyword = "Resolver.";
            BindingPath = path.IndexOf(keyword, StringComparison.Ordinal) is var i && i >= 0 ? path[(i + keyword.Length)..] : path;
            Format = format;
        }
    }
}