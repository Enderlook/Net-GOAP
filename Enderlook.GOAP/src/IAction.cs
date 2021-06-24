namespace Enderlook.GOAP
{
    /// <summary>
    /// Describes an action.
    /// </summary>
    /// <typeparam name="TWorldState">Type of world state.</typeparam>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    /// <typeparam name="TActionHandle">Type of action handle.</typeparam>
    public interface IAction<TWorldState, TGoal, TActionHandle>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
    {
        /// <summary>
        /// Get the cost and preconditions required to perform this action.
        /// </summary>
        /// <param name="worldState">State of the world.</param>
        /// <param name="handle">Handle from <see cref="ApplyEffect(TWorldState, out TActionHandle)"/>.</param>
        /// <param name="goal">Preconditions required to perform this action.</param>
        /// <param name="cost">Cost of running this action.</param>
        /// <returns>Determines how the action can be used.</returns>
        ActionUsageResult GetCostAndRequiredGoal(TWorldState worldState, TActionHandle handle, out TGoal goal, out float cost);

        /// <summary>
        /// Applies the effects of this action to a world.<br/>
        /// Note that this method must not consume the required preconditions, if any.<br/>
        /// Note that if the world state doesn't allow the execution of this action, it must fail silently.
        /// </summary>
        /// <param name="worldState">World state where effects are being applied.</param>
        /// <param name="handle">Opaque handle that will be passed to <see cref="GetCostAndRequiredGoal(TWorldState, TActionHandle, out TGoal, out float)"/>.</param>
        void ApplyEffect(TWorldState worldState, out TActionHandle handle);
    }
}
