#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Bindings.Internal
{
    internal readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Default = default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Unit _) => true;

        public override string ToString() => "()";
    }
}