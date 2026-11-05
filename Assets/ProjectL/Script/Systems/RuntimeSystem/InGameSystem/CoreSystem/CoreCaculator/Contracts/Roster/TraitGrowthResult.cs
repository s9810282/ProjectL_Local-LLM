using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>선택 명령의 결과다. 성공한 경우에만 저장된 최종 보유 기록을 제공한다.</summary>
    public sealed class TraitGrowthResult
    {
        public bool Succeeded => FailureReason == TraitGrowthFailureReason.None;
        public TraitGrowthFailureReason FailureReason { get; }
        public OwnedCharacterRecord Final { get; }

        private TraitGrowthResult(TraitGrowthFailureReason reason, OwnedCharacterRecord final)
        {
            FailureReason = reason;
            Final = final;
        }

        public static TraitGrowthResult Success(OwnedCharacterRecord final) =>
            new(TraitGrowthFailureReason.None, final ?? throw new ArgumentNullException(nameof(final)));

        public static TraitGrowthResult Failure(TraitGrowthFailureReason reason)
        {
            if (reason == TraitGrowthFailureReason.None || !Enum.IsDefined(typeof(TraitGrowthFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new TraitGrowthResult(reason, null);
        }
    }
}
