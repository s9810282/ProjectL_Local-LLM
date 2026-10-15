using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class InitialEnemyPlacementSpec
    {
        public EntityKey UnitKey { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }

        public InitialEnemyPlacementSpec(
            EntityKey unitKey, CellCoord anchor, Rotation4 rotation)
        {
            if (unitKey == EntityKey.None)
                throw new ArgumentOutOfRangeException(nameof(unitKey));
            if (Enum.IsDefined(typeof(Rotation4), rotation) == false)
                throw new ArgumentOutOfRangeException(nameof(rotation));

            UnitKey = unitKey;
            Anchor = anchor;
            Rotation = rotation;
        }
    }
}