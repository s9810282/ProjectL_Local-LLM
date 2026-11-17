using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    public sealed class ThornBombTraitData : UnitTraitData
    {
        public override TraitKey TraitKey => TraitKey.ThornBomb;
        public int DamagePercent { get; }

        public ThornBombTraitData(int damagePercent)
        {
            if (damagePercent < 0) throw new ArgumentOutOfRangeException(nameof(damagePercent));
            DamagePercent = damagePercent;
        }

        public override void Validate()
        {
            if (DamagePercent < 0)
                throw new InvalidOperationException("가시 폭탄 피해 백분율은 음수일 수 없습니다.");
        }
    }
}
