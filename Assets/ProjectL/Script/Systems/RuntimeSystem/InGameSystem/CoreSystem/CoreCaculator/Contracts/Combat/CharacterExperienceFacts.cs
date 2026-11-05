using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 전투 사실의 경험치 묶음이다. 적은 처치 경험치의 기준인 생성 레벨(<see cref="EnemyLevel"/>)을,
    /// 아군은 이번 전투에서 번 경험치(<see cref="Earned"/>)를 가진다. 둘 다 전투 동안만 살고 전투가 끝나면 사실과 함께 버려진다.
    /// 보유 카드의 레벨·진행 경험치는 여기에 복사하지 않는다(원본은 보유 기록).
    /// </summary>
    public sealed class CharacterExperienceFacts
    {
        /// <summary>레벨을 지정하지 않은 적의 기본값(레벨 1)이다.</summary>
        public static CharacterExperienceFacts EnemyDefault { get; } = new CharacterExperienceFacts(1, 0);
        /// <summary>아직 번 경험치가 없는 아군의 기본값이다.</summary>
        public static CharacterExperienceFacts AllyDefault { get; } = new CharacterExperienceFacts(null, 0);

        /// <summary>적의 생성 레벨이다. 아군은 null이다.</summary>
        public int? EnemyLevel { get; }
        /// <summary>아군이 이번 전투에서 번 경험치다. 적은 0이다. 승리 때 보유 기록에 반영된다.</summary>
        public int Earned { get; }

        private CharacterExperienceFacts(int? enemyLevel, int earned)
        {
            EnemyLevel = enemyLevel;
            Earned = earned;
        }

        /// <summary>생성 레벨 <paramref name="level"/>인 적의 경험치 묶음이다. 레벨은 1 이상이다.</summary>
        public static CharacterExperienceFacts ForEnemy(int level)
        {
            if (level < 1)
                throw new ArgumentOutOfRangeException(nameof(level));

            return level == 1 ? EnemyDefault : new CharacterExperienceFacts(level, 0);
        }

        /// <summary>이번 전투에서 <paramref name="earned"/>만큼 번 아군의 경험치 묶음이다. 0 이상이다.</summary>
        public static CharacterExperienceFacts ForAlly(int earned)
        {
            if (earned < 0)
                throw new ArgumentOutOfRangeException(nameof(earned));

            return earned == 0 ? AllyDefault : new CharacterExperienceFacts(null, earned);
        }

        /// <summary>적 레벨과 획득 경험치가 모두 같은지 비교한다.</summary>
        public bool SameValues(CharacterExperienceFacts other) =>
            other != null && EnemyLevel == other.EnemyLevel && Earned == other.Earned;
    }
}
