namespace Enderlook.GOAP
{
    /// <summary>
    /// Describes an action.
    /// </summary>
    /// <typeparam name="TWorldState">Type of world state.</typeparam>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    public interface IAction<TWorldState, TGoal>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
    {
        /// <summary>
        /// Get the cost and preconditions required to perform this action.
        /// </summary>
        /// <param name="goal">Preconditions required to perform this action.</param>
        /// <param name="cost">Cost of running this action.</param>
        /// <returns>If <see langword="false"/>, the <paramref name="goal"/> precondition is ignored.</returns>
        bool GetCostAndRequiredGoal(out TGoal goal, out float cost);

        /// <summary>
        /// Applies the effects of this action to a memory.<br/>
        /// Note that this method must not consume the required goal.
        /// </summary>
        /// <param name="memory">World state where effects are being applied.</param>
        void ApplyEffect(TWorldState memory);
    }
}
