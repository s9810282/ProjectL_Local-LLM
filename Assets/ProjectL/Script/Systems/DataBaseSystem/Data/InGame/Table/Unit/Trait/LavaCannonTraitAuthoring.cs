using System;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait
{
    [Serializable]
    internal sealed class LavaCannonTraitAuthoring : UnitTraitAuthoring
    {
        internal override UnitTraitData ToData() => new LavaCannonTraitData();
    }
}
