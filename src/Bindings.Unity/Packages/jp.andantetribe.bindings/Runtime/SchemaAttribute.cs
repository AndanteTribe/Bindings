#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Bindings
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class SchemaAttribute : Attribute
    {
        public readonly string BindingPath;

        public readonly int Id;

        public readonly string Format;

        public SchemaAttribute(string bindingPath, int id = -1, string format = "")
        {
            BindingPath = bindingPath;
            Id = id;
            Format = format;
        }

        public SchemaAttribute(object bindingPath, int id = -1, string format = "", [CallerArgumentExpression("bindingPath")]string path = "")
        {
            const string keyword = "Resolver.";
            BindingPath = path.IndexOf(keyword, StringComparison.Ordinal) is var i && i >= 0 ? path[(i + keyword.Length)..] : path;
            Id = id;
            Format = format;
        }
    }
}