using Enderlook.GOAP.Watchdogs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Enderlook.GOAP.Planning
{
    /// <summary>
    /// This type is an implementation detail an shall never be used in parameters, fields, variables or return types.<br/>
    /// It should only be used in chaining calls.
    /// </summary>
    public readonly struct PlanBuilderHelper<TWorldState, TGoal, TGoals, TAction, TActionHandle, TActions, TWatchdog, THelper>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
        where TAction : IAction<TWorldState, TGoal, TActionHandle>
        where TActions : IEnumerable<TAction>
        where TWatchdog : IWatchdog
    {
        private readonly Plan<TGoal, TAction, TActionHandle> plan;
        private readonly TWorldState worldState;
        private readonly TActions actions;
        private readonly TGoals goals;
        private readonly Action<string>? log;
        private readonly TWatchdog watchdog;
        private readonly THelper helper;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal PlanBuilderHelper(Plan<TGoal, TAction, TActionHandle> plan, TWorldState worldState, TActions actions, TGoals goals, Action<string>? log, TWatchdog watchdog, THelper helper)
        {
            Debug.Assert(plan is not null);
            Debug.Assert(worldState is not null);
            Debug.Assert(actions is not null);
            Debug.Assert(goals is not null);
            Debug.Assert(watchdog is not null);

            if (helper is null)
                ThrowNullHelperException();

            Type helperType = typeof(THelper).IsValueType ? typeof(THelper) : helper.GetType();
            if (!typeof(IGoalPool<TGoal>).IsAssignableFrom(helperType) &&
                !typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(helperType) &&
                !typeof(IGoalMerge<TGoal>).IsAssignableFrom(helperType))
                ThrowTypeMistmachException();

            this.plan = plan;
            this.worldState = worldState;
            this.actions = actions;
            this.goals = goals;
            this.log = log;
            this.watchdog = watchdog;
            this.helper = helper;
            [DoesNotReturn]
            static void ThrowNullHelperException() => throw new ArgumentNullException(nameof(helper));
        }

        /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActionHandle, TActions}.Execute"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Plan<TGoal, TAction, TActionHandle> Execute()
        {
            if (plan is null)
                Planner.ThrowInstanceIsDefault();
            DebugAssert();

            Debug.Assert(helper is not null);
            Type helperType = typeof(THelper).IsValueType ? typeof(THelper) : helper.GetType();

            bool goalPool = typeof(IGoalPool<TGoal>).IsAssignableFrom(helperType);
            bool worldStatePool = typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(helperType);
            bool goalMerge = typeof(IGoalMerge<TGoal>).IsAssignableFrom(helperType);
            bool actionHandlePool = typeof(IActionHandlePool<TActionHandle>).IsAssignableFrom(helperType);

            if (goalPool)
            {
                if (worldStatePool)
                {
                    if (goalMerge)
                    {
                        if (actionHandlePool)
                            Planner.RunAndDispose<AgentWrapperPoolGoalPoolWorldMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                        else
                            Planner.RunAndDispose<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    }
                    else
                    {
                        if (actionHandlePool)
                            Planner.RunAndDispose<AgentWrapperPoolGoalPoolWorldPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                        else
                            Planner.RunAndDispose<AgentWrapperPoolGoalPoolWorld<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    }
                }
                else if (goalMerge)
                {
                    if (actionHandlePool)
                        Planner.RunAndDispose<AgentWrapperPoolGoalMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        Planner.RunAndDispose<AgentWrapperPoolGoalMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else
                {
                    if (actionHandlePool)
                        Planner.RunAndDispose<AgentWrapperPoolGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        Planner.RunAndDispose<AgentWrapperPoolGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
            }
            else if (worldStatePool)
            {
                if (goalMerge)
                {
                    if (actionHandlePool)
                        Planner.RunAndDispose<AgentWrapperPoolWorldMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        Planner.RunAndDispose<AgentWrapperPoolWorldMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else
                {
                    if (actionHandlePool)
                        Planner.RunAndDispose<AgentWrapperPoolWorldPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        Planner.RunAndDispose<AgentWrapperPoolWorld<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
            }
            else if (goalMerge)
            {
                if (actionHandlePool)
                    Planner.RunAndDispose<AgentWrapperMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                    Planner.RunAndDispose<AgentWrapperMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
            }
            else if (actionHandlePool)
                Planner.RunAndDispose<AgentWrapperPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                    new(worldState, goals, actions, helper), plan, watchdog, log);
            else
                ThrowTypeMistmachException();
            return plan;
        }

        /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActionHandle, TActions}.ExecuteAsync"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask<Plan<TGoal, TAction, TActionHandle>> ExecuteAsync()
        {
            if (plan is null)
                Planner.ThrowInstanceIsDefault();
            DebugAssert();

            Debug.Assert(helper is not null);
            Type helperType = typeof(THelper).IsValueType ? typeof(THelper) : helper.GetType();

            bool goalPool = typeof(IGoalPool<TGoal>).IsAssignableFrom(helperType);
            bool worldStatePool = typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(helperType);
            bool goalMerge = typeof(IGoalMerge<TGoal>).IsAssignableFrom(helperType);
            bool actionHandlePool = typeof(IActionHandlePool<TActionHandle>).IsAssignableFrom(helperType);

            if (goalPool)
            {
                if (worldStatePool)
                {
                    if (goalMerge)
                    {
                        if (actionHandlePool)
                            return Planner.RunAndDisposeAsync<AgentWrapperPoolGoalPoolWorldMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                        else
                            return Planner.RunAndDisposeAsync<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    }
                    else
                    {
                        if (actionHandlePool)
                            return Planner.RunAndDisposeAsync<AgentWrapperPoolGoalPoolWorldPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                        else
                            return Planner.RunAndDisposeAsync<AgentWrapperPoolGoalPoolWorld<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    }
                }
                else if (goalMerge)
                {
                    if (actionHandlePool)
                        return Planner.RunAndDisposeAsync<AgentWrapperPoolGoalMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return Planner.RunAndDisposeAsync<AgentWrapperPoolGoalMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else
                {
                    if (actionHandlePool)
                        return Planner.RunAndDisposeAsync<AgentWrapperPoolGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return Planner.RunAndDisposeAsync<AgentWrapperPoolGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
            }
            else if (worldStatePool)
            {
                if (goalMerge)
                {
                    if (actionHandlePool)
                        return Planner.RunAndDisposeAsync<AgentWrapperPoolWorldMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                             new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return Planner.RunAndDisposeAsync<AgentWrapperPoolWorldMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                             new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else
                {
                    if (actionHandlePool)
                        return Planner.RunAndDisposeAsync<AgentWrapperPoolWorldPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return Planner.RunAndDisposeAsync<AgentWrapperPoolWorld<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
            }
            else if (goalMerge)
            {
                if (actionHandlePool)
                    return Planner.RunAndDisposeAsync<AgentWrapperMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                    return Planner.RunAndDisposeAsync<AgentWrapperMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
            }
            else if (actionHandlePool)
                return Planner.RunAndDisposeAsync<AgentWrapperPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                    new(worldState, goals, actions, helper), plan, watchdog, log);
            else
            {
                ThrowTypeMistmachException();
                return default;
            }
        }

        /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActionHandle, TActions}.ExecuteCoroutine"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PlanningCoroutine<TGoal, TAction, TActionHandle> ExecuteCoroutine()
        {
            if (plan is null)
                Planner.ThrowInstanceIsDefault();
            DebugAssert();

            Debug.Assert(helper is not null);
            Type helperType = typeof(THelper).IsValueType ? typeof(THelper) : helper.GetType();

            bool goalPool = typeof(IGoalPool<TGoal>).IsAssignableFrom(helperType);
            bool worldStatePool = typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(helperType);
            bool goalMerge = typeof(IGoalMerge<TGoal>).IsAssignableFrom(helperType);
            bool actionHandlePool = typeof(IActionHandlePool<TActionHandle>).IsAssignableFrom(helperType);

            if (goalPool)
            {
                if (worldStatePool)
                {
                    if (goalMerge)
                    {
                        if (actionHandlePool)
                            return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoalPoolWorldMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                        else
                            return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    }
                    else
                    {
                        if (actionHandlePool)
                            return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoalPoolWorldPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                        else
                            return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoalPoolWorld<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    }
                }
                else if (goalMerge)
                {
                    if (actionHandlePool)
                        return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoalMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoalMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else
                {
                    if (actionHandlePool)
                        return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return Planner.RunAndDisposeCoroutine<AgentWrapperPoolGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
            }
            else if (worldStatePool)
            {
                if (goalMerge)
                {
                    if (actionHandlePool)
                        return Planner.RunAndDisposeCoroutine<AgentWrapperPoolWorldMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                             new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return Planner.RunAndDisposeCoroutine<AgentWrapperPoolWorldMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                             new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else
                {
                    if (actionHandlePool)
                        return Planner.RunAndDisposeCoroutine<AgentWrapperPoolWorldPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return Planner.RunAndDisposeCoroutine<AgentWrapperPoolWorld<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                }
            }
            else if (goalMerge)
            {
                if (actionHandlePool)
                    return Planner.RunAndDisposeCoroutine<AgentWrapperMergeGoalPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                    return Planner.RunAndDisposeCoroutine<AgentWrapperMergeGoal<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                        new(worldState, goals, actions, helper), plan, watchdog, log);
            }
            else if (actionHandlePool)
                return Planner.RunAndDisposeCoroutine<AgentWrapperPoolActionHandle<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TActionHandle, TWatchdog>(
                    new(worldState, goals, actions, helper), plan, watchdog, log);
            else
            {
                ThrowTypeMistmachException();
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

        [DoesNotReturn]
        private static void ThrowTypeMistmachException() => throw new ArgumentException($"Must implement any of {nameof(IGoalPool<TGoal>)}, {nameof(IWorldStatePool<TWorldState>)}, {nameof(IGoalMerge<TGoal>)}.", nameof(helper));
    }
}
