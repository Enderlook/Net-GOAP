namespace Enderlook.GOAP
{
    /// <summary>
    /// Represent the handle of an action.
    /// </summary>
    /// <typeparam name="TWorldState">Type of world state.</typeparam>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    public interface IActionHandle<TWorldState, TGoal>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
    {
        /// <summary>
        /// Check if this action meets procedural preconditions.<br/>
        /// Not confuse with the preconditions of <see cref="GetCostAndRequiredGoal(out float, out TGoal)"/>, this ones are not actually tied to the world state.
        /// </summary>
        /// <returns><see langword="true"/> if procedural preconditions are satisfied.</returns>
        bool CheckProceduralPreconditions()
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            => true
#endif
            ;

        /// <summary>
        /// Get the cost of execution this action and the preconditions required to execute this action.<br/>
        /// </summary>
        /// <param name="cost">Cost required to execute this action.</param>
        /// <param name="goal">Preconditions requires to execute this action if returns <see langword="true"/>.</param>
        /// <returns>If <see langword="true"/>, <paramref name="goal"/> contains the required preconditions. On <see langword="false"/>, there are no preconditions.</returns>
        bool GetCostAndRequiredGoal(out float cost, out TGoal goal);

        /// <summary>
        /// Applies the effects of this action to a world.<br/>
        /// Note that this method must not consume the required preconditions, if any.
        /// </summary>
        /// <param name="worldState">World state where effects are being applied.</param>
        void ApplyEffect(ref TWorldState worldState);
    }
}
