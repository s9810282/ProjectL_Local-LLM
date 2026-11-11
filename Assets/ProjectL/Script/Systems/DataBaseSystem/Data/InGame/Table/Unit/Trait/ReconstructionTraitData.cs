using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 정의에서 내보낸 불변 재구성 단계입니다. 적 전용 특성입니다.</summary>
    public sealed class ReconstructionTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Reconstruction;
        public int Level { get; }

        public ReconstructionTraitData(int level) => Level = level;

        public override void Validate()
        {
            if (!EnemyDeathTraitLevels.IsValid(Level))
                throw new InvalidOperationException("[ReconstructionTraitData] Level must be 1 to 3.");
        }
    }
}
