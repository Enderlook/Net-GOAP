namespace Enderlook.GOAP.Watchdogs
{
    /// <summary>
    /// How planning must continue.
    /// </summary>
    public enum WatchdogResult
    {
        /// <summary>
        /// Can continue planification.
        /// </summary>
        Continue = default,

        /// <summary>
        /// Planification was canceled.
        /// </summary>
        Cancel,

        /// <summary>
        /// Planification was suspended until get a different yield.
        /// </summary>
        Suspend,
    }
}
