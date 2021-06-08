using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Enderlook.GOAP
{
    /// <summary>
    /// An static thread-safe pool.
    /// </summary>
    /// <typeparam name="T">Type of element to pool.</typeparam>
    internal static class Pool<T> where T : class, new()
    {
        private const int INITAL_CAPACITY = 16;
        private const int GROW_FACTOR = 2;
        private const int SHRINK_THRESHOLD = 4;
        private const int SURVIVE_RATIO = 2;
        private static object[]? pool = ArrayPool<object>.Shared.Rent(INITAL_CAPACITY);
        private static int index = -1;

#pragma warning disable CA1806 // Do not ignore method results
        static Pool() => new AutoPurge();
#pragma warning restore CA1806 // Do not ignore method results

        /// <summary>
        /// Get an instance from the pool or create a new one.
        /// </summary>
        /// <returns>Instance of the element.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Get()
        {
            object[] pool_ = GetPool();

            T item;
            if (index == -1)
                item = new T();
            else
            {
                Debug.Assert(index >= 0 && index < pool_.Length);
                ref object slot = ref Utils.Ref(pool_, index--);
                item = Unsafe.As<T>(slot);
                slot = null!;
            }

            pool = pool_;

            return item;
        }

        /// <summary>
        /// Return an instance to the pool giving its ownership.
        /// </summary>
        /// <param name="item">Element to return to the pool.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Return(T item)
        {
            object[] pool_ = GetPool();

            if (unchecked((uint)index >= (uint)pool_.Length))
                SlowPath();

            Utils.Set<object>(pool_, ++index, item);

            pool = pool_;

            [MethodImpl(MethodImplOptions.NoInlining)]
            void SlowPath()
            {
                object[] newPool = ArrayPool<object>.Shared.Rent(pool_.Length * GROW_FACTOR);
                Array.Copy(pool_, newPool, pool_.Length);
                Utils.Set<object>(pool_, ++index, item);
                pool = pool_;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object[] GetPool()
        {
            object[]? pool_;
            do
            {
                pool_ = Interlocked.Exchange(ref pool, null);
            } while (pool_ is null);
            return pool_;
        }

        private class AutoPurge
        {
            ~AutoPurge()
            {
                GC.ReRegisterForFinalize(this);

#if NET5_0_OR_GREATER
                GCMemoryInfo memoryInfo = GC.GetGCMemoryInfo();
                if (memoryInfo.MemoryLoadBytes < memoryInfo.HighMemoryLoadThresholdBytes * .8)
                    return;
#endif

                object[] pool_ = GetPool();
                int index_ = index;
                if (index_ == -1)
                {
                    if (pool_.Length > INITAL_CAPACITY)
                    {
                        pool = ArrayPool<object>.Shared.Rent(pool_.Length / GROW_FACTOR);
                        ArrayPool<object>.Shared.Return(pool_);
                    }
                    else
                        pool = pool_;
                }
                else
                {
                    int newIndex = index / SURVIVE_RATIO;

                    if (pool_.Length <= INITAL_CAPACITY || pool_.Length / newIndex < SHRINK_THRESHOLD)
                    {
                        Array.Clear(pool_, newIndex - 1, index_ - newIndex);
                        index = newIndex;
                        pool = pool_;
                    }
                    else
                    {
                        object[] newPool = ArrayPool<object>.Shared.Rent(pool_.Length / GROW_FACTOR);
                        Array.Copy(pool_, newPool, newIndex);
                        index = newIndex;
                        pool = newPool;
                        Array.Clear(pool_, 0, index_);
                        ArrayPool<object>.Shared.Return(pool_);
                    }
                }
            }
        }
    }
}
