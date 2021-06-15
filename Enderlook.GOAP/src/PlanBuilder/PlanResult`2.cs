using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Stores the result of a planification.
    /// </summary>
    /// <typeparam name="TGoal"></typeparam>
    /// <typeparam name="TAction"></typeparam>
    public readonly struct PlanResult<TGoal, TAction>
    {
        /// <summary>
        /// A cancelled plan.
        /// </summary>
        public static PlanResult<TGoal, TAction> Cancelled {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(PlanResultMode.Cancelled, default, default, default);
        }

        /// <summary>
        /// A not found plan.
        /// </summary>
        public static PlanResult<TGoal, TAction> NotFound {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(PlanResultMode.NotFound, default, default, default);
        }

        private readonly PlanResultMode result;
        private readonly float cost;
        private readonly TGoal? goal;
        private readonly Stack<TAction>? plan;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PlanResult(PlanResultMode result, Stack<TAction>? plan, TGoal? goal, float cost)
        {
            this.result = result;
            this.cost = cost;
            this.goal = goal;
            this.plan = plan;
            if (result == PlanResultMode.FoundPlan)
            {
                Debug.Assert(goal is not null);
                Debug.Assert(plan is not null);
            }
        }

        /// <summary>
        /// <see langword="true"/> if it does has a plan.
        /// </summary>
        public bool FoundPlan => result == PlanResultMode.FoundPlan;

        /// <summary>
        /// <see langword="true"/> if it planification was cancelled.
        /// </summary>
        public bool WasCancelled => result == PlanResultMode.Cancelled;

        /// <summary>
        /// Try to get the stored plan, if any.
        /// </summary>
        /// <param name="plan">Plan of action to execute.</param>
        /// <param name="goal">Goal that is being reached.</param>
        /// <param name="cost">Total cost of actions.</param>
        /// <returns><see langword="true"/> if a plan was found. <see langword="false"/> if no plan was found.</returns>
        public bool TryGetPlan([MaybeNullWhen(false)] out Stack<TAction> plan, [MaybeNullWhen(false)] out TGoal goal, out float cost)
        {
            if (result == PlanResultMode.FoundPlan)
            {
                Debug.Assert(this.plan is not null);
                Debug.Assert(this.goal is not null);
                plan = this.plan;
                goal = this.goal;
                cost = this.cost;
                return true;
            }
            plan = default;
            goal = default;
            cost = default;
            return false;
        }
    }
}
