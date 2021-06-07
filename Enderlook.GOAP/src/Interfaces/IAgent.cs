using System.Collections.Generic;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Describes an agent.
    /// </summary>
    /// <typeparam name="TWorldState">Type of world state.</typeparam>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    /// <typeparam name="TAction">Type of action.</typeparam>
    public interface IAgent<TWorldState, TGoal, TAction>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
        where TAction : IAction<TWorldState, TGoal>
    {
        /// <summary>
        /// Get the current state of the world.
        /// </summary>
        /// <returns>Current state of the world.</returns>
        TWorldState GetWorldState();

        /// <summary>
        /// Get all the possible goals that this agent want to complete.
        /// </summary>
        /// <returns>All goals that this agent want to complete.</returns>
        IEnumerator<TGoal> GetGoals();

        /// <summary>
        /// Get all the actions that this agent can do.
        /// </summary>
        /// <returns>All the actions that this agent can do.</returns>
        IEnumerator<TAction> GetActions();
    }
}
