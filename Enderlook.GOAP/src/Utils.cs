using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Enderlook.GOAP
{
    internal static class Utils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Ref<T>(T[] array, int index)
        {
            Debug.Assert(array is not null);
            Debug.Assert(unchecked((uint)index < array.Length));
#if NET5_0_OR_GREATER
            return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
#else
            return ref array[index];
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Ref<T>(Array array, int index)
        {
            Debug.Assert(array is not null);
            Debug.Assert(unchecked((uint)index < array.Length));
            if (typeof(T).IsValueType)
            {
                Debug.Assert(array.GetType() == typeof(T[]));
                return ref Ref(Unsafe.As<T[]>(array), index);
            }
            else
            {
                Debug.Assert(array.GetType() == typeof(object[]));
#if NET5_0_OR_GREATER
                return ref Unsafe.Add(ref Unsafe.As<object, T>(ref MemoryMarshal.GetArrayDataReference(Unsafe.As<object[]>(array))), index);
#else
                return ref Unsafe.As<object, T>(ref Unsafe.As<object[]>(array)[index]);
#endif
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<T>(T[] array, int index, T element)
        {
            Debug.Assert(array is not null);
            Debug.Assert(unchecked((uint)index < array.Length));
#if NET5_0_OR_GREATER
            Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index) = element;
#else
            array[index] = element;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set<T>(Array array, int index, T element)
        {
            Debug.Assert(array is not null);
            Debug.Assert(unchecked((uint)index < array.Length));
            if (typeof(T).IsValueType)
            {
                Debug.Assert(array.GetType() == typeof(T[]));
                Set(Unsafe.As<T[]>(array), index, element);
            }
            else
            {
                Debug.Assert(array.GetType() == typeof(object[]));
                Unsafe.As<T[]>(array)[index] = element;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(T[] array, int index)
        {
            Debug.Assert(array is not null);
            Debug.Assert(unchecked((uint)index < array.Length));
#if NET5_0_OR_GREATER
            return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
#else
            return array[index];
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get<T>(Array array, int index)
        {
            Debug.Assert(array is not null);
            Debug.Assert(unchecked((uint)index < array.Length));
            if (typeof(T).IsValueType)
            {
                Debug.Assert(array.GetType() == typeof(T[]));
                return Get(Unsafe.As<T[]>(array), index);
            }
            else
            {
                Debug.Assert(array.GetType() == typeof(object[]));
                return Unsafe.As<T[]>(array)[index];
            }
        }
    }
}
