using System;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP.Watchdogs
{
    /// <summary>
    /// Suspends a GOAP operation as soon the computation time reaches an specified value.
    /// </summary>
    public struct TimeSliceWatchdog : IWatchdog
    {
        private int time;
        private int upTo;

        /// <summary>
        /// Creates a watchdog that suspends as soon the computation time of GOAP reaches the specified <paramref name="maximumMiliseconds"/>.
        /// </summary>
        /// <param name="maximumMiliseconds">Maximum miliseconds spent computating.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TimeSliceWatchdog(int maximumMiliseconds)
        {
            if (maximumMiliseconds <= 0)
                ThrowHelper.ThrowArgumentOutOfRangeException_MaximumMilisecondsMustBeGreaterThanZero();

            time = maximumMiliseconds;
            upTo = DateTime.Now.AddMilliseconds(maximumMiliseconds).Millisecond;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        WatchdogResult IWatchdog.Poll(float cost)
        {
            if (DateTime.Now.Millisecond >= upTo)
            {
                time = -time;

                if (time > 0)
                {
                    upTo = DateTime.Now.AddMilliseconds(time).Millisecond;
                    return WatchdogResult.Continue;
                }

                return WatchdogResult.Suspend;
            }
            else
                return WatchdogResult.Continue;
        }
    }
}
