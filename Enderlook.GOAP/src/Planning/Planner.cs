using Enderlook.GOAP.Utilities;
using Enderlook.GOAP.Watchdogs;

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Enderlook.GOAP.Planning
{
    /// <summary>
    /// Extension methods for <see cref="Plan{TGoal, TAction}"/> used to fill the instance with a GOAP plan.<br/>
    /// </summary>
    internal static class Planner
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RunAndDispose<TAgent, TWorldState, TGoal, TAction, TWatchdog>(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
            where TAgent : IAgent<TWorldState, TGoal, TAction>
            where TWorldState : IWorldState<TWorldState>
            where TGoal : IGoal<TWorldState>
            where TAction : IAction<TWorldState, TGoal>
            where TWatchdog : IWatchdog
        {
            if (log is null)
                PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, Toggle.No>
                    .RunAndDispose(agent, plan, watchdog, log);
            else
                PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, Toggle.Yes>
                    .RunAndDispose(agent, plan, watchdog, log);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<Plan<TGoal, TAction>> RunAndDisposeAsync<TAgent, TWorldState, TGoal, TAction, TWatchdog>(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
            where TAgent : IAgent<TWorldState, TGoal, TAction>
            where TWorldState : IWorldState<TWorldState>
            where TGoal : IGoal<TWorldState>
            where TAction : IAction<TWorldState, TGoal>
            where TWatchdog : IWatchdog
        {
            if (log is null)
                return PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, Toggle.No>
                    .RunAndDisposeAsync(agent, plan, watchdog, log);
            else
                return PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, Toggle.Yes>
                    .RunAndDisposeAsync(agent, plan, watchdog, log);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlanningCoroutine<TGoal, TAction> RunAndDisposeCoroutine<TAgent, TWorldState, TGoal, TAction, TWatchdog>(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
            where TAgent : IAgent<TWorldState, TGoal, TAction>
            where TWorldState : IWorldState<TWorldState>
            where TGoal : IGoal<TWorldState>
            where TAction : IAction<TWorldState, TGoal>
            where TWatchdog : IWatchdog
        {
            if (log is null)
                return PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, Toggle.No>
                    .RunAndDisposeCoroutine(agent, plan, watchdog, log);
            else
                return PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, Toggle.Yes>
                    .RunAndDisposeCoroutine(agent, plan, watchdog, log);
        }
    }
}