using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Type used to allow enable features using generics.
    /// </summary>
    internal struct Toggle
    {
        /// <summary>
        /// The given feature is enabled.
        /// </summary>
        internal struct Yes { }

        /// <summary>
        /// The given feature is disasbled.
        /// </summary>
        internal struct No { }

        /// <summary>
        /// Determines if the feature is enabled.
        /// </summary>
        /// <typeparam name="T"><see cref="Yes"/> or <see cref="No"/>.</typeparam>
        /// <returns>Whenever the feature is enabled.</returns>
        // AggrssiveInlining is very important here for constant propagation.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOn<T>()
        {
            Assert<T>();
            return typeof(T) == typeof(Yes);
        }

        /// <summary>
        /// Asserts that <typeparamref name="T"/> is of type <see cref="Yes"/> or <see cref="No"/>.
        /// </summary>
        /// <typeparam name="T"><see cref="Yes"/> or <see cref="No"/>.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("DEBUG")]
        public static void Assert<T>() => Debug.Assert(typeof(T) == typeof(Yes) || typeof(T) == typeof(No));
    }
}
