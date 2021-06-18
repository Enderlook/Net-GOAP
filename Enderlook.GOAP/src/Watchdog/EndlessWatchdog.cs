using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Don't suspend nor cancelates the computation of a GOAP.
    /// </summary>
    internal readonly struct EndlessWatchdog : IWatchdog
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        WatchdogResult IWatchdog.Poll(float cost) => WatchdogResult.Continue;
    }
}
