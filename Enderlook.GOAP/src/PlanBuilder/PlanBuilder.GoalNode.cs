using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal sealed partial class PlanBuilder<TWorld, TGoal, TAction>
    {
        public readonly struct GoalNode
        {
            public readonly TGoal Goal;
            public readonly int Previous;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private GoalNode(TGoal goal, int previous)
            {
                Goal = goal;
                Previous = previous;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int WithPush(PlanBuilder<TWorld, TGoal, TAction> builder, int currentIndex, TGoal goal)
            {
                int id = builder.goals.Count;
                builder.goals.Add(new(goal, currentIndex));
                return id;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool WithPop(out int index)
            {
                if (Previous == -1)
                {
#if NET5_0_OR_GREATER
                    Unsafe.SkipInit(out index);
#else
                    index = default;
#endif
                    return false;
                }
                index = Previous;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int WithReplacement(PlanBuilder<TWorld, TGoal, TAction> builder, TGoal goal)
            {
                int id = builder.goals.Count;
                builder.goals.Add(new(goal, Previous));
                return id;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int Create(PlanBuilder<TWorld, TGoal, TAction> builder, TGoal goal)
            {
                int id = builder.goals.Count;
                builder.goals.Add(new(goal, -1));
                return id;
            }
        }
    }
}
