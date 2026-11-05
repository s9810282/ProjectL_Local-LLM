using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster
{
    /// <summary>
    /// 전투 밖 기준 체력 공식의 유일한 위치다. 최대 체력은 영혼 반영 전 최대 체력과 영혼 기여분의 합이다.
    /// 의존자는 기준 능력치 계산, 회복 계산, 공개 메인 체력 함수다. 이 파일을 바꾸면 세 경로를 모두 다시 확인한다.
    /// 위치·환경·전투 임시 보정은 다루지 않는다.
    /// </summary>
    internal static class CharacterHealthRules
    {
        /// <summary>영혼 수로 계산한 최대 체력 기여분이다.</summary>
        internal static int SoulMaxHealthBonus(int soulCount)
        {
            if (soulCount < 0)
                throw new ArgumentOutOfRangeException(nameof(soulCount));

            return checked(soulCount * SoulGrowthRules.MaxHealthBonusPerSoul);
        }

        /// <summary>영혼 반영 전 최대 체력과 영혼 수로 기준 체력을 만든다. 지속 현재 체력이 없으면 최대 체력에서 시작한다.</summary>
        internal static HealthStateSnapshot Health(int baseMaxHealth, int soulCount, int? persistentCurrentHealth)
        {
            if (baseMaxHealth < 1)
                throw new ArgumentOutOfRangeException(nameof(baseMaxHealth));

            int maxHealth = checked(baseMaxHealth + SoulMaxHealthBonus(soulCount));
            int currentHealth = persistentCurrentHealth ?? maxHealth;
            if (currentHealth < 0 || currentHealth > maxHealth)
                throw new InvalidOperationException("지속 현재 체력은 0 이상, 최대 체력 이하여야 합니다.");

            return new HealthStateSnapshot(currentHealth, maxHealth);
        }

        /// <summary>메인 지속 상태와 영혼 수로 계산한 전투 밖 메인 체력이다.</summary>
        internal static HealthStateSnapshot PersistentMainHealth(MainCharacterPersistentState main, int soulCount)
        {
            if (main == null)
                throw new ArgumentNullException(nameof(main));

            return Health(main.PersistentMaxHealth, soulCount, main.CurrentHealth);
        }
    }
}
