using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Chapter;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem
{
    public interface IChapterProgressionCoordinator : IServiceModule
    {
        RunOutcome Outcome { get; }
        RunCompletionReceipt Completion { get; }
        ChapterContinuationResult CompleteChapter(int chapterKey, StageNodeId bossNodeId);
        ChapterProgressionResult EnterChapter(int chapterKey);
        /// <summary>진입 규칙과 데이터를 확인한다. 현재 위치는 기록하지 않는다.</summary>
        ChapterProgressionResult PrepareStageEntry(int chapterKey, StageNodeId targetNodeId);
        /// <summary>실제 진입 준비를 완료한 진행 Job이 호출하여 현재 위치를 기록한다.</summary>
        ChapterProgressionResult ConfirmStageEntry(int chapterKey, StageNodeId targetNodeId);
        ChapterProgressionResult ResolveStage(int chapterKey, StageOutcome outcome);
        /// <summary>전투의 합동 게시에 사용할 진행 판단만 반환한다.</summary>
        ChapterProgressionResult PlanStageResolution(int chapterKey, StageOutcome outcome);
    }

    internal sealed class ChapterProgressionCoordinator :
        IChapterProgressionCoordinator, ISystemPostBindService, IExportSystemServices
    {
        private readonly IStageGeneratorSystem _generator;
        private readonly StageServiceManager _stageService;
        private readonly IChapterContinuationSource _continuationSource;
        private ICurrentProgressBufferReadSink _currentRead;
        private ICurrentProgressBufferWriteSink _currentWrite;
        private IStageProgressBufferReadSink _stageRead;
        private IStageProgressBufferWriteSink _stageWrite;
        private IMapProgressBufferReadSink _mapRead;
        private IMapProgressBufferWriteSink _mapWrite;
        private IRunProgressBufferReadSink _runRead;
        private IBufferQuery _buffers;
        public RunOutcome Outcome { get { EnsureBound(); return _runRead.Outcome; } }
        public RunCompletionReceipt Completion { get { EnsureBound(); return _runRead.Completion; } }

        internal ChapterProgressionCoordinator(
            IStageGeneratorSystem generator,
            StageServiceManager stageService, IChapterContinuationSource continuationSource = null)
        {
            _generator = generator ?? throw new ArgumentNullException(nameof(generator));
            _stageService = stageService ?? throw new ArgumentNullException(nameof(stageService));
            _continuationSource = continuationSource ?? new UnavailableChapterContinuationSource();
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            _buffers = buffers;
            _runRead = buffers.RunProgressReadSink;
            _currentRead = buffers.CurrentProgressReadSink;
            _currentWrite = buffers.CurrentProgressWriteSink;
            _stageRead = buffers.StageProgressReadSink;
            _stageWrite = buffers.StageProgressWriteSink;
            _mapRead = buffers.MapProgressReadSink;
            _mapWrite = buffers.MapProgressWriteSink;
        }

        public ChapterContinuationResult CompleteChapter(int chapterKey, StageNodeId bossNodeId)
            => ContinueChapter(chapterKey, bossNodeId, 0);

        private ChapterContinuationResult ContinueChapter(int chapterKey, StageNodeId bossNodeId, int expectedTarget)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (bossNodeId.IsEmpty) throw new ArgumentException("완료할 보스 노드가 필요합니다.", nameof(bossNodeId));
            EnsureBound();
            var admission = ContinuationAdmission(chapterKey, bossNodeId);
            if (admission != ChapterContinuationFailureReason.None) return ChapterContinuationResult.Reject(admission);

            var selected = _currentRead.CurrentStage;
            var completion = _buffers.CombatStateReadSink.Completion;
            // 경로 조회가 아직 모르는 대상 챕터를 바꿔도 감지할 수 있도록 값만 복사한다.
            var maps = _mapRead.Maps.ToDictionary(map => map.MapKey, map => map.IsClear);
            var graphs = maps.Keys.ToDictionary(key => key, key => _stageRead.GetStages(key).ToArray());
            var source = graphs[chapterKey];
            var bossFailure = ValidateBossDefinition(chapterKey, bossNodeId, selected, source);
            if (bossFailure != ChapterContinuationFailureReason.None) return ChapterContinuationResult.Reject(bossFailure);
            var liveFailure = ValidateContinuationOrigin(chapterKey, bossNodeId, selected, completion, source);
            if (liveFailure != ChapterContinuationFailureReason.None) return ChapterContinuationResult.Reject(liveFailure);

            ChapterContinuationDefinition definition;
            try { definition = _continuationSource.Read(chapterKey); }
            catch (NotSupportedException) { definition = null; }
            liveFailure = ValidateContinuationOrigin(chapterKey, bossNodeId, selected, completion, source);
            if (liveFailure != ChapterContinuationFailureReason.None) return ChapterContinuationResult.Reject(liveFailure);
            bool next = definition?.Kind == ChapterContinuationDefinitionKind.NextChapter;
            var decision = EvaluateContinuation(chapterKey, bossNodeId, selected, definition, maps);
            if (!decision.Accepted) return decision;
            if (expectedTarget > 0 && (!next || decision.NextChapterKey != expectedTarget))
                return ChapterContinuationResult.Reject(next ? ChapterContinuationFailureReason.ChapterMismatch : ChapterContinuationFailureReason.InvalidDefinition);

            IReadOnlyList<StageProgressData> expectedGraph = Array.Empty<StageProgressData>();
            IReadOnlyList<StageProgressData> generated = Array.Empty<StageProgressData>();
            if (next)
            {
                expectedGraph = graphs[decision.NextChapterKey];
                var targetFailure = PrepareTargetGraph(decision.NextChapterKey, expectedGraph, out generated);
                if (targetFailure != ChapterContinuationFailureReason.None) return ChapterContinuationResult.Reject(targetFailure);
            }

            liveFailure = ValidateContinuationOrigin(chapterKey, bossNodeId, selected, completion, source);
            if (liveFailure != ChapterContinuationFailureReason.None) return ChapterContinuationResult.Reject(liveFailure);
            bool targetClear = false;
            if (next)
            {
                var target = _mapRead.Maps.FirstOrDefault(map => map.MapKey == decision.NextChapterKey);
                if (target.MapKey == 0) return ChapterContinuationResult.Reject(ChapterContinuationFailureReason.TargetNotRegistered);
                if (target.IsClear) return ChapterContinuationResult.Reject(ChapterContinuationFailureReason.TargetAlreadyCleared);
                if (!ChapterContinuationPreparation.SameGraph(expectedGraph, _stageRead.GetStages(decision.NextChapterKey)))
                    return ChapterContinuationResult.Reject(ChapterContinuationFailureReason.InvalidDefinition);
                targetClear = target.IsClear;
            }
            // 판정에 쓴 챕터 완료 상태를 함께 넘겨 Buffer가 규칙 없이 원본 일치만 확인하게 한다.
            _buffers.ChapterContinuationWriteSink.PublishContinuation(new ChapterContinuationPreparation(decision, selected,
                completion, source, expectedGraph, generated, _mapRead.GetMap(chapterKey).IsClear, targetClear));
            return decision;
        }

        private ChapterContinuationFailureReason ValidateBossDefinition(int chapterKey, StageNodeId bossNodeId,
            StageSelectionData selected, IReadOnlyList<StageProgressData> source)
        {
            var sourceNode = source.Single(node => node.NodeId == bossNodeId.Value && node.StageKey == selected.StageKey);
            var root = _stageService.GetStage(selected.StageKey);
            if (root == null || root.StageKey != selected.StageKey || root.MapKey != chapterKey)
                return ChapterContinuationFailureReason.InvalidDefinition;
            if (root.StageData is not CombatStageDetail || root.MapRole != StageMapRole.Boss || sourceNode.NextNodeIds.Count != 0)
                return ChapterContinuationFailureReason.InvalidBoss;
            return ChapterContinuationFailureReason.None;
        }

        private ChapterContinuationResult EvaluateContinuation(int chapterKey, StageNodeId bossNodeId,
            StageSelectionData selected, ChapterContinuationDefinition definition, IReadOnlyDictionary<int, bool> maps)
        {
            bool next = definition?.Kind == ChapterContinuationDefinitionKind.NextChapter;
            // 원본 재검증을 통과한 사실과 조회 전 대상 맵 상태로 순수 판단을 만든다.
            var facts = new ChapterContinuationFacts(
                outcome: Outcome,
                requestedChapterKey: chapterKey,
                currentChapterKey: chapterKey,
                requestedBossNodeId: bossNodeId,
                selectedNodeId: bossNodeId,
                selectedStageKey: new StageKey(selected.StageKey),
                hasSelection: true,
                hasBattle: false,
                sourceMapClear: true,
                sourceNodeClear: true,
                isCombatBoss: true,
                matchingVictory: true,
                targetRegistered: next && maps.ContainsKey(definition.NextChapterKey),
                targetClear: next && maps.TryGetValue(definition.NextChapterKey, out bool clear) && clear);
            return ChapterContinuationCommands.Complete(facts, definition);
        }

        private ChapterContinuationFailureReason PrepareTargetGraph(int targetChapterKey,
            IReadOnlyList<StageProgressData> expectedGraph, out IReadOnlyList<StageProgressData> generated)
        {
            generated = Array.Empty<StageProgressData>();
            var map = _stageService.GetStageMap(targetChapterKey);
            if (map == null || map.MapKey != targetChapterKey)
                return ChapterContinuationFailureReason.InvalidDefinition;
            map.Validate();
            if (expectedGraph.Count == 0)
            {
                try { generated = _generator.PrepareGraph(targetChapterKey); }
                catch (InvalidOperationException) when (Outcome != RunOutcome.InProgress)
                { return ChapterContinuationFailureReason.RunEnded; }
            }
            var targetGraph = expectedGraph.Count == 0 ? generated : expectedGraph;
            return ValidateTargetGraph(map, targetGraph)
                ? ChapterContinuationFailureReason.None : ChapterContinuationFailureReason.InvalidDefinition;
        }

        private ChapterContinuationFailureReason ContinuationAdmission(int chapterKey, StageNodeId bossNodeId)
        {
            if (Outcome != RunOutcome.InProgress) return ChapterContinuationFailureReason.RunEnded;
            if (_buffers.CombatStateReadSink.HasBattle) return ChapterContinuationFailureReason.BattleRetained;
            if (_currentRead.CurrentMapKey != chapterKey) return ChapterContinuationFailureReason.ChapterMismatch;
            if (!_currentRead.HasSelection) return ChapterContinuationFailureReason.StageNotFound;
            var selected = _currentRead.CurrentStage;
            if (selected.NodeId != bossNodeId.Value) return ChapterContinuationFailureReason.StageMismatch;
            var node = _stageRead.GetStages(chapterKey).FirstOrDefault(n => n.NodeId == selected.NodeId && n.StageKey == selected.StageKey);
            if (!_mapRead.GetMap(chapterKey).IsClear || !node.IsClear) return ChapterContinuationFailureReason.ChapterNotCleared;
            if (node.StageType != StageType.Combat || node.MapRole != StageMapRole.Boss || node.NextNodeIds.Count != 0)
                return ChapterContinuationFailureReason.InvalidBoss;
            if (!ChapterContinuationCommands.IsMatchingVictory(_buffers.CombatStateReadSink.Completion, chapterKey, selected.NodeId, selected.StageKey))
                return ChapterContinuationFailureReason.CompletionMismatch;
            return ChapterContinuationFailureReason.None;
        }

        private ChapterContinuationFailureReason ValidateContinuationOrigin(int chapterKey, StageNodeId bossNodeId,
            StageSelectionData selected, ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.BattleCompletionChanges completion,
            IReadOnlyList<StageProgressData> source)
        {
            var admission = ContinuationAdmission(chapterKey, bossNodeId);
            if (admission != ChapterContinuationFailureReason.None) return admission;
            if (_currentRead.CurrentStage.StageKey != selected.StageKey || _currentRead.CurrentStage.StageName != selected.StageName)
                return ChapterContinuationFailureReason.StageMismatch;
            if (!ReferenceEquals(completion, _buffers.CombatStateReadSink.Completion)) return ChapterContinuationFailureReason.CompletionMismatch;
            if (!ChapterContinuationPreparation.SameGraph(source, _stageRead.GetStages(chapterKey))) return ChapterContinuationFailureReason.InvalidDefinition;
            return ChapterContinuationFailureReason.None;
        }

        private bool ValidateTargetGraph(StageMapData map, IReadOnlyList<StageProgressData> nodes)
        {
            if (nodes.Count == 0 || nodes.Select(n => n.NodeId).Distinct().Count() != nodes.Count) return false;
            var starts = nodes.Where(n => n.Depth == 0).ToArray();
            if (starts.Length != 1 || starts[0].StageKey != 0 || starts[0].WidthIndex != 0) return false;
            var bosses = nodes.Where(n => n.MapRole == StageMapRole.Boss && n.StageKey != 0).ToArray();
            if (bosses.Length != 1 || bosses[0].Depth != map.DepthCount - 1 || bosses[0].NextNodeIds.Count != 0 || bosses[0].StageType != StageType.Combat)
                return false;
            var byId = nodes.ToDictionary(n => n.NodeId);
            var incoming = new HashSet<int>();
            foreach (var node in nodes)
            {
                if (node.Depth < 0 || node.Depth >= map.DepthCount || node.WidthIndex >= map.WidthMax ||
                    (node.Depth < map.DepthCount - 1 && node.NextNodeIds.Count == 0)) return false;
                foreach (int id in node.NextNodeIds)
                {
                    if (!byId.TryGetValue(id, out var target) || target.Depth != node.Depth + 1) return false;
                    incoming.Add(id);
                }
                if (node.StageKey == 0) { if (node.Depth != 0) return false; continue; }
                var root = _stageService.GetStage(node.StageKey);
                if (root == null || root.StageKey != node.StageKey || root.MapKey != map.MapKey || root.StageType != node.StageType ||
                    root.MapRole != node.MapRole || root.StageData.StageType != root.StageType ||
                    (node.Depth == map.DepthCount - 1 ? root.MapRole != StageMapRole.Boss || root.StageData is not CombatStageDetail
                        : root.MapRole != StageMapRole.Regular)) return false;
                root.StageData.Validate();
            }
            return nodes.All(n => n.Depth == 0 || incoming.Contains(n.NodeId));
        }

        public ChapterProgressionResult EnterChapter(int chapterKey)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            EnsureBound();
            if (Outcome != RunOutcome.InProgress) return RunEnded();
            if (_currentRead.HasMap && _currentRead.CurrentMapKey != chapterKey)
            {
                if (!_currentRead.HasSelection) return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ContinuationRejected);
                var continuation = ContinueChapter(_currentRead.CurrentMapKey, new StageNodeId(_currentRead.CurrentStage.NodeId), chapterKey);
                if (continuation.Accepted) return ChapterProgressionResult.EnterChapter(chapterKey);
                return ChapterProgressionResult.Reject(continuation.FailureReason == ChapterContinuationFailureReason.RunEnded
                    ? ChapterProgressionFailureReason.RunEnded : continuation.FailureReason == ChapterContinuationFailureReason.ChapterMismatch
                    ? ChapterProgressionFailureReason.ChapterMismatch : ChapterProgressionFailureReason.ContinuationRejected);
            }
            MapProgressData requestedMap = _mapRead.GetMap(chapterKey);
            int currentChapterKey = _currentRead.CurrentMapKey;
            bool currentChapterClear = currentChapterKey > 0 &&
                _mapRead.GetMap(currentChapterKey).IsClear;
            var stages = _stageRead.GetStages(chapterKey).ToArray();
            IReadOnlyList<StageProgressData> generated = Array.Empty<StageProgressData>();
            ChapterProgressionResult decision = ChapterProgressionCommands.EnterChapter(
                chapterKey, currentChapterKey, currentChapterClear,
                requestedMap.IsClear, stages.Length > 0);

            if (decision.FailureReason == ChapterProgressionFailureReason.StageNotFound)
            {
                // 그래프는 만들기만 하고, 진입이 승인될 때 맵 선택과 같은 제안으로 등록한다.
                generated = _generator.PrepareGraph(chapterKey);
                decision = ChapterProgressionCommands.EnterChapter(
                    chapterKey, currentChapterKey, currentChapterClear,
                    requestedMap.IsClear, generated.Count > 0);
            }

            if (Outcome != RunOutcome.InProgress) return RunEnded();
            if (decision.Accepted)
                _buffers.ChapterProgressWriteSink.PublishChapterEntry(new ChapterEntryPreparation(
                    currentChapterKey, currentChapterClear, chapterKey, requestedMap.IsClear, stages, generated));
            return decision;
        }

        public ChapterProgressionResult PrepareStageEntry(int chapterKey, StageNodeId targetNodeId)
        {
            ChapterProgressionResult decision = EvaluateStageEntry(chapterKey, targetNodeId);
            if (decision.Accepted)
                _stageService.ValidateEntry(decision.StageKey.Value);
            if (Outcome != RunOutcome.InProgress) return RunEnded();
            return decision;
        }

        public ChapterProgressionResult ConfirmStageEntry(int chapterKey, StageNodeId targetNodeId)
        {
            // 진입 준비 성공 후 호출한다. 판단에 쓰는 상태는 항상 Buffer에서 다시 읽는다.
            ChapterProgressionResult decision = PrepareStageEntry(chapterKey, targetNodeId);
            if (Outcome != RunOutcome.InProgress) return RunEnded();
            if (decision.Accepted)
                _currentWrite.SelectStage(decision.NodeId, decision.StageKey,
                    $"Stage {decision.StageKey.Value}");
            return decision;
        }

        private ChapterProgressionResult EvaluateStageEntry(int chapterKey, StageNodeId targetNodeId)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (targetNodeId.IsEmpty) throw new ArgumentException("진입할 노드 ID가 필요합니다.", nameof(targetNodeId));
            EnsureBound();
            if (Outcome != RunOutcome.InProgress) return RunEnded();
            bool isChapterClear = _mapRead.GetMap(chapterKey).IsClear;
            ChapterNodeProgress[] nodes = SnapshotNodes(chapterKey);
            StageNodeId fromNodeId = _currentRead.HasSelection
                ? new StageNodeId(_currentRead.CurrentStage.NodeId)
                : StageNodeId.Empty;
            return ChapterProgressionCommands.ClickNode(
                chapterKey, _currentRead.CurrentMapKey, chapterKey,
                fromNodeId, isChapterClear, nodes, targetNodeId);
        }

        public ChapterProgressionResult ResolveStage(int chapterKey, StageOutcome outcome)
        {
            ChapterProgressionResult decision = PlanStageResolution(chapterKey, outcome);
            if (!decision.Accepted) return decision;
            if (decision.EndAttempt)
                throw new InvalidOperationException("완전한 전투 정산이 필요합니다 (complete battle settlement required).");
            if (decision.ClearStage) _stageWrite.SetClear(chapterKey, decision.NodeId.Value, true);
            if (decision.ClearMap) _mapWrite.SetClear(chapterKey, true);
            return decision;
        }

        /// <summary>외부 조회가 끝난 뒤 DB를 다시 호출하지 않고 최신 Buffer의 진입 규칙을 확인한다.</summary>
        internal ChapterProgressionResult RevalidateStageEntry(int chapterKey, StageNodeId targetNodeId)
            => EvaluateStageEntry(chapterKey, targetNodeId);

        public ChapterProgressionResult PlanStageResolution(int chapterKey, StageOutcome outcome)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            EnsureBound();
            if (Outcome != RunOutcome.InProgress) return RunEnded();
            MapProgressData map = _mapRead.GetMap(chapterKey);
            ChapterNodeProgress[] nodes = SnapshotNodes(chapterKey);
            bool hasEnteredStage = _currentRead.HasSelection;
            StageNodeId nodeId = hasEnteredStage
                ? new StageNodeId(_currentRead.CurrentStage.NodeId)
                : StageNodeId.Empty;
            StageKey stageKey = hasEnteredStage
                ? new StageKey(_currentRead.CurrentStage.StageKey)
                : StageKey.Empty;
            bool isBossStage = hasEnteredStage && _stageService.IsBossStage(stageKey.Value);
            if (Outcome != RunOutcome.InProgress) return RunEnded();
            ChapterProgressionResult decision = ChapterProgressionCommands.ResolveStage(
                chapterKey, _currentRead.CurrentMapKey, chapterKey,
                hasEnteredStage, nodeId, stageKey, map.IsClear, nodes,
                outcome, isBossStage);
            return decision;
        }

        /// <summary>회복 서비스가 준비와 종류를 검증한 뒤 호출하며 현재 식별자를 다시 확인한다.</summary>
        internal ChapterProgressionResult CompleteRecoveryStage(
            int chapterKey, StageNodeId expectedNodeId, StageKey expectedStageKey)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (expectedNodeId.IsEmpty || expectedStageKey.IsEmpty)
                throw new ArgumentException("완료할 회복 노드와 스테이지 키가 필요합니다.");
            EnsureBound();
            if (Outcome != RunOutcome.InProgress) return RunEnded();
            if (_currentRead.CurrentMapKey != chapterKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (!_currentRead.HasSelection)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            var selected = _currentRead.CurrentStage;
            if (selected.NodeId != expectedNodeId.Value || selected.StageKey != expectedStageKey.Value)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);

            ChapterProgressionResult decision = ChapterProgressionCommands.CompleteRecoveryStage(
                chapterKey, _currentRead.CurrentMapKey, chapterKey, true,
                expectedNodeId, expectedStageKey, _mapRead.GetMap(chapterKey).IsClear, SnapshotNodes(chapterKey));
            if (decision.Accepted) _buffers.ChapterProgressWriteSink.PublishStageClear(chapterKey, expectedNodeId, expectedStageKey);
            return decision;
        }

        /// <summary>상점 서비스가 준비와 종류를 검증한 뒤 호출하며 현재 식별자를 다시 확인한다.</summary>
        internal ChapterProgressionResult CompleteShopStage(int chapterKey, StageNodeId expectedNodeId, StageKey expectedStageKey)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (expectedNodeId.IsEmpty || expectedStageKey.IsEmpty)
                throw new ArgumentException("완료할 상점 노드와 스테이지 키가 필요합니다.");
            EnsureBound();
            if (Outcome != RunOutcome.InProgress) return RunEnded();
            if (_currentRead.CurrentMapKey != chapterKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (!_currentRead.HasSelection)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            var selected = _currentRead.CurrentStage;
            if (selected.NodeId != expectedNodeId.Value || selected.StageKey != expectedStageKey.Value)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            ChapterProgressionResult decision = ChapterProgressionCommands.CompleteShopStage(
                chapterKey, _currentRead.CurrentMapKey, chapterKey, true,
                expectedNodeId, expectedStageKey, _mapRead.GetMap(chapterKey).IsClear, SnapshotNodes(chapterKey));
            if (decision.Accepted) _buffers.ChapterProgressWriteSink.PublishStageClear(chapterKey, expectedNodeId, expectedStageKey);
            return decision;
        }

        private ChapterNodeProgress[] SnapshotNodes(int chapterKey)
        {
            var stages = _stageRead.GetStages(chapterKey);
            var nodes = new ChapterNodeProgress[stages.Count];
            for (int i = 0; i < stages.Count; i++)
            {
                StageProgressData stage = stages[i];
                var next = new StageNodeId[stage.NextNodeIds.Count];
                for (int j = 0; j < next.Length; j++)
                    next[j] = new StageNodeId(stage.NextNodeIds[j]);
                nodes[i] = new ChapterNodeProgress(
                    new StageNodeId(stage.NodeId),
                    stage.StageKey == 0 ? StageKey.Empty : new StageKey(stage.StageKey),
                    stage.Depth, stage.IsClear, next);
            }
            return nodes;
        }

        private void EnsureBound()
        {
            if (_currentRead == null || _currentWrite == null ||
                _stageRead == null || _stageWrite == null ||
                _mapRead == null || _mapWrite == null || _runRead == null)
                throw new InvalidOperationException("Chapter progression requires Buffer binding.");
        }

        private static ChapterProgressionResult RunEnded()
            => ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded);

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IChapterProgressionCoordinator>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
