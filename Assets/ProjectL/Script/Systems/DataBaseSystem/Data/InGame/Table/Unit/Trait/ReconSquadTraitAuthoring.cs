using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class ReconSquadTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int increasePercent = 10;
        [SerializeField, Min(1)] private int radius = 1;

        internal override UnitTraitData ToData() => new ReconSquadTraitData(increasePercent, radius);
    }
}
