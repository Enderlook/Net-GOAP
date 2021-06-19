namespace Enderlook.GOAP
{
    /// <summary>
    /// Determines the result of an iteration of the planner.
    /// </summary>
    public enum PlanningCoroutineResult
    {
        /// <summary>
        /// Planification can continue.
        /// </summary>
        Continue,

        /// <summary>
        /// Planification was cancelled.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Planification is suspended until get a different yield.
        /// </summary>
        Suspended,

        /// <summary>
        /// Planification has finalized (either found a valid plan or exhausted all possibilities).
        /// </summary>
        Finalized,
    }
}
