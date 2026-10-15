using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class StageContentRef
    {
        public StageKey Key { get; }
        public StageExecutionKind ExecutionKind { get; }

        public StageContentRef(StageKey key, StageExecutionKind executionKind)
        {
            if (key.IsEmpty) throw new ArgumentException(nameof(key));
            if (!Enum.IsDefined(typeof(StageExecutionKind), executionKind))
                throw new ArgumentOutOfRangeException(nameof(executionKind));

            Key = key;
            ExecutionKind = executionKind;
        }
    }
}
