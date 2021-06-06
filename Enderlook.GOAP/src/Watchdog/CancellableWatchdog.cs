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
        public bool CanContinue(float cost) => !token.IsCancellationRequested;
    }
}
