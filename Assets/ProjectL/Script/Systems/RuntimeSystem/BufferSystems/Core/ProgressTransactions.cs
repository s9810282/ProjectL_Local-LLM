using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Fame;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>
    /// Run 초기화와 챕터 진행(진입·계속·비전투 노드 완료)을 게시한다.
    /// 진행 가능 판정은 Core가 하고, 여기서는 판정에 쓴 위치·챕터·그래프 원본이 그대로인지만 본다.
    /// </summary>
    internal sealed class ProgressTransactions :
        IRunProgressBufferWriteSink, IChapterContinuationBufferWriteSink, IChapterProgressBufferWriteSink
    {
        private readonly ITraitGrowthTransaction _growth;
        private readonly IItemInventoryStore _items;
        private readonly BufferGuards _guards;
        private readonly IBattleBufferWriteSink _battle;
        private readonly ICurrentProgressBufferReadSink _currentRead;
        private readonly ICurrentProgressBufferWriteSink _currentWrite;
        private readonly IMapProgressBufferReadSink _mapRead;
        private readonly IMapProgressBufferWriteSink _mapWrite;
        private readonly IStageProgressBufferReadSink _stageRead;
        private readonly IStageProgressBufferWriteSink _stageWrite;
        private readonly ICombatStateBufferReadSink _combatRead;
        private readonly ICurrencyBufferWriteSink _currencyWrite;
        private readonly ICurrencyBufferReadSink _currencyRead;
        private readonly IFameBufferReadSink _fameRead;
        private readonly IFameBufferStore _fameStore;
        private readonly ICharacterBufferStore _characterStore;
        private readonly ICurrentProgressBufferStore _currentStore;
        private readonly IStageProgressBufferStore _stageStore;
        private readonly IRunProgressBufferStore _runStore;
        private readonly IRunDeckStore _deckStore;
        private readonly ICombatStateBufferStore _combatStore;

        internal ProgressTransactions(BufferGuards guards, IBattleBufferWriteSink battle,
            ICurrentProgressBufferReadSink currentRead, ICurrentProgressBufferWriteSink currentWrite,
            IMapProgressBufferReadSink mapRead, IMapProgressBufferWriteSink mapWrite,
            IStageProgressBufferReadSink stageRead, IStageProgressBufferWriteSink stageWrite,
            ICombatStateBufferReadSink combatRead, ICurrencyBufferReadSink currencyRead, ICurrencyBufferWriteSink currencyWrite,
            IFameBufferReadSink fameRead, IFameBufferStore fameStore,
            ICharacterBufferStore characterStore, ICurrentProgressBufferStore currentStore,
            IStageProgressBufferStore stageStore, IRunProgressBufferStore runStore, ICombatStateBufferStore combatStore, IItemInventoryStore items, IRunDeckStore deckStore, ITraitGrowthTransaction growth)
        {
            _growth = growth ?? throw new System.ArgumentNullException(nameof(growth));
            _currencyRead = currencyRead ?? throw new System.ArgumentNullException(nameof(currencyRead));
            _fameRead = fameRead ?? throw new System.ArgumentNullException(nameof(fameRead));
            _fameStore = fameStore ?? throw new System.ArgumentNullException(nameof(fameStore));
            _deckStore = deckStore ?? throw new System.ArgumentNullException(nameof(deckStore));
            _items = items ?? throw new System.ArgumentNullException(nameof(items));
            _guards = guards ?? throw new System.ArgumentNullException(nameof(guards));
            _battle = battle ?? throw new System.ArgumentNullException(nameof(battle));
            _currentRead = currentRead ?? throw new System.ArgumentNullException(nameof(currentRead));
            _currentWrite = currentWrite ?? throw new System.ArgumentNullException(nameof(currentWrite));
            _mapRead = mapRead ?? throw new System.ArgumentNullException(nameof(mapRead));
            _mapWrite = mapWrite ?? throw new System.ArgumentNullException(nameof(mapWrite));
            _stageRead = stageRead ?? throw new System.ArgumentNullException(nameof(stageRead));
            _stageWrite = stageWrite ?? throw new System.ArgumentNullException(nameof(stageWrite));
            _combatRead = combatRead ?? throw new System.ArgumentNullException(nameof(combatRead));
            _currencyWrite = currencyWrite ?? throw new System.ArgumentNullException(nameof(currencyWrite));
            _characterStore = characterStore ?? throw new System.ArgumentNullException(nameof(characterStore));
            _currentStore = currentStore ?? throw new System.ArgumentNullException(nameof(currentStore));
            _stageStore = stageStore ?? throw new System.ArgumentNullException(nameof(stageStore));
            _runStore = runStore ?? throw new System.ArgumentNullException(nameof(runStore));
            _combatStore = combatStore ?? throw new System.ArgumentNullException(nameof(combatStore));
        }

        public void PublishContinuation(ChapterContinuationPreparation preparation)
        {
            if (preparation == null)
                throw new System.ArgumentNullException(nameof(preparation));
            _guards.EnsureRunInProgress();
            var decision = preparation.Decision;
            _guards.ValidateSelection(decision.ChapterKey, decision.NodeId, decision.StageKey);
            var selected = _currentRead.CurrentStage;
            var expected = preparation.ExpectedSelection;
            // 계속 가능 여부(클리어·승리 일치·대상 미완료)는 Core가 판정한다. 여기서는 판정에 쓴 원본이 그대로인지와 참조 존재만 본다.
            if (_combatRead.HasBattle || selected.NodeId != expected.NodeId || selected.StageKey != expected.StageKey ||
                selected.StageName != expected.StageName ||
                _mapRead.GetMap(decision.ChapterKey).IsClear != preparation.ExpectedSourceMapClear ||
                !_stageRead.GetStages(decision.ChapterKey).Any(node => node.NodeId == decision.NodeId.Value &&
                    node.StageKey == decision.StageKey.Value) ||
                !ReferenceEquals(_combatRead.Completion, preparation.ExpectedCompletion) ||
                !ChapterContinuationPreparation.SameGraph(preparation.ExpectedSourceStages, _stageRead.GetStages(decision.ChapterKey)))
                throw new System.InvalidOperationException("챕터 완료 제안의 원본이 현재 진행과 다릅니다.");

            if (decision.RunCompletion != null)
            {
                var receipt = _runStore.PrepareCompletion(decision.RunCompletion);
                _runStore.InstallCompletion(receipt, _currencyRead.RunSpent, _fameRead.RunEarned);
                _growth.Clear();
                return;
            }

            if (!_mapRead.Maps.Any(map => map.MapKey == decision.NextChapterKey && map.IsClear == preparation.ExpectedTargetMapClear) ||
                !ChapterContinuationPreparation.SameGraph(preparation.ExpectedTargetStages, _stageRead.GetStages(decision.NextChapterKey)))
                throw new System.InvalidOperationException("다음 챕터의 등록 또는 원본 그래프가 변경되었습니다.");
            InstallChapter(decision.NextChapterKey, preparation.GeneratedTargetStages);
        }

        public void PublishChapterEntry(ChapterEntryPreparation preparation)
        {
            if (preparation == null)
                throw new System.ArgumentNullException(nameof(preparation));
            _guards.EnsureRunInProgress();
            // 진입 가능 판정은 Core가 한다. 여기서는 판정에 쓴 위치·챕터 상태·그래프가 그대로인지만 본다.
            int current = preparation.ExpectedCurrentChapterKey;
            if (_currentRead.CurrentMapKey != current ||
                current > 0 && !_mapRead.Maps.Any(map => map.MapKey == current && map.IsClear == preparation.ExpectedCurrentChapterClear) ||
                !_mapRead.Maps.Any(map => map.MapKey == preparation.TargetChapterKey && map.IsClear == preparation.ExpectedTargetChapterClear) ||
                !ChapterContinuationPreparation.SameGraph(preparation.ExpectedTargetStages, _stageRead.GetStages(preparation.TargetChapterKey)))
                throw new System.InvalidOperationException("챕터 진입 제안의 원본이 현재 진행과 다릅니다.");
            InstallChapter(preparation.TargetChapterKey, preparation.GeneratedTargetStages);
        }

        /// <summary>새 그래프가 있으면 등록을 준비하고, 위치 준비까지 마친 뒤 둘을 함께 설치한다.</summary>
        private void InstallChapter(int chapterKey, IReadOnlyList<StageProgressData> generatedStages)
        {
            var registration = generatedStages.Count == 0 ? null
                : _stageStore.PrepareRegistration(chapterKey, generatedStages);
            var selection = _currentStore.PrepareMapSelection(chapterKey);
            if (registration != null)
                _stageStore.InstallRegistration(registration);
            _currentStore.InstallMapSelection(selection);
        }

        public void PublishStageClear(int chapterKey, StageNodeId nodeId, StageKey stageKey)
        {
            _stageStore.InstallClear(PrepareStageClear(chapterKey, nodeId, stageKey));
        }

        internal PreparedStageClear PrepareStageClear(int chapterKey, StageNodeId nodeId, StageKey stageKey)
        {
            _guards.EnsureRunInProgress();
            // 완료 가능 판정은 Core가 한다. 여기서는 현재 선택이 같은 노드이고 아직 완료 기록이 없는지만 본다.
            _guards.ValidateSelection(chapterKey, nodeId, stageKey);
            if (!_stageRead.GetStages(chapterKey).Any(node =>
                    node.NodeId == nodeId.Value && node.StageKey == stageKey.Value && !node.IsClear))
                throw new System.InvalidOperationException("완료할 노드가 없거나 이미 완료되었습니다.");
            return _stageStore.PrepareClear(chapterKey, nodeId.Value, true);
        }

        public RosterAcquireResult PublishNewRun(NewRunPreparation preparation)
        {
            if (preparation == null)
                throw new System.ArgumentNullException(nameof(preparation));
            if (_runStore.RunId == preparation.RunId)
                return RosterAcquireResult.Failure(RosterAcquireFailureReason.InvalidRequest);
            // 실패할 수 있는 준비를 모두 먼저 끝낸다. 이후 초기화와 설치는 실패하지 않는다.
            PreparedRosterAcquisition roster = _characterStore.PrepareInitialRoster(preparation.Roster);
            if (!roster.Result.Succeeded)
                return roster.Result;

            // Core가 덱 규칙으로 정규화한 grant index를 발급 예정 ID로 연결하고, 형식·증가분을 첫 쓰기 전에 준비한다.
            // 여기서는 index가 이번 지급을 가리키고 저장할 메인 ID(0번)가 메인 지급인지만 확인한다.
            var ids = new List<CharacterInstanceId>();
            var seen = new HashSet<int>();
            foreach (int index in preparation.InitialDeckGrantIndices)
            {
                if (index < 0 || index >= roster.Result.Acquired.Count || !seen.Add(index))
                    return RosterAcquireResult.Failure(RosterAcquireFailureReason.InvalidRequest);
                ids.Add(roster.Result.Acquired[index].Id);
            }
            if (ids.Count == 0 ||
                roster.Result.Acquired[preparation.InitialDeckGrantIndices[0]].Role != CharacterOwnershipRole.Main)
                return RosterAcquireResult.Failure(RosterAcquireFailureReason.InvalidRequest);
            var deck = _deckStore.Prepare(preparation.RunId, ids);
            ResetRunProgress(clearRoster: false);
            _characterStore.InstallAcquire(roster);
            _runStore.InstallRunId(preparation.RunId);
            _deckStore.Install(deck);
            _currencyWrite.SetAmount(preparation.StartingRunCurrency);
            _runStore.InstallStartingHandSize(preparation.StartingHandSize);
            return roster.Result;
        }

        public void ResetRunProgress() => ResetRunProgress(clearRoster: true);

        private void ResetRunProgress(bool clearRoster)
        {
            long revision = clearRoster ? _characterStore.PrepareClearRoster() : 0;
            // 완료 값을 바꾸는 동안 맵 목록을 열거하지 않도록 키만 복사한다.
            var mapKeys = new List<int>(_mapRead.Maps.Count);
            foreach (var map in _mapRead.Maps)
                mapKeys.Add(map.MapKey);

            _growth.Clear();
            _battle.EndBattle();
            _combatStore.ClearCompletion();

            _currentWrite.Clear();
            _stageWrite.Clear();
            // 맵 등록은 DB 초기화에서 한 번만 수행하므로 Clear로 제거하지 않는다.
            foreach (int mapKey in mapKeys)
                _mapWrite.SetClear(mapKey, false);
            if (clearRoster)
                _characterStore.ClearRoster(revision);
            _deckStore.Reset();
            _currencyWrite.Clear();
            // 명성 잔액은 Run을 넘어 유지하고, 이번 Run 획득 합계만 0으로 돌린다(Run 요약 설계 §10.6).
            _fameStore.ResetRunEarned();
            _runStore.Reset();
            _items.Reset();
            // Artifact 보유 기록은 Run 초기화 범위에 포함하지 않는다.
        }
    }
}
