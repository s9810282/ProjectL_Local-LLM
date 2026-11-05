using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>유닛별 처치 보상 정의입니다. 미설정(null)과 명시적인 제외를 구분합니다.</summary>
    public sealed class UnitKillRewardSpec
    {
        public static UnitKillRewardSpec Excluded { get; } = new UnitKillRewardSpec(false, 0, 0);
        public bool IsConfigured => true;
        public bool IsEligible { get; }
        public int Gold { get; }
        public int Experience { get; }

        public UnitKillRewardSpec(int gold, int experience) : this(true, gold, experience)
        {
        }

        private UnitKillRewardSpec(bool eligible, int gold, int experience)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            if (experience < 0) throw new ArgumentOutOfRangeException(nameof(experience));
            IsEligible = eligible;
            Gold = gold;
            Experience = experience;
        }
    }
}
