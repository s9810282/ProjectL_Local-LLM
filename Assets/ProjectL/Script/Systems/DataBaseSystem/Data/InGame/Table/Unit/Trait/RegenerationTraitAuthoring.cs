using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>유닛 SO에서 입력하는 재생 회복량입니다.</summary>
    [Serializable]
    internal sealed class RegenerationTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int healAmount = 1;
        [SerializeField, Min(1)] private int healEveryOwnSideTurnEnds = 1;

        internal override UnitTraitData ToData() => new RegenerationTraitData(healAmount, healEveryOwnSideTurnEnds);
    }
}
