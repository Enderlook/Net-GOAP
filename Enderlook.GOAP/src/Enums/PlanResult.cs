namespace Enderlook.GOAP
{
    /// <summary>
    /// Determines the result of a plan.
    /// </summary>
    public enum PlanResult
    {
        /// <summary>
        /// A plan to reach the requested goal was found.
        /// </summary>
        FoundPlan,

        /// <summary>
        /// No plan was found to reach the requested goal.
        /// </summary>
        NotFound,

        /// <summary>
        /// The planification was cancelled before founding a plan to reach the requested goal.
        /// </summary>
        Cancelled,
    }
}
