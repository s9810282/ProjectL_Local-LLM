using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using System;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    /// <summary>거리와 현재 공격력, 이번 전달의 실제 피격 순번입니다. 구조물은 공격력이 없습니다.</summary>
    internal readonly struct TargetDamageEffectContext
    {
        public int ActorAttack { get; }
        public HealthStateSnapshot? RecipientHealth { get; }
        public int? CapturedEnemyCount { get; }
        public int? RecipientAttack { get; }
        public int Distance { get; }
        public int ActualRecipientIndex { get; }
        public TargetDamageEffectContext(int actorAttack, int? recipientAttack, int distance, int actualRecipientIndex, HealthStateSnapshot? recipientHealth = null, int? capturedEnemyCount = null)
        {
            if (actorAttack < 0 || recipientAttack < 0 || distance < 0 || actualRecipientIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(actorAttack));
            if (capturedEnemyCount < 0) throw new ArgumentOutOfRangeException(nameof(capturedEnemyCount));
            RecipientHealth = recipientHealth;
            CapturedEnemyCount = capturedEnemyCount;
            ActorAttack = actorAttack;
            RecipientAttack = recipientAttack;
            Distance = distance;
            ActualRecipientIndex = actualRecipientIndex;
        }
    }
}
