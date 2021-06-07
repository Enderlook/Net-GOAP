using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal struct Toggle
    {
        internal struct Yes { }
        internal struct No { }

        // AggrssiveInlining is very important here for constant propagation.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOn<T>()
        {
            Assert<T>();
            return typeof(T) == typeof(Yes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("DEBUG")]
        public static void Assert<T>() => Debug.Assert(typeof(T) == typeof(Yes) || typeof(T) == typeof(No));
    }
}
