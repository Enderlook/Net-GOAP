using System.Collections.Generic;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Describes an agent.
    /// </summary>
    /// <typeparam name="TMemory">Type of memory.</typeparam>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    /// <typeparam name="TAction">Type of action.</typeparam>
    public interface IAgent<TMemory, TGoal, TAction>
        where TMemory : IWorldState<TMemory>
        where TGoal : IGoal<TMemory>
        where TAction : IAction<TMemory, TGoal>
    {
        /// <summary>
        /// Get the current state of the world.
        /// </summary>
        /// <returns>Current state of the world.</returns>
        TMemory GetWorldState();

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
