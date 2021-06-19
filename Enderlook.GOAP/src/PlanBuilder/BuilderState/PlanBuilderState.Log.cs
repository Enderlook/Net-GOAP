using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Enderlook.GOAP
{
    internal sealed partial class PlanBuilderState<TWorldState, TGoal, TAction>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendAndLog(string message)
        {
            builder.Append(message);
            Log();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendToLog(string message) => builder.Append(message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendToLog(int number) => builder.Append(number);

        private void AppendToLogNode(int id)
        {
            builder.Append(nodesText[id]);
            PathNode node = nodes[id];
            while (node.Mode != PathNode.Type.Start)
            {
                id = node.Parent;
                node = nodes[id];
                builder.Append(" -> ").Append(nodesText[id]);
            }
            builder.Append('.');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log()
        {
            Debug.Assert(log is not null);
            log(builder.ToString());
            builder.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLog(Action<string> log)
        {
            Debug.Assert(log is not null);
            this.log = log;
        }
    }
}
