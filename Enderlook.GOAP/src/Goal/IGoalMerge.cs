namespace Enderlook.GOAP
{
    /// <summary>
    /// Interface that allows merging goals to reduce memory consumption.<br/>
    /// This interface can be implemented by a helper type.
    /// </summary>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    public interface IGoalMerge<TGoal>
    {
        /// <summary>
        /// Try to merge <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">First goal to merge.</param>
        /// <param name="b">Second goal to merge.</param>
        /// <param name="c">Produced combination of <paramref name="a"/> and <paramref name="b"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="a"/> and <paramref name="b"/> could be merged.</returns>
        bool TryMerge(TGoal a, TGoal b, out TGoal c);
    }
}
