using Enderlook.GOAP.Utilities;
using Enderlook.GOAP.Watchdogs;

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Enderlook.GOAP
{
    internal struct PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> : IDisposable, IActionHandleAcceptor<TWorldState, TGoal>
        where TAgent : IAgent<TWorldState, TGoal, TAction>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
        where TAction : IAction<TWorldState, TGoal>
        where TWatchdog : IWatchdog
    {
        private TAgent agent;
        private PlanBuilderState<TWorldState, TGoal, TAction>? builder;
        private TWatchdog watchdog;
        public Plan<TGoal, TAction> Plan { get; }
        private float lastCost;

        // Fields used by IActionHandleAcceptor<TWorldState, TGoal>
        private TWorldState? currentWorldState;
        private int currentGoalIndex;
        private int id;
        private float currentCost;
        private int actionIndex;

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

#if NET5_0_OR_GREATER

            Unsafe.SkipInit(out currentWorldState);
            Unsafe.SkipInit(out currentGoalIndex);
            Unsafe.SkipInit(out id);
            Unsafe.SkipInit(out currentCost);
            Unsafe.SkipInit(out actionIndex);
#else
            currentWorldState = default;
            currentGoalIndex = default;
            id = default;
            currentCost = default;
            actionIndex = default;
#endif
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
            using PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> iterator = new(agent, plan, watchdog, log);
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
            using PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> iterator = new(agent, plan, watchdog, log);
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

        public static PlanningCoroutine<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> RunAndDisposeCoroutine(TAgent agent, Plan<TGoal, TAction> plan, TWatchdog watchdog, Action<string>? log = null)
            => new(new(agent, plan, watchdog, log));

        public void Initialize()
        {
            Debug.Assert(builder is not null, "Is disposed.");

            TWorldState memory = agent.GetWorldState();

            if (memory is null)
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: world state is null.");
                ThrowHelper.ThrowInvalidOperationException_WorldStateIsNull();
            }

            if (Toggle.IsOn<TLog>())
            {
                builder.AppendToLog("Start planning.\nInitial world state: <");
                builder.AppendToLog(memory.ToString() ?? "Null");
                builder.AppendToLog(">.");
            }

            agent.SetGoals(ref this);

            Debug.Assert(builder is not null, "Is disposed.");
            if (builder.NodesCount() == 0)
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: goals is empty.");
                ThrowHelper.ThrowInvalidOperationException_GoalsIsEmpty();
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
                        builder.AppendToLog("\n - <");
                        builder.AppendToLog(builder.GetActionText(i));
                        builder.AppendToLog(">.");
                    }
                    builder.Log();
                }
            }

            Debug.Assert(builder is not null, "Is disposed.");
            if (builder.ActionsCount() == 0)
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Error: actions is empty.");
                ThrowHelper.ThrowInvalidOperationException_ActionsIsEmpty();
            }
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
                    ThrowHelper.ThrowInvalidOperationException_WatchdogResultIsInvalid();
                    break;
            }

            if (builder.TryDequeue<TAgent, TLog>(out id, out currentCost, out currentGoalIndex, out currentWorldState))
            {
                Debug.Assert(currentWorldState is not null);

                lastCost = currentCost;

                for (actionIndex = 0; actionIndex < builder.ActionsCount(); actionIndex++)
                {
                    TAction action = builder.GetAction(actionIndex);

                    if (Toggle.IsOn<TLog>())
                    {
                        builder.AppendToLog(" - Check action: <");
                        builder.AppendToLog(builder.GetActionText(actionIndex));
                        builder.AppendToLog(">.");
                    }

                    Debug.Assert(currentWorldState is not null);
                    action.Visit(ref this, currentWorldState);
                }

                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                {
                    Debug.Assert(currentWorldState is not null);
                    ((IWorldStatePool<TWorldState>)agent).Return(currentWorldState);
                }

                return PlanningCoroutineResult.Continue;
            }

            return PlanningCoroutineResult.Finalized;
        }

        internal void AddAction(TAction action)
        {
            Debug.Assert(builder is not null, "Is disposed.");

            if (action is null)
                ThrowHelper.ThrowInvalidOperationException_ActionIsNull();

            builder.AddAction<TLog>(action);
        }

        internal void AddGoal(TGoal goal)
        {
            Debug.Assert(builder is not null, "Is disposed.");

            TWorldState memory = agent.GetWorldState();

            if (goal is null)
                ThrowHelper.ThrowInvalidOperationException_GoalIsNull();

            TWorldState newMemory;
            if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                newMemory = ((IWorldStatePool<TWorldState>)agent).Clone(memory);
            else
                newMemory = memory.Clone();

            builder.EnqueueGoal<TLog>(goal, newMemory);
        }

        void IActionHandleAcceptor<TWorldState, TGoal>.Accept<TActionHandle>(TActionHandle action)
        {
            Debug.Assert(builder is not null, "Is disposed.");
            Debug.Assert(currentWorldState is not null);

            if (!action.CheckProceduralPreconditions())
            {
                if (Toggle.IsOn<TLog>())
                    builder.AppendAndLog("Doesn't satisfy procedural preconditions.");
                return;
            }

            if (Toggle.IsOn<TLog>())
            {
                builder.AppendToLog("\n   Which mutates world state from <");
                builder.AppendToLog(currentWorldState.ToString() ?? "Null");
                builder.AppendToLog("> to <");
            }

            TWorldState newWorldState;
            if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                newWorldState = ((IWorldStatePool<TWorldState>)agent).Clone(currentWorldState);
            else
                newWorldState = currentWorldState.Clone();
            action.ApplyEffect(ref newWorldState);

            if (Toggle.IsOn<TLog>())
            {
                builder.AppendToLog(newWorldState.ToString() ?? "Null");
                builder.AppendToLog(">.");
            }

            PlanBuilderState<TWorldState, TGoal, TAction>.GoalNode currentGoal = builder.GetGoal(currentGoalIndex);

            if (Toggle.IsOn<TLog>())
            {
                builder.AppendToLog("\n   Having as goal <");
                builder.AppendToLog(currentGoal.Goal.ToString() ?? "Null");
                builder.AppendToLog('>');
            }

            switch (currentGoal.Goal.CheckAndTrySatisfy(currentWorldState, ref newWorldState))
            {
                case SatisfactionResult.Satisfied:
                {
                    Debug.Assert(builder is not null, "Is disposed.");

                    if (Toggle.IsOn<TLog>())
                        builder.AppendToLog(" which is satisfied:\n    - ");

                    float totalCost;
                    int goalIndex;
                    if (action.GetCostAndRequiredGoal(out float actionCost, out TGoal requiredGoal))
                    {
                        goalIndex = currentGoal.WithReplacement(builder, requiredGoal);
                        goto process;
                    }

                    if (currentGoal.WithPop(out goalIndex))
                        goto process;

                    totalCost = currentCost + actionCost;

                foundValidPath:
                    if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                        ((IWorldStatePool<TWorldState>)agent).Return(newWorldState);
                    builder.EnqueueValidPath<TLog>(id, actionIndex, totalCost);
                    break;

                process:
                    int newGoals = goalIndex;

                    PlanBuilderState<TWorldState, TGoal, TAction>.GoalNode newGoal = builder.GetGoal(newGoals);
                    if (newGoal.Goal.CheckAndTrySatisfy(ref newWorldState))
                    {
                        do
                        {
                            if (Toggle.IsOn<TLog>())
                            {
                                builder.AppendToLog("The goal <");
                                builder.AppendToLog(newGoal.Goal.ToString() ?? "Null");
                                builder.AppendToLog("> was also satisfied with the executed action.\\n    - ");
                            }

                            if (!newGoal.WithPop(out newGoals))
                            {
                                totalCost = currentCost;
                                goto foundValidPath;
                            }

                            newGoal = builder.GetGoal(newGoals);
                        } while (newGoal.Goal.CheckAndTrySatisfy(ref newWorldState));
                    }

                    builder.Enqueue<TLog>(id, actionIndex, currentCost + actionCost, newGoals, newWorldState);
                    break;
                }
                case SatisfactionResult.Progressed:
                {
                    if (Toggle.IsOn<TLog>())
                        builder.AppendToLog(" which has progressed:\n    - ");

                    if (action.GetCostAndRequiredGoal(out float actionCost, out TGoal requiredGoal))
                    {
                        Debug.Assert(builder is not null, "Is disposed.");
                        if (requiredGoal.CheckAndTrySatisfy(ref newWorldState))
                        {
                            if (Toggle.IsOn<TLog>())
                            {
                                builder.AppendToLog("The goal <");
                                builder.AppendToLog(requiredGoal.ToString() ?? "Null");
                                builder.AppendToLog("> was also satisfied with the executed action.\n    - ");
                            }

                            builder.Enqueue<TLog>(id, actionIndex, currentCost + actionCost, currentGoalIndex, newWorldState);
                        }
                        else
                        {
                            int newGoals = PlanBuilderState<TWorldState, TGoal, TAction>.GoalNode.WithPush(builder, currentGoalIndex, requiredGoal);
                            builder.Enqueue<TLog>(id, actionIndex, currentCost + actionCost, newGoals, newWorldState);
                        }
                    }
                    else
                        builder.Enqueue<TLog>(id, actionIndex, currentCost + actionCost, currentGoalIndex, newWorldState);
                    break;
                }
                case SatisfactionResult.NotProgressed:
                {
                    if (Toggle.IsOn<TLog>())
                        builder.AppendAndLog(" which hasn't progressed.");
                    if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                        ((IWorldStatePool<TWorldState>)agent).Return(newWorldState);
                    break;
                }
                default:
                {
                    if (Toggle.IsOn<TLog>())
                        builder.AppendAndLog($" Error: Invalid value of {nameof(SatisfactionResult)}.");
                    ThrowHelper.ThrowInvalidOperationException_SatisfactionResultIsInvalid();
                    break;
                }
            }
        }
    }
}
