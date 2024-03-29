﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP.Planning
{
    /// <summary>
    /// This type is an implementation detail an shall never be used in parameters, fields, variables or return types.
    /// </summary>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    public readonly struct SingleGoal<TGoal>
    {
        internal readonly TGoal Goal;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal SingleGoal(TGoal goal)
        {
            if (goal is null)
                ThrowGoalNullException();

            Goal = goal;

            [DoesNotReturn]
            static void ThrowGoalNullException() => throw new ArgumentNullException(nameof(goal));
        }
    }
}