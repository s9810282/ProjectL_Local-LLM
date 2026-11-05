using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class ArmorBreakerTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Tooltip("미입력은 -1입니다. 사용할 수치를 명시해야 합니다.")] private int defenseReductionPercentPerHit = -1;

        internal override UnitTraitData ToData() => new ArmorBreakerTraitData(defenseReductionPercentPerHit);
    }
}
