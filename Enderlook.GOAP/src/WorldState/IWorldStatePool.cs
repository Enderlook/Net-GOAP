namespace Enderlook.GOAP
{
    /// <summary>
    /// Interface that allows pooling of world states.<br/>
    /// This interface can be implemented by a type which also implements <see cref="IAgent{TMemory, TGoal, TAction}"/>.
    /// </summary>
    /// <typeparam name="TWorld">Type of world state.</typeparam>
    public interface IWorldStatePool<TWorld>
    {
        /// <summary>
        /// Clones the content of a world.
        /// </summary>
        /// <param name="value">World to clone.</param>
        /// <returns>New clone of the world.</returns>
        TWorld Clone(TWorld value);

        /// <summary>
        /// Gives ownership of the world.
        /// </summary>
        /// <param name="value">World to give ownership.</param>
        void Return(TWorld value);
    }
}
