using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>자폭 특성의 공개 상태입니다. 단계만 냅니다(피해 비율은 단계 표에서 읽음).</summary>
    public sealed class SelfDestructTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int Level { get; }

        public SelfDestructTraitStatusSnapshot(int level) : base(TraitKey.SelfDestruct)
        {
            if (!EnemyDeathTraitLevels.IsValid(level)) throw new ArgumentOutOfRangeException(nameof(level));
            Level = level;
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is SelfDestructTraitStatusSnapshot s && s.Level == Level;
    }
}
