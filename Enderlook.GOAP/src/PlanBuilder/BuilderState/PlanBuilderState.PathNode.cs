﻿using Enderlook.Collections.LowLevel;

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

                builder.Append("(Id:").Append(id);
                if (Mode == Type.Start)
                {
                    Debug.Assert(Parent == -1);
                    builder.Append(" Root");
                }
                else
                {
                    builder.Append(" Parent-Id:").Append(Parent);
                    if (Mode == Type.End)
                        builder.Append(" Leaf");
                }
                builder
                    .Append(" Total-Cost:").Append(cost)
                    .Append(" Current-Action:<");
                if (Action != -1)
                    builder.Append(planBuilder.actionsText[Action]);
                builder
                    .Append("> World-State:<")
                    .Append(World?.ToString() ?? "Null")
                    .Append("> Remaining-Goals:");
                if (Mode == Type.End)
                    builder.Append("[]");
                else
                {
                    builder.Append('[');
                    RawList<GoalNode> goals = planBuilder.goals;
                    GoalNode node = goals[Goals];
                    builder
                        .Append('<')
                        .Append(node.Goal.ToString() ?? "Null");
                    while (node.Previous != -1)
                    {
                        builder.Append(">, <");
                        node = goals[node.Previous];
                        builder.Append(node.Goal.ToString() ?? "Null");
                    }
                    builder.Append(">]");
                }
                builder.Append(')');
                string text = builder.ToString(initialLength, builder.Length - initialLength);
                builder.Length = initialLength;
                return text;
            }
        }
    }
}
