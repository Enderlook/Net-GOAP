using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Text;

namespace Enderlook.GOAP
{
    [Generator]
    internal sealed class AgentsGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            ReadOnlySpan<bool> boolean = stackalloc[] { false, true };
            foreach (bool poolGoal in boolean)
            {
                foreach (bool poolWorld in boolean)
                {
                    foreach (bool mergeGoal in boolean)
                    {
                        (string name, string file) = GetFile(poolGoal, poolWorld, mergeGoal);
                        context.AddSource(name, SourceText.From(file, Encoding.UTF8));
                    }
                }
            }
        }

        private (string name, string file) GetFile(bool poolGoal, bool poolWorld, bool mergeGoal)
        {
            string name = $"AgentWrapper{(poolGoal ? "PoolGoal" : "")}{(poolWorld ? "PoolWorld" : "")}{(mergeGoal ? "MergeGoal" : "")}";
            return (name, $@"
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{{
    internal struct {name}
        <TWorldState, TAction, TGoal> :
        IAgent<TWorldState, TGoal, TAction>
        {(poolGoal ? ", IGoalPool<TGoal>" : "")}
        {(poolWorld ? ", IWorldStatePool<TWorldState>" : "")}
        {(mergeGoal ? ", IGoalMerge<TGoal>" : "")}
        where TWorldState : IWorldState<TWorldState>
        where TAction : IAction<TWorldState, TGoal>
        where TGoal : IGoal<TWorldState>
    {{
        private readonly IAgent<TWorldState, TGoal, TAction> plan;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public {name}(IAgent<TWorldState, TGoal, TAction> plan)
        {{
            {(poolGoal ? "Debug.Assert(typeof(IGoalPool<TGoal>).IsAssignableFrom(plan.GetType()));" : "")}
            {(poolWorld ? "Debug.Assert(typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(plan.GetType()));" : "")}
            {(mergeGoal ? "Debug.Assert(typeof(IGoalMerge<TGoal>).IsAssignableFrom(plan.GetType()));" : "")}
            this.plan = plan;
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<TAction> GetActions() => plan.GetActions();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerator<TGoal> GetGoals() => plan.GetGoals();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TWorldState GetWorldState() => plan.GetWorldState();

{(mergeGoal ? @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryMerge(TGoal a, TGoal b, out TGoal c) => Unsafe.As<IGoalMerge<TGoal>>(plan).TryMerge(a, b, out c);
" : "" )}

{(poolGoal ? @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(TGoal value) => Unsafe.As<IGoalPool<TGoal>>(plan).Return(value);
" : "" )}

{(poolWorld ? @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TWorldState Clone(TWorldState value) => Unsafe.As<IWorldStatePool<TWorldState>>(plan).Clone(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(TWorldState value) => Unsafe.As<IWorldStatePool<TWorldState>>(plan).Return(value);
" : "" )}
    }}
}}
");
        }

        public void Initialize(GeneratorInitializationContext context) { }
    }
}
