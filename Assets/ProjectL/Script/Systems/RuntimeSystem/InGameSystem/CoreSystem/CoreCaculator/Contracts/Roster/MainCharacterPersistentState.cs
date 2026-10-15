using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>메인의 현재 체력과 영혼·전투 임시 보정 전의 지속 수치다.</summary>
    public sealed class MainCharacterPersistentState
    {
        public int CurrentHealth { get; }
        public int PersistentMaxHealth { get; }
        public int PersistentAttack { get; }
        public int MaxActionCost { get; }
        public int MoveRange { get; }
        public int MoveCost { get; }

        public MainCharacterPersistentState(int currentHealth, int persistentMaxHealth,
            int persistentAttack, int maxActionCost, int moveRange, int moveCost)
        {
            if (currentHealth < 0)
                throw new ArgumentOutOfRangeException(nameof(currentHealth));
            if (persistentMaxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(persistentMaxHealth));
            if (persistentAttack < 0)
                throw new ArgumentOutOfRangeException(nameof(persistentAttack));
            if (maxActionCost < 0)
                throw new ArgumentOutOfRangeException(nameof(maxActionCost));
            if (moveRange < 0)
                throw new ArgumentOutOfRangeException(nameof(moveRange));
            if (moveCost <= 0)
                throw new ArgumentOutOfRangeException(nameof(moveCost));

            // 현재 체력의 상한은 최종 영혼·효과가 반영된 최대 체력으로 Buffer에서 검증한다.
            CurrentHealth = currentHealth;
            PersistentMaxHealth = persistentMaxHealth;
            PersistentAttack = persistentAttack;
            MaxActionCost = maxActionCost;
            MoveRange = moveRange;
            MoveCost = moveCost;
        }
    }
}
