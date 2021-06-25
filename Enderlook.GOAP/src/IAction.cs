using System.Diagnostics.CodeAnalysis;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Describes an action.
    /// </summary>
    /// <typeparam name="TWorldState">Type of world state.</typeparam>
    /// <typeparam name="TGoal">Type of goal.</typeparam>
    /// <typeparam name="TActionHandle">Type of action handle.</typeparam>
    public interface IAction<TWorldState, TGoal, TActionHandle>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
    {
        /// <summary>
        /// Check if this action meets procedural preconditions.<br/>
        /// Not confuse with the preconditions of <see cref="GetCostAndRequiredGoal(TActionHandle, out float, out TGoal)"/>, this ones are not actually tied to the world state.<br/>
        /// If procedural preconditions are meet, generates an action handle which will be passed to <see cref="GetCostAndRequiredGoal(TActionHandle, out float, out TGoal)"/> and <see cref="ApplyEffect(TWorldState, TActionHandle)"/>.
        /// </summary>
        /// <returns><see langword="true"/> if procedural preconditions are satisfied.</returns>
        bool CheckProceduralPreconditions(TWorldState worldState, [MaybeNullWhen(false)] out TActionHandle handle);

        /// <summary>
        /// Get the cost of execution this action and the preconditions required to execute this action..
        /// </summary>
        /// <param name="handle">Handle got from <see cref="CheckProceduralPreconditions(TWorldState, out TActionHandle)"/>.</param>
        /// <param name="cost">Cost required to execute this action.</param>
        /// <param name="goal">Preconditions requires to execute this action if returns <see langword="true"/>.</param>
        /// <returns>If <see langword="true"/>, <paramref name="goal"/> contains the required preconditions. On <see langword="false"/>, there are no preconditions.</returns>
        bool GetCostAndRequiredGoal(TActionHandle handle, out float cost, out TGoal goal);

        /// <summary>
        /// Applies the effects of this action to a world.<br/>
        /// Note that this method must not consume the required preconditions, if any.<br/>
        /// </summary>
        /// <param name="worldState">World state where effects are being applied.</param>
        /// <param name="handle">Handle got from <see cref="CheckProceduralPreconditions(TWorldState, out TActionHandle)"/>.</param>
        void ApplyEffect(TWorldState worldState, TActionHandle handle);
    }
}
