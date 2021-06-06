using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal readonly struct EndlessWatchdog : IWatchdog
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanContinue(float cost) => true;
    }
}
