using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    public static partial class Planning
    {
        /// <summary>
        /// Multiple goals will be calculated, but only the goal with lower plan cost will be used.
        /// </summary>
        /// <typeparam name="TGoal">Type of goal.</typeparam>
        public readonly struct CheapestGoal<TGoal>
        {
            internal readonly IEnumerable<TGoal> Goals;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal CheapestGoal(IEnumerable<TGoal> goals)
            {
                if (goals is null)
                    ThrowGoalsNullException();

                Goals = goals;

                [DoesNotReturn]
                static void ThrowGoalsNullException() => throw new ArgumentNullException(nameof(goals));
            }
        }
    }
}
