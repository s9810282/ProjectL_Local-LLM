using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    /// <summary>
    /// 호출마다 불변 원본을 읽고 후보를 평가하여 선택만 확정한다.
    /// Read는 표시용 일관 스냅샷(능력치 포함)을, Evaluate·Confirm은 규칙 판정 원본(<see cref="DeckRuleState"/>)만 돌려준다.
    /// </summary>
    public interface IRunDeckService : IServiceModule
    {
        DeckReadResult Read();
        DeckEvaluationResult Evaluate(DeckEditRequest request);
        DeckEditResult Confirm(DeckEditRequest request);
    }

    internal sealed class RunDeckService : IRunDeckService, ISystemInitData<IDataBaseQueryContext>,
        ISystemPostBindService, IExportSystemServices
    {
        private readonly IDeckEditAvailabilityRead _availability;
        private IInGameQuery _query;
        private IBufferQuery _buffers;

        /// <param name="availability">
        /// Stage 흐름 소유자가 구현하는 편집 가능 시점 읽기다. null은 아직 연결되지 않았다는 뜻이며,
        /// 조회는 허용하되 편집은 <see cref="DeckUnavailableReason.AuthorityUnbound"/>로 막는다.
        /// </param>
        internal RunDeckService(IDeckEditAvailabilityRead availability) => _availability = availability;

        public void InitData(IDataBaseQueryContext data) =>
            _query = data?.InGameQuery ?? throw new ArgumentNullException(nameof(data));

        public void PostBindService(ISystemServiceStoreReader resolver) =>
            _buffers = resolver.GetService(BufferSystemKey.Buffer);

        public DeckReadResult Read()
        {
            var status = TryCapture(out var capture);
            if (status != DeckReadStatus.Ready) return new DeckReadResult(status);

            // 표시용 능력치는 같은 원본에서 계산한다. 영혼·메인 지속 상태는 로스터 버전이 덮으므로 끝에 원본을 다시 대조한다.
            // 능력치 설정 오류는 표시 경로에서 그대로 드러나며 규칙 판정(Evaluate·Confirm)에는 쓰이지 않는다.
            IReadOnlyList<OwnedCharacterStatsSnapshot> stats;
            try
            {
                OwnedStatsProjection.ReadPersistent(_buffers.CharacterReadSink, capture.Owned, out var souls, out var mains);
                stats = OwnedStatsProjection.Calculate(_query, capture.Owned, mains, souls);
            }
            catch when (!capture.IsCurrent(_buffers))
            {
                return new DeckReadResult(DeckReadStatus.StateChanged);
            }
            if (!capture.IsCurrent(_buffers)) return new DeckReadResult(DeckReadStatus.StateChanged);
            return new DeckReadResult(DeckReadStatus.Ready, new DeckEditorSnapshot(capture.Source.RosterRevision,
                capture.Source.Deck, capture.Owned, capture.Definitions, stats, capture.Availability));
        }

        public DeckEvaluationResult Evaluate(DeckEditRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var status = TryCapture(out var capture);
            var current = ToRuleState(status, capture);
            var reason = CheckRequest(request, status, capture);
            if (reason != DeckFailureReason.None) return new DeckEvaluationResult(request, reason, current);
            var decision = DeckRules.Evaluate(request.CandidateIds, capture.Owned, capture.Definitions);
            return new DeckEvaluationResult(request, decision.Reason, current, decision.CanonicalIds);
        }

        public DeckEditResult Confirm(DeckEditRequest request)
        {
            var evaluation = Evaluate(request);
            if (!evaluation.Accepted) return new DeckEditResult(request, evaluation.Reason, evaluation.Current);

            // 이 외부 읽기 뒤에는 버전 재검증과 콜백 없는 게시 꼬리만 실행한다.
            var availability = ReadAvailability();
            if (!availability.CanEdit) return new DeckEditResult(request, DeckFailureReason.WrongPhase, CaptureRuleState());
            var reason = _buffers.RunDeckWriteSink.TryPublish(request, evaluation.CanonicalIds);
            if (reason != DeckFailureReason.None) return new DeckEditResult(request, reason, CaptureRuleState());

            // 게시 뒤에는 DB·Flow·능력치를 다시 읽지 않는다. Buffer에 실제로 설치된 덱과 게시가 검증한 로스터 버전을 돌려준다.
            // 같은 덱(no-op)이면 Buffer가 기존 인스턴스를 유지하므로 같은 덱 버전이 된다.
            _buffers.RunDeckReadSink.TryGetCurrent(out var installed);
            return new DeckEditResult(request, DeckFailureReason.None,
                DeckRuleState.Ready(evaluation.Current.RosterRevision, installed, availability));
        }

        /// <summary>규칙 판정에 필요한 원본만 같은 시점에 포착한다. 능력치 계산은 하지 않는다.</summary>
        private DeckReadStatus TryCapture(out RuleCapture capture)
        {
            capture = null;
            if (_query == null || _buffers == null) return DeckReadStatus.NotReady;
            var source = RosterSourceVersion.Capture(_buffers);
            if (source.RunId.IsEmpty || source.Deck == null) return DeckReadStatus.NotReady;
            if (source.Deck.RunId != source.RunId) return DeckReadStatus.StateChanged;
            var outcome = _buffers.RunProgressReadSink.Outcome;
            bool battle = _buffers.CombatStateReadSink.HasBattle;
            var owned = _buffers.CharacterReadSink.Owned.ToArray();
            var pending = new RuleCapture(source, outcome, battle, owned);
            try
            {
                // 정의는 획득 규칙과 같은 단일 어댑터에서 읽는다(병과·초기 메인 포함).
                var definitions = CharacterDefinitionAdapter.ReadAll(_query.UnitTableQuery, owned);
                var availability = ReadAvailability();
                if (!pending.IsCurrent(_buffers)) return DeckReadStatus.StateChanged;
                var canonical = DeckRules.Evaluate(source.Deck.OrderedIds, owned, definitions);
                if (!canonical.Accepted || !canonical.CanonicalIds.SequenceEqual(source.Deck.OrderedIds))
                    throw new InvalidOperationException("설치된 덱은 적법한 정규 순서여야 합니다.");
                pending.Complete(definitions, availability);
            }
            catch when (!pending.IsCurrent(_buffers))
            {
                // 외부 정의·흐름 조회의 재진입으로 원본이 달라졌다면 섞인 사실을 반환하지 않는다.
                return DeckReadStatus.StateChanged;
            }
            capture = pending;
            return DeckReadStatus.Ready;
        }

        private DeckRuleState CaptureRuleState() => ToRuleState(TryCapture(out var capture), capture);

        private static DeckRuleState ToRuleState(DeckReadStatus status, RuleCapture capture) =>
            status == DeckReadStatus.Ready
                ? DeckRuleState.Ready(capture.Source.RosterRevision, capture.Source.Deck, capture.Availability)
                : DeckRuleState.Unavailable(status);

        private DeckEditAvailability ReadAvailability()
        {
            // Buffer가 확정하는 막는 사실을 먼저 본다. 그 밖의 Stage 상호작용 시점은 흐름 소유자의 판단이다.
            if (_buffers.CombatStateReadSink.HasBattle)
                return new DeckEditAvailability(DeckUnavailableReason.BattleRetained);
            if (_buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress)
                return new DeckEditAvailability(DeckUnavailableReason.RunEnded);
            if (_availability == null)
                return new DeckEditAvailability(DeckUnavailableReason.AuthorityUnbound);
            return _availability.ReadDeckEditAvailability()
                ?? throw new InvalidOperationException("설정된 덱 편집 가능 시점 읽기는 결과를 반환해야 합니다.");
        }

        private static DeckFailureReason CheckRequest(DeckEditRequest request, DeckReadStatus status, RuleCapture capture)
        {
            if (status == DeckReadStatus.NotReady) return DeckFailureReason.NotReady;
            if (status == DeckReadStatus.StateChanged) return DeckFailureReason.StateChanged;
            if (request.ExpectedRunId != capture.Source.RunId) return DeckFailureReason.StaleRun;
            if (request.ExpectedRosterRevision != capture.Source.RosterRevision) return DeckFailureReason.StaleRoster;
            if (request.ExpectedDeckRevision != capture.Source.Deck.DeckRevision) return DeckFailureReason.StaleDeck;
            return capture.Availability.CanEdit ? DeckFailureReason.None : DeckFailureReason.WrongPhase;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRunDeckService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        /// <summary>한 호출 안에서만 쓰는 규칙 판정 원본이다. 저장하지 않으며 공개 계약이 아니다.</summary>
        private sealed class RuleCapture
        {
            internal RosterSourceVersion Source { get; }
            internal IReadOnlyList<OwnedCharacterRecord> Owned { get; }
            internal IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> Definitions { get; private set; }
            internal DeckEditAvailability Availability { get; private set; }
            private readonly RunOutcome _outcome;
            private readonly bool _battle;

            internal RuleCapture(RosterSourceVersion source, RunOutcome outcome, bool battle, IReadOnlyList<OwnedCharacterRecord> owned)
            {
                Source = source;
                _outcome = outcome;
                _battle = battle;
                Owned = owned;
            }

            internal void Complete(IReadOnlyDictionary<EntityKey, CharacterDefinitionFacts> definitions, DeckEditAvailability availability)
            {
                Definitions = definitions;
                Availability = availability;
            }

            /// <summary>전투 유무·Run 결과는 로스터 버전이 덮지 않으므로 이 판정에서 따로 비교한다.</summary>
            internal bool IsCurrent(IBufferQuery buffers) => Source.IsCurrent(buffers) &&
                buffers.RunProgressReadSink.Outcome == _outcome && buffers.CombatStateReadSink.HasBattle == _battle;
        }
    }
}
