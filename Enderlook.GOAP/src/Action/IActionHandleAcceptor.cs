namespace Enderlook.GOAP
{
    /// <summary>
    /// Acceptor of an <see cref="IActionHandle{TWorldState, TGoal}"/>
    /// </summary>
    /// <typeparam name="TWorldState">Type of world state.</typeparam>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    public interface IActionHandleAcceptor<TWorldState, TGoal>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
    {
        /// <summary>
        /// Accept an <see cref="IActionHandle{TWorldState, TGoal}"/> to process.
        /// </summary>
        /// <typeparam name="TActionHandle">Type of action handle.</typeparam>
        /// <param name="handle">Action handle to process.</param>
        void Accept<TActionHandle>(TActionHandle handle)
            where TActionHandle : IActionHandle<TWorldState, TGoal>;
    }
}
