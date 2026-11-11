using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class BleedingTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.Bleeding;
        public int DamagePerStack { get; }

        public BleedingTraitData(int damagePerStack)
        {
            DamagePerStack = damagePerStack;
        }

        public override void Validate()
        {
            if (DamagePerStack < 1)
                throw new InvalidOperationException("출혈의 중첩당 피해는 1 이상이어야 합니다.");
        }
    }
}
