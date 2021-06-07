﻿using Enderlook.Collections;
using Enderlook.Collections.LowLevel;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enderlook.GOAP
{
    internal sealed partial class PlanBuilder<TWorld, TGoal, TAction>
        where TWorld : IWorldState<TWorld>
        where TGoal : IGoal<TWorld>
        where TAction : IAction<TWorld, TGoal>
    {
        private RawList<Node> nodes = RawList<Node>.Create();
        private BinaryHeapMin<int, float> toVisit = new();

        private RawList<GoalNode> goals = RawList<GoalNode>.Create();

        private State state;
        private int endNode;
        private float cost;

        private RawList<string> nodesText = RawList<string>.Create();
        private StringBuilder builder = new();
        public Action<string>? log;

        [Flags]
        private enum State : byte
        {
            Cancelled,
            Found,
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Enqueue<TLog>(Node node, float cost)
        {
            int count = nodes.Count;
            toVisit.Enqueue(count, cost);
            nodes.Add(node);

            if (Toggle.IsOn<TLog>())
            {
                nodesText.Add(node.ToLogText(this, count));
                builder.Append(nodesText[count]);
                Log();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EnqueueGoal<TLog>(TGoal goal, TWorld world)
        {
            if (Toggle.IsOn<TLog>())
                builder.Append("Enqueue Goal: ");
            Enqueue<TLog>(new(GoalNode.Create(this, goal), world), 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EnqueueValidPath<TLog>(int parent, TAction action, float cost)
        {
            if (Toggle.IsOn<TLog>())
                builder.Append("Enqueue Valid Path: ");
            endNode = nodes.Count;
            this.cost = cost;
            state |= State.Found;
            Enqueue<TLog>(new(parent, action), cost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Enqueue<TLog>(int parent, TAction action, float cost, int goals, TWorld world)
        {
            if (Toggle.IsOn<TLog>())
                builder.Append("Enqueue: ");
            Enqueue<TLog>(new(parent, action, goals, world), cost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Enqueue<TLog>(int parent, TAction action, float cost, TGoal goal, TWorld world)
        {
            if (Toggle.IsOn<TLog>())
                builder.Append("Enqueue: ");
            Enqueue<TLog>(new(parent, action, GoalNode.Create(this, goal), world), cost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GoalNode GetGoal(int index) => goals[index];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryDequeue<TAgent, TLog>(out int id, out float cost, out int goals, [MaybeNullWhen(false)] out TWorld world)
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");

            if (toVisit.TryDequeue(out id, out cost))
            {
                ref Node node = ref nodes[id];

                if (Toggle.IsOn<TLog>())
                {
                    builder.Append("Dequeue Success: ");
                    builder.Append(nodesText[id]);
                    Log();
                }

                if (typeof(IWorldStatePool<TWorld>).IsAssignableFrom(typeof(TAgent)))
                    node.WasDequeue();

                if (node.Mode == Node.Type.End)
                {
                    endNode = id;
                    this.cost = cost;

#if NET5_0_OR_GREATER
                    Unsafe.SkipInit(out goals);
                    Unsafe.SkipInit(out world);
#else
                    goals = default;
                    world = default;
#endif

                    return false;
                }

                goals = node.Goals;
                world = node.World!;
                return true;
            }

            if (Toggle.IsOn<TLog>())
                AppendAndLog("Dequeue Failed. Reason: Empty.");

#if NET5_0_OR_GREATER
            Unsafe.SkipInit(out goals);
            Unsafe.SkipInit(out world);
#else
            goals = default;
            world = default;
#endif
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Cancel<TLog>()
        {
            state |= State.Cancelled;
            if (Toggle.IsOn<TLog>())
                AppendAndLog("Cancelled.");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal PlanResult Finalize<TAgent, TLog>(TAgent agent, Stack<TAction> actions, out TGoal? goal, out float cost)
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");

            if ((state & State.Found) == 0)
            {
                goal = default;
                cost = 0;
                Clear<Toggle.No>(0);
                return (state & State.Cancelled) != 0 ? PlanResult.Cancelled : PlanResult.NotFound;
            }

            cost = this.cost;

            if (Toggle.IsOn<TLog>())
            { 
                builder.Append("Finalized: ");
                AppendToLog(endNode);
                builder.Append("\nTotal cost of plan ").Append(cost).Append('.');
                builder.Append("\nTotal actions enqueued: ").Append(nodes.Count).Append('.');
                builder.Append("\nTotal goals stored: ").Append(goals.Count).Append('.');
                builder.Append("\nRemaining nodes to visit: ").Append(toVisit.Count).Append('.');
            }

            int actionsCount = actions.Count;

            int index = endNode;
            int lastIndex = index;
            while (true)
            {
                ref Node node = ref nodes[index];
                index = node.Parent;
                if (index == -1)
                {
                    goal = goals[node.Goals].Goal;
                    break;
                }
                lastIndex = index;
                actions.Push(node.Action!);
            }

            if (Toggle.IsOn<TLog>())
            {
                builder.Append("\nLength of plan ").Append(actions.Count - actionsCount).Append('.');
                Log();
            }

            Clear<Toggle.Yes>(lastIndex);

            return (state & State.Cancelled) != 0 ? PlanResult.CancelledButFound :  PlanResult.FoundPlan;

            void Clear<TIgnore>(int ignore)
            {
                bool poolMemory = typeof(IWorldStatePool<TWorld>).IsAssignableFrom(typeof(TAgent));

                for (int i = 0; i < nodes.Count; i++)
                {
                    Node node = nodes[i];
                    if (Toggle.IsOn<TIgnore>() && i == ignore)
                    {
                        Debug.Assert(node.Mode == Node.Type.Start);
                        if (poolMemory)
                            ((IWorldStatePool<TWorld>)agent).Return(node.World!);
                    }
                    else
                    {
                        if ((node.Mode & Node.Type.WasDequeued) != 0)
                            continue;

                        if ((node.Mode & (Node.Type.Start | Node.Type.Normal)) != 0)
                        {
                            if (poolMemory)
                                ((IWorldStatePool<TWorld>)agent).Return(node.World!);
                        }
                        else
                            Debug.Assert((node.Mode & (Node.Type.End)) != 0);
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
                goals.Clear();
                log = default;
            }
        }

        private void AppendToLog(int id)
        {
            builder.Append(nodesText[id]);
            Node node = nodes[id];
            while (node.Mode != Node.Type.Start)
            {
                id = node.Parent;
                node = nodes[id];
                builder.Append(" -> ").Append(nodesText[id]);
            }
            builder.Append('.');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log()
        {
            log(builder.ToString());
            builder.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendAndLog(string message)
        {
            builder.Append(message);
            Log();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendToLog(string message) => builder.Append(message);
    }
}