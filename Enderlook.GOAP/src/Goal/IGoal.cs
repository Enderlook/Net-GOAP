﻿namespace Enderlook.GOAP
{
    /// <summary>
    /// Describes a goal.
    /// </summary>
    /// <typeparam name="TWorldState">Type of world state.</typeparam>
    public interface IGoal<TWorldState> where TWorldState : IWorldState<TWorldState>
    {
        /// <summary>
        /// Check if the goal can be satisfied by <paramref name="now"/>. If <see langword="true"/>, satisfy it (e.g: mutate the memory to consume the state).<br/>
        /// If the goal can not be satisfied by <paramref name="now"/>, check if the satisfaction has progressed since <paramref name="before"/> by comparing it wil <paramref name="now"/>.
        /// </summary>
        /// <param name="before">Previous memory state to compare progress towards satisfaction.</param>
        /// <param name="now">Memory to check if it can satify the goal.</param>
        /// <returns>How satisfaction of this goal has progressed.</returns>
        SatisfactionResult CheckAndTrySatisfy(TWorldState before, ref TWorldState now);

        /// <summary>
        /// Check if the goal can be satisfied by <paramref name="worldState"/>. If <see langword="true"/>, satisfy it (e.g: mutate the memory to consume the state).<br/>
        /// </summary>
        /// <param name="worldState">Memory to check if it can satify the goal.</param>
        /// <returns><see langword="true"/> if the goal was satisfied.</returns>
        bool CheckAndTrySatisfy(ref TWorldState worldState)
#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
            => CheckAndTrySatisfy(worldState, ref worldState) == SatisfactionResult.Satisfied
#endif
            ;
    }
}
