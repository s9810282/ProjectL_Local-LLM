using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class EntrenchmentTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int radiusCells = 1;
        [SerializeField, Range(0, 100)] private int damageReductionPercent = 50;

        internal override UnitTraitData ToData() => new EntrenchmentTraitData(radiusCells, damageReductionPercent);
    }
}
