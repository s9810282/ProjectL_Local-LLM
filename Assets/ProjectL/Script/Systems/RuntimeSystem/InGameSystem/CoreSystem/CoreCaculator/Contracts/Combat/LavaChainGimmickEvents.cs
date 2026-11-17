using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum LavaChainGimmickStep { Warning, Retracted }
    public enum LavaChainPushFailure { None, Immune, NoLanding }

    public sealed class CombatLavaChainEvent : CombatEvent, IEnvironmentRandomEvidence, IStageGimmickStateChange
    {
        public LavaChainGimmickStep Step { get; }
        public StageGimmickBoundary Boundary { get; }
        public LavaChainGimmickState Expected { get; }
        public LavaChainGimmickState Final { get; }
        public IReadOnlyList<CellCoord> HeadPath { get; }
        public uint? ExpectedRandomState { get; }
        public uint? FinalRandomState { get; }
        StageGimmickState IStageGimmickStateChange.FinalGimmick => Final;

        public CombatLavaChainEvent(LavaChainGimmickStep step, StageGimmickBoundary boundary,
            LavaChainGimmickState expected, LavaChainGimmickState final, IReadOnlyList<CellCoord> headPath,
            uint? expectedRandomState = null, uint? finalRandomState = null) : base(CombatEventKind.LavaChainAdvanced)
        {
            Expected = expected ?? throw new ArgumentNullException(nameof(expected));
            Final = final ?? throw new ArgumentNullException(nameof(final));
            var path = (headPath ?? throw new ArgumentNullException(nameof(headPath))).ToArray();
            bool warning = step == LavaChainGimmickStep.Warning;
            if (!Enum.IsDefined(typeof(LavaChainGimmickStep), step) || expected.Id != final.Id || path.Length == 0 ||
                path[0] != expected.HeadCell || path[path.Length - 1] != final.HeadCell ||
                warning != expectedRandomState.HasValue || warning != finalRandomState.HasValue ||
                boundary.Phase != (warning ? StageGimmickBoundaryPhase.Warning : StageGimmickBoundaryPhase.Scheduled))
                throw new ArgumentException("사슬 사건의 경계·경로·난수 근거가 올바르지 않습니다.");
            Step = step;
            Boundary = boundary;
            HeadPath = Array.AsReadOnly(path);
            ExpectedRandomState = expectedRandomState;
            FinalRandomState = finalRandomState;
        }
    }

    public sealed class CombatLavaChainContactEvent : CombatEvent
    {
        public StageGimmickId GimmickId { get; }
        public int PathIndex { get; }
        public CellCoord ContactCell { get; }
        public EntityId RecipientId { get; }
        public CellCoord From { get; }
        public CellCoord? LandingCell { get; }
        public CellCoord? FinalCell { get; }
        public LavaChainPushFailure Failure { get; }
        public bool Survived { get; }

        public CombatLavaChainContactEvent(StageGimmickId gimmickId, int pathIndex, CellCoord contactCell,
            EntityId recipientId, CellCoord from, CellCoord? landingCell, CellCoord? finalCell,
            LavaChainPushFailure failure, bool survived) : base(CombatEventKind.LavaChainContact)
        {
            if (gimmickId.IsEmpty || pathIndex < 1 || recipientId.IsEmpty ||
                !Enum.IsDefined(typeof(LavaChainPushFailure), failure) || (failure == LavaChainPushFailure.None) != landingCell.HasValue)
                throw new ArgumentException("사슬 접촉 결과가 올바르지 않습니다.");
            GimmickId = gimmickId;
            PathIndex = pathIndex;
            ContactCell = contactCell;
            RecipientId = recipientId;
            From = from;
            LandingCell = landingCell;
            FinalCell = finalCell;
            Failure = failure;
            Survived = survived;
        }
    }
}
