using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>진영별 공격 행동 횟수입니다. 턴 순서와 예산은 기존 전투 상태가 소유합니다.</summary>
    public sealed class TraitTurnFacts
    {
        public static TraitTurnFacts Empty { get; } = new();
        public int PlayerAttackActionCount { get; }
        public int EnemyAttackActionCount { get; }

        public TraitTurnFacts(int playerAttackActionCount = 0, int enemyAttackActionCount = 0)
        {
            if (playerAttackActionCount < 0) throw new ArgumentOutOfRangeException(nameof(playerAttackActionCount));
            if (enemyAttackActionCount < 0) throw new ArgumentOutOfRangeException(nameof(enemyAttackActionCount));
            PlayerAttackActionCount = playerAttackActionCount;
            EnemyAttackActionCount = enemyAttackActionCount;
        }

        public TraitTurnFacts BeginTurn(Owner owner) => owner switch
        {
            Owner.Player => new TraitTurnFacts(0, EnemyAttackActionCount),
            Owner.Enemy => new TraitTurnFacts(PlayerAttackActionCount, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(owner))
        };

        public bool SameValues(TraitTurnFacts other) => other != null &&
            PlayerAttackActionCount == other.PlayerAttackActionCount && EnemyAttackActionCount == other.EnemyAttackActionCount;
    }
}
