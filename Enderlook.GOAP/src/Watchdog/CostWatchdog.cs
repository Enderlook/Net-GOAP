using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal readonly struct CostWatchdog : IWatchdog
    {
        private readonly float cost;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CostWatchdog(float cost) => this.cost = cost;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanContinue(float cost) => cost < this.cost;
    }
}
