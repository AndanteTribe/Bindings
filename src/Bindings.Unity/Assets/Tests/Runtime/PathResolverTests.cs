#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Bindings.Tests
{
    public sealed class PathResolverTests
    {
        // World-space text and TMP implementation components are outside the uGUI binding surface.
        private static readonly Type[] s_excludedTypes =
        {
            typeof(TextMeshPro), typeof(TextContainer), typeof(TMP_SubMesh), typeof(TMP_SubMeshUI),
            typeof(TMP_ScrollbarEventHandler), typeof(TMP_SelectionCaret), typeof(TMP_SpriteAnimator)
        };

        private static bool IsUiType(Type type) => type.Namespace is "UnityEngine.UI" or "TMPro";

        private static bool IsComponentOrComponentArray(Type type)
        {
            while (type.IsArray)
            {
                type = type.GetElementType()!;
            }

            return typeof(Component).IsAssignableFrom(type);
        }

        private static IEnumerable<Type> ComponentTypes =>
            new[] { typeof(Selectable).Assembly, typeof(TMP_Text).Assembly }
                .SelectMany(static assembly => assembly.GetExportedTypes())
                .Where(static type => IsUiType(type) && typeof(Component).IsAssignableFrom(type) && !s_excludedTypes.Contains(type))
                .OrderBy(static type => type.FullName);

        private static IEnumerable<string> ExpectedMembers(Type type)
        {
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsUiType(property.DeclaringType!)
                    && property.GetGetMethod() != null
                    && property.GetSetMethod() != null
                    && property.GetIndexParameters().Length == 0
                    && !IsComponentOrComponentArray(property.PropertyType))
                {
                    yield return property.Name;
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (IsUiType(field.DeclaringType!)
                    && !field.IsInitOnly
                    && !IsComponentOrComponentArray(field.FieldType))
                {
                    yield return field.Name;
                }
            }
        }

        [TestCaseSource(nameof(ComponentTypes))]
        public void UiPathsMatchPublicReadWritePropertiesAndWritableFields(Type componentType)
        {
            var expected = ExpectedMembers(componentType).Distinct().OrderBy(name => name).ToArray();
            var resolverType = typeof(PathResolver).Assembly.GetType(
                typeof(PathResolver).FullName + "+" + componentType.FullName!.Replace('.', '+'));
            if (resolverType == null)
            {
                Assert.That(expected, Is.Empty, $"Missing resolver type: {componentType.FullName}");
                return;
            }

            var fields = resolverType.GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                .Where(field =>
                {
                    var path = (string)field.GetRawConstantValue()!;
                    return path.StartsWith("UnityEngine.UI.", StringComparison.Ordinal)
                        || path.StartsWith("TMPro.", StringComparison.Ordinal);
                }).ToArray();

            Assert.That(
                fields.Select(field => field.Name).OrderBy(name => name),
                Is.EqualTo(expected),
                "Update PathResolver.tt for " + componentType.FullName + ", then regenerate PathResolver.cs.");

            foreach (var field in fields)
            {
                var path = (string)field.GetRawConstantValue()!;
                var separator = path.LastIndexOf('.');
                var owner = componentType.Assembly.GetType(path[..separator])
                    ?? typeof(Selectable).Assembly.GetType(path[..separator]);
                Assert.That(owner, Is.Not.Null, path);
                Assert.That(owner!.IsAssignableFrom(componentType), Is.True, path);
                Assert.That(path[(separator + 1)..], Is.EqualTo(field.Name));
                Assert.That(owner.GetMember(field.Name, BindingFlags.Public | BindingFlags.Instance), Is.Not.Empty, path);
            }
        }
    }
}