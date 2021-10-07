using Enderlook.Collections.LowLevel;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enderlook.GOAP
{
    internal sealed partial class PlanBuilderState<TWorldState, TGoal, TAction>
    {
        private struct PathNode
        {
            public int Parent;
            public int Action;
            public int Goals;
            public TWorldState? World;
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
            public PathNode(int goals, TWorldState world)
            {
                Parent = -1;
                Goals = goals;
                World = world;
                Action = -1;
                Mode = Type.Start;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PathNode(int parent, int action)
            {
                Parent = parent;
                Action = action;
                Goals = default;
                World = default;
                Mode = Type.End;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PathNode(int parent, int action, int goals, TWorldState world)
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
                if (RuntimeHelpers.IsReferenceOrContainsReferences<TWorldState>())
#endif
                    World = default;
            }

            public string ToLogText(PlanBuilderState<TWorldState, TGoal, TAction> planBuilder, int id, float cost)
            {
                StringBuilder? builder = planBuilder.builder;
                Debug.Assert(builder is not null);
                int initialLength = builder.Length;
                builder
                    .Append("(I:").Append(id)
                    .Append(" P:").Append(Parent)
                    .Append(" T:").Append(Mode.ToString())
                    .Append(" C:").Append(cost)
                    .Append(" A:").Append(Action == -1 ? "<>" : planBuilder.actionsText[Action])
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
                        builder.Append(", ");
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
