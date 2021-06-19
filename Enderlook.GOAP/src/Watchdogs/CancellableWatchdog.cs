using System.Runtime.CompilerServices;
using System.Threading;

namespace Enderlook.GOAP.Watchdogs
{
    /// <summary>
    /// Represent a watchdog that wrap a <see cref="CancellationToken"/>.
    /// </summary>
    public readonly struct CancellableWatchdog : IWatchdog
    {
        private readonly CancellationToken token;

        /// <summary>
        /// Wrap a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="token"><see cref="CancellationToken"/> to wrap.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CancellableWatchdog(CancellationToken token) => this.token = token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        WatchdogResult IWatchdog.Poll(float cost) => token.IsCancellationRequested ? WatchdogResult.Cancel : WatchdogResult.Continue;

        /// <summary>
        /// Wrap a <see cref="CancellationToken"/>.
        /// </summary>
        /// <param name="token"><see cref="CancellationToken"/> to wrap.</param>
        public static implicit operator CancellableWatchdog(CancellationToken token) => new(token);
    }
}
