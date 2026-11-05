using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>메인의 현재 체력과 영혼·전투 임시 보정 전의 지속 수치다.</summary>
    public sealed class MainCharacterPersistentState
    {
        // ⚠ 필드를 추가·삭제할 때 반드시 함께 고친다: SameValues, Rebuild(With* 공통 경로), ContractShapeGuardTests 목록.
        //   전투 사실에 그대로 비치는 값이면 대응 규칙(CharacterCombatFacts.MatchesMainState)도. 건너뛰면 ContractShapeGuardTests가 실패한다.
        public int CurrentHealth { get; }
        public int PersistentMaxHealth { get; }
        public int PersistentAttack { get; }
        /// <summary>메인 랜덤 성장으로 쌓이는 지속 방어력이다. 초기값은 메인 정의 방어력이다.</summary>
        public int PersistentDefense { get; }

        public MainCharacterPersistentState(int currentHealth, int persistentMaxHealth,
            int persistentAttack, int persistentDefense = 0)
        {
            if (persistentDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(persistentDefense));
            if (currentHealth < 0)
                throw new ArgumentOutOfRangeException(nameof(currentHealth));
            if (persistentMaxHealth <= 0)
                throw new ArgumentOutOfRangeException(nameof(persistentMaxHealth));
            if (persistentAttack < 0)
                throw new ArgumentOutOfRangeException(nameof(persistentAttack));

            // 현재 체력의 상한은 최종 영혼·효과가 반영된 최대 체력으로 Buffer에서 검증한다.
            CurrentHealth = currentHealth;
            PersistentMaxHealth = persistentMaxHealth;
            PersistentAttack = persistentAttack;

            PersistentDefense = persistentDefense;
        }

        /// <summary>지속 값이 모두 같은지 비교한다. 인자가 null이면 예외가 난다(옛 Buffer 비교와 같다).</summary>
        public bool SameValues(MainCharacterPersistentState other) =>
            CurrentHealth == other.CurrentHealth &&
            PersistentMaxHealth == other.PersistentMaxHealth &&
            PersistentAttack == other.PersistentAttack &&
            PersistentDefense == other.PersistentDefense;

        /// <summary>지정한 값만 바꾼 새 지속 상태다. 지정하지 않은 값은 그대로 두고 생성자 검증을 다시 거친다.</summary>
        public MainCharacterPersistentState With(int? currentHealth = null, int? persistentMaxHealth = null,
            int? persistentAttack = null, int? persistentDefense = null) =>
            Rebuild(currentHealth ?? CurrentHealth, persistentMaxHealth ?? PersistentMaxHealth,
                persistentAttack ?? PersistentAttack, persistentDefense ?? PersistentDefense);

        /// <summary>현재 체력만 바꾼 새 지속 상태다.</summary>
        public MainCharacterPersistentState WithCurrentHealth(int currentHealth) => With(currentHealth: currentHealth);

        // 모든 사본은 이 경로 하나로 공개 생성자를 거친다. 검증을 건너뛰는 사본 경로를 만들지 않는다.
        private static MainCharacterPersistentState Rebuild(int currentHealth, int persistentMaxHealth,
            int persistentAttack, int persistentDefense) =>
            new MainCharacterPersistentState(currentHealth, persistentMaxHealth, persistentAttack, persistentDefense);
    }
}
