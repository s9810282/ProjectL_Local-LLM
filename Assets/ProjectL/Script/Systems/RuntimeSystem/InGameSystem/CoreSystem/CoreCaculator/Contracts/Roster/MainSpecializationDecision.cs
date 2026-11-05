using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    /// <summary>전직의 순수 판정 결과다. 성공한 변경도 저장은 호출자가 담당한다.</summary>
    public sealed class MainSpecializationDecision
    {
        public bool Succeeded => FailureReason == MainSpecializationFailureReason.None;
        public MainSpecializationChange Change { get; }
        public MainSpecializationFailureReason FailureReason { get; }

        private MainSpecializationDecision(MainSpecializationFailureReason reason, MainSpecializationChange change)
        {
            FailureReason = reason;
            Change = change;
        }

        public static MainSpecializationDecision Success(MainSpecializationChange change) =>
            new(MainSpecializationFailureReason.None, change ?? throw new ArgumentNullException(nameof(change)));

        public static MainSpecializationDecision Failure(MainSpecializationFailureReason reason)
        {
            if (reason == MainSpecializationFailureReason.None || !Enum.IsDefined(typeof(MainSpecializationFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new MainSpecializationDecision(reason, null);
        }
    }
}
