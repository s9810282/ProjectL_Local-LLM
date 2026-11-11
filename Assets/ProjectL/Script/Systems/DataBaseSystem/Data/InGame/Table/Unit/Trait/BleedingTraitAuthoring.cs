using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class BleedingTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int damagePerStack = 1;

        internal override UnitTraitData ToData() => new BleedingTraitData(damagePerStack);
    }
}
