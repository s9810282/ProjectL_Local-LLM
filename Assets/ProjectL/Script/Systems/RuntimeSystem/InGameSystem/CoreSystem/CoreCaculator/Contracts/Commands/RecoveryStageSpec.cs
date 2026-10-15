using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class RecoveryStageSpec
    {
        public StageKey StageKey { get; }
        public ReadOnlyMemory<RecoveryOptionSpec> Options { get; }

        public RecoveryStageSpec(StageKey stageKey, IReadOnlyList<RecoveryOptionSpec> options)
        {
            if (stageKey.IsEmpty) throw new ArgumentException(nameof(stageKey));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.Count == 0) throw new ArgumentException("At least one option is required.", nameof(options));

            var copied = new RecoveryOptionSpec[options.Count];
            var ids = new HashSet<RecoveryOptionId>();
            for (int i = 0; i < options.Count; i++)
            {
                RecoveryOptionSpec option = options[i];
                if (option == null) throw new ArgumentException("Options cannot contain null.", nameof(options));
                if (!ids.Add(option.Id)) throw new ArgumentException("Option IDs must be unique.", nameof(options));
                copied[i] = option;
            }

            StageKey = stageKey;
            Options = copied;
        }
    }
}
