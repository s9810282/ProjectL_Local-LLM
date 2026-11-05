using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 정의에서 내보낸 불변 재생 회복량입니다.</summary>
    public sealed class RegenerationTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Regeneration;
        public int HealAmount { get; }
        public int HealEveryOwnSideTurnEnds { get; }

        public RegenerationTraitData(int healAmount, int healEveryOwnSideTurnEnds = 1)
        {
            HealAmount = healAmount;
            HealEveryOwnSideTurnEnds = healEveryOwnSideTurnEnds;
        }

        public override void Validate()
        {
            if (HealAmount < 1)
                throw new InvalidOperationException("[RegenerationTraitData] HealAmount must be positive.");
            if (HealEveryOwnSideTurnEnds < 1)
                throw new InvalidOperationException("[RegenerationTraitData] HealEveryOwnSideTurnEnds must be positive.");
        }
    }
}
