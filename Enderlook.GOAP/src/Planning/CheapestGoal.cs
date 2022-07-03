using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP.Planning
{
    /// <summary>
    /// This type is an implementation detail an shall never be used in parameters, fields, variables or return types.
    /// </summary>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    public readonly struct CheapestGoal<TGoal>
    {
        internal readonly IEnumerable<TGoal> Goals;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal CheapestGoal(IEnumerable<TGoal> goals)
        {
            if (goals is null)
                ThrowHelper.ThrowArgumentNullException_Goals();

            Goals = goals;
        }
    }
}
