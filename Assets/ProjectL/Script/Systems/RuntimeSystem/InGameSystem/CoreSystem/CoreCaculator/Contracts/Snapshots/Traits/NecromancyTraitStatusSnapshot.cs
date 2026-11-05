using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    /// <summary>사령술 정의값의 공개 사본이다. 소환 수 같은 진행값은 소환 기록에서 계산하므로 두지 않는다.</summary>
    public sealed class NecromancyTraitStatusSnapshot : TraitStatusSnapshot
    {
        public EntityKey SummonedUnitKey { get; }
        public int MaxLivingSummons { get; }
        public long FirstActionDelay { get; }

        public NecromancyTraitStatusSnapshot(EntityKey summonedUnitKey, int maxLivingSummons, long firstActionDelay)
            : base(TraitKey.Necromancy)
        {
            if (summonedUnitKey == EntityKey.None)
                throw new ArgumentOutOfRangeException(nameof(summonedUnitKey));
            if (maxLivingSummons < 1)
                throw new ArgumentOutOfRangeException(nameof(maxLivingSummons));
            if (firstActionDelay < 0)
                throw new ArgumentOutOfRangeException(nameof(firstActionDelay));
            SummonedUnitKey = summonedUnitKey;
            MaxLivingSummons = maxLivingSummons;
            FirstActionDelay = firstActionDelay;
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is NecromancyTraitStatusSnapshot n &&
            SummonedUnitKey == n.SummonedUnitKey && MaxLivingSummons == n.MaxLivingSummons && FirstActionDelay == n.FirstActionDelay;
    }
}
