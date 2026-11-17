using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>부착 당시 공격력에서 사용할 폭발 피해 백분율을 입력합니다.</summary>
    [Serializable]
    internal sealed class ThornBombTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int damagePercent = 50;

        internal override UnitTraitData ToData() => new ThornBombTraitData(damagePercent);
    }
}
