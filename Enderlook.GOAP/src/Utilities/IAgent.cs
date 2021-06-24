using Enderlook.GOAP.Watchdogs;

namespace Enderlook.GOAP.Utilities
{
    /// <summary>
    /// Describes an agent.
    /// </summary>
    /// <typeparam name="TWorldState">Type of world state.</typeparam>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    /// <typeparam name="TAction">Type of action.</typeparam>
    /// <typeparam name="TActionHandle">Type of action handle.</typeparam>
    internal interface IAgent<TWorldState, TGoal, TAction, TActionHandle>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
        where TAction : IAction<TWorldState, TGoal, TActionHandle>
    {
        /// <summary>
        /// Get the current state of the world.
        /// </summary>
        /// <returns>Current state of the world.</returns>
        TWorldState GetWorldState();

        /// <summary>
        /// Set all the possible goals that this agent want to complete.
        /// </summary>
        void SetGoals<TAgent, TWatchdog, TLog>(ref PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TActionHandle, TWatchdog, TLog> builder)
            where TAgent : IAgent<TWorldState, TGoal, TAction, TActionHandle>
            where TWatchdog : IWatchdog;

        /// <summary>
        /// Set all the actions that this agent can do.
        /// </summary>
        void SetActions<TAgent, TWatchdog, TLog>(ref PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TActionHandle, TWatchdog, TLog> builder)
            where TAgent : IAgent<TWorldState, TGoal, TAction, TActionHandle>
            where TWatchdog : IWatchdog;
    }
}
