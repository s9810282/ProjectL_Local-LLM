using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>
    /// 적 전용 재구성·자폭 특성의 단계(1~3) 수치표입니다. DB는 단계만 입력하고 수치는 이 한 곳이 정합니다.
    /// DB 데이터 검사와 Spec이 같은 단계 범위를 쓰도록 Contracts에 둡니다.
    /// </summary>
    public static class EnemyDeathTraitLevels
    {
        public const int MinLevel = 1;
        public const int MaxLevel = 3;

        public static bool IsValid(int level) => level >= MinLevel && level <= MaxLevel;

        /// <summary>재구성 순간 받는 보호막의 최대 체력 대비 비율(%)입니다. Lv1은 보호막이 없습니다.</summary>
        public static int ReconstructionShieldPercent(int level) => Require(level) switch
        {
            1 => 0,
            _ => 50
        };

        /// <summary>부활할 때 주변 8칸의 캐릭터를 1칸 밀어내는지입니다. Lv3만 밉니다.</summary>
        public static bool ReconstructionPushesOnRevive(int level) => Require(level) == 3;

        /// <summary>자폭 피해의 사망 직전 공격력 대비 비율(%)입니다.</summary>
        public static int SelfDestructDamagePercent(int level) => Require(level) switch
        {
            1 => 50,
            2 => 75,
            _ => 110
        };

        private static int Require(int level)
        {
            if (!IsValid(level)) throw new ArgumentOutOfRangeException(nameof(level));
            return level;
        }
    }
}
