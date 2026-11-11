using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    /// <summary>적 전용 재구성 특성의 설정입니다. 보호막·부활 넉백은 단계로 <see cref="EnemyDeathTraitLevels"/>에서 읽습니다.</summary>
    public sealed class ReconstructionTraitDefinition : TraitDefinition
    {
        public int Level { get; }

        public ReconstructionTraitDefinition(int level) : base(TraitKey.Reconstruction)
        {
            if (!EnemyDeathTraitLevels.IsValid(level)) throw new ArgumentOutOfRangeException(nameof(level));
            Level = level;
        }
    }
}
