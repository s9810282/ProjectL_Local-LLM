using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    /// <summary>스테이지 승리 때 적용할 불변 보상 수치입니다.</summary>
    public sealed class StageClearRewardSpec
    {
        public int Gold { get; }
        public int Fame { get; }
        public int ExperiencePerParticipant { get; }

        public StageClearRewardSpec(int gold, int fame, int experiencePerParticipant)
        {
            if (gold < 0) throw new ArgumentOutOfRangeException(nameof(gold));
            if (fame < 0) throw new ArgumentOutOfRangeException(nameof(fame));
            if (experiencePerParticipant < 0) throw new ArgumentOutOfRangeException(nameof(experiencePerParticipant));
            Gold = gold;
            Fame = fame;
            ExperiencePerParticipant = experiencePerParticipant;
        }
    }
}
