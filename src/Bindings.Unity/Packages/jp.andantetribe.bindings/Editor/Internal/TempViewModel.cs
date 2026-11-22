#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Bindings.Editor.Internal
{
    [Serializable]
    internal sealed class TempViewModel
    {
        private const BindingFlags InstanceMembers = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [SerializeField]
        private string _typeName;

        [SerializeReference]
        internal List<ISlot> _slots = new();

        public Type ViewModelType => Type.GetType(_typeName, throwOnError: true)!;

        public TempViewModel(Type type)
        {
            _typeName = type.AssemblyQualifiedName
                ?? throw new ArgumentException($"Preview requires an assembly-qualified type name for '{type}'.", nameof(type));
            var properties = new HashSet<string>();
            foreach (var property in type.GetProperties(InstanceMembers).AsSpan())
            {
                if ((property.SetMethod?.IsPublic == true || property.IsDefined(typeof(RequiredAttribute)))
                    && property.GetIndexParameters().Length == 0
                    && Slot.Create(property.Name, property.PropertyType) is { } slot)
                {
                    _slots.Add(slot);
                    _ = properties.Add(property.Name);
                }
            }
            foreach (var field in type.GetFields(InstanceMembers).AsSpan())
            {
                if (!field.IsPublic && !field.IsDefined(typeof(RequiredAttribute)))
                {
                    continue;
                }
                if (field.IsDefined(typeof(SchemaAttribute)) && properties.Contains(SchemaPropertyName(field)))
                {
                    continue;
                }
                if (Slot.Create(field.Name, field.FieldType) is { } slot)
                {
                    _slots.Add(slot);
                }
            }
        }

        public void LoadDefaults(SerializedProperty slots, IMvvmPublisher publisher)
        {
            var type = ViewModelType;
            var (constructor, arguments) = PrepareConstructor(type, publisher);
            var model = constructor.Invoke(arguments);
            for (var i = _slots.Count - 1; i >= 0; i--)
            {
                if (slots.GetArrayElementAtIndex(i).FindPropertyRelative(nameof(Slot<int>.Value)) == null)
                {
                    _slots.RemoveAt(i);
                    continue;
                }
                var slot = _slots[i];
                var member = type.GetMember(slot.MemberName, InstanceMembers).Single();
                slot.BoxedValue = member switch
                {
                    PropertyInfo { CanRead: true } property => property.GetValue(model),
                    FieldInfo field => field.GetValue(model),
                    _ when TryGetConstructorArgIndex(constructor, member, out var index) => arguments[index],
                    _ => slot.BoxedValue
                };
            }
        }

        public IViewModel CreateViewModel(IMvvmPublisher publisher)
        {
            var type = ViewModelType;
            var (constructor, arguments) = PrepareConstructor(type, publisher);
            var assignments = new List<(MemberInfo Member, object? Value)>();
            foreach (var slot in _slots)
            {
                var member = type.GetMember(slot.MemberName, InstanceMembers).Single();
                if (TryGetConstructorArgIndex(constructor, member, out var index))
                {
                    arguments[index] = slot.BoxedValue;
                }
                // A generated setter is still applied when its backing field supplies a constructor argument.
                if (!member.IsDefined(typeof(RequiredAttribute)))
                {
                    assignments.Add((member, slot.BoxedValue));
                }
            }
            var model = (IViewModel)constructor.Invoke(arguments);
            foreach (var (member, value) in assignments)
            {
                if (member is FieldInfo field)
                {
                    field.SetValue(model, value);
                }
                else
                {
                    ((PropertyInfo)member).SetValue(model, value);
                }
            }
            return model;
        }

        private static ReadOnlySpan<char> NormalizeName(ReadOnlySpan<char> memberName)
        {
            var name = memberName.TrimStart('_');
            return name.StartsWith("m_".AsSpan(), StringComparison.Ordinal) ? name[2..] : name;
        }

        private static string SchemaPropertyName(FieldInfo field)
        {
            var memberName = field.Name;
            var name = NormalizeName(memberName.AsSpan());
            var first = char.ToUpperInvariant(name[0]);
            if (name.Length == memberName.Length && first == name[0])
            {
                return memberName;
            }
            return string.Create(name.Length, (memberName, first), static (destination, state) =>
            {
                state.memberName.AsSpan(state.memberName.Length - destination.Length).CopyTo(destination);
                destination[0] = state.first;
            });
        }

        private static bool TryGetConstructorArgIndex(ConstructorInfo constructor, MemberInfo member, out int index)
        {
            index = default;
            if (!member.IsDefined(typeof(RequiredAttribute)))
            {
                if (member is not PropertyInfo property)
                {
                    return false;
                }
                var field = Array.Find(
                    property.DeclaringType!.GetFields(InstanceMembers),
                    field =>
                        field.IsDefined(typeof(RequiredAttribute))
                        && field.IsDefined(typeof(SchemaAttribute))
                        && SchemaPropertyName(field) == property.Name);
                if (field == null)
                {
                    return false;
                }
                member = field;
            }
            var name = NormalizeName(member.Name.AsSpan());
            var first = char.ToLowerInvariant(name[0]);
            foreach (var parameter in (ReadOnlySpan<ParameterInfo>)constructor.GetParameters())
            {
                var candidate = parameter.Name.AsSpan();
                if (candidate.Length == name.Length && candidate[0] == first && candidate[1..].SequenceEqual(name[1..]))
                {
                    index = parameter.Position;
                    return true;
                }
            }
            throw new InvalidOperationException($"No constructor parameter '{first}{name[1..].ToString()}' for required member '{constructor.DeclaringType}.{member.Name}'.");
        }

        private static (ConstructorInfo Constructor, object?[] Arguments) PrepareConstructor(Type viewModelType, IMvvmPublisher publisher)
        {
            var constructor = viewModelType.GetConstructors().Single();
            var parameters = (ReadOnlySpan<ParameterInfo>)constructor.GetParameters();
            if (parameters.Length == 0 || parameters[^1].ParameterType != typeof(IMvvmPublisher))
            {
                throw new InvalidOperationException($"Preview expects a constructor ending in IMvvmPublisher on {constructor.DeclaringType}.");
            }
            var arguments = new object?[parameters.Length];
            for (var i = 0; i < parameters.Length - 1; i++)
            {
                var type = parameters[i].ParameterType;
                arguments[i] = type switch
                {
                    _ when type == typeof(string) => "",
                    { IsArray: true } => Array.CreateInstance(type.GetElementType()!, new int[type.GetArrayRank()]),
                    { IsInterface: true } => InterfacePreviewService.Create(type),
                    _ when typeof(UnityEngine.Object).IsAssignableFrom(type) => null,
                    { IsAbstract: true } => AbstractPreviewService.Create(type),
                    _ when type.IsValueType || type.GetConstructor(Type.EmptyTypes) != null => Activator.CreateInstance(type),
                    _ => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type)
                };
            }
            arguments[^1] = publisher;
            return (constructor, arguments);
        }
    }
}