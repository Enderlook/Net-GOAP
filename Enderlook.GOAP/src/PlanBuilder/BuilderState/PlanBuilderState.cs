using Enderlook.Collections;
using Enderlook.Collections.LowLevel;
using Enderlook.GOAP.Utilities;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enderlook.GOAP
{
    internal sealed partial class PlanBuilderState<TWorldState, TGoal, TAction>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
    {
        private RawList<PathNode> nodes = RawList<PathNode>.Create();
        private BinaryHeapMin<int, float> toVisit = new();
        private RawList<GoalNode> goals = RawList<GoalNode>.Create();
        private RawList<TAction> actions = RawList<TAction>.Create();

        private State state;
        private int endNode;
        private float cost;

        private RawList<string> nodesText = RawList<string>.Create();
        private RawList<string> actionsText = RawList<string>.Create();
        private StringBuilder builder = new();
        private Action<string>? log;

        [Flags]
        private enum State : byte
        {
            Cancelled,
            Found,
            Finalized,
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddAction<TLog>(TAction action)
        {
            Debug.Assert(action is not null);
            Toggle.Assert<TLog>();
            actions.Add(action);
            if (Toggle.IsOn<TLog>())
                actionsText.Add(action.ToString() ?? "<Null>");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ActionsCount() => actions.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int NodesCount() => nodes.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetActionText(int index) => actionsText[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TAction GetAction(int index) => actions[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GoalNode GetGoal(int index) => goals[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Cancel<TLog>()
        {
            state |= State.Cancelled;
            if (Toggle.IsOn<TLog>())
                AppendAndLog("Cancelled.");
        }

        internal void Finalize<TAgent, TLog>(Plan<TGoal, TAction> plan)
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");
            Debug.Assert(plan is not null);

            builder.Append("Finalized planning.\n");
            if ((state & State.Cancelled) != 0)
            {
                if (Toggle.IsOn<TLog>())
                {
                    builder.Append("Cancelled: ");
                    builder.Append("\nTotal actions enqueued: ").Append(nodes.Count).Append('.');
                    builder.Append("\nTotal goals stored: ").Append(goals.Count).Append('.');
                    builder.Append("\nRemaining nodes to visit: ").Append(toVisit.Count).Append('.');
                }
                plan.SetCancelled();
            }

            if ((state & State.Found) == 0)
            {
                if (Toggle.IsOn<TLog>())
                {
                    builder.Append("Not found: ");
                    builder.Append("\nTotal actions enqueued: ").Append(nodes.Count).Append('.');
                    builder.Append("\nTotal goals stored: ").Append(goals.Count).Append('.');
                    builder.Append("\nRemaining nodes to visit: ").Append(toVisit.Count).Append('.');
                }
                plan.SetNotFound();
            }

            if (Toggle.IsOn<TLog>())
            {
                builder.Append("Found:\n    ");
                AppendToLogNode(endNode);
                builder.Append("\nTotal cost of plan ").Append(cost).Append('.');
                builder.Append("\nTotal actions enqueued: ").Append(nodes.Count).Append('.');
                builder.Append("\nTotal goals stored: ").Append(goals.Count).Append('.');
                builder.Append("\nRemaining nodes to visit: ").Append(toVisit.Count).Append('.');
            }

            int index = endNode;
            int lastIndex = index;
            int goalIndex;
            while (true)
            {
                ref PathNode node = ref nodes[index];
                index = node.Parent;
                if (index == -1)
                {
                    goalIndex = node.Goals;
                    break;
                }
                lastIndex = index;

                plan.AddActionToPlan(node.Action);
            }

            if (Toggle.IsOn<TLog>())
            {
                builder.Append("\nLength of plan: ").Append(plan.PlanCountInternal()).Append('.');
                Log();
            }

            endNode = lastIndex;

            state |= State.Finalized;

            plan.SetFound(ref actions, cost, goalIndex, goals[goalIndex].Goal);
        }

        public void Clear<TAgent>(TAgent agent)
        {
            Debug.Assert(agent is not null);
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");

            if ((state & State.Found) == 0 || (state & State.Finalized) == 0)
                Clear<Toggle.No>(0);
            else
                Clear<Toggle.Yes>(endNode);

            void Clear<TIgnore>(int ignore)
            {
                bool poolMemory = typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent));

                for (int i = 0; i < nodes.Count; i++)
                {
                    PathNode node = nodes[i];
                    if (Toggle.IsOn<TIgnore>() && i == ignore)
                    {
                        Debug.Assert(node.Mode == PathNode.Type.Start);
                        if (poolMemory)
                            ((IWorldStatePool<TWorldState>)agent).Return(node.World!);
                    }
                    else
                    {
                        if ((node.Mode & PathNode.Type.WasDequeued) != 0)
                            continue;

                        if ((node.Mode & (PathNode.Type.Start | PathNode.Type.Normal)) != 0)
                        {
                            if (poolMemory)
                                ((IWorldStatePool<TWorldState>)agent).Return(node.World!);
                        }
                        else
                            Debug.Assert((node.Mode & (PathNode.Type.End)) != 0);
                    }
                }

                if (typeof(IGoalPool<TGoal>).IsAssignableFrom(typeof(TAgent)))
                    for (int i = 0; i < goals.Count; i++)
                        ((IGoalPool<TGoal>)agent).Return(goals[i].Goal);

                state = default;
                endNode = default;
                nodes.Clear();
                toVisit.Clear();
                nodesText.Clear();
                actionsText.Clear();
                goals.Clear();
                log = default;
            }
        }
    }
}
