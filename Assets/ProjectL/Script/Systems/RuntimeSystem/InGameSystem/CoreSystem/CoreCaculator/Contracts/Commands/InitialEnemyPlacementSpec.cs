using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class InitialEnemyPlacementSpec
    {
        public int? StartingItemKey { get; }
        public EntityKey UnitKey { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }
        /// <summary>지정 보스 목표의 대상 표식이다. 보스 목표 전투에서 정확히 한 배치만 가진다.</summary>
        public bool IsVictoryTarget { get; }
        /// <summary>이 적의 시작 레벨이다. 기준 능력치를 레벨로 성장시키는 입력이며 전투 중 저장하지 않는다.</summary>
        public int Level { get; }

        public InitialEnemyPlacementSpec(
            EntityKey unitKey, CellCoord anchor, Rotation4 rotation, bool isVictoryTarget = false, int? startingItemKey = null,
            int level = 1)
        {
            if (unitKey == EntityKey.None)
                throw new ArgumentOutOfRangeException(nameof(unitKey));
            if (Enum.IsDefined(typeof(Rotation4), rotation) == false)
                throw new ArgumentOutOfRangeException(nameof(rotation));
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level));

            if (startingItemKey <= 0) throw new ArgumentOutOfRangeException(nameof(startingItemKey));
            StartingItemKey = startingItemKey;
            UnitKey = unitKey;
            Anchor = anchor;
            Rotation = rotation;
            IsVictoryTarget = isVictoryTarget;
            Level = level;
        }
    }
}
