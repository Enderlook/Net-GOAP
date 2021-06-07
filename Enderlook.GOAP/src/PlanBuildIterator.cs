using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal struct PlanBuildIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> : IDisposable
        where TAgent : IAgent<TWorldState, TGoal, TAction>
        where TWorldState : IWorldState<TWorldState>
        where TAction : IAction<TWorldState, TGoal>
        where TGoal : IGoal<TWorldState>
        where TWatchdog : IWatchdog
    {
        private TAgent agent;
        private PlanBuilder<TWorldState, TGoal, TAction>? builder;
        private TWatchdog watchdog;
        private Stack<TAction> actions;
        private IEnumerator<TAction>? availableActions;

        public PlanBuildIterator(TAgent agent, Stack<TAction> actions, TWatchdog watchdog, Action<string>? log = null)
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");
            Toggle.Assert<TLog>();

            if (Toggle.IsOn<TLog>())
                Debug.Assert(log is not null, "Log is enabled, but log is null.");

            if (agent is null)
                ThrowNullAgentException();
            if (actions is null)
                ThrowNullActionsException();
            if (watchdog is null)
                ThrowNullWatchdogException();

            this.agent = agent;
            this.actions = actions;
            this.watchdog = watchdog;
            builder = Pool<PlanBuilder<TWorldState, TGoal, TAction>>.Get();
            builder.log = log;
            availableActions = default;

            [DoesNotReturn]
            static void ThrowNullAgentException() => throw new ArgumentNullException(nameof(agent));

            [DoesNotReturn]
            static void ThrowNullActionsException() => throw new ArgumentNullException(nameof(actions));

            [DoesNotReturn]
            static void ThrowNullWatchdogException() => throw new ArgumentNullException(nameof(watchdog));
        }

        public void Dispose()
        {
            if (builder is null)
                return;
            builder.Clear(agent);
            Pool<PlanBuilder<TWorldState, TGoal, TAction>>.Return(builder);
            builder = null;
        }

        public static PlanResult RunAndDispose(TAgent agent, Stack<TAction> plan, TWatchdog watchdog, out TGoal? goal, out float cost, Action<string>? log = null)
        {
            using PlanBuildIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> iterator = new(agent, plan, watchdog, log);
            iterator.Initialize();
            Iterate();
            PlanResult result = iterator.Finalize(out goal, out cost);
            iterator.Dispose();
            return result;

            void Iterate()
            {
                while (iterator.MoveNext()) ;
            }
        }

        public void Initialize()
        {
            EnqueueGoals();
            GetActions();
        }

        public PlanResult Finalize(out TGoal? goal, out float cost)
        {
            Debug.Assert(builder is not null, "Is disposed.");
            return builder.Finalize<TAgent, TLog>(actions, out goal, out cost);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            Debug.Assert(builder is not null, "Is disposed.");
            if (builder.TryDequeue<TAgent, TLog>(out int id, out float currentCost, out int currentGoalIndex, out TWorldState? currentMemory))
            {
                if (!watchdog.CanContinue(currentCost))
                {
                    builder.Cancel<TLog>();
                    return false;
                }

                Debug.Assert(availableActions is not null, "Initialize method was not called.");
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

                    TWorldState newMemory;
                    if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                        newMemory = ((IWorldStatePool<TWorldState>)agent).Clone(currentMemory);
                    else
                        newMemory = currentMemory.Clone();
                    action.ApplyEffect(newMemory);

                    if (Toggle.IsOn<TLog>())
                        builder.AppendToLog(newMemory.ToString());

                    PlanBuilder<TWorldState, TGoal, TAction>.GoalNode currentGoal = builder.GetGoal(currentGoalIndex);

                    if (Toggle.IsOn<TLog>())
                    {
                        builder.AppendToLog(". And goal: ");
                        builder.AppendToLog(currentGoal.Goal.ToString());
                    }

                    switch (currentGoal.Goal.CheckAndTrySatisfy(currentMemory, newMemory))
                    {
                        case SatisfactionResult.Satisfied:
                        {
                            if (Toggle.IsOn<TLog>())
                                builder.AppendToLog(". Satisfied.\n   ");

                            float newCost = currentCost + action.GetCost();
                            if (action.TryGetRequiredGoal(out TGoal requiredGoal))
                            {
                                int newGoals = currentGoal.WithReplacement(builder, requiredGoal);
                                builder.Enqueue<TLog>(id, action, newCost, newGoals, newMemory);
                            }
                            else if (currentGoal.WithPop(out int newGoals))
                                builder.Enqueue<TLog>(id, action, newCost, newGoals, newMemory);
                            else
                                FoundValidPath(ref this, id, newCost, action, newMemory);

                            break;
                        }
                        case SatisfactionResult.Progressed:
                        {
                            if (Toggle.IsOn<TLog>())
                                builder.AppendToLog(". Progressed.\n   ");

                            float newCost = currentCost + action.GetCost();
                            if (action.TryGetRequiredGoal(out TGoal requiredGoal))
                            {
                                int newGoals = PlanBuilder<TWorldState, TGoal, TAction>.GoalNode.WithPush(builder, currentGoalIndex, requiredGoal);
                                builder.Enqueue<TLog>(id, action, newCost, newGoals, newMemory);
                            }
                            else
                                builder.Enqueue<TLog>(id, action, newCost, currentGoalIndex, newMemory);
                            break;
                        }
                        case SatisfactionResult.NotProgressed:
                        {
                            if (Toggle.IsOn<TLog>())
                                builder.AppendAndLog(". Not progressed.");
                            if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                                ((IWorldStatePool<TWorldState>)agent).Return(newMemory);
                            break;
                        }
                        default:
                        {
                            ThrowInvalidSatisfactionResultException();
                            break;
                        }
                    }
                }

                availableActions.Reset();

                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    ((IWorldStatePool<TWorldState>)agent).Return(currentMemory);

                return true;
            }

            return false;

            static void FoundValidPath(ref PlanBuildIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> self, int id, float cost, TAction action, TWorldState newMemory)
            {
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    ((IWorldStatePool<TWorldState>)self.agent).Return(newMemory);
                self.builder.EnqueueValidPath<TLog>(id, action, cost);
            }

            [DoesNotReturn]
            static void ThrowInvalidSatisfactionResultException() => throw new InvalidOperationException($"Returned value is not a valid value of {nameof(SatisfactionResult)}");
        }

        private void GetActions()
        {
            availableActions = agent.GetActions();
            try
            {
                if (availableActions is null)
                {
                    if (Toggle.IsOn<TLog>())
                        builder.AppendAndLog("Error: actions is null.");
                    ThrowActionsIsNullException();
                }

                if (Toggle.IsOn<TLog>())
                {
                    builder.AppendToLog("Actions:");
                    bool has = false;
                    while (availableActions.MoveNext())
                    {
                        has = true;
                        builder.AppendToLog("\n - ");
                        builder.AppendToLog(availableActions.Current.ToString());
                    }
                    if (!has)
                        builder.AppendToLog("\n - <>");
                    builder.Log();
                    availableActions.Reset();
                }

                if (!availableActions.MoveNext())
                {
                    if (Toggle.IsOn<TLog>())
                        builder.AppendAndLog("Error: actions is empty.");
                    ThrowActionsIsEmptyException();
                }

                availableActions.Reset();
            }
            catch
            {
                availableActions.Dispose();
                throw;
            }

            [DoesNotReturn]
            static void ThrowActionsIsEmptyException() => throw new InvalidOperationException("Must have at least one action.");

            [DoesNotReturn]
            static void ThrowActionsIsNullException() => throw new InvalidOperationException("Actions can't be null.");
        }

        private void EnqueueGoals()
        {
            TWorldState memory = agent.GetWorldState();

            if (memory is null)
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: world state is null.");
                ThrowWorldStateIsNullException();
            }

            if (Toggle.IsOn<TLog>())
            {
                builder.AppendToLog("Start planning. Initial memory ");
                builder.AppendAndLog(memory.ToString());
            }

            using IEnumerator<TGoal> goals = agent.GetGoals();

            if (goals is null)
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: goals is null.");
                ThrowGoalsIsNullException();
            }

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
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: goals is empty.");
                ThrowGoalsIsEmptyException();
            }

            EnqueueGoal(ref this);
            while (goals.MoveNext())
                EnqueueGoal(ref this);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void EnqueueGoal(ref PlanBuildIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> self)
            {
                TWorldState newMemory;
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    newMemory = ((IWorldStatePool<TWorldState>)self.agent).Clone(memory);
                else
                    newMemory = memory.Clone();
                self.builder.EnqueueGoal<TLog>(goals.Current, newMemory);
            }

            [DoesNotReturn]
            static void ThrowWorldStateIsNullException() => throw new InvalidOperationException("World state can't be null.");

            [DoesNotReturn]
            static void ThrowGoalsIsEmptyException() => throw new InvalidOperationException("Must have at least one goal.");

            [DoesNotReturn]
            static void ThrowGoalsIsNullException() => throw new InvalidOperationException("Goals can't be null.");
        }
    }
}
