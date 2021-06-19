namespace Enderlook.GOAP
{
    /// <summary>
    /// Determines how much a certain memory state can satisfy a goal.
    /// </summary>
    public enum SatisfactionResult
    {
        /// <summary>
        /// The memory has satisfied the goal.
        /// </summary>
        Satisfied,

        /// <summary>
        /// The memory has progressed towards satisfaction of the goal.
        /// </summary>
        Progressed,

        /// <summary>
        /// The memory has not progressed towards satisfaction of the goal.
        /// </summary>
        NotProgressed,
    }
}
