using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Linq;
using System.Text;

namespace Enderlook.GOAP
{
    [Generator]
    internal sealed class PlannerGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            ReadOnlySpan<bool> boolean = stackalloc[] { false, true };
            foreach (Mode mode in stackalloc[] { Mode.Sync, Mode.Async, Mode.Coroutine })
            {
                foreach (bool log in boolean)
                {
                    foreach (bool helper in boolean)
                    {
                        (string name, string file) = GetFile(mode, log, helper);
                        context.AddSource(name, SourceText.From(file, Encoding.UTF8));
                    }
                }
            }
        }

        public void Initialize(GeneratorInitializationContext context) { }

        private enum Mode
        {
            Sync,
            Async,
            Coroutine,
        }

        private (string name, string file) GetFile(Mode mode, bool log, bool helper)
        {
            string name = $"Planner.{(log ? "Log" : "Logless")}.{mode}.{(helper ? "Helper" : "NoHelper")}";

            string logParameter = log ? ", Action<string> log" : "";
            string logArgument = log ? ", log" : "";
            string logToggle = log ? "Toggle.Yes" : "Toggle.No";

            string resultType = mode switch
            {
                Mode.Sync => "void",
                Mode.Async => "ValueTask",
                Mode.Coroutine => "PlanningCoroutine<TGoal, TAction>",
            };

            string method = mode switch
            {
                Mode.Sync => "RunAndDispose",
                Mode.Async => "RunAndDisposeAsync",
                Mode.Coroutine => "RunAndDisposeCorotuine",
            };

            string postfix = (helper ? "WithHelper" : "") + mode switch
            {
                Mode.Sync => "",
                Mode.Async => "Async",
                Mode.Coroutine => "Coroutine",
            };

            string returnKeyword = mode switch
            {
                Mode.Sync => "",
                Mode.Async or Mode.Coroutine => "return",
            };

            bool returns = mode != Mode.Sync;

            string helperGenericParameter = helper ? ", THelper" : "";
            string helperParameter = helper ? ", THelper helper" : "";
            string helperArgument = helper ? ", helper" : "";

            return (name, $@"
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Enderlook.GOAP
{{
    public static partial class Planner
    {{
        {string.Join("\n", new[] {
            new { parameterName = "token", parameterType = "CancellationToken", type = "CancellableWatchdog", description = "Cancellation token." },
            new { parameterName = "cost", parameterType = "float", type = "CostWatchdog", description = "Cancelates the execution of the plan if the plan cost is higher than this value." },
            new { parameterName = "", parameterType = (string)null, type = "EndlessWatchdog", description = "" },
        }.Select(e => @$"
        {GetDocumentation(log, e.parameterName, e.description)}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static {resultType} Plan{postfix}<TWorldState, TGoal, TAction, TGoals, TActions{helperGenericParameter}>(
            TWorldState worldState, TGoals goals, TActions actions, Plan<TGoal, TAction> plan{(e.parameterType is null ? "" : $", {e.parameterType} {e.parameterName}")}{helperParameter}{logParameter})
            where TWorldState : IWorldState<TWorldState>
            where TGoal : IGoal<TWorldState>
            where TAction : IAction<TWorldState, TGoal>
            where TGoals : IEnumerable<TGoal>
            where TActions : IEnumerable<TAction>
            => PlanInner{postfix}<TWorldState, TGoal, TAction, TGoals, TActions, {e.type}{helperGenericParameter}>(worldState, goals, actions, plan, new {e.type}({e.parameterName}){helperArgument}{logArgument});
        "))}

        {GetDocumentation(log, "", null)}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static {resultType} Plan{postfix}<TWorldState, TGoal, TAction, TGoals, TActions, TWatchdog{helperGenericParameter}>(
            TWorldState worldState, TGoals goals, TActions actions, Plan<TGoal, TAction> plan, TWatchdog watchdog{helperParameter}{logParameter})
            where TWorldState : IWorldState<TWorldState>
            where TGoal : IGoal<TWorldState>
            where TAction : IAction<TWorldState, TGoal>
            where TGoals : IEnumerable<TGoal>
            where TActions : IEnumerable<TAction>
            where TWatchdog : IWatchdog
            => PlanInner{postfix}<TWorldState, TGoal, TAction, TGoals, TActions, TWatchdog{helperGenericParameter}>(worldState, goals, actions, plan, watchdog{helperArgument}{logArgument});

        private static {resultType} PlanInner{postfix}<TWorldState, TGoal, TAction, TGoals, TActions, TWatchdog{helperGenericParameter}>(
            TWorldState worldState, TGoals goals, TActions actions, Plan<TGoal, TAction> plan, TWatchdog watchdog{helperParameter}{logParameter})
            where TWorldState : IWorldState<TWorldState>
            where TGoal : IGoal<TWorldState>
            where TAction : IAction<TWorldState, TGoal>
            where TGoals : IEnumerable<TGoal>
            where TActions : IEnumerable<TAction>
            where TWatchdog : IWatchdog
        {{
            if (worldState is null)
                ThrowNullWorldStateException();
            if (goals is null)
                ThrowNullGoalsException();
            if (actions is null)
                ThrowNullActionsException();
            if (plan is null)
                ThrowNullPlanException();
            if (watchdog is null)
                ThrowNullWatchdogException();
            {(log ? @"
            if (log is null)
                ThrowNullLogException();
            " : "")}
            if (plan.IsInProgress)
                ThrowPlanIsInProgress();

            {(!helper ? @$"
            {returnKeyword} PlanBuilderIterator<AgentWrapper<TWorldState, TGoal, TAction, TGoals, TActions>, TWorldState, TGoal, TAction, TWatchdog, {logToggle}>
                    .{method}(new(worldState, goals, actions), plan, watchdog{logArgument});"
            : @$"
            Type helperType = typeof(THelper).IsValueType ? typeof(THelper) : helper.GetType();

            bool goalPool = typeof(IGoalPool<TGoal>).IsAssignableFrom(helperType);
            bool worldStatePool = typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(helperType);
            bool goalMerge = typeof(IGoalMerge<TGoal>).IsAssignableFrom(helperType);

            if (goalPool)
            {{
                if (worldStatePool)
                {{
                    if (goalMerge)
                    {{
                        {returnKeyword} PlanBuilderIterator<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog, {logToggle}>
                            .{method}(new(worldState, goals, actions, helper), plan, watchdog{logArgument});
                    }}
                    else
                    {{
                        {returnKeyword} PlanBuilderIterator<AgentWrapperPoolGoalPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog, {logToggle}>
                            .{method}(new(worldState, goals, actions, helper), plan, watchdog{logArgument});
                    }}
                }}
                else if (goalMerge)
                {{
                    {returnKeyword} PlanBuilderIterator<AgentWrapperPoolGoalMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog, {logToggle}>
                        .{method}(new(worldState, goals, actions, helper), plan, watchdog);
                }}
                else
                {{
                    {returnKeyword} PlanBuilderIterator<AgentWrapperPoolGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog, {logToggle}>
                        .{method}(new(worldState, goals, actions, helper), plan, watchdog{logArgument});
                }}
            }}
            else if (worldStatePool)
            {{
                if (goalMerge)
                {{
                    {returnKeyword} PlanBuilderIterator<AgentWrapperPoolWorldMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog, {logToggle}>
                        .{method}(new(worldState, goals, actions, helper), plan, watchdog{logArgument});
                }}
                else
                {{
                    {returnKeyword} PlanBuilderIterator<AgentWrapperPoolWorld<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog, {logToggle}>
                        .{method}(new(worldState, goals, actions, helper), plan, watchdog{logArgument});
                }}
            }}
            else if (goalMerge)
            {{
                {returnKeyword} PlanBuilderIterator<AgentWrapperMergeGoal<TWorldState, TGoal, TAction, TGoals, TActions, THelper>, TWorldState, TGoal, TAction, TWatchdog, {logToggle}>
                    .{method}(new(worldState, goals, actions, helper), plan, watchdog{logArgument});
            }}
            else
            {{
                ThrowInvalidHelperType();
                {(returns ? "return default;" : "")}
            }}
            ")}
        }}
    }}
}}
");
    }

        private static string GetDocumentation(bool hasLog, string watchdogName, string watchdogDescription)
            => @$"
        /// <summary>
        /// Uses GOAP to computes how to complete the goal with the lowest cost from <paramref name=""agent""/>.
        /// </summary>
        /// <typeparam name=""TAgent"">Type of agent.</typeparam>
        /// <typeparam name=""TWorldState"">Type of world state.</typeparam>
        /// <typeparam name=""TAction"">Type of actions.</typeparam>
        /// <typeparam name=""TGoal"">Type of goals.</typeparam>
        /// This type can implement the following interfaces for additional features:
        /// <see cref=""IGoalMerge{{TGoal}}""/>, <see cref=""IGoalPool{{TGoal}}""/>, <see cref=""IWorldStatePool{{TWorld}}""/>.</param>
        /// <param name=""worldState"">Initial state of the world.</param>
        /// <param name=""goals"">Goals to archive. Only the most cheap goal will be completed.</param>
        /// <param name=""actions"">Available actions to perform in the world.</param>
        {(watchdogName == "" ? @"/// <param name=""watchdog"">Token used to cancelate or suspend the execution </param>" : watchdogName is null ? "" : @$"/// <param name=""{watchdogName}"">{watchdogDescription}</param>")}
        {(hasLog ? @"/// <param name=""log"">Log action used to debug the planification. The layout of the log content is an implementation detail.</param>" : "")}
        /// <exception cref=""ArgumentNullException"">Throw if <paramref name=""agent""/> is <see langword=""null""/>.</exception>
        /// <exception cref=""ArgumentNullException"">Throw if <paramref name=""worldState""/> is <see langword=""null""/>.</exception>
        /// <exception cref=""ArgumentNullException"">Throw if <paramref name=""goals""/> is <see langword=""null""/>.</exception>
        /// <exception cref=""ArgumentNullException"">Throw if <paramref name=""actions""/> is <see langword=""null""/>.</exception>
        {(watchdogName == "" ? @"/// <exception cref=""ArgumentNullException"">Throw if <paramref name=""watchdog""/> is <see langword=""null""/>.</exception>" : "")}
        {(hasLog ? @"/// <exception cref=""ArgumentNullException"">Throw if <paramref name=""log""/> is <see langword=""null""/>.</exception>" : "")}";
    }
}
