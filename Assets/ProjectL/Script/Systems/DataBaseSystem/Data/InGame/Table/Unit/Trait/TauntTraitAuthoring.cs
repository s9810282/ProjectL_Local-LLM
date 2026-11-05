using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class TauntTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int radiusCells = 2;

        [SerializeField, Range(0, 100)] private int damageReductionPercent;

        internal override UnitTraitData ToData() => new TauntTraitData(radiusCells, damageReductionPercent);
    }
}
