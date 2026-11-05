using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class FocusedBombardmentTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int requiredEnemyCount = 1;
        [SerializeField, Min(0)] private int extraDamagePercent = 50;

        internal override UnitTraitData ToData() => new FocusedBombardmentTraitData(requiredEnemyCount, extraDamagePercent);
    }
}
