using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal sealed partial class PlanBuilderState<TWorldState, TGoal, TAction>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Enqueue<TLog>(PathNode node, float cost)
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
        public void EnqueueGoal<TLog>(TGoal goal, TWorldState world)
        {
            if (Toggle.IsOn<TLog>())
                builder.Append("Enqueue Goal: ");
            Enqueue<TLog>(new(GoalNode.Create(this, goal), world), 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnqueueValidPath<TLog>(int parent, int action, float cost)
        {
            if (Toggle.IsOn<TLog>())
                builder.Append("Enqueue Valid Path: ");
            endNode = nodes.Count;
            this.cost = cost;
            state |= State.Found;
            Enqueue<TLog>(new(parent, action), cost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue<TLog>(int parent, int action, float cost, int goals, TWorldState world)
        {
            if (Toggle.IsOn<TLog>())
                builder.Append("Enqueue: ");
            Enqueue<TLog>(new(parent, action, goals, world), cost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Enqueue<TLog>(int parent, int action, float cost, TGoal goal, TWorldState world)
        {
            if (Toggle.IsOn<TLog>())
                builder.Append("Enqueue: ");
            Enqueue<TLog>(new(parent, action, GoalNode.Create(this, goal), world), cost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryDequeue<TAgent, TLog>(out int id, out float cost, out int goals, [MaybeNullWhen(false)] out TWorldState world)
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");

            if (toVisit.TryDequeue(out id, out cost))
            {
                ref PathNode node = ref nodes[id];

                if (Toggle.IsOn<TLog>())
                {
                    builder.Append("Dequeue Success: ");
                    builder.Append(nodesText[id]);
                    Log();
                }

                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    node.WasDequeue();

                if (node.Mode == PathNode.Type.End)
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
    }
}
