using Enderlook.GOAP.Planning;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Extension methods for <see cref="Plan{TGoal, TAction}"/> used to fill the instance with a GOAP plan.<br/>
    /// </summary>
    public static class PlanExtensions
    {
        /// <summary>
        /// Initializes the planification.
        /// </summary>
        /// <typeparam name="TWorldState">Type of world state.</typeparam>
        /// <typeparam name="TGoal">Type of goal.</typeparam>
        /// <typeparam name="TAction">Type of action.</typeparam>
        /// <typeparam name="TActionHandle">Type of action handle.</typeparam>
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
        public static PlanBuilder<TWorldState, TGoal, TAction, TActionHandle, TActions> Plan<TWorldState, TGoal, TAction, TActionHandle, TActions>(
            this Plan<TGoal, TAction> plan, TWorldState worldState, TActions actions, Action<string>? log = null)
            where TWorldState : IWorldState<TWorldState>
            where TGoal : IGoal<TWorldState>
            where TAction : IAction<TWorldState, TGoal, TActionHandle>
            where TActions : IEnumerable<TAction>
            => new(plan, worldState, actions, log);

        /// <inheritdoc cref="Plan{TWorldState}(Plan{IGoal{TWorldState}, IAction{TWorldState, IGoal{TWorldState}, object}}, TWorldState, IEnumerable{IAction{TWorldState, IGoal{TWorldState}, object}}, Action{string}?)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlanBuilder<TWorldState, IGoal<TWorldState>, IAction<TWorldState, IGoal<TWorldState>, object>, object, IEnumerable<IAction<TWorldState, IGoal<TWorldState>, object>>> Plan<TWorldState>(
            this Plan<IGoal<TWorldState>,  IAction<TWorldState, IGoal<TWorldState>, object>> plan, TWorldState worldState, IEnumerable<IAction<TWorldState, IGoal<TWorldState>, object>> actions, Action<string>? log = null)
            where TWorldState : IWorldState<TWorldState>
            => new(plan, worldState, actions, log);
    }
}
