using System;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Represent the planification process as a coroutine.
    /// </summary>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    /// <typeparam name="TAction">Type of action.</typeparam>
    public abstract class PlanningCoroutine<TGoal, TAction> : IDisposable
    {
        private protected const byte Initialize = 0;
        private protected const byte Continue = 1;
        private protected const byte ToCancel = 2;
        private protected const byte ToFinalize = 3;
        private protected const byte Finalized = 4;
        private protected const byte Cancelled = 5;
        private protected const byte Disposed = 6;

        private protected PlanResult<TGoal, TAction> result;
        private protected byte state;

        /// <inheritdoc cref="IDisposable.Dispose"/>
        public abstract void Dispose();

        /// <summary>
        /// Continues execution of coroutine.
        /// </summary>
        /// <returns>State of the planification.</returns>
        public abstract PlanningCoroutineResult MoveNext();

        /// <summary>
        /// Get the result of the coroutine.
        /// </summary>
        /// <returns>Result of the coroutine.</returns>
        /// <exception cref="InvalidOperationException">Throw if coroutine hasn't end.</exception>
        public PlanResult<TGoal, TAction> GetResult()
        {
            if (state != Finalized && state != Cancelled)
                ThrowHasNotFinalizedException();
            return result;
        }

        private static void ThrowHasNotFinalizedException() => throw new InvalidOperationException("Coroutine has not finalized.");
    }
}
