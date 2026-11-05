using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>재생 생산자의 이번 주기 진행값입니다. 체력과 수신 효과 상태는 저장하지 않습니다.</summary>
    public sealed class RegenerationRuntimeState : TraitRuntimeState
    {
        public int CompletedEligibleTurnEnds { get; private set; }

        public RegenerationRuntimeState(int completedEligibleTurnEnds = 0)
            : base(TraitKey.Regeneration)
        {
            if (completedEligibleTurnEnds < 0) throw new ArgumentOutOfRangeException(nameof(completedEligibleTurnEnds));
            CompletedEligibleTurnEnds = completedEligibleTurnEnds;
        }

        internal override TraitRuntimeState Clone() => new RegenerationRuntimeState(CompletedEligibleTurnEnds);

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not RegenerationRuntimeState regeneration)
                throw new ArgumentException("재생 상태에는 같은 타입의 최종 상태가 필요합니다.", nameof(source));
            CompletedEligibleTurnEnds = regeneration.CompletedEligibleTurnEnds;
        }
    }
}
