#nullable enable

using NUnit.Framework;

namespace Bindings.Tests
{
    public sealed class SchemaAttributeTests
    {
        [TestCase("Resolver.UnityEngine.GameObject.name", "UnityEngine.GameObject.name")]
        [TestCase("global::Bindings.PathResolver.UnityEngine.GameObject.name", "UnityEngine.GameObject.name")]
        [TestCase("Custom.Path", "Custom.Path")]
        [TestCase("", "")]
        public void SchemaExpressionExtractsPathAndPreservesMetadata(string expression, string expected)
        {
            var schema = new SchemaAttribute(0, 4, "F2", "tooltip", expression);
            Assert.That(schema.BindingPath, Is.EqualTo(expected));
            Assert.That(schema.Id, Is.EqualTo(4));
            Assert.That(schema.Format, Is.EqualTo("F2"));
            Assert.That(schema.Tooltip, Is.EqualTo("tooltip"));
            var literal = new SchemaAttribute(expected, 4, "F2", "tooltip");
            Assert.That(literal.BindingPath, Is.EqualTo(expected));
            Assert.That(literal.Id, Is.EqualTo(schema.Id));
            Assert.That(literal.Format, Is.EqualTo(schema.Format));
            Assert.That(literal.Tooltip, Is.EqualTo(schema.Tooltip));
        }
    }
}