using System;

namespace Bindings
{
    internal readonly struct Unit : IEquatable<Unit>
    {
        public static readonly Unit Default = default(Unit);

        public override int GetHashCode() => 0;

        public bool Equals(Unit _) => true;

        public override string ToString() => "()";
    }
}