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
        /// Request the processing of this action.
        /// This method must call <see cref="IActionHandleAcceptor{TWorldState, TGoal}.Accept{TActionHandle}(TActionHandle)"/>.
        /// </summary>
        /// <typeparam name="TActionHandleAcceptor">Type of the action hadnle acceptor.</typeparam>
        /// <param name="aceptor">Processor of the action.</param>
        /// <param name="worldState">State of the world, this can be used to later calculate dynamic costs and goals in the action handle.</param>
        void Visit<TActionHandleAcceptor>(ref TActionHandleAcceptor aceptor, TWorldState worldState)
            where TActionHandleAcceptor : IActionHandleAcceptor<TWorldState, TGoal>;
    }
}
