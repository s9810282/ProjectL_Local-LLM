using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class GroundCompactionTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int movementRangeBonus = 1;
        [SerializeField, Min(1), InspectorName("생성 턴 이후 만료까지의 아군 진영 턴 종료 횟수")]
        private int opposingTurnEndsToExpire = 1;

        internal override UnitTraitData ToData() => new GroundCompactionTraitData(movementRangeBonus, opposingTurnEndsToExpire);
    }
}
