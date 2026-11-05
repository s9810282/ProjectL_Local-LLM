using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 대상 검증, 진행 판단, 초기 정의 조회, 준비 계산, 진입 재검증을 맡는다.
    /// 검증된 준비·선택·정의는 <see cref="BattleLifetime"/>에 넘기며 포기·종료 정책과 정의 해제 절차는 모른다.
    /// </summary>
    internal sealed class BattleEntryService : IBattleEntryService,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private readonly IBattleInitializationSource _source;
        private readonly ICharacterRosterService _roster;
        private readonly IChapterProgressionCoordinator _progression;
        private readonly BattleLifetime _lifetime;
        private IDataBaseQueryContext _database;
        private ICharacterBufferReadSink _characters;
        private ICurrentProgressBufferReadSink _current;
        private ICombatStateBufferReadSink _combat;
        private IRunProgressBufferReadSink _run;
        private IRunDeckReadSink _deck;

        /// <param name="source">없으면 진입 때 상태 오류다(기존 계약).</param>
        /// <param name="progression">없으면 진입 때 상태 오류다(기존 계약).</param>
        internal BattleEntryService(IBattleInitializationSource source, ICharacterRosterService roster,
            IChapterProgressionCoordinator progression, BattleLifetime lifetime)
        {
            _source = source;
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
            _progression = progression;
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
        }

        public void InitData(IDataBaseQueryContext data)
        {
            _database = data ?? throw new ArgumentNullException(nameof(data));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _characters = buffers.CharacterReadSink;
            _current = buffers.CurrentProgressReadSink;
            _combat = buffers.CombatStateReadSink;
            _run = buffers.RunProgressReadSink;
            _deck = buffers.RunDeckReadSink;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IBattleEntryService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public ChapterProgressionResult EnterBattle(int chapterKey, StageNodeId targetNodeId)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (targetNodeId.IsEmpty) throw new ArgumentException("진입할 노드 ID가 필요합니다.", nameof(targetNodeId));
            EnsureDependencies();
            if (RunEnded) return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded);
            if (_combat.HasBattle)
                throw new InvalidOperationException("이미 준비된 전투가 있습니다.");
            if (_progression == null) throw new InvalidOperationException("진입 판단 서비스가 필요합니다.");
            // 로스터가 없으면 전투를 준비할 수 없다. 정의를 읽거나 선택을 바꾸기 전에 거절한다.
            if (!_deck.TryGetCurrent(out var deck) || !_characters.TryGetMainState(deck.MainId, out _))
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RosterNotInitialized);

            // 보유 기록·영혼·메인 지속 상태는 로스터 버전이, 선택은 덱 인스턴스가 덮는다. 이 묶음 하나로 외부 조회 뒤 원본을 대조한다.
            var source = RosterSourceVersion.Capture(_run, _characters, _deck);
            long revision = source.RosterRevision;
            if (deck.RunId != source.RunId) return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RosterNotInitialized);
            var owned = deck.OrderedIds.Select(id => _characters.TryGetOwned(id, out var record) ? record :
                throw new InvalidOperationException("선택한 보유 기록이 없습니다.")).ToArray();
            _characters.TryGetMainState(deck.MainId, out var main);
            var souls = owned.ToDictionary(o => o.Id, o => _characters.GetSoulCount(o.Id));
            int originChapter = _current.CurrentMapKey;
            bool originSelected = _current.HasSelection;
            StageSelectionData originStage = _current.CurrentStage;
            ChapterProgressionResult decision = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!decision.Accepted) return decision;

            ValidateCombatTarget(decision);
            BattleInitialization initialization = _source.Read(_database, decision.ChapterKey, decision.StageKey)
                ?? throw new InvalidOperationException("전투 정의 입력이 필요합니다.");
            BattlePreparation preparation = BattlePreparationCalculator.Prepare(initialization, owned, main, souls,
                _roster.PlanInitialDraw(initialization.Stage.TimelineSeed),
                _characters.Characters.Select(c => c.ToFacts()).ToArray(), deck, revision);

            // 정의 조회와 계산이 끝난 뒤에도 현재 원본과 합법적인 대상이 같은지 확인한다.
            ChapterProgressionResult revalidated = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!revalidated.Accepted) return revalidated;
            if (_current.CurrentMapKey != originChapter || _current.HasSelection != originSelected ||
                _current.CurrentStage.NodeId != originStage.NodeId || _current.CurrentStage.StageKey != originStage.StageKey ||
                _current.CurrentStage.StageName != originStage.StageName ||
                revalidated.ChapterKey != decision.ChapterKey || revalidated.NodeId != decision.NodeId ||
                revalidated.StageKey != decision.StageKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);

            if (!source.IsCurrent(_run, _characters, _deck))
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            var selection = new BattleEntrySelection(originChapter, originSelected, originStage,
                revalidated.NodeId, revalidated.StageKey, $"Stage {revalidated.StageKey.Value}");
            _lifetime.Install(preparation, selection, initialization);
            return revalidated;
        }

        private void ValidateCombatTarget(ChapterProgressionResult decision)
        {
            var root = _database.InGameQuery.StageTableQuery.GetStage(decision.StageKey.Value)
                ?? throw new InvalidOperationException("진입 대상의 스테이지 정의가 필요합니다.");
            root.Validate();
            if (root.StageKey != decision.StageKey.Value || root.MapKey != decision.ChapterKey ||
                root.StageData is not CombatStageDetail)
                throw new InvalidOperationException("진입 대상은 해당 챕터의 전투 스테이지여야 합니다.");
        }

        private void EnsureDependencies()
        {
            if (_database == null || _characters == null)
                throw new InvalidOperationException("전투 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
            if (_source == null)
                throw new InvalidOperationException("전투 초기화 입력 경로(G7)가 연결되지 않았습니다.");
        }

        private bool RunEnded => _run.Outcome != RunOutcome.InProgress;
    }
}
