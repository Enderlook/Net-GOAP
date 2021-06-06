using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal struct PlanWithoutPooling<TWorld, TAction, TGoal> :
        IAgent<TWorld, TGoal, TAction>
        where TWorld : IWorldState<TWorld>
        where TAction : IAction<TWorld, TGoal>
        where TGoal : IGoal<TWorld>
    {
        private readonly IAgent<TWorld, TGoal, TAction> plan;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlanWithoutPooling(IAgent<TWorld, TGoal, TAction> plan) => this.plan = plan;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<TAction> GetActions() => plan.GetActions();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<TGoal> GetGoals() => plan.GetGoals();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TWorld GetWorldState() => plan.GetWorldState();
    }
}
