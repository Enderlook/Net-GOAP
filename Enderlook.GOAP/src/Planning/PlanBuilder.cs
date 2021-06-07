using Enderlook.Collections;
using Enderlook.Collections.LowLevel;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Enderlook.GOAP
{
    internal sealed class PlanBuilder<TWorld, TGoal, TAction>
        where TWorld : IWorldState<TWorld>
        where TGoal : IGoal<TWorld>
        where TAction : IAction<TWorld, TGoal>
    {
        private RawList<Node> nodes = RawList<Node>.Create();
        private BinaryHeapMin<int, float> toVisit = new();

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
                AppendToLog(count);
                Log();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void EnqueueGoal<TLog>(TGoal goal, TWorld world)
        {
            if (Toggle.IsOn<TLog>())
                builder.Append("Enqueue Goal: ");
            Enqueue<TLog>(new(new(goal), world), 0);
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
        internal void Enqueue<TLog>(int parent, TAction action, float cost, Goals<TWorld, TGoal> goals, TWorld world)
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
            Enqueue<TLog>(new(parent, action, new(goal), world), cost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool TryDequeue<TAgent, TLog>(out int id, out float cost, out Goals<TWorld, TGoal> goals, [MaybeNullWhen(false)] out TWorld world)
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");

            if (toVisit.TryDequeue(out id, out cost))
            {
                ref Node node = ref nodes[id];

                if (Toggle.IsOn<TLog>())
                {
                    builder.Append("Dequeue Success: ");
                    AppendToLog(id);
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
                Log();
            }

            int index = endNode;
            int lastIndex = index;
            while (true)
            {
                ref Node node = ref nodes[index];
                index = node.Parent;
                if (index == -1)
                {
                    goal = node.Goals.Single;
                    break;
                }
                lastIndex = index;
                actions.Push(node.Action!);
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
                            node.Goals.Return(agent);
                        }
                        else
                        {
                            Debug.Assert((node.Mode & (Node.Type.End)) != 0);
                            node.Goals.Return(agent);
                        }
                    }
                }

                state = default;
                endNode = default;
                nodes.Clear();
                toVisit.Clear();
                nodesText.Clear();
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

        private struct Node
        {
            public int Parent;
            public TAction? Action;
            public Goals<TWorld, TGoal> Goals;
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
            public Node(Goals<TWorld, TGoal> goal, TWorld world)
            {
                Parent = -1;
                Goals = goal;
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
            public Node(int parent, TAction action, Goals<TWorld, TGoal> goals, TWorld world)
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
                Goals = default;
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
                    Goals.Append(planBuilder.builder);
                planBuilder.builder.Append(')');
                string text = planBuilder.builder.ToString(initialLength, planBuilder.builder.Length - initialLength);
                planBuilder.builder.Length = initialLength;
                return text;
            }
        }
    }
}
