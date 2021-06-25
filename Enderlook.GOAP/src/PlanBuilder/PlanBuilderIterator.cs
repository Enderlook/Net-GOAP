using Enderlook.GOAP.Utilities;
using Enderlook.GOAP.Watchdogs;

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Enderlook.GOAP
{
    internal struct PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TActionHandle, TWatchdog, TLog> : IDisposable
        where TAgent : IAgent<TWorldState, TGoal, TAction, TActionHandle>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
        where TAction : IAction<TWorldState, TGoal, TActionHandle>
        where TWatchdog : IWatchdog
    {
        private TAgent agent;
        private PlanBuilderState<TWorldState, TGoal, TAction>? builder;
        private TWatchdog watchdog;
        public Plan<TGoal, TAction> Plan { get; }
        private float lastCost;

        private PlanBuilderIterator(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
        {
            Debug.Assert(typeof(TAgent).IsValueType, $"{nameof(TAgent)} must be a value type to constant propagate type checks.");
            Toggle.Assert<TLog>();

            Debug.Assert(agent is not null);
            Debug.Assert(plan is not null);
            Debug.Assert(watchdog is not null);

            this.agent = agent;
            Plan = plan;
            this.watchdog = watchdog;

            plan.SetInProgress();

            lastCost = 0;
            builder = Pool<PlanBuilderState<TWorldState, TGoal, TAction>>.Get();

            if (Toggle.IsOn<TLog>())
            {
                Debug.Assert(log is not null, "Log is enabled, but log is null.");
                builder.SetLog(log);
            }
        }

        public void Dispose()
        {
            if (builder is null)
                return;

            builder.Clear(agent);
            Pool<PlanBuilderState<TWorldState, TGoal, TAction>>.Return(builder);
            builder = null;
        }

        public static void RunAndDispose(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
        {
            using PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TActionHandle, TWatchdog, TLog> iterator = new(agent, plan, watchdog, log);
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
            iterator.Finalize_();
        }

        public static async ValueTask<Plan<TGoal, TAction>> RunAndDisposeAsync(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
        {
            using PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TActionHandle, TWatchdog, TLog> iterator = new(agent, plan, watchdog, log);
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
            iterator.Finalize_();
            return plan;
        }

        public static PlanningCoroutine<TAgent, TWorldState, TGoal, TAction, TActionHandle, TWatchdog, TLog> RunAndDisposeCoroutine(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
            => new(new(agent, plan, watchdog, log));

        public void Initialize()
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
                builder.AppendToLog("Start planning.\nInitial memory: ");
                builder.AppendAndLog(memory.ToString() ?? "<Null>");
            }

            agent.SetGoals(ref this);

            Debug.Assert(builder is not null, "Is disposed.");
            if (builder.NodesCount() == 0)
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: goals is empty.");
                ThrowGoalsIsEmptyException();
            }

            agent.SetActions(ref this);

            if (Toggle.IsOn<TLog>())
            {
                Debug.Assert(builder is not null, "Is disposed.");
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

            Debug.Assert(builder is not null, "Is disposed.");
            if (builder.ActionsCount() == 0)
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: actions is empty.");
                ThrowActionsIsEmptyException();
            }

            [DoesNotReturn]
            static void ThrowWorldStateIsNullException() => throw new InvalidOperationException("World state can't be null.");

            [DoesNotReturn]
            static void ThrowActionsIsEmptyException() => throw new InvalidOperationException("Must have at least one action.");

            [DoesNotReturn]
            static void ThrowGoalsIsEmptyException() => throw new InvalidOperationException("Must have at least one goal.");
        }

        public void Finalize_()
        {
            Debug.Assert(builder is not null, "Is disposed.");
            builder.Finalize<TAgent, TLog>(Plan);
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

                    if (!action.CheckProceduralPreconditions(currentMemory, out TActionHandle? handle))
                    {
                        if (Toggle.IsOn<TLog>())
                            builder.AppendAndLog("Doesn't satisfy procedural preconditions.");
                        continue;
                    }

                    TWorldState newMemory;
                    if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                        newMemory = ((IWorldStatePool<TWorldState>)agent).Clone(currentMemory);
                    else
                        newMemory = currentMemory.Clone();
                    action.ApplyEffect(newMemory, handle);

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

                            if (action.GetCostAndRequiredGoal(handle, out float actionCost, out TGoal requiredGoal))
                            {
                                int newGoals = currentGoal.WithReplacement(builder, requiredGoal);
                                ProcessGoalAndCheckForChainedSatisfaction(ref this, id, currentCost, actionIndex, newMemory, actionCost, newGoals);
                            }
                            else
                            {
                                if (currentGoal.WithPop(out int newGoals))
                                    ProcessGoalAndCheckForChainedSatisfaction(ref this, id, currentCost, actionIndex, newMemory, actionCost, newGoals);
                                else
                                    FoundValidPath(ref this, id, currentCost + actionCost, actionIndex, newMemory);
                            }
                            break;
                        }
                        case SatisfactionResult.Progressed:
                        {
                            if (Toggle.IsOn<TLog>())
                                builder.AppendToLog(". Progressed.\n   ");

                            if (action.GetCostAndRequiredGoal(handle, out float actionCost, out TGoal requiredGoal))
                            {
                                int newGoals = PlanBuilderState<TWorldState, TGoal, TAction>.GoalNode.WithPush(builder, currentGoalIndex, requiredGoal);
                                builder.Enqueue<TLog>(id, actionIndex, currentCost + actionCost, newGoals, newMemory);
                            }
                            else
                                builder.Enqueue<TLog>(id, actionIndex, currentCost + actionCost, currentGoalIndex, newMemory);
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

                    if (typeof(IActionHandlePool<TActionHandle>).IsAssignableFrom(typeof(TAgent)))
                        ((IActionHandlePool<TActionHandle>)agent).Return(handle);
                }

                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    ((IWorldStatePool<TWorldState>)agent).Return(currentMemory);

                return PlanningCoroutineResult.Continue;
            }

            return PlanningCoroutineResult.Finalized;

            static void FoundValidPath(
                ref PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TActionHandle, TWatchdog, TLog> self,
                int id, float cost, int action, TWorldState newMemory)
            {
                Debug.Assert(self.builder is not null, "Is disposed.");
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    ((IWorldStatePool<TWorldState>)self.agent).Return(newMemory);
                self.builder.EnqueueValidPath<TLog>(id, action, cost);
            }

            [DoesNotReturn]
            static void ThrowInvalidSatisfactionResultException() => throw new InvalidOperationException($"Returned value is not a valid value of {nameof(SatisfactionResult)}.");

            [DoesNotReturn]
            static void ThrowInvalidWatchdogResultException() => throw new InvalidOperationException($"Returned value is not a valid value of {nameof(WatchdogResult)}.");

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void ProcessGoalAndCheckForChainedSatisfaction(
                ref PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TActionHandle, TWatchdog, TLog> self,
                int id, float currentCost, int actionIndex, TWorldState newMemory, float actionCost, int newGoals)
            {
                Debug.Assert(self.builder is not null, "Is disposed.");

                TWorldState newMemory2;
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    newMemory2 = ((IWorldStatePool<TWorldState>)self.agent).Clone(newMemory);
                else
                    newMemory2 = newMemory.Clone();

                // This loop check if multiple goals were satisfied due the current action.
                while (true)
                {
                    PlanBuilderState<TWorldState, TGoal, TAction>.GoalNode newGoal = self.builder.GetGoal(newGoals);
                    switch (newGoal.Goal.CheckAndTrySatisfy(newMemory, newMemory2))
                    {
                        case SatisfactionResult.Satisfied:
                        {
                            if (Toggle.IsOn<TLog>())
                            {
                                self.builder.AppendToLog("The goal ");
                                self.builder.AppendToLog(newGoal.Goal.ToString() ?? "<Null>");
                                self.builder.AppendToLog(" was also satisfied with the executed action.\n   ");
                            }

                            if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                                ((IWorldStatePool<TWorldState>)self.agent).Return(newMemory);
                            newMemory = newMemory2;

                            if (!newGoal.WithPop(out newGoals))
                            {
                                FoundValidPath(ref self, id, currentCost, actionIndex, newMemory);
                                return;
                            }

                            if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                                newMemory2 = ((IWorldStatePool<TWorldState>)self.agent).Clone(newMemory);
                            else
                                newMemory2 = newMemory.Clone();

                            break;
                        }
                        case SatisfactionResult.Progressed: // Actually, this case should never happen if use implemented the interface properly.
                        case SatisfactionResult.NotProgressed:
                            goto stop;
                    }
                }

                stop:
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    ((IWorldStatePool<TWorldState>)self.agent).Return(newMemory2);
                self.builder.Enqueue<TLog>(id, actionIndex, currentCost + actionCost, newGoals, newMemory);
            }
        }

        internal void AddAction(TAction action)
        {
            Debug.Assert(builder is not null, "Is disposed.");

            if (action is null)
                ThrowActionIsNullException();

            builder.AddAction<TLog>(action);

            [DoesNotReturn]
            static void ThrowActionIsNullException() => throw new InvalidOperationException("Action can't be null.");
        }

        internal void AddGoal(TGoal goal)
        {
            Debug.Assert(builder is not null, "Is disposed.");

            TWorldState memory = agent.GetWorldState();

            if (goal is null)
                ThrowGoalIsNullException();

            TWorldState newMemory;
            if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                newMemory = ((IWorldStatePool<TWorldState>)agent).Clone(memory);
            else
                newMemory = memory.Clone();

            builder.EnqueueGoal<TLog>(goal, newMemory);

            [DoesNotReturn]
            static void ThrowGoalIsNullException() => throw new InvalidOperationException("Goal can't be null.");
        }
    }
}
