using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class FinishingShotTraitAuthoring : UnitTraitAuthoring
    {
        [SerializeField, Min(0)] private int attackThresholdPercent = 20;

        internal override UnitTraitData ToData() => new FinishingShotTraitData(attackThresholdPercent);
    }
}
