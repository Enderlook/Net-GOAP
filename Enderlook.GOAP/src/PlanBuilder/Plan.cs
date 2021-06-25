using Enderlook.Collections.LowLevel;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Stores the result of a planification.
    /// </summary>
    /// <typeparam name="TGoal">Type of goals.</typeparam>
    /// <typeparam name="TAction">Type of actions.</typeparam>
    public sealed class Plan<TGoal, TAction>
    {
        private PlanMode mode;

        private TGoal? goal;
        private int goalIndex;
        private float cost;
        private RawList<int> plan = RawList<int>.Create();
        private RawList<TAction> actions = RawList<TAction>.Create();

        /// <summary>
        /// <see langword="true"/> if it does has a plan.
        /// </summary>
        public bool FoundPlan {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => mode == PlanMode.FoundPlan;
        }

        /// <summary>
        /// <see langword="true"/> if it planification was cancelled.
        /// </summary>
        public bool WasCancelled {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => mode == PlanMode.Cancelled;
        }

        /// <summary>
        /// <see langword="true"/> if it is planifying.
        /// </summary>
        public bool IsInProgress {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => mode == PlanMode.InProgress;
        }

        /// <summary>
        /// Get the cost of the plan.
        /// </summary>
        /// <returns>Cost of the plan.</returns>
        /// <exception cref="InvalidOperationException">Thrown when there is no plan.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPlanCost()
        {
            if (mode != PlanMode.FoundPlan)
                HasNoPlan();
            return cost;
        }

        /// <summary>
        /// Get the goal that the plan will archive.
        /// </summary>
        /// <returns>Goal to archive.</returns>
        /// <exception cref="InvalidOperationException">Thrown when there is no plan.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TGoal GetGoal()
        {
            if (mode != PlanMode.FoundPlan)
                HasNoPlan();

            Debug.Assert(goal is not null);
            return goal!;
        }

        /// <summary>
        /// Get the goal index that the plan will archive.
        /// </summary>
        /// <returns>Goal to archive. The index represent the element from passed enumeration.</returns>
        /// <exception cref="InvalidOperationException">Thrown when there is no plan.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetGoalIndex()
        {
            if (mode != PlanMode.FoundPlan)
                HasNoPlan();

            Debug.Assert(goal is not null);
            return goalIndex;
        }

        /// <summary>
        /// Get the amount of actions required by this plan.
        /// </summary>
        /// <returns>Amount of actions required by the plan.</returns>
        /// <exception cref="InvalidOperationException">Thrown when there is no plan.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetActionsCount()
        {
            if (mode != PlanMode.FoundPlan)
                HasNoPlan();

            return plan.Count;
        }

        /// <summary>
        /// Get the index of the action to execute at index <paramref name="step"/>.
        /// </summary>
        /// <param name="step">Number of action from plan.</param>
        /// <returns>Index of the action to execute. The index correspond to elements from the passed enumeration.</returns>
        /// <exception cref="InvalidOperationException">Thrown when there is no plan.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="step"/> is negative, equal or higher than <see cref="GetActionsCount"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetActionIndex(int step)
        {
            if (mode != PlanMode.FoundPlan)
                HasNoPlan();
            return plan[step];
        }

        /// <summary>
        /// Get the action to execute at index <paramref name="step"/>.
        /// </summary>
        /// <param name="step">Number of action from plan.</param>
        /// <returns>Action to execute.</returns>
        /// <exception cref="InvalidOperationException">Thrown when there is no plan.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="step"/> is negative, equal or higher than <see cref="GetActionsCount"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TAction GetAction(int step)
        {
            if (mode != PlanMode.FoundPlan)
                HasNoPlan();
            return actions[plan[step]];
        }

        private void HasNoPlan() => throw new InvalidOperationException(mode switch
        {
            PlanMode.Cancelled => "Planification was cancelled.",
            PlanMode.NotFound => "Plan was not found.",
            PlanMode.None => "This instance was never used to build a plan.",
            PlanMode.InProgress => "Plan is being building."
        });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetCancelled()
        {
            Debug.Assert(mode == PlanMode.InProgress);
            mode = PlanMode.Cancelled;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetInProgress()
        {
            Debug.Assert(mode != PlanMode.InProgress);
            mode = PlanMode.InProgress;
            plan.Clear();
            actions.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetNotFound()
        {
            Debug.Assert(mode == PlanMode.InProgress);
            mode = PlanMode.NotFound;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetFound(ref RawList<TAction> actions, float cost, int goalIndex, TGoal goal)
        {
            this.cost = cost;
            this.goalIndex = goalIndex;
            this.goal = goal;
            TAction[] array = this.actions.UnderlyingArray;
            this.actions = actions;
            actions = RawList<TAction>.FromEmpty(array);
            mode = PlanMode.FoundPlan;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AddActionToPlan(int actionIndex)
        {
            Debug.Assert(mode == PlanMode.InProgress);
            plan.Add(actionIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int PlanCountInternal() => plan.Count;
    }
}
