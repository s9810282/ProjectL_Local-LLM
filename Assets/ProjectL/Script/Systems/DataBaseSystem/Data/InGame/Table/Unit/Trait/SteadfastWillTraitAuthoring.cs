using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class SteadfastWillTraitAuthoring : UnitTraitAuthoring
    {
        internal override UnitTraitData ToData() => new SteadfastWillTraitData();
    }
}
