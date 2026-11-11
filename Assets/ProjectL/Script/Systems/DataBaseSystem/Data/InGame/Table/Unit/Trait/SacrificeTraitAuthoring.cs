using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class SacrificeTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int radiusCells = 2;
        [SerializeField, Min(1)] private int maxTriggersPerEnemyTurn = 1;
        [SerializeField] private bool preventsLethalSacrificeDamage;
        [SerializeField, Min(0)] private int shieldOnActivation;

        internal override UnitTraitData ToData() => new SacrificeTraitData(radiusCells, maxTriggersPerEnemyTurn,
            preventsLethalSacrificeDamage, shieldOnActivation);
    }
}
