using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 SO에서 입력하는 위압의 공격력 감소율입니다.</summary>
    [Serializable]
    internal sealed class IntimidationTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Range(1, 100)] private int attackReductionPercent;

        internal override UnitTraitData ToData() =>
            new IntimidationTraitData(attackReductionPercent);
    }
}
