using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>보스 사령술 정의다. 진영 턴에서 소환 시점이 정해지지 않아 보류 중이며, 소환은 실행되지 않는다(DeferredCombatFeatures).</summary>
    public sealed class NecromancyTraitSpec : TraitSpec
    {
        /// <summary>소환할 유닛 정의 키다. 전투 준비의 유닛·프로필 목록에 있어야 한다.</summary>
        public EntityKey SummonedUnitKey { get; }
        /// <summary>같은 출처의 살아 있는 소환수(Field + EnemyWaiting) 최대 수다.</summary>
        public int MaxLivingSummons { get; }
        /// <summary>소환 시각에 더하는 첫 행동 지연(행동 타임라인 시간)이다.</summary>
        public long FirstActionDelay { get; }

        public NecromancyTraitSpec(EntityKey summonedUnitKey, int maxLivingSummons, long firstActionDelay)
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

        /// <inheritdoc />
        protected override bool SameValuesCore(TraitSpec other)
        {
            var otherSpec = (NecromancyTraitSpec)other;
            return SummonedUnitKey == otherSpec.SummonedUnitKey &&
                MaxLivingSummons == otherSpec.MaxLivingSummons &&
                FirstActionDelay == otherSpec.FirstActionDelay;
        }
    }
}
