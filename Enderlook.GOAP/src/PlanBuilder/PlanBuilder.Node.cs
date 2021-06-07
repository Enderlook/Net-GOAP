using System;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal sealed partial class PlanBuilder<TWorld, TGoal, TAction>
    {
        private struct Node
        {
            public int Parent;
            public TAction? Action;
            public int Goals;
            public TWorld? World;
            public Type Mode;

            [Flags]
            public enum Type : byte
            {
                Normal = 1 << 1,
                Start = 1 << 2,
                End = 1 << 3,
                WasDequeued = 1 << 4,
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node(int goals, TWorld world)
            {
                Parent = -1;
                Goals = goals;
                World = world;
                Action = default;
                Mode = Type.Start;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node(int parent, TAction action)
            {
                Parent = parent;
                Action = action;
                Goals = default;
                World = default;
                Mode = Type.End;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Node(int parent, TAction action, int goals, TWorld world)
            {
                Parent = parent;
                Action = action;
                Goals = goals;
                World = world;
                Mode = Type.Normal;
            }

            public void WasDequeue()
            {
                Mode |= Type.WasDequeued;
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                if (RuntimeHelpers.IsReferenceOrContainsReferences<TWorld>())
#endif
                    World = default;
            }

            public string ToLogText(PlanBuilder<TWorld, TGoal, TAction> planBuilder, int id)
            {
                int initialLength = planBuilder.builder.Length;
                planBuilder.builder
                    .Append("(I:").Append(id)
                    .Append(" P:").Append(Parent)
                    .Append(" T:").Append(Mode.ToString())
                    .Append(" A:").Append(Action?.ToString() ?? "<>")
                    .Append(" M:").Append(World?.ToString() ?? "<>")
                    .Append(" G:");
                if (Mode == Type.End)
                    planBuilder.builder.Append("<>");
                else
                {
                    planBuilder.builder.Append('[');
                    GoalNode node = planBuilder.goals[Goals];
                    planBuilder.builder.Append(node.Goal.ToString());
                    while (node.Previous != -1)
                    {
                        node = planBuilder.goals[node.Previous];
                        planBuilder.builder.Append(node.Goal.ToString());
                    }
                    planBuilder.builder.Append(']');
                }
                planBuilder.builder.Append(')');
                string text = planBuilder.builder.ToString(initialLength, planBuilder.builder.Length - initialLength);
                planBuilder.builder.Length = initialLength;
                return text;
            }
        }
    }
}
