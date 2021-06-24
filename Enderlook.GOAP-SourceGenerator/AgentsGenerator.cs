﻿using Microsoft.CodeAnalysis;
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
                        foreach (bool poolActionHandle in boolean)
                        {
                            (string name, string file) = GetFile(poolGoal, poolWorld, mergeGoal, poolActionHandle);
                            context.AddSource(name, SourceText.From(file, Encoding.UTF8));
                        }
                    }
                }
            }
        }

        private (string name, string file) GetFile(bool poolGoal, bool poolWorld, bool mergeGoal, bool poolActionHandle)
        {
            bool special = poolGoal || poolWorld || mergeGoal || poolActionHandle;
            string name = $"AgentWrapper{(poolGoal ? "PoolGoal" : "")}{(poolWorld ? "PoolWorld" : "")}{(mergeGoal ? "MergeGoal" : "")}{(poolActionHandle ? "PoolActionHandle" : "")}";
            return (name, $@"
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using Enderlook.GOAP.Utilities;
using Enderlook.GOAP.Watchdogs;

namespace Enderlook.GOAP
{{
    internal struct {name}<TWorldState, TGoal, TAction, TActionHandle, TGoals, TActions{(special ? ", THelper" : "")}> :
        IAgent<TWorldState, TGoal, TAction, TActionHandle>
        {(poolGoal ? ", IGoalPool<TGoal>" : "")}
        {(poolWorld ? ", IWorldStatePool<TWorldState>" : "")}
        {(mergeGoal ? ", IGoalMerge<TGoal>" : "")}
        {(poolActionHandle ? ", IActionHandlePool<TActionHandle>" : "")}
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
        where TAction : IAction<TWorldState, TGoal, TActionHandle>
        where TActions : IEnumerable<TAction>
    {{
        private TWorldState worldState;
        private TGoals goals;
        private TActions actions;
        {(special ? "private THelper helper;" : "")}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public {name}(TWorldState worldState, TGoals goals, TActions actions{(special ? ", THelper helper" : "")})
        {{
            this.worldState = worldState;
            this.goals = goals;
            this.actions = actions;
            {(poolGoal ? "Debug.Assert(typeof(IGoalPool<TGoal>).IsAssignableFrom(helper.GetType()));" : "")}
            {(poolWorld ? "Debug.Assert(typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(helper.GetType()));" : "")}
            {(mergeGoal ? "Debug.Assert(typeof(IGoalMerge<TGoal>).IsAssignableFrom(helper.GetType()));" : "")}
            {(poolActionHandle ? "Debug.Assert(typeof(IActionHandlePool<TActionHandle>).IsAssignableFrom(helper.GetType()));" : "")}
            {(special ? "this.helper = helper;" : "")}
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetActions<TAgent, TWatchdog, TLog>(ref PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TActionHandle, TWatchdog, TLog> builder)
            where TAgent : IAgent<TWorldState, TGoal, TAction, TActionHandle>
            where TWatchdog : IWatchdog
        {{
            if (typeof(TActions).IsValueType)
            {{
                if (typeof(IList<TAction>).IsAssignableFrom(typeof(TActions)))
                {{
                    int count = ((IList<TAction>)actions).Count;
                    for (int i = 0; i < count; i++)
                        builder.AddAction(((IList<TAction>)actions)[i]);
                }}
                else
                {{
                    foreach (TAction action in actions)
                        builder.AddAction(action);
                }}
            }}
            else
            {{
                switch (actions)
                {{
                    case TAction[] array:
                        for (int i = 0; i < array.Length; i++)
                            builder.AddAction(array[i]);
                        break;
                    case List<TAction> list:
                        for (int i = 0; i < list.Count; i++)
                            builder.AddAction(list[i]);
                        break;
                    case IList<TAction> ilist:
                        for (int i = 0; i < ilist.Count; i++)
                            builder.AddAction(ilist[i]);
                        break;
                    default:
                        foreach (TAction action in actions)
                            builder.AddAction(action);
                        break;
                }}
            }}
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGoals<TAgent, TWatchdog, TLog>(ref PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TActionHandle, TWatchdog, TLog> builder)
            where TAgent : IAgent<TWorldState, TGoal, TAction, TActionHandle>
            where TWatchdog : IWatchdog
        {{
            Debug.Assert(typeof(TGoals) == typeof(Planning.SingleGoal<TGoal>) || typeof(TGoals) == typeof(Planning.CheapestGoal<TGoal>));

            switch (goals)
            {{
                case Planning.SingleGoal<TGoal> singleGoal:
                    builder.AddGoal(singleGoal.Goal);
                    break;
                case Planning.CheapestGoal<TGoal> cheapestGoal:
                    switch (cheapestGoal.Goals)
                    {{
                        case TGoal[] array:
                            for (int i = 0; i < array.Length; i++)
                                builder.AddGoal(array[i]);
                            break;
                        case List<TGoal> list:
                            for (int i = 0; i < list.Count; i++)
                                builder.AddGoal(list[i]);
                            break;
                        case IList<TGoal> ilist:
                            for (int i = 0; i < ilist.Count; i++)
                                builder.AddGoal(ilist[i]);
                            break;
                        default:
                            foreach (TGoal goal in cheapestGoal.Goals)
                                builder.AddGoal(goal);
                            break;
                    }}
                break;
            }}
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TWorldState GetWorldState() => worldState;

{(mergeGoal ? @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryMerge(TGoal a, TGoal b, out TGoal c)
        {
            if (typeof(THelper).IsValueType)
                return ((IGoalMerge<TGoal>)helper).TryMerge(a, b, out c);
            else
                return Unsafe.As<IGoalMerge<TGoal>>(helper).TryMerge(a, b, out c);
        }
" : "" )}

{(poolGoal ? @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(TGoal value)
        {
            if (typeof(THelper).IsValueType)
                ((IGoalPool<TGoal>)helper).Return(value);
            else
                Unsafe.As<IGoalPool<TGoal>>(helper).Return(value);
        }
" : "" )}

{(poolWorld ? @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TWorldState Clone(TWorldState value)
        {
            if (typeof(THelper).IsValueType)
                return ((IWorldStatePool<TWorldState>)helper).Clone(value);
            else
                return Unsafe.As<IWorldStatePool<TWorldState>>(helper).Clone(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(TWorldState value)
        {
            if (typeof(THelper).IsValueType)
                ((IWorldStatePool<TWorldState>)helper).Return(value);
            else
                Unsafe.As<IWorldStatePool<TWorldState>>(helper).Return(value);
        }
" : "" )}

{(poolActionHandle ? @"
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(TActionHandle value)
        {
            if (typeof(THelper).IsValueType)
                ((IActionHandlePool<TActionHandle>)helper).Return(value);
            else
                Unsafe.As<IActionHandlePool<TActionHandle>>(helper).Return(value);
        }
" : "")}
    }}
}}
");
        }

        public void Initialize(GeneratorInitializationContext context) { }
    }
}
