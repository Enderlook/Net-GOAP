using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal class PlanningCoroutine<TAgent, TWorldState, TGoal, TAction, TWatchdog, TLog, TGoalResult, TActionResult> : PlanningCoroutine<TGoalResult, TActionResult>
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
                        SetResult();
                        return PlanningCoroutineResult.Cancelled;
                    case ToFinalize:
                        state = Finalized;
                        SetResult();
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

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                void SetResult()
                {
                    if (typeof(TGoalResult) == typeof(int))
                    {
                        Debug.Assert(typeof(TActionResult) == typeof(int));
                        PlanResult<int, int> tmp = iterator.FinalizeInt();
                        result = Unsafe.As<PlanResult<int, int>, PlanResult<TGoalResult, TActionResult>>(ref tmp);
                    }
                    else
                    {
                        Debug.Assert(typeof(TGoalResult) == typeof(TGoal));
                        Debug.Assert(typeof(TActionResult) == typeof(TAction));
                        PlanResult<TGoal, TAction> tmp = iterator.FinalizeTyped();
                        result = Unsafe.As<PlanResult<TGoal, TAction>, PlanResult<TGoalResult, TActionResult>>(ref tmp);
                    }
                }
            }

            static void ThrowAlreadyDisposedException() => throw new ObjectDisposedException("Planning");

            static void ThrowAlreadyFinalizedException() => throw new InvalidOperationException("Planification already finalized.");
        }
    }
}
