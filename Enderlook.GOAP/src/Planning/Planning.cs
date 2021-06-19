using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Extension methods for <see cref="Plan{TGoal, TAction}"/> used to fill the instance with a GOAP plan.<br/>
    /// All nested types inside this type are an implementation detail and so they shall never be used as parameters, variables, fields or return values.<br/>
    /// Instead the purpose of this types is to be used as chained calls, such as in the example:
    /// <code>
    /// return await new Plan&lt;ConcreteGoal, ConcreteAction&gt;<br/>
    ///     .Plan(GetInitialWorldState(), GetAvailableActions())<br/>
    ///     .CompleteGoal(GetGoal())<br/>
    ///     .WithHelper(GetHelper())<br/>
    ///     .ExecuteAsync()<br/>
    /// </code>
    /// </summary>
    public static partial class Planning
    {
        /// <summary>
        /// Initializes the planification.
        /// </summary>
        /// <typeparam name="TWorldState">Type of world state.</typeparam>
        /// <typeparam name="TGoal">Type of goal.</typeparam>
        /// <typeparam name="TAction">Type of action.</typeparam>
        /// <typeparam name="TActions">Type of enumeration which contains all available actions.</typeparam>
        /// <param name="plan">Instance where plan will be stored if found any.</param>
        /// <param name="worldState">Initial state of the world.</param>
        /// <param name="actions">Avariable actions to perfom in the world.</param>
        /// <param name="log">If not <see langword="null"/>, log information will be send to this delegate.<br/>
        /// The layout of the information is an implementation detail, so this should only be used for debugging purposes.</param>
        /// <returns>Instance of the builder for the plan.<br/>
        /// Shall only be used for method chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="plan"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="worldState"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="actions"/> is <see langword="null"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlanBuilder<TWorldState, TGoal, TAction, TActions> Plan<TWorldState, TGoal, TAction, TActions>(
            this Plan<TGoal, TAction> plan, TWorldState worldState, TActions actions, Action<string>? log = null)
            where TWorldState : IWorldState<TWorldState>
            where TGoal : IGoal<TWorldState>
            where TAction : IAction<TWorldState, TGoal>
            where TActions : IEnumerable<TAction>
            => new(plan, worldState, actions, log);

        [DoesNotReturn]
        private static void ThrowInstanceIsDefault() => throw new ArgumentException("Instance is default.", "this");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void RunAndDispose<TAgent, TWorldState, TGoal, TAction, TWatchdog>(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
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
        internal static ValueTask<Plan<TGoal, TAction>> RunAndDisposeAsync<TAgent, TWorldState, TGoal, TAction, TWatchdog>(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
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
        internal static PlanningCoroutine<TGoal, TAction> RunAndDisposeCoroutine<TAgent, TWorldState, TGoal, TAction, TWatchdog>(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
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
