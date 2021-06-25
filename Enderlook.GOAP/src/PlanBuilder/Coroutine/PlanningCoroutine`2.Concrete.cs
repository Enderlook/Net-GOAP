using Enderlook.GOAP.Utilities;
using Enderlook.GOAP.Watchdogs;

using System;
using System.Diagnostics;

namespace Enderlook.GOAP
{
    internal class PlanningCoroutine<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> : PlanningCoroutine<TGoal, TAction>
        where TAgent : IAgent<TWorldState, TGoal, TAction>
        where TWorldState : IWorldState<TWorldState>
        where TGoal : IGoal<TWorldState>
        where TAction : IAction<TWorldState, TGoal>
        where TWatchdog : IWatchdog
    {
        private PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> iterator;

        public PlanningCoroutine(PlanBuilderIterator<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog> iterator)
        {
            this.iterator = iterator;
            state = Initialize;
        }

        public override void Dispose()
        {
            iterator.Dispose();
            state = Disposed;
        }

        public override Plan<TGoal, TAction> GetAssociatedPlan() => iterator.Plan;

        public override bool MoveNext()
        {
            if (state == Continue)
            {
                while (true)
                {
                    PlanningCoroutineResult result = iterator.MoveNext();

                    if (result != PlanningCoroutineResult.Continue)
                        return Rare2();

                    bool Rare2()
                    {
                        switch (result)
                        {
                            case PlanningCoroutineResult.Finalized:
                                state = Finalized;
                                iterator.Finalize_();
                                return false;
                            case PlanningCoroutineResult.Suspended:
                                return true;
                            case PlanningCoroutineResult.Cancelled:
                                state = Cancelled;
                                iterator.Finalize_();
                                return false;
                            default:
                                Debug.Fail("Impossible state.");
                                goto case PlanningCoroutineResult.Finalized;
                        }
                    }
                }
            }
            else
                return Rare();

            bool Rare()
            {
                switch (state)
                {
                    case Initialize:
                        iterator.Initialize();
                        state = Continue;
                        return MoveNext();
                    case Disposed:
                        ThrowAlreadyDisposedException();
                        break;
                    case Finalized:
                    case Cancelled:
                        ThrowAlreadyFinalizedException();
                        break;
                    default:
                        Debug.Fail("Impossible state.");
                        break;
                }
                return default;
            }

            static void ThrowAlreadyDisposedException() => throw new ObjectDisposedException("Planning");

            static void ThrowAlreadyFinalizedException() => throw new InvalidOperationException("Planification already finalized.");
        }
    }
}
