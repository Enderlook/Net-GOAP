namespace Enderlook.GOAP
{
    /// <summary>
    /// Describes a cancellator of a planification.
    /// </summary>
    public interface IWatchdog
    {
        /// <summary>
        /// Determines if the planning how should continue.
        /// </summary>
        /// <param name="cost">Current cost of total actions to perform.</param>
        /// <returns>How the planning should continue.</returns>
        WatchdogResult Poll(float cost);
    }
}
