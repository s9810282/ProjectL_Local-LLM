using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    /// <summary>약점 노출의 피해 증가율과 대상 진영 종료 횟수를 입력합니다.</summary>
    [Serializable]
    internal sealed class HuntersMarkTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int bonusDamagePercent = 10;

        [SerializeField, Min(1)] private int durationAffectedSideTurnEnds = 1;

        internal override UnitTraitData ToData() => new HuntersMarkTraitData(bonusDamagePercent, durationAffectedSideTurnEnds);
    }
}
