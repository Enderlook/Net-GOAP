using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal struct PlanWithoutPoolingMergeGoal<TWorld, TAction, TGoal> :
        IAgent<TWorld, TGoal, TAction>,
        IGoalMerge<TGoal>
        where TWorld : IWorldState<TWorld>
        where TAction : IAction<TWorld, TGoal>
        where TGoal : IGoal<TWorld>
    {
        private readonly IAgent<TWorld, TGoal, TAction> plan;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlanWithoutPoolingMergeGoal(IAgent<TWorld, TGoal, TAction> plan)
        {
            Debug.Assert(typeof(IGoalMerge<TGoal>).IsAssignableFrom(plan.GetType()));
            this.plan = plan;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<TAction> GetActions() => plan.GetActions();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<TGoal> GetGoals() => plan.GetGoals();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TWorld GetWorldState() => plan.GetWorldState();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryMerge(TGoal a, TGoal b, out TGoal c) => Unsafe.As<IGoalMerge<TGoal>>(plan).TryMerge(a, b, out c);
    }
}
