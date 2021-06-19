using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Enderlook.GOAP
{
    public static partial class Planning
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

            /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActions}.Execute"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Plan<TGoal, TAction> Execute()
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
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
                            RunAndDispose<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                        else
                            RunAndDispose<AgentWrapperPoolGoalPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    }
                    else if (goalMerge)
                        RunAndDispose<AgentWrapperPoolGoalMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        RunAndDispose<AgentWrapperPoolGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else if (worldStatePool)
                {
                    if (goalMerge)
                        RunAndDispose<AgentWrapperPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        RunAndDispose<AgentWrapperPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else if (goalMerge)
                    RunAndDispose<AgentWrapperMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                    ThrowTypeMistmachException();
                return plan;
            }

            /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActions}.ExecuteAsync"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ValueTask<Plan<TGoal, TAction>> ExecuteAsync()
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
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
                            return RunAndDisposeAsync<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                        else
                            return RunAndDisposeAsync<AgentWrapperPoolGoalPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    }
                    else if (goalMerge)
                        return RunAndDisposeAsync<AgentWrapperPoolGoalMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return RunAndDisposeAsync<AgentWrapperPoolGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else if (worldStatePool)
                {
                    if (goalMerge)
                        return RunAndDisposeAsync<AgentWrapperPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return RunAndDisposeAsync<AgentWrapperPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else if (goalMerge)
                    return RunAndDisposeAsync<AgentWrapperMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                            new(worldState, goals, actions, helper), plan, watchdog, log);
                else
                {
                    ThrowTypeMistmachException();
                    return default;
                }
            }

            /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActions}.ExecuteCoroutine"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PlanningCoroutine<TGoal, TAction> ExecuteCoroutine()
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
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
                            return RunAndDisposeCoroutine<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                        else
                            return RunAndDisposeCoroutine<AgentWrapperPoolGoalPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    }
                    else if (goalMerge)
                        return RunAndDisposeCoroutine<AgentWrapperPoolGoalMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return RunAndDisposeCoroutine<AgentWrapperPoolGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else if (worldStatePool)
                {
                    if (goalMerge)
                        return RunAndDisposeCoroutine<AgentWrapperPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                    else
                        return RunAndDisposeCoroutine<AgentWrapperPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
                                new(worldState, goals, actions, helper), plan, watchdog, log);
                }
                else if (goalMerge)
                    return RunAndDisposeCoroutine<AgentWrapperMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog>(
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
}
