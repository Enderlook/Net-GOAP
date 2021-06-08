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
        private PlanBuilderIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> iterator;

        public PlanningCoroutine(PlanBuilderIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, TLog> iterator)
        {
            this.iterator = iterator;
            state = Initialize;
        }

        public override void Dispose()
        {
            iterator.Dispose();
            state = Disposed;
        }

        public override PlanningCoroutineResult MoveNext()
        {
            if (state == Continue)
            {
                switch (iterator.MoveNext())
                {
                    case PlanningCoroutineResult.Suspended:
                        return PlanningCoroutineResult.Suspended;
                    case PlanningCoroutineResult.Cancelled:
                        state = ToCancel;
                        return PlanningCoroutineResult.Continue;
                    case PlanningCoroutineResult.Finalized:
                        state = ToFinalize;
                        return PlanningCoroutineResult.Continue;
                }
                return PlanningCoroutineResult.Continue;
            }

            return Rare();

            PlanningCoroutineResult Rare()
            {
                switch (state)
                {
                    case Initialize:
                        iterator.Initialize();
                        state = Continue;
                        return PlanningCoroutineResult.Continue;
                    case ToCancel:
                        state = Cancelled;
                        result = iterator.Finalize();
                        return PlanningCoroutineResult.Cancelled;
                    case ToFinalize:
                        state = Finalized;
                        result = iterator.Finalize();
                        return PlanningCoroutineResult.Finalized;
                    case Disposed:
                        ThrowAlreadyDisposedException();
                        return default;
                    case Finalized:
                    case Cancelled:
                        ThrowAlreadyFinalizedException();
                        return default;
                    default:
                        Debug.Fail("Impossible state.");
                        goto case Finalized;
                }
            }

            static void ThrowAlreadyDisposedException() => throw new ObjectDisposedException("Planning");

            static void ThrowAlreadyFinalizedException() => throw new InvalidOperationException("Planification already finalized.");
        }
    }
}
