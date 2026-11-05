using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class AnkleBreakerTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(1)] private int durationAffectedSideTurnEnds = 1;

        internal override UnitTraitData ToData() => new AnkleBreakerTraitData(durationAffectedSideTurnEnds);
    }
}
