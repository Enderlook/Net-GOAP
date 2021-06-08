using System.Runtime.CompilerServices;
using System.Threading;

namespace Enderlook.GOAP
{
    internal readonly struct CancellableWatchdog : IWatchdog
    {
        private readonly CancellationToken token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CancellableWatchdog(CancellationToken token) => this.token = token;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public WatchdogResult Poll(float cost) => token.IsCancellationRequested ? WatchdogResult.Cancel : WatchdogResult.Continue;
    }
}
