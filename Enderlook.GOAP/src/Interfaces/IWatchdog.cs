namespace Enderlook.GOAP
{
    /// <summary>
    /// Describes a cancellator of a planification.
    /// </summary>
    public interface IWatchdog
    {
        /// <summary>
        /// Determines if the planning can continue or must cancel.
        /// </summary>
        /// <param name="cost">Current cost of total actions to perform.</param>
        /// <returns><see langword="true"/> if it can continue. <see langword="false"/> if it must stop.</returns>
        bool CanContinue(float cost);
    }
}
