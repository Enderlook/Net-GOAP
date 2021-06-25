using System;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP.Watchdogs
{
    /// <summary>
    /// Cancelates a GOAP operation as soon the computation time reaches an specified value.
    /// </summary>
    public readonly struct TimeOutWatchdog : IWatchdog
    {
        private readonly int upTo;

        /// <summary>
        /// Creates a watchdog that cancelates as soon the computation time of GOAP reaches the specified <paramref name="maximumMilliseconds"/>.
        /// </summary>
        /// <param name="maximumMilliseconds">Maximum miliseconds spent computating.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="maximumMilliseconds"/> is 0 or negative.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeOutWatchdog(int maximumMilliseconds)
        {
            if (maximumMilliseconds <= 0)
                ThrowHelper.ThrowArgumentOutOfRangeException_MaximumMilisecondsMustBeGreaterThanZero();

            upTo = DateTime.Now.AddMilliseconds(maximumMilliseconds).Millisecond;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        WatchdogResult IWatchdog.Poll(float cost) => DateTime.Now.Millisecond >= upTo ? WatchdogResult.Cancel : WatchdogResult.Continue;
    }
}
