using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Enderlook.GOAP
{
    /// <summary>
    /// Helper methods to compute GOAP.
    /// </summary>
    public static class Planner
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlanResult Plan<TAgent, TWorld, TAction, TGoal>(
            TAgent agent, Stack<TAction> actions, out TGoal goal, out float cost, CancellationToken token, Action<string> log = null)
            where TAgent : IAgent<TWorld, TGoal, TAction>
            where TWorld : IWorldState<TWorld>
            where TAction : IAction<TWorld, TGoal>
            where TGoal : IGoal<TWorld>
            => Plan<TAgent, TWorld, TAction, TGoal, CancellableWatchdog>(agent, actions, out goal, out cost, new CancellableWatchdog(token), log);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlanResult Plan<TAgent, TWorld, TAction, TGoal>(
            TAgent agent, Stack<TAction> actions, out TGoal goal, out float cost, float maximumCost, Action<string> log = null)
            where TAgent : IAgent<TWorld, TGoal, TAction>
            where TWorld : IWorldState<TWorld>
            where TAction : IAction<TWorld, TGoal>
            where TGoal : IGoal<TWorld>
            => Plan<TAgent, TWorld, TAction, TGoal, CostWatchdog>(agent, actions, out goal, out cost, new CostWatchdog(maximumCost), log);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PlanResult Plan<TAgent, TWorld, TAction, TGoal>(
            TAgent agent, Stack<TAction> actions, out TGoal goal, out float cost, Action<string> log = null)
            where TAgent : IAgent<TWorld, TGoal, TAction>
            where TWorld : IWorldState<TWorld>
            where TAction : IAction<TWorld, TGoal>
            where TGoal : IGoal<TWorld>
            => Plan<TAgent, TWorld, TAction, TGoal, EndlessWatchdog>(agent, actions, out goal, out cost, new EndlessWatchdog(), log);

        private static PlanResult Plan<TAgent, TWorld, TAction, TGoal, TWatchdog>(
            TAgent agent, Stack<TAction> actions, out TGoal goal, out float cost, TWatchdog watchdog, Action<string> log)
            where TAgent : IAgent<TWorld, TGoal, TAction>
            where TWorld : IWorldState<TWorld>
            where TAction : IAction<TWorld, TGoal>
            where TGoal : IGoal<TWorld>
            where TWatchdog : IWatchdog
        {
            if (agent is null)
                ThrowNullPlanException();

            if (actions is null)
                ThrowNullActionsException();

            PlanBuilder<TWorld, TGoal, TAction> builder = Pool<PlanBuilder<TWorld, TGoal, TAction>>.Get();
            PlanResult result;
            if (log is null)
                result = Plan<TAgent, TWorld, TAction, TGoal, TWatchdog, Toggle.No>(
                    agent, builder, actions, out goal, out cost, watchdog);
            else
            {
                builder.log = log;
                result = Plan<TAgent, TWorld, TAction, TGoal, TWatchdog, Toggle.Yes>(
                    agent, builder, actions, out goal, out cost, watchdog);
            }
            Pool<PlanBuilder<TWorld, TGoal, TAction>>.Return(builder);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static PlanResult Plan<TAgent, TWorld, TAction, TGoal, TWatchdog, TLog>(
            TAgent agent, PlanBuilder<TWorld, TGoal, TAction> builder, Stack<TAction> actions,
            out TGoal goal, out float cost, TWatchdog watchdog)
            where TAgent : IAgent<TWorld, TGoal, TAction>
            where TWorld : IWorldState<TWorld>
            where TAction : IAction<TWorld, TGoal>
            where TGoal : IGoal<TWorld>
            where TWatchdog : IWatchdog
        {
            Debug.Assert(builder is not null);

            if (typeof(TAgent).IsValueType)
                return PlanInner<TAgent, TWorld, TAction, TGoal, TWatchdog, TLog>(builder, agent, actions, out goal, out cost, watchdog);

#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
            IAgent<TWorld, TGoal, TAction> plan_ = agent;
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

            Type planType = agent.GetType();
            if (typeof(IGoalPool<TGoal>).IsAssignableFrom(planType))
            {
                if (typeof(IWorldStatePool<TWorld>).IsAssignableFrom(planType))
                {
                    if (typeof(IGoalMerge<TGoal>).IsAssignableFrom(planType))
                        return PlanInner<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorld, TAction, TGoal>, TWorld, TAction, TGoal, TWatchdog, TLog>(
                            builder, new(plan_), actions, out goal, out cost, watchdog);
                    return PlanInner<AgentWrapperPoolGoalPoolWorld<TWorld, TAction, TGoal>, TWorld, TAction, TGoal, TWatchdog, TLog>(
                        builder, new(plan_), actions, out goal, out cost, watchdog);
                }
                if (typeof(IGoalMerge<TGoal>).IsAssignableFrom(planType))
                    return PlanInner<AgentWrapperPoolGoalMergeGoal<TWorld, TAction, TGoal>, TWorld, TAction, TGoal, TWatchdog, TLog>(
                        builder, new(plan_), actions, out goal, out cost, watchdog);
                return PlanInner<AgentWrapperPoolGoal<TWorld, TAction, TGoal>, TWorld, TAction, TGoal, TWatchdog, TLog>(
                    builder, new(plan_), actions, out goal, out cost, watchdog);
            }

            if (typeof(IWorldStatePool<TWorld>).IsAssignableFrom(planType))
            {
                if (typeof(IGoalMerge<TGoal>).IsAssignableFrom(planType))
                    return PlanInner<AgentWrapperPoolWorldMergeGoal<TWorld, TAction, TGoal>, TWorld, TAction, TGoal, TWatchdog, TLog>(
                        builder, new(plan_), actions, out goal, out cost, watchdog);
                return PlanInner<AgentWrapperPoolWorld<TWorld, TAction, TGoal>, TWorld, TAction, TGoal, TWatchdog, TLog>(
                    builder, new(plan_), actions, out goal, out cost, watchdog);
            }

            if (typeof(IGoalMerge<TGoal>).IsAssignableFrom(planType))
                return PlanInner<AgentWrapperMergeGoal<TWorld, TAction, TGoal>, TWorld, TAction, TGoal, TWatchdog, TLog>(
                    builder, new(plan_), actions, out goal, out cost, watchdog);

            return PlanInner<AgentWrapper<TWorld, TAction, TGoal>, TWorld, TAction, TGoal, TWatchdog, TLog>(
                builder, new(plan_), actions, out goal, out cost, watchdog);
        }

        private static PlanResult PlanInner<TAgent, TWorld, TAction, TGoal, TWatchdog, TLog>(
            PlanBuilder<TWorld, TGoal, TAction> builder, TAgent agent, Stack<TAction> actions, out TGoal goal, out float cost, TWatchdog watchdog)
            where TAgent : IAgent<TWorld, TGoal, TAction>
            where TWorld : IWorldState<TWorld>
            where TAction : IAction<TWorld, TGoal>
            where TGoal : IGoal<TWorld>
            where TWatchdog : IWatchdog
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");
            Toggle.Assert<TLog>();

            using IEnumerator<TAction> availableActions = Initialize();

            while (builder.TryDequeue<TAgent, TLog>(out int id, out float currentCost, out Goals<TWorld, TGoal> currentGoals, out TWorld currentMemory))
            {
                if (!watchdog.CanContinue(currentCost))
                {
                    builder.Cancel<TLog>();
                    break;
                }

                while (availableActions.MoveNext())
                {
                    TAction action = availableActions.Current;

                    if (Toggle.IsOn<TLog>())
                    {
                        builder.AppendToLog(" - Check with action: ");
                        builder.AppendToLog(action.ToString());
                        builder.AppendToLog(" ");
                        builder.AppendToLog(currentMemory.ToString());
                        builder.AppendToLog(" -> ");
                    }

                    TWorld newMemory = Clone(currentMemory);
                    action.ApplyEffect(newMemory);

                    if (Toggle.IsOn<TLog>())
                        builder.AppendAndLog(newMemory.ToString());

                    bool shallReturnNewMemory;
                    bool canContinue;
                    bool hasProgressed = false;
                    int index = 0;
                    do
                    {
                        int currentIndex = index;
                        canContinue = currentGoals.TryMoveNext(ref index, out TGoal currentGoal);

                        if (Toggle.IsOn<TLog>())
                        {
                            builder.AppendToLog("   - Check goal: ");
                            builder.AppendToLog(currentGoal.ToString());
                        }

                        switch (currentGoal.CheckAndTrySatisfy(currentMemory, newMemory))
                        {
                            case SatisfactionResult.Satisfied:
                            {
                                if (Toggle.IsOn<TLog>())
                                    builder.AppendToLog(". Satisfied.\n     ");

                                shallReturnNewMemory = true;
                                TWorld newMemory_ = newMemory;
                                newMemory = Clone(currentMemory);
                                action.ApplyEffect(newMemory);

                                float newCost = currentCost + action.GetCost();
                                if (action.TryGetRequiredGoal(out TGoal requiredGoal))
                                {
                                    Goals<TWorld, TGoal> newGoals = currentGoals.WithReplacement(index, requiredGoal);
                                    builder.Enqueue<TLog>(id, action, newCost, newGoals, newMemory_);
                                }
                                else if (currentGoals.Without(currentIndex, out Goals<TWorld, TGoal> newGoals))
                                    builder.Enqueue<TLog>(id, action, newCost, newGoals, newMemory_);
                                else
                                    FoundValidPath(id, newCost, action, newMemory_);
                                break;
                            }
                            case SatisfactionResult.Progressed:
                            {
                                if (Toggle.IsOn<TLog>())
                                    builder.AppendToLog(". Progressed.\n     ");

                                if (hasProgressed)
                                {
                                    shallReturnNewMemory = true;
                                    break;
                                }

                                hasProgressed = true;
                                shallReturnNewMemory = false;

                                float newCost = currentCost + action.GetCost();
                                if (action.TryGetRequiredGoal(out TGoal requiredGoal))
                                {
                                    Goals<TWorld, TGoal> newGoals = currentGoals.WithAdd<TAgent, TAction>(agent, requiredGoal);
                                    builder.Enqueue<TLog>(id, action, newCost, newGoals, newMemory);
                                }
                                else
                                    builder.Enqueue<TLog>(id, action, newCost, currentGoals, newMemory);
                                break;
                            }
                            case SatisfactionResult.NotProgressed:
                            {
                                if (Toggle.IsOn<TLog>())
                                    builder.AppendAndLog(". Not progressed.");
                                shallReturnNewMemory = true;
                                break;
                            }
                            default:
                            {
                                ThrowInvalidSatisfactionResultException();
                                break;
                            }
                        }
                    } while (canContinue);

                    if (shallReturnNewMemory && typeof(IWorldStatePool<TWorld>).IsAssignableFrom(typeof(TAgent)))
                        ((IWorldStatePool<TWorld>)agent).Return(newMemory);
                }

                availableActions.Reset();

                currentGoals.Return(agent);
                if (typeof(IWorldStatePool<TWorld>).IsAssignableFrom(typeof(TAgent)))
                    ((IWorldStatePool<TWorld>)agent).Return(currentMemory);
            }

            return builder.Finalize<TAgent, TLog>(agent, actions, out goal, out cost);

            [MethodImpl(MethodImplOptions.NoInlining)]
            IEnumerator<TAction> Initialize()
            {
                TWorld memory = agent.GetWorldState();

                if (Toggle.IsOn<TLog>())
                {
                    builder.AppendToLog("Start planning. Initial memory ");
                    builder.AppendAndLog(memory.ToString());
                }

                using IEnumerator<TGoal> goals = agent.GetGoals();

                if (Toggle.IsOn<TLog>())
                {
                    builder.AppendToLog("Goals:");
                    bool has = false;
                    while (goals.MoveNext())
                    {
                        has = true;
                        builder.AppendToLog("\n - ");
                        builder.AppendToLog(goals.Current.ToString());
                    }
                    if (!has)
                        builder.AppendToLog("\n - <>");
                    builder.Log();
                    goals.Reset();
                }

                if (!goals.MoveNext())
                    throw new InvalidOperationException("Must have at least one goal.");

                EnqueueGoal();
                while (goals.MoveNext())
                    EnqueueGoal();

                IEnumerator<TAction> actions = agent.GetActions();
                try
                {
                    if (Toggle.IsOn<TLog>())
                    {
                        builder.AppendToLog("Actions:");
                        bool has = false;
                        while (actions.MoveNext())
                        {
                            has = true;
                            builder.AppendToLog("\n - ");
                            builder.AppendToLog(actions.Current.ToString());
                        }
                        if (!has)
                            builder.AppendToLog("\n - <>");
                        builder.Log();
                        actions.Reset();
                    }

                    if (!actions.MoveNext())
                        throw new InvalidOperationException("Must have at least one action.");
                    actions.Reset();

                    return actions;
                }
                catch
                {
                    actions.Dispose();
                    throw;
                }

                void EnqueueGoal()
                {
                    TWorld newMemory;
                    if (typeof(IWorldStatePool<TWorld>).IsAssignableFrom(typeof(TAgent)))
                        newMemory = ((IWorldStatePool<TWorld>)agent).Clone(memory);
                    else
                        newMemory = memory.Clone();
                    builder.EnqueueGoal<TLog>(goals.Current, newMemory);
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            void FoundValidPath(int id, float cost, TAction action, TWorld newMemory)
            {
                if (typeof(IWorldStatePool<TWorld>).IsAssignableFrom(typeof(TAgent))) 
                    ((IWorldStatePool<TWorld>)agent).Return(newMemory);
                builder.EnqueueValidPath<TLog>(id, action, cost + action.GetCost());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            TWorld Clone(TWorld currentMemory)
            {
                if (typeof(IWorldStatePool<TWorld>).IsAssignableFrom(typeof(TAgent)))
                    return ((IWorldStatePool<TWorld>)agent).Clone(currentMemory);
                else
                    return currentMemory.Clone();
            }

            static void ThrowInvalidSatisfactionResultException() => throw new InvalidOperationException($"Returned value is not a valid value of {nameof(SatisfactionResult)}");
        }

        private static void ThrowNullActionsException() => throw new ArgumentNullException("actions");

        private static void ThrowNullPlanException() => throw new ArgumentNullException("plan");
    }
}
