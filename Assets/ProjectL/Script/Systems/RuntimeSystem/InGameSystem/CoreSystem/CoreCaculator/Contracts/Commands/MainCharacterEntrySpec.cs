using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class MainCharacterEntrySpec
    {
        public EntityKey UnitKey { get; }
        public HealthStateSnapshot Health { get; }
        public CellCoord Anchor { get; }
        public Rotation4 Rotation { get; }

        public MainCharacterEntrySpec(
            EntityKey unitKey,
            HealthStateSnapshot health,
            CellCoord anchor,
            Rotation4 rotation)
        {
            if (unitKey != EntityKey.Unit_MainCharacter)
                throw new ArgumentException("메인 캐릭터 전용 UnitKey가 필요합니다.", nameof(unitKey));
            if (health.MaxHealth < 1 ||
                health.CurrentHealth < 0 ||
                health.CurrentHealth > health.MaxHealth)
            {
                throw new ArgumentException("유효한 초기 체력 상태가 필요합니다.", nameof(health));
            }
            if (rotation != Rotation4.R0 &&
                rotation != Rotation4.R90 &&
                rotation != Rotation4.R180 &&
                rotation != Rotation4.R270)
            {
                throw new ArgumentOutOfRangeException(nameof(rotation));
            }

            UnitKey = unitKey;
            Health = health;
            Anchor = anchor;
            Rotation = rotation;
        }
    }
}
