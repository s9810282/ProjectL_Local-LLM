using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class DisarmTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int durationAffectedSideTurnEnds = 1;

        internal override UnitTraitData ToData() => new DisarmTraitData(durationAffectedSideTurnEnds);
    }
}
