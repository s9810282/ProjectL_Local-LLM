using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects
{
    /// <summary>
    /// 적 전용 재구성 중 상태입니다. 이 상태인 동안 이동·공격할 수 없고 피해·강제 이동은 그대로 받습니다.
    /// 걸린 쪽 자기 진영 턴 시작마다 1 줄고, 0이 되는 턴 시작에 상태가 풀려 그 턴에 행동합니다.
    /// </summary>
    public sealed class ReconstructionStatus : AppliedStatus
    {
        public int RemainingOwnTurnStarts { get; }
        public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.AffectedSideTurnStarts;

        public ReconstructionStatus(EffectOriginKey origin, int remainingOwnTurnStarts) : base(StatusEffectKey.Reconstructing, origin)
        {
            if (remainingOwnTurnStarts < 1) throw new ArgumentOutOfRangeException(nameof(remainingOwnTurnStarts));
            RemainingOwnTurnStarts = remainingOwnTurnStarts;
        }

        public override bool SameValues(AppliedStatus other) => other is ReconstructionStatus status &&
            Origin == status.Origin && RemainingOwnTurnStarts == status.RemainingOwnTurnStarts;
    }
}
