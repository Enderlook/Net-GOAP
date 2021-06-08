using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Enderlook.GOAP
{
    internal struct PlanBuilderIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> : IDisposable
        where TAgent : IAgent<TWorldState, TGoal, TAction>
        where TWorldState : IWorldState<TWorldState>
        where TAction : IAction<TWorldState, TGoal>
        where TGoal : IGoal<TWorldState>
        where TWatchdog : IWatchdog
    {
        private TAgent agent;
        private PlanBuilderState<TWorldState, TGoal, TAction>? builder;
        private TWatchdog watchdog;
        private Stack<TAction> plan;
        private float lastCost;

        public PlanBuilderIterator(TAgent agent, Stack<TAction> plan, TWatchdog watchdog, Action<string>? log = null)
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");
            Toggle.Assert<TLog>();

            if (agent is null)
                ThrowNullAgentException();
            if (plan is null)
                ThrowNullActionsException();
            if (watchdog is null)
                ThrowNullWatchdogException();

            this.agent = agent;
            this.plan = plan;
            this.watchdog = watchdog;
            lastCost = 0;
            builder = Pool<PlanBuilderState<TWorldState, TGoal, TAction>>.Get();

            if (Toggle.IsOn<TLog>())
            {
                Debug.Assert(log is not null, "Log is enabled, but log is null.");
                builder.SetLog(log);
            }

            [DoesNotReturn]
            static void ThrowNullAgentException() => throw new ArgumentNullException(nameof(agent));

            [DoesNotReturn]
            static void ThrowNullActionsException() => throw new ArgumentNullException(nameof(plan));

            [DoesNotReturn]
            static void ThrowNullWatchdogException() => throw new ArgumentNullException(nameof(watchdog));
        }

        public void Dispose()
        {
            if (builder is null)
                return;

            builder.Clear(agent);
            Pool<PlanBuilderState<TWorldState, TGoal, TAction>>.Return(builder);
            builder = null;
        }

        public static PlanResult<TGoal, TAction> RunAndDispose(TAgent agent, Stack<TAction> plan, TWatchdog watchdog, Action<string>? log = null)
        {
            using PlanBuilderIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> iterator = new(agent, plan, watchdog, log);
            iterator.Initialize();
            while (true)
            {
                switch (iterator.MoveNext())
                {
                    case PlanningCoroutineResult.Cancelled:
                    case PlanningCoroutineResult.Finalized:
                        goto end;
                    case PlanningCoroutineResult.Suspended:
                        Thread.Yield();
                        break;
                }
            }
            end:
            return iterator.Finalize();
        }

        public static async ValueTask<PlanResult<TGoal, TAction>> RunAndDisposeAsync(TAgent agent, Stack<TAction> plan, TWatchdog watchdog, Action<string>? log = null)
        {
            using PlanBuilderIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> iterator = new(agent, plan, watchdog, log);
            iterator.Initialize();
            while (true)
            {
                switch (iterator.MoveNext())
                {
                    case PlanningCoroutineResult.Cancelled:
                    case PlanningCoroutineResult.Finalized:
                        goto end;
                    case PlanningCoroutineResult.Suspended:
                        await Task.Yield();
                        break;
                }
            }
            end:
            return iterator.Finalize();
        }

        public static PlanningCoroutine<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> RunAndDisposeCorotuine(TAgent agent, Stack<TAction> plan, TWatchdog watchdog, Action<string>? log = null)
            => new(new(agent, plan, watchdog, log));

        public void Initialize()
        {
            EnqueueGoals();
            GetActions();
        }

        public PlanResult<TGoal, TAction> Finalize()
        {
            Debug.Assert(builder is not null, "Is disposed.");
            return builder.Finalize<TAgent, TLog>(plan);
        }

        public PlanningCoroutineResult MoveNext()
        {
            PlanBuilderState<TWorldState, TGoal, TAction>? builder = this.builder;
            Debug.Assert(builder is not null, "Is disposed.");

            switch (watchdog.Poll(lastCost))
            {
                case WatchdogResult.Continue:
                    break;
                case WatchdogResult.Cancel:
                    builder.Cancel<TLog>();
                    return PlanningCoroutineResult.Cancelled;
                case WatchdogResult.Suspend:
                    return PlanningCoroutineResult.Suspended;
                default:
                    ThrowInvalidWatchdogResultException();
                    break;
            }

            if (builder.TryDequeue<TAgent, TLog>(out int id, out float currentCost, out int currentGoalIndex, out TWorldState? currentMemory))
            {
                Debug.Assert(currentMemory is not null);

                lastCost = currentCost;

                for (int actionIndex = 0; actionIndex < builder.ActionsCount(); actionIndex++)
                {
                    TAction action = builder.GetAction(actionIndex);

                    if (Toggle.IsOn<TLog>())
                    {
                        builder.AppendToLog(" - Check with action: ");
                        builder.AppendToLog(builder.GetActionText(actionIndex));
                        builder.AppendToLog(" ");
                        builder.AppendToLog(currentMemory.ToString() ?? "<Null>");
                        builder.AppendToLog(" -> ");
                    }

                    TWorldState newMemory;
                    if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                        newMemory = ((IWorldStatePool<TWorldState>)agent).Clone(currentMemory);
                    else
                        newMemory = currentMemory.Clone();
                    action.ApplyEffect(newMemory);

                    if (Toggle.IsOn<TLog>())
                        builder.AppendToLog(newMemory.ToString() ?? "<Null>");

                    PlanBuilderState<TWorldState, TGoal, TAction>.GoalNode currentGoal = builder.GetGoal(currentGoalIndex);

                    if (Toggle.IsOn<TLog>())
                    {
                        builder.AppendToLog(". And goal: ");
                        builder.AppendToLog(currentGoal.Goal.ToString() ?? "<Null>");
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
                                builder.Enqueue<TLog>(id, actionIndex, newCost, newGoals, newMemory);
                            }
                            else if (currentGoal.WithPop(out int newGoals))
                                builder.Enqueue<TLog>(id, actionIndex, newCost, newGoals, newMemory);
                            else
                                FoundValidPath(ref this, id, newCost, actionIndex, newMemory);

                            break;
                        }
                        case SatisfactionResult.Progressed:
                        {
                            if (Toggle.IsOn<TLog>())
                                builder.AppendToLog(". Progressed.\n   ");

                            float newCost = currentCost + action.GetCost();
                            if (action.TryGetRequiredGoal(out TGoal requiredGoal))
                            {
                                int newGoals = PlanBuilderState<TWorldState, TGoal, TAction>.GoalNode.WithPush(builder, currentGoalIndex, requiredGoal);
                                builder.Enqueue<TLog>(id, actionIndex, newCost, newGoals, newMemory);
                            }
                            else
                                builder.Enqueue<TLog>(id, actionIndex, newCost, currentGoalIndex, newMemory);
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

                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    ((IWorldStatePool<TWorldState>)agent).Return(currentMemory);

                return PlanningCoroutineResult.Continue;
            }

            return PlanningCoroutineResult.Finalized;

            static void FoundValidPath(ref PlanBuilderIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> self, int id, float cost, int action, TWorldState newMemory)
            {
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    ((IWorldStatePool<TWorldState>)self.agent).Return(newMemory);
                Debug.Assert(self.builder is not null, "Is disposed.");
                self.builder.EnqueueValidPath<TLog>(id, action, cost);
            }

            [DoesNotReturn]
            static void ThrowInvalidSatisfactionResultException() => throw new InvalidOperationException($"Returned value is not a valid value of {nameof(SatisfactionResult)}.");

            [DoesNotReturn]
            static void ThrowInvalidWatchdogResultException() => throw new InvalidOperationException($"Returned value is not a valid value of {nameof(WatchdogResult)}.");
        }

        private void GetActions()
        {
            Debug.Assert(builder is not null, "Is disposed.");

            using IEnumerator<TAction>? actions = agent.GetActions();

            if (actions is null)
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: actions is null.");
                ThrowActionsIsNullException();
            }

            while (actions.MoveNext())
            {
                TAction current = actions.Current;

                if (current is null)
                    ThrowActionIsNullException();

                builder.AddAction<TLog>(current);
            }

            if (Toggle.IsOn<TLog>())
            {
                builder.AppendToLog("Actions (");
                int actionsCount = builder.ActionsCount();
                builder.AppendToLog(actionsCount);
                builder.AppendToLog("):");

                if (actionsCount == 0)
                    builder.AppendAndLog("\n - <>");
                else
                {
                    for (int i = 0; i < actionsCount; i++)
                    {
                         builder.AppendToLog("\n - ");
                         builder.AppendToLog(builder.GetActionText(i));
                    }
                    builder.Log();
                }
            }

            if (builder.ActionsCount() == 0)
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: actions is empty.");
                ThrowActionsIsEmptyException();
            }

            [DoesNotReturn]
            static void ThrowActionsIsEmptyException() => throw new InvalidOperationException("Must have at least one action.");

            [DoesNotReturn]
            static void ThrowActionsIsNullException() => throw new InvalidOperationException("Actions can't be null.");

            [DoesNotReturn]
            static void ThrowActionIsNullException() => throw new InvalidOperationException("Action can't be null.");
        }

        private void EnqueueGoals()
        {
            Debug.Assert(builder is not null, "Is disposed.");

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
                builder.AppendAndLog(memory.ToString() ?? "<Nulll>");
            }

            using IEnumerator<TGoal> goals = agent.GetGoals();

            if (goals is null)
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: goals is null.");
                ThrowGoalsIsNullException();
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
            void EnqueueGoal(ref PlanBuilderIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> self)
            {
                Debug.Assert(self.builder is not null, "Is disposed.");

                TGoal current = goals.Current;
                if (current is null)
                    ThrowGoalIsNullException();

                TWorldState newMemory;
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    newMemory = ((IWorldStatePool<TWorldState>)self.agent).Clone(memory);
                else
                    newMemory = memory.Clone();

                self.builder.EnqueueGoal<TLog>(current, newMemory);
            }

            [DoesNotReturn]
            static void ThrowWorldStateIsNullException() => throw new InvalidOperationException("World state can't be null.");

            [DoesNotReturn]
            static void ThrowGoalsIsEmptyException() => throw new InvalidOperationException("Must have at least one goal.");

            [DoesNotReturn]
            static void ThrowGoalsIsNullException() => throw new InvalidOperationException("Goals can't be null.");

            [DoesNotReturn]
            static void ThrowGoalIsNullException() => throw new InvalidOperationException("Goal can't be null.");
        }
    }
}
