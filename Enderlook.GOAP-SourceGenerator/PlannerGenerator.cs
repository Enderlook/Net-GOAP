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
                    (string name, string file) = GetFile(mode, log);
                    context.AddSource(name, SourceText.From(file, Encoding.UTF8));
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

        private (string name, string file) GetFile(Mode mode, bool log)
        {
            string name = $"Planner.{(log ? "Log" : "Logless")}.{mode}";

            string logParameter = log ? ", Action<string> log" : "";
            string logArgument = log ? ", log" : "";
            string logToggle = log ? "Toggle.Yes" : "Toggle.No";

            string resultType = mode switch
            {
                Mode.Sync => "PlanResult<TGoal, TAction>",
                Mode.Async => "ValueTask<PlanResult<TGoal, TAction>>",
                Mode.Coroutine => "PlanningCoroutine<TGoal, TAction>",
                _ => default,
            };

            string method = mode switch
            {
                Mode.Sync => "RunAndDispose",
                Mode.Async => "RunAndDisposeAsync",
                Mode.Coroutine => "RunAndDisposeCorotuine",
                _ => default,
            };

            return (name, $@"
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Enderlook.GOAP
{{
    /// <summary>
    /// Helper methods to compute GOAP.
    /// </summary>
    public static partial class Planner
    {{
        /// <summary>
        /// Helper synchronous methods to compute GOAP.
        /// </summary>
        public static partial class {mode}
        {{
            {string.Join("\n", new[] {
                new { parameterName = "token", parameterType = "CancellationToken", type = "CancellableWatchdog", description = "Cancellation token." },
                new { parameterName = "cost", parameterType = "float", type = "CostWatchdog", description = "Cancelates the execution of the plan if the plan cost is higher than this value." },
                new { parameterName = "", parameterType = (string)null, type = "EndlessWatchdog", description = "" },
            }.Select(e => @$"
            {GetDocumentation(log, e.parameterName, e.description)}
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static {resultType} Plan<TAgent, TWorldState, TAction, TGoal>(
                TAgent agent, Stack<TAction> plan{(e.parameterType is null ? "" : $", {e.parameterType} {e.parameterName}")}{logParameter})
                where TAgent : IAgent<TWorldState, TGoal, TAction>
                where TWorldState : IWorldState<TWorldState>
                where TAction : IAction<TWorldState, TGoal>
                where TGoal : IGoal<TWorldState>
                => PlanInner<TAgent, TWorldState, TAction, TGoal, {e.type}>(agent, plan, new {e.type}({e.parameterName}){logArgument});
            "))}

            {GetDocumentation(log, "", null)}
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static {resultType} Plan<TAgent, TWorldState, TAction, TGoal, TWatchdog>(
                TAgent agent, Stack<TAction> plan, TWatchdog watchdog{logParameter})
                where TAgent : IAgent<TWorldState, TGoal, TAction>
                where TWorldState : IWorldState<TWorldState>
                where TAction : IAction<TWorldState, TGoal>
                where TGoal : IGoal<TWorldState>
                where TWatchdog : IWatchdog
                => PlanInner<TAgent, TWorldState, TAction, TGoal, TWatchdog>(agent, plan, watchdog{logArgument});

            private static {resultType} PlanInner<TAgent, TWorldState, TAction, TGoal, TWatchdog>(
                TAgent agent, Stack<TAction> plan, TWatchdog watchdog{logParameter})
                where TAgent : IAgent<TWorldState, TGoal, TAction>
                where TWorldState : IWorldState<TWorldState>
                where TAction : IAction<TWorldState, TGoal>
                where TGoal : IGoal<TWorldState>
                where TWatchdog : IWatchdog
            {{
                if (typeof(TAgent).IsValueType)
                    return PlanBuilderIterator<TAgent, TWorldState, TAction, TGoal, TWatchdog, {logToggle}>
                        .{method}(agent, plan, watchdog{logArgument});

#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
                IAgent<TWorldState, TGoal, TAction> agent_ = agent;
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation

                Type planType = agent.GetType();

                bool goalPool = typeof(IGoalPool<TGoal>).IsAssignableFrom(planType);
                bool worldStatePool = typeof(IWorldStatePool<TWorldState>).IsAssignableFrom(planType);
                bool goalMerge = typeof(IGoalMerge<TGoal>).IsAssignableFrom(planType);

                if (goalPool)
                {{
                    if (worldStatePool)
                    {{
                        if (goalMerge)
                            return PlanBuilderIterator<AgentWrapperPoolGoalPoolWorldMergeGoal<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, {logToggle}>
                                .{method}(new(agent_), plan, watchdog{logArgument});
                        return PlanBuilderIterator<AgentWrapperPoolGoalPoolWorld<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, {logToggle}>
                            .{method}(new(agent_), plan, watchdog{logArgument});
                    }}
                    if (goalMerge)
                        return PlanBuilderIterator<AgentWrapperPoolGoalMergeGoal<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, {logToggle}>
                            .{method}(new(agent_), plan, watchdog);
                    return PlanBuilderIterator<AgentWrapperPoolGoal<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, {logToggle}>
                        .{method}(new(agent_), plan, watchdog{logArgument});
                }}

                if (worldStatePool)
                {{
                    if (goalMerge)
                        return PlanBuilderIterator<AgentWrapperPoolWorldMergeGoal<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, {logToggle}>
                           .{method}(new(agent_), plan, watchdog{logArgument});
                    return PlanBuilderIterator<AgentWrapperPoolWorld<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, {logToggle}>
                        .{method}(new(agent_), plan, watchdog{logArgument});
                }}

                if (goalMerge)
                    return PlanBuilderIterator<AgentWrapperMergeGoal<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, {logToggle}>
                        .{method}(new(agent_), plan, watchdog{logArgument});

                return PlanBuilderIterator<AgentWrapper<TWorldState, TAction, TGoal>, TWorldState, TAction, TGoal, TWatchdog, {logToggle}>
                    .{method}(new(agent_), plan, watchdog{logArgument});
            }}
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
            /// <param name=""agent"">Agent where world state, goals and available actions are got.<br/>
            /// This type can implement the following interfaces for additional features:
            /// <see cref=""IGoalMerge{{TGoal}}""/>, <see cref=""IGoalPool{{TGoal}}""/>, <see cref=""IWorldStatePool{{TWorld}}""/>.</param>
            /// <param name=""plan"">Collection where actions required to complete the goal will be stored.</param>
            {(watchdogName == "" ? @"/// <param name=""watchdog"">Token used to cancelate or suspend the execution </param>" : watchdogName is null ? "" : @$"/// <param name=""{watchdogName}"">{watchdogDescription}</param>")}
            {(hasLog ? @"/// <param name=""log"">Log action used to debug the planification. The layout of the log content is an implementation detail.</param>" : "")}
            /// <returns></returns>
            /// <exception cref=""ArgumentNullException"">Throw if <paramref name=""agent""/> is <see langword=""null""/>.</exception>
            /// <exception cref=""ArgumentNullException"">Throw if <paramref name=""plan""/> is <see langword=""null""/>.</exception>
            {(watchdogName == "" ? @"/// <exception cref=""ArgumentNullException"">Throw if <paramref name=""watchdog""/> is <see langword=""null""/>.</exception>" : "")}
            {(hasLog ? @"/// <exception cref=""ArgumentNullException"">Throw if <paramref name=""log""/> is <see langword=""null""/>.</exception>" : "")}";
    }
}
