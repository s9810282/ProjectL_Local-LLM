using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class StationaryFireTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int extraDamagePercent = 100;

        internal override UnitTraitData ToData() => new StationaryFireTraitData(extraDamagePercent);
    }
}
