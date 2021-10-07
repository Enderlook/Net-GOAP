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
            Debug.Assert(builder is not null);
            builder.Append(message);
            Log();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendToLog(string message)
        {
            Debug.Assert(builder is not null);
            builder.Append(message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AppendToLog(int number)
        {
            Debug.Assert(builder is not null);
            builder.Append(number);
        }

        private void AppendToLogNode(int id)
        {
            Debug.Assert(builder is not null);
            builder.Append(nodesText[id]);
            PathNode node = nodes[id];
            while (node.Mode != PathNode.Type.Start)
            {
                id = node.Parent;
                node = nodes[id];
                builder.Append("\n -> ").Append(nodesText[id]);
            }
            builder.Append('.');
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Log()
        {
            Debug.Assert(log is not null);
            Debug.Assert(builder is not null);
            log(builder.ToString());
            builder.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLog(Action<string> log)
        {
            if (builder is null)
                builder = new();
            Debug.Assert(log is not null);
            this.log = log;
        }
    }
}
