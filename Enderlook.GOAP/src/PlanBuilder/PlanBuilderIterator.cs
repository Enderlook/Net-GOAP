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
                builder.AppendToLog("Start planning.\nInitial memory: ");
                builder.AppendAndLog(memory.ToString() ?? "<Null>");
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
                        builder.AppendToLog(" - Check with action: ");
                        builder.AppendToLog(builder.GetActionText(actionIndex));
                        builder.AppendToLog(" ");
                        Debug.Assert(currentWorldState is not null);
                        builder.AppendToLog(currentWorldState.ToString() ?? "<Null>");
                        builder.AppendToLog(" -> ");
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

            TWorldState newWorldState;
            if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                newWorldState = ((IWorldStatePool<TWorldState>)agent).Clone(currentWorldState);
            else
                newWorldState = currentWorldState.Clone();
            action.ApplyEffect(newWorldState);

            if (Toggle.IsOn<TLog>())
                builder.AppendToLog(newWorldState.ToString() ?? "<Null>");

            PlanBuilderState<TWorldState, TGoal, TAction>.GoalNode currentGoal = builder.GetGoal(currentGoalIndex);

            if (Toggle.IsOn<TLog>())
            {
                builder.AppendToLog(". And goal: ");
                builder.AppendToLog(currentGoal.Goal.ToString() ?? "<Null>");
            }

            switch (currentGoal.Goal.CheckAndTrySatisfy(currentWorldState, newWorldState))
            {
                case SatisfactionResult.Satisfied:
                {
                    if (Toggle.IsOn<TLog>())
                        builder.AppendToLog(". Satisfied.\n   ");

                    if (action.GetCostAndRequiredGoal(out float actionCost, out TGoal requiredGoal))
                    {
                        int newGoals = currentGoal.WithReplacement(builder, requiredGoal);
                        ProcessGoalAndCheckForChainedSatisfaction(ref this, id, currentCost, actionIndex, newWorldState, actionCost, newGoals);
                    }
                    else
                    {
                        if (currentGoal.WithPop(out int newGoals))
                            ProcessGoalAndCheckForChainedSatisfaction(ref this, id, currentCost, actionIndex, newWorldState, actionCost, newGoals);
                        else
                            FoundValidPath(ref this, id, currentCost + actionCost, actionIndex, newWorldState);
                    }
                    break;
                }
                case SatisfactionResult.Progressed:
                {
                    if (Toggle.IsOn<TLog>())
                        builder.AppendToLog(". Progressed.\n   ");

                    if (action.GetCostAndRequiredGoal(out float actionCost, out TGoal requiredGoal))
                    {
                        int newGoals = PlanBuilderState<TWorldState, TGoal, TAction>.GoalNode.WithPush(builder, currentGoalIndex, requiredGoal);
                        builder.Enqueue<TLog>(id, actionIndex, currentCost + actionCost, newGoals, newWorldState);
                    }
                    else
                        builder.Enqueue<TLog>(id, actionIndex, currentCost + actionCost, currentGoalIndex, newWorldState);
                    break;
                }
                case SatisfactionResult.NotProgressed:
                {
                    if (Toggle.IsOn<TLog>())
                        builder.AppendAndLog(". Not progressed.");
                    if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                        ((IWorldStatePool<TWorldState>)agent).Return(newWorldState);
                    break;
                }
                default:
                {
                    ThrowHelper.ThrowInvalidOperationException_SatisfactionResultIsInvalid();
                    break;
                }
            }

            static void FoundValidPath(
                ref PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> self,
                int id, float cost, int action, TWorldState newMemory)
            {
                Debug.Assert(self.builder is not null, "Is disposed.");
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    ((IWorldStatePool<TWorldState>)self.agent).Return(newMemory);
                self.builder.EnqueueValidPath<TLog>(id, action, cost);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void ProcessGoalAndCheckForChainedSatisfaction(
                ref PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> self,
                int id, float currentCost, int actionIndex, TWorldState newWorldState, float actionCost, int newGoals)
            {
                Debug.Assert(self.builder is not null, "Is disposed.");

                TWorldState newWorldState2;
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    newWorldState2 = ((IWorldStatePool<TWorldState>)self.agent).Clone(newWorldState);
                else
                    newWorldState2 = newWorldState.Clone();

                // This loop check if multiple goals were satisfied due the current action.
                while (true)
                {
                    PlanBuilderState<TWorldState, TGoal, TAction>.GoalNode newGoal = self.builder.GetGoal(newGoals);
                    switch (newGoal.Goal.CheckAndTrySatisfy(newWorldState, newWorldState2))
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
                                ((IWorldStatePool<TWorldState>)self.agent).Return(newWorldState);
                            newWorldState = newWorldState2;

                            if (!newGoal.WithPop(out newGoals))
                            {
                                FoundValidPath(ref self, id, currentCost, actionIndex, newWorldState);
                                return;
                            }

                            if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                                newWorldState2 = ((IWorldStatePool<TWorldState>)self.agent).Clone(newWorldState);
                            else
                                newWorldState2 = newWorldState.Clone();

                            break;
                        }
                        case SatisfactionResult.Progressed: // Actually, this case should never happen if user implemented the interface properly.
                        case SatisfactionResult.NotProgressed:
                            goto stop;
                    }
                }

                stop:
                if (typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(typeof(TAgent)))
                    ((IWorldStatePool<TWorldState>)self.agent).Return(newWorldState2);
                self.builder.Enqueue<TLog>(id, actionIndex, currentCost + actionCost, newGoals, newWorldState);
            }
        }
    }
}
