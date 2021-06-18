using System;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Cancelates a GOAP operation as soon the computation time reaches an specified value.
    /// </summary>
    public readonly struct TimeOutWatchdog : IWatchdog
    {
        private readonly int upTo;

        /// <summary>
        /// Creates a watchdog that cancelates as soon the computation time of GOAP reaches the specified <paramref name="maximumMiliseconds"/>.
        /// </summary>
        /// <param name="maximumMiliseconds">Maximum miliseconds spent computating.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeOutWatchdog(int maximumMiliseconds)
        {
            if (maximumMiliseconds <= 0)
                ThrowMustBeGreaterThanZero();

            upTo = DateTime.Now.AddMilliseconds(maximumMiliseconds).Millisecond;

            static void ThrowMustBeGreaterThanZero() => throw new ArgumentException("maximumMiliseconds", "Must be greater than 0.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        WatchdogResult IWatchdog.Poll(float cost) => DateTime.Now.Millisecond >= upTo ? WatchdogResult.Cancel : WatchdogResult.Continue;
    }
}
