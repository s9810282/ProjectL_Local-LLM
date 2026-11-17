using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum ObstacleDestructionCause
    {
        DeliveredHit = 1,
        EnvironmentalContact,
        BarrelBlast,
        BarrelChain,
        ExplicitDetonation,
        Fall,
        PillarCrush
    }

    /// <summary>장애물 타수의 기대값과 결과 및 물리적 원인을 기록한다.</summary>
    public sealed class CombatObstacleDurabilityEvent : CombatEvent
    {
        public EntityId ObstacleId => Expected.EntityId;
        public StructureCombatFacts Expected { get; }
        public StructureCombatFacts Final { get; }
        public ObstacleDestructionCause Cause { get; }
        public CombatDamageOrigin Origin { get; }
        public CombatEvent ContextEvent { get; }
        public MovementTileFact EnvironmentSource { get; }

        public CombatObstacleDurabilityEvent(StructureCombatFacts expected, StructureCombatFacts final,
            ObstacleDestructionCause cause, CombatDamageOrigin origin, CombatEvent contextEvent = null,
            MovementTileFact environmentSource = null) : base(CombatEventKind.ObstacleDurabilityChanged)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            if (expected.ObstacleDurability == null || final.ObstacleDurability == null ||
                expected.EntityId != final.EntityId)
                throw new ArgumentException("같은 장애물의 내구도 변경이 필요합니다.");
            if (!Enum.IsDefined(typeof(ObstacleDestructionCause), cause))
                throw new ArgumentOutOfRangeException(nameof(cause));
            if (cause == ObstacleDestructionCause.Fall ? origin != null : origin?.PhysicalCell == null)
                throw new ArgumentException("장애물 내구도 변경의 물리 출처가 올바르지 않습니다.", nameof(origin));
            Cause = cause;
            Origin = origin;
            ContextEvent = contextEvent;
            EnvironmentSource = environmentSource;
        }
    }
}
