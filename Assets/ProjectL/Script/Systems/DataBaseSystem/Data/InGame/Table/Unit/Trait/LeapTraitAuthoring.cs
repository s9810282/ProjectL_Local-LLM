using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class LeapTraitAuthoring : UnitTraitAuthoring
    {
        internal override UnitTraitData ToData() => new LeapTraitData();
    }
}
