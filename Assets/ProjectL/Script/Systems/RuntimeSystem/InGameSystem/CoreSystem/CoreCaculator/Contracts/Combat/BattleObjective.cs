using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum BattleObjectiveKind
    {
        None = 0,
        EnemyHeadquartersDestroyed = 1,
        DesignatedBossDefeated = 2,
    }

    /// <summary>전투 준비 때 한 번 결합하고 전투 동안 바뀌지 않는 승리 목표다. 원본은 CombatCommandState에 둔다.</summary>
    public sealed class BattleObjective
    {
        public static BattleObjective EnemyHeadquarters { get; } =
            new BattleObjective(BattleObjectiveKind.EnemyHeadquartersDestroyed, EntityId.Empty);

        public BattleObjectiveKind Kind { get; }
        /// <summary>지정 보스 목표의 전투 개체 ID다. 본부 목표에서는 비어 있다.</summary>
        public EntityId TargetEntityId { get; }

        private BattleObjective(BattleObjectiveKind kind, EntityId targetEntityId)
        {
            if (kind == BattleObjectiveKind.EnemyHeadquartersDestroyed ? !targetEntityId.IsEmpty :
                kind != BattleObjectiveKind.DesignatedBossDefeated || targetEntityId.IsEmpty)
                throw new ArgumentException("전투 목표 종류와 대상 조합이 잘못되었습니다.");
            Kind = kind;
            TargetEntityId = targetEntityId;
        }

        public static BattleObjective DesignatedBoss(EntityId target) =>
            new BattleObjective(BattleObjectiveKind.DesignatedBossDefeated, target);

        public bool SameValues(BattleObjective other) => other != null &&
            Kind == other.Kind && TargetEntityId == other.TargetEntityId;
    }
}
