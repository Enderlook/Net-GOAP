using System;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Represent the planification process as a coroutine.
    /// </summary>
    public abstract class PlanningCoroutine : IDisposable
    {
        /// <inheritdoc cref="IDisposable.Dispose"/>
        public abstract void Dispose();

        /// <summary>
        /// Continues execution of coroutine.
        /// </summary>
        /// <returns>State of the planification.</returns>
        public abstract PlanningCoroutineResult MoveNext();
    }
}
