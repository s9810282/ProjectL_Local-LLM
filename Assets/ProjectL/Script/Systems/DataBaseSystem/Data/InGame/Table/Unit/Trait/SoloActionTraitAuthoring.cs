using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class SoloActionTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int shieldAmount = 10;
        [SerializeField, Min(1)] private int radiusCells = 1;
        [SerializeField, Min(0)] private int requiredOccupiedNeighborCount = 0;
        [SerializeField, Min(1)] private int resetEveryControllerTurnEnds = 1;

        internal override UnitTraitData ToData() => new SoloActionTraitData(shieldAmount, radiusCells, requiredOccupiedNeighborCount, resetEveryControllerTurnEnds);
    }
}
