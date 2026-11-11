using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>적 전용 자폭 특성의 입력입니다. 단계 수치는 <see cref="EnemyDeathTraitLevels"/>가 정합니다.</summary>
    public sealed class SelfDestructTraitSpec : TraitSpec
    {
        public int Level { get; }

        public SelfDestructTraitSpec(int level)
            : base(TraitKey.SelfDestruct)
        {
            if (!EnemyDeathTraitLevels.IsValid(level))
                throw new ArgumentOutOfRangeException(nameof(level));

            Level = level;
        }

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other) => Level == ((SelfDestructTraitSpec)other).Level;
    }
}
