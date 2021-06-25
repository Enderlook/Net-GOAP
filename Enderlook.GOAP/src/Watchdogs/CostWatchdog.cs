using System;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP.Watchdogs
{
    /// <summary>
    /// Cancelates a GOAP operation as soon the cost reaches an specified value.
    /// </summary>
    public readonly struct CostWatchdog : IWatchdog
    {
        private readonly float cost;

        /// <summary>
        /// Creates a watchdog that cancelates as soon the cost of GOAP reaches the specified <paramref name="maximumCost"/>.
        /// </summary>
        /// <param name="maximumCost">Cost at which GOAP is cancelated.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="maximumCost"/> is 0 or negative.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CostWatchdog(float maximumCost)
        {
            if (maximumCost <= 0)
                ThrowHelper.ThrowArgumentOutOfRangeException_CostMustBeGreaterThanZero();

            cost = maximumCost;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        WatchdogResult IWatchdog.Poll(float cost) => cost >= this.cost ? WatchdogResult.Cancel : WatchdogResult.Continue;
    }
}
