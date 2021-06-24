namespace Enderlook.GOAP
{
    /// <summary>
    /// Represent how an action can be used.
    /// </summary>
    public enum ActionUsageResult
    {
        /// <summary>
        /// The action can't be used.
        /// </summary>
        NotAllowed = 0,

        /// <summary>
        /// The action is valid and doesn't have a required goal.
        /// </summary>
        Allowed,

        /// <summary>
        /// The action is valid and have a required goal.
        /// </summary>
        AllowedAndHasGoal,
    }
}
