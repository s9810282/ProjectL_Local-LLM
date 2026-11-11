using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>적 전용 재구성 특성의 입력입니다. 단계 수치는 <see cref="EnemyDeathTraitLevels"/>가 정합니다.</summary>
    public sealed class ReconstructionTraitSpec : TraitSpec
    {
        public int Level { get; }

        public ReconstructionTraitSpec(int level)
            : base(TraitKey.Reconstruction)
        {
            if (!EnemyDeathTraitLevels.IsValid(level))
                throw new ArgumentOutOfRangeException(nameof(level));

            Level = level;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other) => Level == ((ReconstructionTraitSpec)other).Level;
    }
}
