using Enderlook.GOAP.Watchdogs;

using System;
using System.Diagnostics.CodeAnalysis;

namespace Enderlook.GOAP
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        public static void ThrowArgumentException_HelperTypeMistmach() => throw new ArgumentException("Must implement any of IGoalPool<TGoal>, IWorldStatePool<TWorldState>, IGoalMerge<TGoal>. Using the generic parameters provided by the method.", "helper");

        [DoesNotReturn]
        public static void ThrowArgumentException_InstanceIsDefault() => throw new ArgumentException("Instance is default.", "this");

        [DoesNotReturn]
        public static void ThrowArgumentNullException_Helper() => throw new ArgumentNullException("helper");

        [DoesNotReturn]
        public static void ThrowArgumentNullException_Actions() => throw new ArgumentNullException("actions");

        [DoesNotReturn]
        public static void ThrowArgumentNullException_Goals() => throw new ArgumentNullException("goals");

        [DoesNotReturn]
        public static void ThrowArgumentNullException_Plan() => throw new ArgumentNullException("plan");

        [DoesNotReturn]
        public static void ThrowArgumentNullException_Watchdog() => throw new ArgumentNullException("watchdog");

        [DoesNotReturn]
        public static void ThrowArgumentNullException_WorldState() => throw new ArgumentNullException("worldState");

        public static void ThrowArgumentOutOfRangeException_CostMustBeGreaterThanZero() => throw new ArgumentOutOfRangeException("cost", "Must be greater than 0.");

        public static void ThrowArgumentOutOfRangeException_MaximumMilisecondsMustBeGreaterThanZero() => throw new ArgumentException("maximumMiliseconds", "Must be greater than 0.");

        public static void ThrowObjectDisposedException_Planning() => throw new ObjectDisposedException("Planning");

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ActionIsNull() => throw new InvalidOperationException("Action can't be null.");

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_ActionsIsEmpty() => throw new InvalidOperationException("Must have at least one action.");

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_GoalIsNull() => throw new InvalidOperationException("Goal can't be null.");

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_GoalsIsEmpty() => throw new InvalidOperationException("Must have at least one goal.");

        public static void ThrowInvalidOperationException_PlanificationIsAlreadyFinalized() => throw new InvalidOperationException("Planification already finalized.");

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_SatisfactionResultIsInvalid() => throw new InvalidOperationException($"Returned value is not a valid value of {nameof(SatisfactionResult)}.");

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_WatchdogResultIsInvalid() => throw new InvalidOperationException($"Returned value is not a valid value of {nameof(WatchdogResult)}.");

        [DoesNotReturn]
        public static void ThrowInvalidOperationException_WorldStateIsNull() => throw new InvalidOperationException("World state can't be null.");
    }
}

