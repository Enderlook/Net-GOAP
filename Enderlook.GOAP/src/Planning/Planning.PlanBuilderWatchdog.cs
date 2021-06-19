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
        public readonly struct PlanBuilderWatchdog<TWorldState, TGoal, TGoals, TAction, TActions, TWatchdog>
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal PlanBuilderWatchdog(Plan<TGoal, TAction> plan, TWorldState worldState, TActions actions, TGoals goals, Action<string>? log, TWatchdog? watchdog)
            {
                Debug.Assert(plan is not null);
                Debug.Assert(worldState is not null);
                Debug.Assert(actions is not null);
                Debug.Assert(goals is not null);

                if (watchdog is null)
                    ThrowNullWatchdogException();

                this.plan = plan;
                this.worldState = worldState;
                this.actions = actions;
                this.goals = goals;
                this.log = log;
                this.watchdog = watchdog;

                [DoesNotReturn]
                static void ThrowNullWatchdogException() => throw new ArgumentNullException(nameof(watchdog));
            }

            /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActions}.WithHelper{THelper}(THelper)"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PlanBuilderHelper<TWorldState, TGoal, TGoals, TAction, TActions, TWatchdog, THelper> WithHelper<THelper>(THelper helper)
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
                DebugAssert();
                return new(plan, worldState, actions, goals, log, watchdog, helper);
            }

            /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActions}.Execute"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Plan<TGoal, TAction> Execute()
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
                DebugAssert();

                RunAndDispose<AgentWrapper<TWorldState, TGoal, TAction, TGoals, TActions>, TWorldState, TGoal, TAction, TWatchdog>(
                    new(worldState, goals, actions), plan, watchdog, log);
                return plan;
            }

            /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActions}.ExecuteAsync"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public async ValueTask<Plan<TGoal, TAction>> ExecuteAsync()
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
                DebugAssert();

                await RunAndDisposeAsync<AgentWrapper<TWorldState, TGoal, TAction, TGoals, TActions>, TWorldState, TGoal, TAction, TWatchdog>(
                    new(worldState, goals, actions), plan, watchdog, log);
                return plan;
            }

            /// <inheritdoc cref="PlanBuilderGoal{TWorldState, TGoal, TGoals, TAction, TActions}.ExecuteCoroutine"/>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PlanningCoroutine<TGoal, TAction> ExecuteCoroutine()
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
                DebugAssert();

                return RunAndDisposeCoroutine<AgentWrapper<TWorldState, TGoal, TAction, TGoals, TActions>, TWorldState, TGoal, TAction, TWatchdog>(
                    new(worldState, goals, actions), plan, watchdog, log);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("DEBUG")]
            private void DebugAssert()
            {
                Debug.Assert(plan is not null);
                Debug.Assert(worldState is not null);
                Debug.Assert(actions is not null);
                Debug.Assert(goals is not null);
                Debug.Assert(watchdog is not null);
            }
        }
    }
}
