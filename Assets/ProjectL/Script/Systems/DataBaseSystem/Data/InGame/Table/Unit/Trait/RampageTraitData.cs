using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class RampageTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Rampage;
        public int BaseAttackBonus { get; }
        public int MovementBonus { get; }

        public RampageTraitData(int baseAttackBonus, int movementBonus)
        {
            BaseAttackBonus = baseAttackBonus;
            MovementBonus = movementBonus;
        }

        public override void Validate()
        {
            if (BaseAttackBonus < 0 || MovementBonus < 0)
                throw new InvalidOperationException("광란의 기본 공격력 보너스와 이동 보너스는 0 이상이어야 합니다.");
        }
    }
}
