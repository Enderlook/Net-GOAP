namespace Enderlook.GOAP
{
    /// <summary>
    /// Represent the planification process as a coroutine.
    /// </summary>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    /// <typeparam name="TAction">Type of action.</typeparam>
    public abstract class PlanningCoroutine<TGoal, TAction> : PlanningCoroutine
    {
        private protected const byte Initialize = 0;
        private protected const byte Continue = 1;
        private protected const byte Finalized = 3;
        private protected const byte Cancelled = 4;
        private protected const byte Disposed = 5;

        private protected byte state;

        /// <summary>
        /// Get the associated plan with the coroutine.
        /// </summary>
        /// <returns>Asociated plan.</returns>
        public abstract Plan<TGoal, TAction> GetAssociatedPlan();
    }
}
