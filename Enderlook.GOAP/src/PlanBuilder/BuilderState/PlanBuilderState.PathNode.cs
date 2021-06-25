using Enderlook.Collections.LowLevel;

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enderlook.GOAP
{
    internal sealed partial class PlanBuilderState<TWorldState, TGoal, TAction, TActionHandle>
    {
        private struct PathNode
        {
            public int Parent;
            public int Action;
            public TActionHandle? Handle;
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
                Handle = default;
                Mode = Type.Start;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PathNode(int parent, int action, TActionHandle handle)
            {
                Parent = parent;
                Action = action;
                Handle = handle;
                Goals = default;
                World = default;
                Mode = Type.End;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PathNode(int parent, int action, TActionHandle handle, int goals, TWorldState world)
            {
                Parent = parent;
                Action = action;
                Handle = handle;
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

            public string ToLogText(PlanBuilderState<TWorldState, TGoal, TAction, TActionHandle> planBuilder, int id)
            {
                StringBuilder builder = planBuilder.builder;
                int initialLength = builder.Length;
                builder
                    .Append("(I:").Append(id)
                    .Append(" P:").Append(Parent)
                    .Append(" T:").Append(Mode.ToString())
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
