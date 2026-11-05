using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>
    /// 경험치·레벨업 설정값 묶음이다. 기본값은 꺼짐이며 꺼진 설정에서는 처치 경험치가 생기지 않는다.
    /// 켜진 값은 생성자가 범위를 강제한다. 공식은 Application의 ExperienceRules가 가진다.
    /// </summary>
    public readonly struct LevelProgressionSpec
    {
        public bool IsEnabled { get; }
        /// <summary>메인이 레벨 L에서 L+1로 오를 때 필요한 경험치는 이 값 × L이다.</summary>
        public int MainExperiencePerLevel { get; }
        /// <summary>서브가 레벨 L에서 L+1로 오를 때 필요한 경험치는 이 값 × L이다.</summary>
        public int SubExperiencePerLevel { get; }
        /// <summary>이전 호출자의 호환성 값이다. 처치 보상 계산에는 사용하지 않는다.</summary>
        public int KillExperiencePerEnemyLevel { get; }
        /// <summary>메인 랜덤 성장에서 최대 체력 선택지가 더하는 양이다.</summary>
        public int MainMaxHealthGrowth { get; }
        /// <summary>메인 랜덤 성장에서 공격력 선택지가 더하는 양이다.</summary>
        public int MainAttackGrowth { get; }
        /// <summary>메인 랜덤 성장에서 방어력 선택지가 더하는 양이다.</summary>
        public int MainDefenseGrowth { get; }
        public LevelProgressionSpec(int mainExperiencePerLevel, int subExperiencePerLevel,
            int killExperiencePerEnemyLevel, int mainMaxHealthGrowth, int mainAttackGrowth, int mainDefenseGrowth)
            : this(mainExperiencePerLevel, subExperiencePerLevel, mainMaxHealthGrowth, mainAttackGrowth, mainDefenseGrowth)
        {
            if (killExperiencePerEnemyLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(killExperiencePerEnemyLevel));
            KillExperiencePerEnemyLevel = killExperiencePerEnemyLevel;
        }

        public LevelProgressionSpec(int mainExperiencePerLevel, int subExperiencePerLevel,
            int mainMaxHealthGrowth, int mainAttackGrowth, int mainDefenseGrowth)
        {
            if (mainExperiencePerLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(mainExperiencePerLevel));
            if (subExperiencePerLevel < 1)
                throw new ArgumentOutOfRangeException(nameof(subExperiencePerLevel));
            if (mainMaxHealthGrowth < 0)
                throw new ArgumentOutOfRangeException(nameof(mainMaxHealthGrowth));
            if (mainAttackGrowth < 0)
                throw new ArgumentOutOfRangeException(nameof(mainAttackGrowth));
            if (mainDefenseGrowth < 0)
                throw new ArgumentOutOfRangeException(nameof(mainDefenseGrowth));
            IsEnabled = true;
            MainExperiencePerLevel = mainExperiencePerLevel;
            SubExperiencePerLevel = subExperiencePerLevel;
            KillExperiencePerEnemyLevel = 0;
            MainMaxHealthGrowth = mainMaxHealthGrowth;
            MainAttackGrowth = mainAttackGrowth;
            MainDefenseGrowth = mainDefenseGrowth;
        }
    }
}
