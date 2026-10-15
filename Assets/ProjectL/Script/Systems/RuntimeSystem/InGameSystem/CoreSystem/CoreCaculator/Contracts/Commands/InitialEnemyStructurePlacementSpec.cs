using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>
    /// 적의 건물을 어디에다가 놓을 것인지 확인하는 데이터
    /// </summary>
    public sealed class InitialEnemyStructurePlacementSpec
    {
        public EntityKey StructureKey { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }

        public InitialEnemyStructurePlacementSpec(EntityKey key, CellCoord anchor, Rotation4 rotation)
        {
            if (EntityKey.None == key || Enum.IsDefined(typeof(EntityKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
            if (Enum.IsDefined(typeof(Rotation4), rotation) == false)
                throw new ArgumentOutOfRangeException(nameof(rotation));

            StructureKey = key;
            Anchor = anchor;
            Rotation = rotation;
        }
    }
}
