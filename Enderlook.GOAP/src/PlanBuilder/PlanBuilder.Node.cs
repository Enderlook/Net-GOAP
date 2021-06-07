using Enderlook.Collections.LowLevel;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

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
                StringBuilder builder = planBuilder.builder;
                int initialLength = builder.Length;
                builder
                    .Append("(I:").Append(id)
                    .Append(" P:").Append(Parent)
                    .Append(" T:").Append(Mode.ToString())
                    .Append(" A:").Append(Action?.ToString() ?? "<>")
                    .Append(" M:").Append(World?.ToString() ?? "<>")
                    .Append(" G:");
                if (Mode == Type.End)
                    builder.Append("<>");
                else
                {
                    builder.Append('[');
                    RawList<GoalNode> goals = planBuilder.goals;
                    GoalNode node = goals[Goals];
                    builder.Append(node.Goal.ToString());
                    while (node.Previous != -1)
                    {
                        node = goals[node.Previous];
                        builder.Append(node.Goal.ToString());
                    }
                    builder.Append(']');
                }
                builder.Append(')');
                string text = builder.ToString(initialLength, builder.Length - initialLength);
                builder.Length = initialLength;
                return text;
            }
        }
    }
}
