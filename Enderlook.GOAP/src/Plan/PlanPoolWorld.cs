﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal struct PlanPoolWorld<TWorld, TAction, TGoal> :
        IAgent<TWorld, TGoal, TAction>,
        IWorldStatePool<TWorld>
        where TWorld : IWorldState<TWorld>
        where TAction : IAction<TWorld, TGoal>
        where TGoal : IGoal<TWorld>
    {
        private readonly IAgent<TWorld, TGoal, TAction> plan;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlanPoolWorld(IAgent<TWorld, TGoal, TAction> plan)
        {
            Debug.Assert(typeof(IWorldStatePool<TWorld>).IsAssignableFrom(plan.GetType()));
            this.plan = plan;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<TAction> GetActions() => plan.GetActions();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<TGoal> GetGoals() => plan.GetGoals();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TWorld GetWorldState() => plan.GetWorldState();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TWorld Clone(TWorld value) => Unsafe.As<IWorldStatePool<TWorld>>(plan).Clone(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(TWorld value) => Unsafe.As<IWorldStatePool<TWorld>>(plan).Return(value);
    }
}
