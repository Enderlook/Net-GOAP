using System;
using System.Diagnostics.CodeAnalysis;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Helper methods to compute GOAP.
    /// </summary>
    public static partial class Planner
    {
        [DoesNotReturn]
        private static void ThrowNullAgentException() => throw new ArgumentNullException("agent");

        [DoesNotReturn]
        private static void ThrowNullPlanException() => throw new ArgumentNullException("plan");

        [DoesNotReturn]
        private static void ThrowNullWatchdogException() => throw new ArgumentNullException("watchdog");

        [DoesNotReturn]
        private static void ThrowNullLogException() => throw new ArgumentNullException("log", "Use the overload without log parameter instead.");
    }
}
