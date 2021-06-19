using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Enderlook.GOAP
{
    public static partial class Planning
    {
        /// <summary>
        /// This type is an implementation detail an shall never be used in parameters, fields, variables or return types.<br/>
        /// It should only be used in chaining calls.
        /// </summary>
        public readonly ref struct PlanBuilderGoal<TWorldState, TGoal, TGoals, TAction, TActions>
            where TWorldState : IWorldState<TWorldState>
            where TGoal : IGoal<TWorldState>
            where TAction : IAction<TWorldState, TGoal>
            where TActions : IEnumerable<TAction>
        {
            private readonly Plan<TGoal, TAction> plan;
            private readonly TWorldState worldState;
            private readonly TActions actions;
            private readonly Action<string>? log;
            private readonly TGoals goals;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal PlanBuilderGoal(Plan<TGoal, TAction> plan, TWorldState worldState, TActions actions, Action<string>? log, TGoals goals)
            {
                Debug.Assert(plan is not null);
                Debug.Assert(worldState is not null);
                Debug.Assert(actions is not null);
                Debug.Assert(goals is not null);

                this.plan = plan;
                this.worldState = worldState;
                this.actions = actions;
                this.log = log;
                this.goals = goals;
            }

            /// <summary>
            /// Includes a helper instance to reduce unnecessary allocations.<br/>
            /// The instance (the type is not required, so this also supports class subtyping) must implement at least one of <see cref="IGoalMerge{TGoal}"/>, <see cref="IGoalPool{TGoal}"/>, <see cref="IWorldStatePool{TWorld}"/>.
            /// </summary>
            /// <typeparam name="THelper">Type of helper.</typeparam>
            /// <param name="helper">Helper instance.</param>
            /// <returns>Instance of the builder for the plan.<br/>
            /// Shall only be used for method chaining.</returns>
            /// <exception cref="ArgumentException">Thrown when instance is default.</exception>
            /// <exception cref="ArgumentNullException">Thrown when <paramref name="helper"/> is <see langword="null"/>.</exception>
            /// <exception cref="ArgumentException">Thrown when <paramref name="helper"/> doesn't implement any of  <see cref="IGoalMerge{TGoal}"/>, <see cref="IGoalPool{TGoal}"/>, <see cref="IWorldStatePool{TWorld}"/>.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PlanBuilderHelper<TWorldState, TGoal, TGoals, TAction, TActions, EndlessWatchdog, THelper> WithHelper<THelper>(THelper helper)
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
                DebugAssert();

                return new(plan, worldState, actions, goals, log, new(), helper);
            }

            /// <summary>
            /// Includes a watchdog which may suspend or cancelates the planification.
            /// </summary>
            /// <typeparam name="TWatchdog">Type of watchdog.</typeparam>
            /// <param name="watchdog">Watchdog of planification.</param>
            /// <returns>Instance of the builder for the plan.<br/>
            /// Shall only be used for method chaining.</returns>
            /// <exception cref="ArgumentException">Thrown when instance is default.</exception>
            /// <exception cref="ArgumentNullException">Thrown when <paramref name="watchdog"/> is <see langword="null"/>.</exception>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PlanBuilderWatchdog<TWorldState, TGoal, TGoals, TAction, TActions, TWatchdog> WithWatchdog<TWatchdog>(TWatchdog watchdog)
                where TWatchdog : IWatchdog
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
                DebugAssert();

                return new(plan, worldState, actions, goals, log, watchdog);
            }

            /// <summary>
            /// Executes the planification synchronously.
            /// </summary>
            /// <returns>Instance passed on <see cref="Plan{TWorldState, TGoal, TAction, TActions}(Plan{TGoal, TAction}, TWorldState, TActions, Action{string}?)"/> method.</returns>
            /// <exception cref="ArgumentException">Thrown when instance is default.</exception>
            /// <remarks>If the planifiaction has a watchdog, all <see cref="WatchdogResult.Suspend"/> will be traduced as <see cref="Thread.Yield()"/>.</remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Plan<TGoal, TAction> Execute()
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
                DebugAssert();

                RunAndDispose<AgentWrapper<TWorldState, TGoal, TAction, TGoals, TActions>, TWorldState, TGoal, TAction, EndlessWatchdog>(
                    new (worldState, goals, actions), plan, new(), log);
                return plan;
            }

            /// <summary>
            /// Executes the planification asynchronously.
            /// </summary>
            /// <returns>Instance passed on <see cref="Plan{TWorldState, TGoal, TAction, TActions}(Plan{TGoal, TAction}, TWorldState, TActions, Action{string}?)"/> method.</returns>
            /// <exception cref="ArgumentException">Thrown when instance is default.</exception>
            /// <remarks>If the planifiaction has a watchdog, all <see cref="WatchdogResult.Suspend"/> will be traduced as <see cref="Task.Yield()"/>.</remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public ValueTask<Plan<TGoal, TAction>> ExecuteAsync()
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
                DebugAssert();

                return RunAndDisposeAsync<AgentWrapper<TWorldState, TGoal, TAction, TGoals, TActions>, TWorldState, TGoal, TAction, EndlessWatchdog>(
                    new(worldState, goals, actions), plan, new(), log);
            }

            /// <summary>
            /// Executes the planification asynchronously.
            /// </summary>
            /// <returns>Instance passed on <see cref="Plan{TWorldState, TGoal, TAction, TActions}(Plan{TGoal, TAction}, TWorldState, TActions, Action{string}?)"/> method.</returns>
            /// <exception cref="ArgumentException">Thrown when instance is default.</exception>
            /// <remarks>If the planifiaction has a watchdog, all <see cref="WatchdogResult.Suspend"/> will be traduced as an enumerator yield.</remarks>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public PlanningCoroutine<TGoal, TAction> ExecuteCoroutine()
            {
                if (plan is null)
                    ThrowInstanceIsDefault();
                DebugAssert();

                return RunAndDisposeCoroutine<AgentWrapper<TWorldState, TGoal, TAction, TGoals, TActions>, TWorldState, TGoal, TAction, EndlessWatchdog>(
                    new(worldState, goals, actions), plan, new(), log);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining), Conditional("DEBUG")]
            private void DebugAssert()
            {
                Debug.Assert(plan is not null);
                Debug.Assert(worldState is not null);
                Debug.Assert(actions is not null);
                Debug.Assert(goals is not null);
            }
        }
    }
}
