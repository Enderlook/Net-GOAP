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
        private static void ThrowNullWorldStateException() => throw new ArgumentNullException("worldState");

        [DoesNotReturn]
        private static void ThrowNullGoalsException() => throw new ArgumentNullException("goals");

        [DoesNotReturn]
        private static void ThrowNullActionsException() => throw new ArgumentNullException("actions");

        [DoesNotReturn]
        private static void ThrowNullPlanException() => throw new ArgumentNullException("plan");

        [DoesNotReturn]
        private static void ThrowNullWatchdogException() => throw new ArgumentNullException("watchdog");

        [DoesNotReturn]
        private static void ThrowPlanIsInProgress() => throw new ArgumentException("plan", "Plan is already in progress.");

        [DoesNotReturn]
        private static void ThrowNullLogException() => throw new ArgumentNullException("log", "Use the overload without log parameter instead.");

        [DoesNotReturn]
        private static void ThrowInvalidHelperType() => throw new ArgumentException("helper", "Must implement any of: IGoalPool<>, IGoalMerge<> or IWorldStatePool<> using the correct parameter types.");
    }
}
