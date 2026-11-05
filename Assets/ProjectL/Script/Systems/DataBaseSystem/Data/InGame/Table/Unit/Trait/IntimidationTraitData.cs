using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 정의에서 내보낸 불변 위압 설정입니다.</summary>
    public sealed class IntimidationTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Intimidation;
        public int AttackReductionPercent { get; }

        public IntimidationTraitData(int attackReductionPercent)
        {
            AttackReductionPercent = attackReductionPercent;
        }

        public override void Validate()
        {
            if (AttackReductionPercent < 1 || AttackReductionPercent > 100)
                throw new InvalidOperationException("[IntimidationTraitData] AttackReductionPercent must be between 1 and 100.");
        }
    }
}
