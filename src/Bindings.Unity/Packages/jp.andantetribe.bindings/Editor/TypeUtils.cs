#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEditor;

namespace Bindings.Editor
{
    internal static class TypeUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type[] GetAllViewTypes()
        {
            var list = new List<Type>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<IView>())
            {
                if (type is { IsAbstract: false, IsInterface: false, IsGenericType: false } && Attribute.IsDefined(type, typeof(SerializableAttribute)))
                {
                    list.Add(type);
                }
            }
            list.Sort(static (x, y) => string.Compare(x.Namespace, y.Namespace, StringComparison.Ordinal));
            return list.ToArray();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetNestedTypeName(Type type)
        {
            var container = new NativeArray<char>(64, Allocator.Temp);
            var written = 0;
            try
            {
                Write(type, ref container, ref written);
                return container.AsSpan()[..written].ToString();
            }
            finally
            {
                container.Dispose();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Write(Type t, ref NativeArray<char> container, ref int written)
            {
                if (t.DeclaringType != null)
                {
                    Write(t.DeclaringType, ref container, ref written);
                    Append(ref container, ref written, ".");
                }
                Append(ref container, ref written, t.Name);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Append(ref NativeArray<char> container, ref int written, ReadOnlySpan<char> literal)
            {
                if (literal.TryCopyTo(container.AsSpan()[written..]))
                {
                    written += literal.Length;
                    return;
                }
                var newSize = container.Length + literal.Length;
                var newContainer = new NativeArray<char>(newSize, Allocator.Temp);
                container.AsSpan().CopyTo(newContainer);
                literal.CopyTo(newContainer.AsSpan()[written..]);
                written += literal.Length;
                container.Dispose();
                container = newContainer;
            }
        }
    }
}