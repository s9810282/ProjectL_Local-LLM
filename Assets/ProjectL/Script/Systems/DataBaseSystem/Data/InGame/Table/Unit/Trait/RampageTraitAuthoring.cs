using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class RampageTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0), Tooltip("기본 공격력 보너스 n입니다. 밸런스 입력 전 임시값은 0입니다.")]
        private int baseAttackBonus = 0;
        [SerializeField, Min(0)] private int movementBonus = 1;

        internal override UnitTraitData ToData() => new RampageTraitData(baseAttackBonus, movementBonus);
    }
}
