namespace Enderlook.GOAP
{
    /// <summary>
    /// Describes the state of the world.
    /// </summary>
    /// <typeparam name="TSelf">It's own type.</typeparam>
    public interface IWorldState<TSelf> where TSelf : IWorldState<TSelf>
    {
        /// <summary>
        /// Creates a deep clone of the state of this world.
        /// </summary>
        /// <returns>New deep clone of this world.</returns>
        TSelf Clone();
    }
}
