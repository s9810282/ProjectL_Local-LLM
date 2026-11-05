using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class PositionSwapTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int shieldAmount;

        internal override UnitTraitData ToData() => new PositionSwapTraitData(shieldAmount);
    }
}
