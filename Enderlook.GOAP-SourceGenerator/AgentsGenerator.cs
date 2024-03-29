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
                        (string name, string file) = GetFile(poolGoal, poolWorld, mergeGoal);
                        context.AddSource(name, SourceText.From(file, Encoding.UTF8));
                    }
                }
            }
        }

        private (string name, string file) GetFile(bool poolGoal, bool poolWorld, bool mergeGoal)
        {
            bool special = poolGoal || poolWorld || mergeGoal ;
            string name = $"AgentWrapper{(poolGoal ? "PoolGoal" : "")}{(poolWorld ? "PoolWorld" : "")}{(mergeGoal ? "MergeGoal" : "")}";
            return (name, $@"
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Enderlook.GOAP.Utilities;
using Enderlook.GOAP.Watchdogs;

namespace Enderlook.GOAP
{{
    internal struct {name}<TWorldState, TGoal, TAction, TGoals, TActions{(special ? ", THelper" : "")}> :
        IAgent<TWorldState, TGoal, TAction>
        {(poolGoal ? ", IGoalPool<TGoal>" : "")}
        {(poolWorld ? ", IWorldStatePool<TWorldState>" : "")}
        {(mergeGoal ? ", IGoalMerge<TGoal>" : "")}
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
        where TAction : IAction<TWorldState, TGoal>
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
            {(special ? "this.helper = helper;" : "")}
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetActions<TAgent, TWatchdog, TLog>(ref PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> builder)
            where TAgent : IAgent<TWorldState, TGoal, TAction>
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
                    {{
#if NET5_0_OR_GREATER
                        ref TAction current = ref MemoryMarshal.GetArrayDataReference(array);
#else
                        ref TAction current = ref MemoryMarshal.GetReference((Span<TAction>)array);
#endif
                        ref TAction end = ref Unsafe.Add(ref current, array.Length);
                        while (Unsafe.IsAddressLessThan(ref current, ref end))
                        {{
                            builder.AddAction(current);
                            current = ref Unsafe.Add(ref current, 1);
                        }}
                        break;
                    }}
                    case List<TAction> list:
                    {{
#if NET5_0_OR_GREATER
                        Span<TAction> span = CollectionsMarshal.AsSpan(list);
                        ref TAction current = ref MemoryMarshal.GetReference(span);
                        ref TAction end = ref Unsafe.Add(ref current, span.Length);
                        while (Unsafe.IsAddressLessThan(ref current, ref end))
                        {{
                            builder.AddAction(current);
                            current = ref Unsafe.Add(ref current, 1);
                        }}
#else
                        for (int i = 0; i < list.Count; i++)
                            builder.AddAction(list[i]);
#endif
                        break;
                    }}
                    case IList<TAction> ilist:
                        for (int i = 0; i < ilist.Count; i++)
                            builder.AddAction(ilist[i]);
                        break;
                    case IReadOnlyList<TAction> irlist:
                        for (int i = 0; i < irlist.Count; i++)
                            builder.AddAction(irlist[i]);
                        break;
                    default:
                        foreach (TAction action in actions)
                            builder.AddAction(action);
                        break;
                }}
            }}
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGoals<TAgent, TWatchdog, TLog>(ref PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> builder)
            where TAgent : IAgent<TWorldState, TGoal, TAction>
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
                        {{
#if NET5_0_OR_GREATER
                            ref TGoal current = ref MemoryMarshal.GetArrayDataReference(array);
#else
                            ref TGoal current = ref MemoryMarshal.GetReference((Span<TGoal>)array);
#endif
                            ref TGoal end = ref Unsafe.Add(ref current, array.Length);
                            while (Unsafe.IsAddressLessThan(ref current, ref end))
                            {{
                                builder.AddGoal(current);
                                current = ref Unsafe.Add(ref current, 1);
                            }}
                            break;
                        }}
                        case List<TGoal> list:
                        {{
#if NET5_0_OR_GREATER
                            Span<TGoal> span = CollectionsMarshal.AsSpan(list);
                            ref TGoal current = ref MemoryMarshal.GetReference(span);
                            ref TGoal end = ref Unsafe.Add(ref current, span.Length);
                            while (Unsafe.IsAddressLessThan(ref current, ref end))
                            {{
                                builder.AddGoal(current);
                                current = ref Unsafe.Add(ref current, 1);
                            }}
#else
                            for (int i = 0; i < list.Count; i++)
                                builder.AddGoal(list[i]);
#endif
                            break;
                        }}
                        case IList<TGoal> ilist:
                            for (int i = 0; i < ilist.Count; i++)
                                builder.AddGoal(ilist[i]);
                            break;
                        case IReadOnlyList<TGoal> irlist:
                            for (int i = 0; i < irlist.Count; i++)
                                builder.AddGoal(irlist[i]);
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
    }}
}}
");
        }

        public void Initialize(GeneratorInitializationContext context) { }
    }
}
