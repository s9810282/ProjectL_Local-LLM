using System;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem
{
    public interface IRecoveryStageService : IServiceModule
    {
        /// <summary>합법적인 대상 선택과 DB 회복 선택지를 함께 게시한다. 화면 전환은 호출자가 담당한다.</summary>
        ChapterProgressionResult EnterRecovery(int chapterKey, StageNodeId targetNodeId);
        /// <summary>현재 노드의 선택지를 적용한다. false는 같은 선택지가 이미 적용되었음을 뜻한다.</summary>
        bool Recover(int chapterKey, StageNodeId nodeId, RecoveryOptionId optionId);
        /// <summary>회복 여부와 관계없이 현재 회복 노드만 완료한다. 적용된 회복과 결제는 다시 실행하지 않는다.</summary>
        ChapterProgressionResult CompleteRecovery(int chapterKey, StageNodeId nodeId);
    }

    /// <summary>DB 조회와 진입 판단을 조율하며 현재 선택과 회복 기록은 Buffer가 소유한다.</summary>
    internal sealed class RecoveryStageService : IRecoveryStageService,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private readonly ICombatProcessingService _combat;
        private readonly ChapterProgressionCoordinator _progression;
        private IDataBaseQueryContext _database;
        private IBufferQuery _buffers;

        internal RecoveryStageService(ICombatProcessingService combat, ChapterProgressionCoordinator progression)
        {
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
        }

        public void InitData(IDataBaseQueryContext data)
            => _database = data ?? throw new ArgumentNullException(nameof(data));

        public void PostBindService(ISystemServiceStoreReader resolver)
            => _buffers = resolver.GetService(BufferSystemKey.Buffer);

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRecoveryStageService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        public bool Recover(int chapterKey, StageNodeId nodeId, RecoveryOptionId optionId)
        {
            if (optionId.IsEmpty) throw new ArgumentException("회복 선택지 ID가 필요합니다.", nameof(optionId));
            ChapterProgressionResult target = ValidateRecoveryTarget(chapterKey, nodeId);
            if (!target.Accepted)
                throw new InvalidOperationException($"회복 적용이 거절되었습니다: {target.FailureReason}");
            return _combat.Recover(optionId);
        }

        public ChapterProgressionResult CompleteRecovery(int chapterKey, StageNodeId nodeId)
        {
            ChapterProgressionResult target = ValidateRecoveryTarget(chapterKey, nodeId);
            return target.Accepted
                ? _progression.CompleteRecoveryStage(chapterKey, nodeId, target.StageKey) : target;
        }

        private ChapterProgressionResult ValidateRecoveryTarget(int chapterKey, StageNodeId nodeId)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (nodeId.IsEmpty) throw new ArgumentException("회복 노드 ID가 필요합니다.", nameof(nodeId));
            if (_database == null || _buffers == null)
                throw new InvalidOperationException("회복 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");

            ChapterProgressionResult target = ValidateRecoveryStorage(chapterKey, nodeId);
            if (!target.Accepted) return target;
            var root = _database.InGameQuery?.StageTableQuery?.GetStage(target.StageKey.Value)
                ?? throw new InvalidOperationException("현재 회복 스테이지의 정의가 필요합니다.");
            root.Validate();
            if (root.StageKey != target.StageKey.Value || root.MapKey != chapterKey ||
                root.MapRole == StageMapRole.Boss || root.StageData is not EventStageDetail)
                throw new InvalidOperationException("현재 선택은 해당 챕터의 보스가 아닌 회복 스테이지여야 합니다.");

            // 정의 조회 후에도 식별자와 미완료 상태가 같아야 설치된 선택지를 처리한다.
            ChapterProgressionResult current = ValidateRecoveryStorage(chapterKey, nodeId);
            if (current.Accepted && current.StageKey != target.StageKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            return current;
        }

        private ChapterProgressionResult ValidateRecoveryStorage(int chapterKey, StageNodeId nodeId)
        {
            if (_buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded);
            if (_buffers.CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("남아 있는 전투를 정리한 뒤 회복을 처리해야 합니다.");
            var current = _buffers.CurrentProgressReadSink;
            if (current.CurrentMapKey != chapterKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            if (!current.HasSelection)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
            if (current.CurrentStage.NodeId != nodeId.Value)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
            if (_buffers.MapProgressReadSink.GetMap(chapterKey).IsClear)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterCompleted);
            foreach (var node in _buffers.StageProgressReadSink.GetStages(chapterKey))
            {
                if (node.NodeId != nodeId.Value) continue;
                if (node.StageKey != current.CurrentStage.StageKey)
                    return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);
                if (node.IsClear)
                    return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageAlreadyCleared);
                if (current.Recovery == null || current.Shop != null ||
                    node.StageType != StageType.Event || node.MapRole == StageMapRole.Boss)
                    throw new InvalidOperationException("준비된 일반 회복 노드가 필요합니다.");
                return ChapterProgressionResult.EnterStage(chapterKey, nodeId, new StageKey(node.StageKey));
            }
            return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
        }

        public ChapterProgressionResult EnterRecovery(int chapterKey, StageNodeId targetNodeId)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (targetNodeId.IsEmpty) throw new ArgumentException("진입할 노드 ID가 필요합니다.", nameof(targetNodeId));
            if (_database == null || _buffers == null)
                throw new InvalidOperationException("회복 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
            if (_buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded);
            if (_buffers.CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("남아 있는 전투를 정리한 뒤 회복에 진입해야 합니다.");

            var current = _buffers.CurrentProgressReadSink;
            int originChapter = current.CurrentMapKey;
            bool originSelected = current.HasSelection;
            StageSelectionData originStage = current.CurrentStage;
            ChapterProgressionResult decision = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!decision.Accepted) return decision;

            var definition = RecoveryDefinitionAdapter.Read(_database, decision.ChapterKey, decision.StageKey, out var mapRole);
            // DB 조회 중 현재 선택이나 그래프가 바뀌었으면 새 선택지를 게시하지 않는다.
            ChapterProgressionResult revalidated = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!revalidated.Accepted) return revalidated;
            if (current.CurrentMapKey != originChapter || current.HasSelection != originSelected ||
                current.CurrentStage.NodeId != originStage.NodeId || current.CurrentStage.StageKey != originStage.StageKey ||
                current.CurrentStage.StageName != originStage.StageName ||
                revalidated.ChapterKey != decision.ChapterKey || revalidated.NodeId != decision.NodeId ||
                revalidated.StageKey != decision.StageKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);

            _buffers.RecoveryStageWriteSink.PrepareRecovery(new RecoveryEntryPreparation(originChapter,
                originSelected, originStage, revalidated.NodeId, revalidated.StageKey,
                $"Stage {revalidated.StageKey.Value}", definition.Options.ToArray(), StageType.Event, mapRole));
            return revalidated;
        }
    }
}
