using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.MovePath
{
    internal enum MovementRangeAnswer
    {
        Unrelated,
        Stale,
        Accepted
    }

    /// <summary>이동 범위 요청 당시의 공개 사실을 값으로 보관합니다.</summary>
    internal readonly struct CombatMovementRangeFacts
    {
        internal long BattleInstanceId { get; }
        internal EntityId ActorId { get; }
        internal CellCoord Origin { get; }
        internal long ResultSequence { get; }

        internal long QueryRevision { get; }
        private long TurnSequence { get; }

        internal bool IsInitialized => BattleInstanceId > 0 && !ActorId.IsEmpty && ResultSequence >= 0;

        internal CombatMovementRangeFacts(long battleInstanceId, EntityId actorId, CellCoord origin, long resultSequence, long queryRevision, long turnSequence)
        {
            if (battleInstanceId <= 0)
                throw new ArgumentOutOfRangeException(nameof(battleInstanceId));
            if (actorId.IsEmpty)
                throw new ArgumentException("행위자 ID가 필요합니다.", nameof(actorId));
            if (resultSequence < 0)
                throw new ArgumentOutOfRangeException(nameof(resultSequence));

            BattleInstanceId = battleInstanceId;
            ActorId = actorId;
            Origin = origin;
            ResultSequence = resultSequence;
            QueryRevision = queryRevision;
            TurnSequence = turnSequence;
        }

        internal bool Matches(in CombatMovementRangeFacts other) =>
            BattleInstanceId == other.BattleInstanceId && ActorId == other.ActorId &&
            Origin == other.Origin && ResultSequence == other.ResultSequence && QueryRevision == other.QueryRevision &&
            TurnSequence == other.TurnSequence;
    }

    /// <summary>
    /// 이동 범위 요청의 정체성과 응답 1회 소비만 관리합니다.
    /// Buffer 게시/정리, 입력 허용, 초안 편집, 경로 견적과 실제 표시는 호출자가 담당합니다.
    /// </summary>
    /// <remarks>
    /// <see cref="CombatMovementRangeTracker"/>가 한 개를 소유합니다(경로 입력 분리 설계 RT2).
    /// IsPending 동안 경로 견적 게시만 유예하며 초안을 취소하지 않습니다.
    /// 상태/슬롯이 바뀌면 호출자가 기존 입력 수명에 따라 Cancel하고 표시를 정리해야 합니다.
    /// </remarks>
    internal sealed class CombatMovementRangeRequest
    {
        private CombatMovementRangeFacts _facts;
        private int _requestNumber;
        private long _generation;

        internal bool IsPending => _requestNumber != 0;

        /// <summary>기다리는 요청의 번호입니다. 없으면 0입니다. 자기 슬롯 비우기(번호 있는 비우기)에 넘깁니다.</summary>
        internal int Number => _requestNumber;

        /// <summary>실제 게시된 범위 요청을 기록하고 이번 지정의 응답 수명 토큰을 돌려줍니다.</summary>
        internal long Begin(in CombatMovementRangeFacts facts, in CombatViewPreviewRequest request)
        {
            if (!facts.IsInitialized)
                throw new ArgumentException("이동 범위 요청의 사실이 초기화되지 않았습니다.", nameof(facts));
            if (request.Number <= 0 || request.Data.Kind != CombatViewPreviewKind.MoveRange ||
                request.Data.EntityId != facts.ActorId)
                throw new ArgumentException("행위자와 일치하는 이동 범위 요청이 필요합니다.", nameof(request));

            long generation = checked(_generation + 1);
            _facts = facts;
            _requestNumber = request.Number;
            _generation = generation;
            return generation;
        }

        /// <summary>현재 범위 요청 당시의 전투/행위자/위치/결과 상태와 같은지입니다.</summary>
        internal bool MatchesFacts(in CombatMovementRangeFacts current) =>
            IsPending && current.IsInitialized && _facts.Matches(current);

        /// <summary>
        /// 현재 슬롯을 이 요청이 차지하는지입니다(응답 소비·같은 범위 판단·슬롯 잃음 판단).
        /// 슬롯 비우기는 Buffer의 번호 있는 비우기가 판단합니다(<see cref="Number"/>를 넘김).
        /// </summary>
        internal bool OwnsPreview(bool hasPreview, in CombatViewPreviewRequest currentRequest) =>
            IsPending && hasPreview && currentRequest.Number == _requestNumber &&
            currentRequest.Data.Kind == CombatViewPreviewKind.MoveRange && currentRequest.Data.EntityId == _facts.ActorId;

        /// <summary>
        /// 이번 지정의 현재 범위 응답만 한 번 소비합니다. 실패/빈 범위도 정식 응답이면 소비합니다.
        /// 불일치 응답은 보관 중인 요청을 바꾸지 않으며, 반환값으로 이동 실행을 승인하지 않습니다.
        /// </summary>
        internal MovementRangeAnswer Consume(long generation, in CombatMovementRangeFacts current,
            bool hasPreview, in CombatViewPreviewRequest currentRequest, CombatPreviewResult response,
            out MovementRangeResult range)
        {
            range = default;
            if (generation != _generation || !MatchesFacts(current) || !OwnsPreview(hasPreview, currentRequest) ||
                response == null || response.RequestNumber != _requestNumber || !response.MoveRange.HasValue ||
                response.MoveRange.Value.UnitId != _facts.ActorId)
                return MovementRangeAnswer.Unrelated;

            range = response.MoveRange.Value;
            Cancel();
            return range.SourceRevision > 0 && range.SourceRevision == current.QueryRevision
                ? MovementRangeAnswer.Accepted : MovementRangeAnswer.Stale;
        }

        /// <summary>로컬 요청 수명을 끝냅니다. 다른 입력의 최신 슬롯에는 접근하지 않습니다.</summary>
        internal void Cancel()
        {
            _requestNumber = 0;
            _facts = default;
        }
    }
}
