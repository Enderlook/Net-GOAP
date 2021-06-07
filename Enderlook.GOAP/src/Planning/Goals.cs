using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enderlook.GOAP
{
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    internal struct Goals<TWorld, TGoal>
        where TWorld : IWorldState<TWorld>
        where TGoal : IGoal<TWorld>
    {
        private TGoal? single;
        private Array? array;
        private int count;  // This field only track count of array, so it's zero even when single is set.

        private string DebuggerDisplay {
            get {
                if (this.array is null)
                    return string.Concat("[", single!.ToString(), "]");
                StringBuilder builder = new();
                TGoal[] array = Unsafe.As<TGoal[]>(this.array);
                builder.Append('[');
                for (int i = 0; i < count; i++)
                    builder.Append(array[i].ToString()).Append(", ");
                builder.Length -= ", ".Length;
                builder.Append(']');
                return builder.ToString();
            }
        }

        public TGoal Single {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get {
                Debug.Assert(array is null);
                return single!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Goals(TGoal single)
        {
            this.single = single;
            array = default;
            count = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Goals(Array array, int count)
        {
            Debug.Assert(count > 1);
            Debug.Assert(array is not null);
            this.array = array;
            this.count = count;
            single = default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TGoal Peek()
        {
            if (array is null)
                return single!;
            return Unsafe.As<TGoal[]>(array)[count - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Goals<TWorld, TGoal> WithReplacement(TGoal goal)
        {
            if (array is null)
                return new Goals<TWorld, TGoal>(goal);

            Debug.Assert(count > 1);
            Array array_ = Rent(count);
            int countMinusOne = count - 1;
            Array.Copy(array, array_, countMinusOne);
            Unsafe.As<TGoal[]>(array_)[countMinusOne] = goal;
            return new(array_, count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool WithPop(out Goals<TWorld, TGoal> goals)
        {
            if (array is null)
            {
#if NET5_0_OR_GREATER
                Unsafe.SkipInit(out goals);
#else
                goals = default;
#endif
                return false;
            }

            int newCount = count - 1;
            if (newCount > 1)
            {
                Array array_ = Rent(newCount);
                Array.Copy(array, array_, newCount);
                goals = new(array_, newCount);
            }
            else
            {
                Debug.Assert(count == 2);
                goals = new(Unsafe.As<TGoal[]>(array)[0]);
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Goals<TWorld, TGoal> WithPush<TAgent, TAction>(TAgent agent, TGoal goal)
            where TAgent : IAgent<TWorld, TGoal, TAction>
            where TAction : IAction<TWorld, TGoal>
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");
            Debug.Assert(count >= 0);

            if (array is null)
            {
                if (typeof(IGoalMerge<TGoal>).IsAssignableFrom(typeof(TAgent)) && ((IGoalMerge<TGoal>)agent).TryMerge(single!, goal, out TGoal newGoal))
                    return new(newGoal);

                TGoal[] array_ = Unsafe.As<TGoal[]>(Rent(2));
                array_[1] = goal;
                array_[0] = single!;
                return new(array_, 2);
            }
            else
            {
                TGoal[] array = Unsafe.As<TGoal[]>(this.array);
                if (typeof(IGoalMerge<TGoal>).IsAssignableFrom(typeof(TAgent)) && ((IGoalMerge<TGoal>)agent).TryMerge(array[count - 1], goal, out TGoal newGoal))
                {
                    TGoal[] array_ = Unsafe.As<TGoal[]>(Rent(count));
                    Array.Copy(array, array_, count - 1);
                    array_[count - 1] = newGoal;
                    return new(array_, count);
                }
                else
                {
                    TGoal[] array_ = Unsafe.As<TGoal[]>(Rent(count + 1));
                    Array.Copy(array, array_, count);
                    array_[count] = goal;
                    return new(array_, count + 1);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return<TAgent>(TAgent agent)
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");

            if (array is null)
            {
                if (typeof(IGoalPool<TGoal>).IsAssignableFrom(typeof(TAgent)))
                    ((IGoalPool<TGoal>)agent).Return(single!);

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                if (RuntimeHelpers.IsReferenceOrContainsReferences<TGoal>())
#endif
                    single = default;
            }
            else
            {
                if (typeof(IGoalPool<TGoal>).IsAssignableFrom(typeof(TAgent)))
                {
                    TGoal[] goals = Unsafe.As<TGoal[]>(array);
                    if (unchecked((uint)count >= (uint)array.Length))
                    {
                        Debug.Fail("Index out of range.");
                        goto end;
                    }
                    for (int i = 0; i < count; i++)
                        ((IGoalPool<TGoal>)agent).Return(goals[i]);
                }

                end:
                Return(array, count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Array Rent(int capacity)
        {
            if (typeof(TGoal).IsValueType)
                return ArrayPool<TGoal>.Shared.Rent(capacity);
            return ArrayPool<object>.Shared.Rent(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Return(Array array, int count)
        {
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            if (RuntimeHelpers.IsReferenceOrContainsReferences<TGoal>())
#endif
                Array.Clear(array, 0, count);

            if (typeof(TGoal).IsValueType)
                ArrayPool<TGoal>.Shared.Return(Unsafe.As<TGoal[]>(array));
            ArrayPool<object>.Shared.Return(Unsafe.As<object[]>(array));
        }

        public void Append(StringBuilder builder)
        {
            if (array is null)
                builder.Append('[').Append(single!.ToString()).Append(']');
            else
            {
                builder.Append('[');
                for (int i = 0; i < count; i++)
                    builder.Append(Unsafe.As<TGoal[]>(array)[i].ToString());
                builder.Append(']');
            }
        }
    }
}
