using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum MeteorGimmickStep
    {
        Warning,
        CycleSkipped,
        Resolved,
        Expired
    }

    public sealed class CombatMeteorEvent : CombatEvent, IEnvironmentRandomEvidence, IStageGimmickStateChange
    {
        public MeteorGimmickStep Step { get; }
        public StageGimmickBoundary Boundary { get; }
        public MeteorGimmickState Expected { get; }
        public MeteorGimmickState Final { get; }
        public uint? ExpectedRandomState { get; }
        public uint? FinalRandomState { get; }
        StageGimmickState IStageGimmickStateChange.FinalGimmick => Final;

        public CombatMeteorEvent(MeteorGimmickStep step, StageGimmickBoundary boundary, MeteorGimmickState expected,
            MeteorGimmickState final, uint? expectedRandomState = null, uint? finalRandomState = null) : base(CombatEventKind.MeteorAdvanced)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            bool plans = step is MeteorGimmickStep.Warning or MeteorGimmickStep.CycleSkipped;
            var phase = step switch
            {
                MeteorGimmickStep.Warning or MeteorGimmickStep.CycleSkipped => StageGimmickBoundaryPhase.Warning,
                MeteorGimmickStep.Resolved => StageGimmickBoundaryPhase.Scheduled,
                MeteorGimmickStep.Expired => StageGimmickBoundaryPhase.Expiry,
                _ => throw new ArgumentOutOfRangeException(nameof(step))
            };
            if (expected.Id != final.Id || phase != boundary.Phase || boundary.CompletedControllerTurnCount < 1 ||
                plans != expectedRandomState.HasValue || plans != finalRandomState.HasValue)
                throw new ArgumentException("운석 상태 사건의 경계, 원본 또는 난수 근거가 잘못되었습니다.");
            Step = step;
            Boundary = boundary;
            ExpectedRandomState = expectedRandomState;
            FinalRandomState = finalRandomState;
        }
    }

    public sealed class CombatMeteorImpactEvent : CombatEvent
    {
        public StageGimmickId GimmickId { get; }
        public MeteorFootprint Footprint { get; }
        public bool Cancelled { get; }
        public IReadOnlyList<EntityId> RecipientIds { get; }

        public CombatMeteorImpactEvent(StageGimmickId gimmickId, MeteorFootprint footprint, bool cancelled,
            IReadOnlyList<EntityId> recipientIds) : base(CombatEventKind.MeteorImpact)
        {
            Footprint = footprint ?? throw new ArgumentNullException(nameof(footprint));
            var recipients = (recipientIds ?? throw new ArgumentNullException(nameof(recipientIds))).ToArray();
            if (gimmickId.IsEmpty || cancelled && recipients.Length != 0 || recipients.Any(id => id.IsEmpty) ||
                !recipients.SequenceEqual(recipients.Distinct().OrderBy(id => id)))
                throw new ArgumentException("운석 충돌 수신자 또는 취소 근거가 잘못되었습니다.");
            GimmickId = gimmickId;
            Cancelled = cancelled;
            RecipientIds = Array.AsReadOnly(recipients);
        }
    }
}
