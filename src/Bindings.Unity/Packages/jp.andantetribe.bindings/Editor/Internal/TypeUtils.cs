#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEditor;

namespace Bindings.Editor.Internal
{
    internal static class TypeUtils
    {
        private static readonly IComparer<Type?> s_typeComparer = Comparer<Type?>.Create(static (x, y) =>
        {
            return (x, y) switch
            {
                (null, null) => 0,
                (null, _) => 1,
                (_, null) => -1,
                _ => string.CompareOrdinal(x.Namespace, y.Namespace) is var ns and not 0 ? ns : string.CompareOrdinal(x.Name, y.Name)
            };
        });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlyMemory<Type> GetAllViewTypes()
        {
            var collection = TypeCache.GetTypesDerivedFrom<IView>();
            var array = new Type[collection.Count];
            var written = 0;
            foreach (var type in collection)
            {
                if (type is { IsAbstract: false, IsInterface: false, IsGenericType: false } && Attribute.IsDefined(type, typeof(SerializableAttribute)))
                {
                    array[written++] = type;
                }
            }
            Array.Sort(array, s_typeComparer);
            return new ReadOnlyMemory<Type>(array, 0, written);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<Type, Type> GetAllGenericViewTypes()
        {
            var collection = TypeCache.GetTypesDerivedFrom(typeof(IView<>));
            var dict = new Dictionary<Type, Type>(collection.Count, FastTypeEqualityComparer.Default);
            foreach (var type in collection)
            {
                if (type is { IsAbstract: false, IsInterface: false, IsGenericType: false } && Attribute.IsDefined(type, typeof(SerializableAttribute)))
                {
                    var tType = type
                        .GetInterfaces()
                        .First(static t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IView<>))
                        .GetGenericArguments()[0];

                    dict[type] = tType;
                }
            }
            return dict;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this ReadOnlyMemory<Type> types, Type type)
        {
            return types.Span.BinarySearch(type, s_typeComparer) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetNestedTypeName(Type type)
        {
            var container = new NativeArray<char>(16, Allocator.Temp);
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

        private sealed class FastTypeEqualityComparer : IEqualityComparer<Type>
        {
            public static readonly FastTypeEqualityComparer Default = new FastTypeEqualityComparer();

            private FastTypeEqualityComparer()
            {
            }

            public bool Equals(Type x, Type y) => x.TypeHandle.Equals(y.TypeHandle);

            public int GetHashCode(Type x) => x.TypeHandle.GetHashCode();
        }
    }
}