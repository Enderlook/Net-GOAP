namespace Enderlook.GOAP
{
    /// <summary>
    /// Interface that allows pooling of goals.<br/>
    /// This interface can be implemented by a helper type.
    /// </summary>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    public interface IGoalPool<TGoal>
    {
        /// <summary>
        /// Gives ownership of the goal.
        /// </summary>
        /// <param name="value">Goal to give ownership.</param>
        void Return(TGoal value);
    }
}
