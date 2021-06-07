using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal struct Toggle
    {
        internal struct Yes { }
        internal struct No { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOn<T>()
        {
            Debug.Assert(typeof(T) == typeof(Yes) || typeof(T) == typeof(No));
            return typeof(T) == typeof(Yes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("DEBUG")]
        public static void Assert<T>() => Debug.Assert(typeof(T) == typeof(Yes) || typeof(T) == typeof(No));
    }
}
