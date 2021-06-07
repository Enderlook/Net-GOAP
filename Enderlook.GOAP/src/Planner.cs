using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Helper methods to compute GOAP.
    /// </summary>
    public static class Planner
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlanResult Plan<TAgent, TWorld, TAction, TGoal>(
            TAgent agent, Stack<TAction> actions, out TGoal? goal, out float cost, CancellationToken token, Action<string>? log = null)
            where TAgent : IAgent<TWorld, TGoal, TAction>
            where TWorld : IWorldState<TWorld>
            where TAction : IAction<TWorld, TGoal>
            where TGoal : IGoal<TWorld>
            => Plan<TAgent, TWorld, TAction, TGoal, CancellableWatchdog>(agent, actions, out goal, out cost, new CancellableWatchdog(token), log);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlanResult Plan<TAgent, TWorld, TAction, TGoal>(
            TAgent agent, Stack<TAction> actions, out TGoal? goal, out float cost, float maximumCost, Action<string>? log = null)
            where TAgent : IAgent<TWorld, TGoal, TAction>
            where TWorld : IWorldState<TWorld>
            where TAction : IAction<TWorld, TGoal>
            where TGoal : IGoal<TWorld>
            => Plan<TAgent, TWorld, TAction, TGoal, CostWatchdog>(agent, actions, out goal, out cost, new CostWatchdog(maximumCost), log);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlanResult Plan<TAgent, TWorld, TAction, TGoal>(
            TAgent agent, Stack<TAction> actions, out TGoal? goal, out float cost, Action<string>? log = null)
            where TAgent : IAgent<TWorld, TGoal, TAction>
            where TWorld : IWorldState<TWorld>
            where TAction : IAction<TWorld, TGoal>
            where TGoal : IGoal<TWorld>
            => Plan<TAgent, TWorld, TAction, TGoal, EndlessWatchdog>(agent, actions, out goal, out cost, new EndlessWatchdog(), log);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlanResult Plan<TAgent, TWorldState, TAction, TGoal, TWatchdog>(
            TAgent agent, Stack<TAction> actions, out TGoal? goal, out float cost, TWatchdog watchdog, Action<string>? log = null)
            where TAgent : IAgent<TWorldState, TGoal, TAction>
            where TWorldState : IWorldState<TWorldState>
            where TAction : IAction<TWorldState, TGoal>
            where TGoal : IGoal<TWorldState>
            where TWatchdog : IWatchdog
        {
            if (log is null)
                return PlanInner<TAgent, TWorldState, TAction, TGoal, TWatchdog, Toggle.No>(agent, actions, out goal, out cost, watchdog, log);
            else
                return PlanInner<TAgent, TWorldState, TAction, TGoal, TWatchdog, Toggle.Yes>(agent, actions, out goal, out cost, watchdog, log);
        }

        private static PlanResult PlanInner<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog>(
            TAgent agent, Stack<TAction> actions, out TGoal? goal, out float cost, TWatchdog watchdog, Action<string>? log)
            where TAgent : IAgent<TWorldState, TGoal, TAction>
            where TWorldState : IWorldState<TWorldState>
            where TAction : IAction<TWorldState, TGoal>
            where TGoal : IGoal<TWorldState>
            where TWatchdog : IWatchdog
        {
            if (typeof(TAgent).IsValueType)
                return PlanBuildIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog>
                    .RunAndDispose(agent, actions, watchdog, out goal, out cost, log);

#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
            IAgent<TWorldState, TGoal, TAction> agent_ = agent;
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

            Type planType = agent.GetType();
            if (typeof(IGoalPool<TGoal>).IsAssignableFrom(planType))
            {
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(planType))
                {
                    if (typeof(IGoalMerge<TGoal>).IsAssignableFrom(planType))
                        return PlanBuildIterator<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, TLog>
                            .RunAndDispose(new(agent_), actions, watchdog, out goal, out cost, log);
                    return PlanBuildIterator<AgentWrapperPoolGoalPoolWorld<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, TLog>
                        .RunAndDispose(new(agent_), actions, watchdog, out goal, out cost, log);
                }
                if (typeof(IGoalMerge<TGoal>).IsAssignableFrom(planType))
                    return PlanBuildIterator<AgentWrapperPoolGoalMergeGoal<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, TLog>
                        .RunAndDispose(new(agent_), actions, watchdog, out goal, out cost, log);
                return PlanBuildIterator<AgentWrapperPoolGoal<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, TLog>
                    .RunAndDispose(new(agent_), actions, watchdog, out goal, out cost, log);
            }

            if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(planType))
            {
                if (typeof(IGoalMerge<TGoal>).IsAssignableFrom(planType))
                    return PlanBuildIterator<AgentWrapperPoolWorldMergeGoal<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, TLog>
                       .RunAndDispose(new(agent_), actions, watchdog, out goal, out cost, log);
                return PlanBuildIterator<AgentWrapperPoolWorld<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, TLog>
                    .RunAndDispose(new(agent_), actions, watchdog, out goal, out cost, log);
            }

            if (typeof(IGoalMerge<TGoal>).IsAssignableFrom(planType))
                return PlanBuildIterator<AgentWrapperMergeGoal<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, TLog>
                    .RunAndDispose(new(agent_), actions, watchdog, out goal, out cost, log);

            return PlanBuildIterator<AgentWrapper<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, TLog>
                .RunAndDispose(new(agent_), actions, watchdog, out goal, out cost, log);
        }
    }
}
