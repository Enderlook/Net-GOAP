using Enderlook.GOAP.Watchdogs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Enderlook.GOAP.Planning
{
    /// <summary>
    /// This type is an implementation detail an shall never be used in parameters, fields, variables or return types.<br/>
    /// It should only be used in chaining calls.
    /// </summary>
    public readonly struct PlanBuilderHelper<TWorldState, TGoal, TGoals, TAction, TActions, TWatchdog, THelper>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
        where TAction : IAction<TWorldState, TGoal>
        where TActions : IEnumerable<TAction>
        where TWatchdog : IWatchdog
    {
        private readonly Plan<TGoal, TAction> plan;
        private readonly TWorldState worldState;
        private readonly TActions actions;
        private readonly TGoals goals;
        private readonly Action<string>? log;
        private readonly TWatchdog watchdog;
        private readonly THelper helper;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PlanBuilderHelper(Plan<TGoal, TAction> plan, TWorldState worldState, TActions actions, TGoals goals, Action<string>? log, TWatchdog watchdog, THelper helper)
        {
            Debug.Assert(plan is not null);
            Debug.Assert(worldState is not null);
            Debug.Assert(actions is not null);
            Debug.Assert(goals is not null);
            Debug.Assert(watchdog is not null);

            if (helper is null)
                ThrowHelper.ThrowArgumentNullException_Helper();

            Type helperType = typeof(THelper).IsValueType ? typeof(THelper) : helper.GetType();
            if (!typeof(IGoalPool<TGoal>).IsAssignableFrom(helperType) &&
                !typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(helperType) &&
                !typeof(IGoalMerge<TGoal>).IsAssignableFrom(helperType))
                ThrowHelper.ThrowArgumentException_HelperTypeMistmach();

            this.plan = plan;
            this.worldState = worldState;
            this.actions = actions;
            this.goals = goals;
            this.log = log;
            this.watchdog = watchdog;
            this.helper = helper;
        }

        /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActions}.Execute"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Plan<TGoal, TAction> Execute()
        {
            if (plan is null)
                ThrowHelper.ThrowArgumentException_InstanceIsDefault();
            DebugAssert();

            Debug.Assert(helper is not null);
            Type helperType = typeof(THelper).IsValueType ? typeof(THelper) : helper.GetType();

            bool goalPool = typeof(IGoalPool<TGoal>).IsAssignableFrom(helperType);
            bool worldStatePool = typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(helperType);
            bool goalMerge = typeof(IGoalMerge<TGoal>).IsAssignableFrom(helperType);

            if (goalPool)
            {
                if (worldStatePool)
                {
                    if (goalMerge)
                        Planner.RunAndDispose<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        Planner.RunAndDispose<AgentWrapperPoolGoalPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else if (goalMerge)
                    Planner.RunAndDispose<AgentWrapperPoolGoalMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                    Planner.RunAndDispose<AgentWrapperPoolGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
            }
            else if (worldStatePool)
            {
                if (goalMerge)
                    Planner.RunAndDispose<AgentWrapperPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                    Planner.RunAndDispose<AgentWrapperPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
            }
            else if (goalMerge)
                Planner.RunAndDispose<AgentWrapperMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                    new(worldState, goals, actions, helper), plan, watchdog, log);
            else
                ThrowHelper.ThrowArgumentException_HelperTypeMistmach();
            return plan;
        }

        /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActions}.ExecuteAsync"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<Plan<TGoal, TAction>> ExecuteAsync()
        {
            if (plan is null)
                ThrowHelper.ThrowArgumentException_InstanceIsDefault();
            DebugAssert();

            Debug.Assert(helper is not null);
            Type helperType = typeof(THelper).IsValueType ? typeof(THelper) : helper.GetType();

            bool goalPool = typeof(IGoalPool<TGoal>).IsAssignableFrom(helperType);
            bool worldStatePool = typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(helperType);
            bool goalMerge = typeof(IGoalMerge<TGoal>).IsAssignableFrom(helperType);

            if (goalPool)
            {
                if (worldStatePool)
                {
                    if (goalMerge)
                        return Planner.RunAndDisposeAsync<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return Planner.RunAndDisposeAsync<AgentWrapperPoolGoalPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else if (goalMerge)
                    return Planner.RunAndDisposeAsync<AgentWrapperPoolGoalMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                    return Planner.RunAndDisposeAsync<AgentWrapperPoolGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
            }
            else if (worldStatePool)
            {
                if (goalMerge)
                    return Planner.RunAndDisposeAsync<AgentWrapperPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                    return Planner.RunAndDisposeAsync<AgentWrapperPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
            }
            else if (goalMerge)
                return Planner.RunAndDisposeAsync<AgentWrapperMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                    new(worldState, goals, actions, helper), plan, watchdog, log);
            else
            {
                ThrowHelper.ThrowArgumentException_HelperTypeMistmach();
                return default;
            }
        }

        /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActions}.ExecuteCoroutine"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlanningCoroutine<TGoal, TAction> ExecuteCoroutine()
        {
            if (plan is null)
                ThrowHelper.ThrowArgumentException_InstanceIsDefault();
            DebugAssert();

            Debug.Assert(helper is not null);
            Type helperType = typeof(THelper).IsValueType ? typeof(THelper) : helper.GetType();

            bool goalPool = typeof(IGoalPool<TGoal>).IsAssignableFrom(helperType);
            bool worldStatePool = typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(helperType);
            bool goalMerge = typeof(IGoalMerge<TGoal>).IsAssignableFrom(helperType);

            if (goalPool)
            {
                if (worldStatePool)
                {
                    if (goalMerge)
                        return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoalPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else if (goalMerge)
                    return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoalMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                    return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
            }
            else if (worldStatePool)
            {
                if (goalMerge)
                    return Planner.RunAndDisposeCoroutine<AgentWrapperPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                    return Planner.RunAndDisposeCoroutine<AgentWrapperPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
            }
            else if (goalMerge)
                return Planner.RunAndDisposeCoroutine<AgentWrapperMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                    new(worldState, goals, actions, helper), plan, watchdog, log);
            else
            {
                ThrowHelper.ThrowArgumentException_HelperTypeMistmach();
                return default;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("DEBUG")]
        private void DebugAssert()
        {
            Debug.Assert(plan is not null);
            Debug.Assert(worldState is not null);
            Debug.Assert(actions is not null);
            Debug.Assert(goals is not null);
            Debug.Assert(watchdog is not null);
            Debug.Assert(helper is not null);
        }
    }
}
