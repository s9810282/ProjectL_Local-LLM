using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    /// <summary>적 전용 자폭 특성의 설정입니다. 피해 비율은 단계로 <see cref="EnemyDeathTraitLevels"/>에서 읽습니다.</summary>
    public sealed class SelfDestructTraitDefinition : TraitDefinition
    {
        public int Level { get; }

        public SelfDestructTraitDefinition(int level) : base(TraitKey.SelfDestruct)
        {
            if (!EnemyDeathTraitLevels.IsValid(level)) throw new ArgumentOutOfRangeException(nameof(level));
            Level = level;
        }
    }
}
