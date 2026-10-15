using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class NonCombatStageBinding
    {
        public StageKey StageKey { get; }
        public NonCombatStageKind Kind { get; }

        public NonCombatStageBinding(StageKey stageKey, NonCombatStageKind kind)
        {
            if (stageKey.IsEmpty) throw new ArgumentException(nameof(stageKey));
            if (!Enum.IsDefined(typeof(NonCombatStageKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            StageKey = stageKey;
            Kind = kind;
        }
    }
}
