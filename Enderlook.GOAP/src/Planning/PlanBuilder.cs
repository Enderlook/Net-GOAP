using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP.Planning
{
    /// <summary>
    /// This type is an implementation detail an shall never be used in parameters, fields, variables or return types.<br/>
    /// It should only be used in chaining calls.
    /// </summary>
    public readonly ref struct PlanBuilder<TWorldState, TGoal, TAction, TActions>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
        where TAction : IAction<TWorldState, TGoal>
        where TActions : IEnumerable<TAction>
    {
        private readonly Plan<TGoal, TAction> plan;
        private readonly TWorldState worldState;
        private readonly TActions actions;
        private readonly Action<string>? log;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PlanBuilder(Plan<TGoal, TAction> plan, TWorldState worldState, TActions actions, Action<string>? log)
        {
            if (plan is null)
                ThrowHelper.ThrowArgumentNullException_Plan();
            if (worldState is null)
                ThrowHelper.ThrowArgumentNullException_WorldState();
            if (actions is null)
                ThrowHelper.ThrowArgumentNullException_Actions();

            this.plan = plan;
            this.worldState = worldState;
            this.actions = actions;
            this.log = log;
        }

        /// <summary>
        /// The GOAP will try to complete a single goal.
        /// </summary>
        /// <param name="goal">Goal to satify.</param>
        /// <returns>Instance of the builder for the plan.<br/>
        /// Shall only be used for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when instance is default.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="goal"/> is <see langword="null"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlanBuilderGoal<TWorldState, TGoal, SingleGoal<TGoal>, TAction, TActions> CompleteGoal(TGoal goal)
        {
            if (plan is null)
                ThrowHelper.ThrowArgumentException_InstanceIsDefault();
            return new(plan, worldState, actions, log, new(goal));
        }

        /// <summary>
        /// The GOAP will try to complete the goal with lower cost.
        /// </summary>
        /// <param name="goals">Goal to try to satify. Only the goals whose cost is lower will be satisfied.</param>
        /// <returns>Instance of the builder for the plan.<br/>
        /// Shall only be used for method chaining.</returns>
        /// <exception cref="ArgumentException">Thrown when instance is default.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="goals"/> is <see langword="null"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlanBuilderGoal<TWorldState, TGoal, CheapestGoal<TGoal>, TAction, TActions> CompleteCheapestGoalOf(IEnumerable<TGoal> goals)
        {
            if (plan is null)
                ThrowHelper.ThrowArgumentException_InstanceIsDefault();
            return new(plan, worldState, actions, log, new(goals));
        }
    }
}