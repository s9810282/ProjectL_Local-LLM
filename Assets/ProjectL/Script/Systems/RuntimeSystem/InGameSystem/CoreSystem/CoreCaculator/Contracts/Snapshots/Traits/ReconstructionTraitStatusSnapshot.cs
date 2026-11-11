using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>재구성 특성의 공개 상태입니다. 단계와 이번 전투에서 이미 썼는지(전투당 1번)를 냅니다.</summary>
    public sealed class ReconstructionTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int Level { get; }
        public bool Used { get; }

        public ReconstructionTraitStatusSnapshot(int level, bool used) : base(TraitKey.Reconstruction)
        {
            if (!EnemyDeathTraitLevels.IsValid(level)) throw new ArgumentOutOfRangeException(nameof(level));
            Level = level;
            Used = used;
        }

        public override bool SameValues(TraitStatusSnapshot other) =>
            other is ReconstructionTraitStatusSnapshot s && s.Level == Level && s.Used == Used;
    }
}
