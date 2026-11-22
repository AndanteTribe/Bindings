#nullable enable

using System;
using UnityEngine;

namespace Bindings.Editor.Internal
{
    internal static class Slot
    {
        public static ISlot? Create(string memberName, Type valueType)
        {
            // Only guard types that cannot be generic arguments of Slot<T>.
            if (valueType is { IsPointer: false, IsByRef: false, IsByRefLike: false })
            {
                return (ISlot)Activator.CreateInstance(typeof(Slot<>).MakeGenericType(valueType), memberName)!;
            }
            return null;
        }
    }

    [Serializable]
    internal sealed class Slot<T> : ISlot
    {
        public T Value = default!;

        [SerializeField]
        private string _memberName;

        public string MemberName => _memberName;

        object? ISlot.BoxedValue
        {
            get => Value;
            set => Value = (T)value!;
        }

        public Slot(string memberName) => _memberName = memberName;
    }

    internal interface ISlot
    {
        string MemberName { get; }
        object? BoxedValue { get; set; }
    }
}