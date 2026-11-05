using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class CohesionTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Tooltip("미입력은 -1입니다. 사용할 수치를 명시해야 합니다.")] private int defensePerAdjacentAlly = -1;
        [SerializeField, Tooltip("미입력은 -1입니다. 사용할 수치를 명시해야 합니다.")] private int shieldAmountPerAlly = -1;
        [SerializeField, Min(1)] private int radiusCells = 1;
        [SerializeField, Min(1)] private int resetEveryControllerTurnEnds = 1;

        internal override UnitTraitData ToData() => new CohesionTraitData(defensePerAdjacentAlly, shieldAmountPerAlly, radiusCells, resetEveryControllerTurnEnds);
    }
}
