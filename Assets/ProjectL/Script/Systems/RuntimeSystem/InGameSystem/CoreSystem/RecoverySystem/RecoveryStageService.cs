using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem
{
    public interface IRecoveryStageService : IServiceModule
    {
        RecoveryOperationResult SelectRecovery(RecoveryEntryContext context, RecoveryOptionId optionId);
        RecoveryOperationResult CompleteRecovery(RecoveryEntryContext context);
        /// <summary>합법적인 대상 선택과 DB 회복 선택지를 함께 게시한다. 화면 전환은 호출자가 담당한다.</summary>
        ChapterProgressionResult EnterRecovery(int chapterKey, StageNodeId targetNodeId);
        /// <summary>회복 여부와 관계없이 현재 회복 노드만 완료한다. 적용된 회복과 결제는 다시 실행하지 않는다.</summary>
        ChapterProgressionResult CompleteRecovery(int chapterKey, StageNodeId nodeId);
    }

    /// <summary>DB 조회와 진입 판단을 조율하며 현재 선택과 회복 기록은 Buffer가 소유한다.</summary>
    internal sealed class RecoveryStageService : IRecoveryStageService,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private readonly RecoveryStateProjection _projection;
        private readonly RecoveryOperationGuard _guard;
        private readonly ChapterProgressionCoordinator _progression;
        private IDataBaseQueryContext _database;
        private IBufferQuery _buffers;

        internal RecoveryStageService(ChapterProgressionCoordinator progression,
            RecoveryStateProjection projection = null, RecoveryOperationGuard guard = null)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _projection = projection ?? new RecoveryStateProjection();
            _guard = guard ?? new RecoveryOperationGuard();
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

        public ChapterProgressionResult CompleteRecovery(int chapterKey, StageNodeId nodeId)
        {
            using (BeginOperation())
            {
                ChapterProgressionResult target = ValidateRecoveryTarget(chapterKey, nodeId);
                return target.Accepted
                    ? _progression.CompleteRecoveryStage(chapterKey, nodeId, target.StageKey) : target;
            }
        }

        public RecoveryOperationResult SelectRecovery(RecoveryEntryContext context, RecoveryOptionId optionId)
            => Execute(RecoveryCommandData.Select(context, optionId));

        public RecoveryOperationResult CompleteRecovery(RecoveryEntryContext context)
            => Execute(RecoveryCommandData.Complete(context));

        internal IDisposable BeginOperation() => _guard.Enter();

        private RecoveryOperationResult Execute(RecoveryCommandData command)
        {
            using (BeginOperation())
            {
                var prepared = PrepareOperation(command);
                InstallOperation(prepared);
                return prepared.Result;
            }
        }

        internal RecoveryOperationPreparation PrepareOperation(RecoveryCommandData command)
        {
            _guard.RequireActive();
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (_database == null || _buffers == null)
                throw new InvalidOperationException("회복 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
            if (!RecoveryStateProjection.TryGetEntry(_buffers, out var recovery))
                return Mismatch(command, RecoveryFailureReason.WrongPhase);
            if (recovery.Context != command.Context)
                return Mismatch(command, RecoveryFailureReason.EntryMismatch);
            var facts = _projection.Capture(_buffers);
            if (facts.IsCompleted) return Mismatch(command, RecoveryFailureReason.StaleEntry);
            RecoveryOptionSpec option = null;
            if (command.Kind == RecoveryCommandKind.Select && !recovery.TryGetOption(command.OptionId.Value, out option))
                return Mismatch(command, RecoveryFailureReason.UnknownOption);

            var target = ValidateRecoveryTarget(command.Context.ChapterKey, command.Context.NodeId);
            _projection.RequireUnchanged(_buffers, facts);
            if (!target.Accepted)
                throw new InvalidOperationException("회복 준비 중 대상 판정이 변경되었습니다.");
            var state = _projection.Project(facts);
            RecoveryOperationResult result;
            PreparedRecoveryMutation mutation = null;
            if (command.Kind == RecoveryCommandKind.Complete)
            {
                var progression = _progression.PrepareRecoveryCompletion(command.Context.ChapterKey,
                    command.Context.NodeId, command.Context.StageKey);
                if (!progression.Accepted)
                    throw new InvalidOperationException("회복 완료 준비 중 진행 원본이 변경되었습니다.");
                mutation = _buffers.RecoveryOperations.PrepareCompletion(command.Context);
                result = RecoveryOperationResult.Completed(_projection.Project(facts, completed: true), progression);
            }
            else if (recovery.AppliedOptionId.HasValue)
            {
                result = recovery.AppliedOptionId == option.Id
                    ? RecoveryOperationResult.Selected(state, alreadyApplied: true)
                    : RecoveryOperationResult.TradeRejected(RecoveryFailureReason.AlreadyResolved, state);
            }
            else
            {
                var available = System.Linq.Enumerable.Single(state.Options, x => x.Option.Id == option.Id);
                if (!available.CanSelect)
                {
                    result = RecoveryOperationResult.TradeRejected(available.UnavailableReason, state);
                }
                else
                {
                    var decision = CharacterRecoveryCalculator.Plan(facts.Main, facts.MainId, facts.Souls, option, facts.Gold);
                    mutation = _buffers.RecoveryOperations.PrepareSelection(command.Context, decision);
                    result = RecoveryOperationResult.Selected(_projection.Project(facts,
                        decision.Characters.Main.Final, decision.FinalBalance, mutation.Recovery));
                }
            }
            _projection.RequireUnchanged(_buffers, facts);
            return new RecoveryOperationPreparation(this, _guard.ActiveScope, command, result, mutation, facts);
        }

        private RecoveryOperationPreparation Mismatch(RecoveryCommandData command, RecoveryFailureReason reason)
            => new RecoveryOperationPreparation(this, _guard.ActiveScope, command, RecoveryOperationResult.StateMismatch(reason), null, null);

        internal void InstallOperation(RecoveryOperationPreparation prepared)
        {
            _guard.RequireActive();
            if (prepared == null) throw new ArgumentNullException(nameof(prepared));
            if (!ReferenceEquals(prepared.Owner, this) || !ReferenceEquals(prepared.Scope, _guard.ActiveScope) || prepared.Consumed)
                throw new InvalidOperationException("회복 준비 결과는 같은 소유자가 한 번만 소비해야 합니다.");
            if (prepared.Facts != null) _projection.RequireUnchanged(_buffers, prepared.Facts);
            prepared.Consumed = true;
            if (prepared.Mutation != null) _buffers.RecoveryOperations.InstallMutation(prepared.Mutation);
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
            using (BeginOperation())
            {
                var prepared = PrepareEntry(chapterKey, targetNodeId);
                InstallEntry(prepared, null);
                return prepared.Result;
            }
        }

        internal RecoveryEntryOperationPreparation PrepareEntry(int chapterKey, StageNodeId targetNodeId)
        {
            _guard.RequireActive();
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (targetNodeId.IsEmpty) throw new ArgumentException("진입할 노드 ID가 필요합니다.", nameof(targetNodeId));
            if (_database == null || _buffers == null)
                throw new InvalidOperationException("회복 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
            if (_buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress)
                return PrepareEntryResult(ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded));
            if (_buffers.CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("남아 있는 전투를 정리한 뒤 회복에 진입해야 합니다.");

            if (!_buffers.RunDeckReadSink.TryGetCurrent(out var deck))
                return PrepareEntryResult(ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RosterNotInitialized));
            var current = _buffers.CurrentProgressReadSink;
            int originChapter = current.CurrentMapKey;
            bool originSelected = current.HasSelection;
            StageSelectionData originStage = current.CurrentStage;
            ChapterProgressionResult decision = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!decision.Accepted) return PrepareEntryResult(decision);

            var definition = RecoveryDefinitionAdapter.Read(_database, decision.ChapterKey, decision.StageKey, out var mapRole);
            // DB 조회 중 현재 선택이나 그래프가 바뀌었으면 새 선택지를 게시하지 않는다.
            ChapterProgressionResult revalidated = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!revalidated.Accepted) return PrepareEntryResult(revalidated);
            // 마지막 정의 조회의 콜백도 진행 사실을 바꿀 수 있으므로 성장 진행까지 다시 판정한다.
            revalidated = _progression.RevalidateStageEntry(chapterKey, targetNodeId);
            if (!revalidated.Accepted) return PrepareEntryResult(revalidated);
            if (current.CurrentMapKey != originChapter || current.HasSelection != originSelected ||
                current.CurrentStage.NodeId != originStage.NodeId || current.CurrentStage.StageKey != originStage.StageKey ||
                current.CurrentStage.StageName != originStage.StageName ||
                revalidated.ChapterKey != decision.ChapterKey || revalidated.NodeId != decision.NodeId ||
                revalidated.StageKey != decision.StageKey)
                return PrepareEntryResult(ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch));

            var context = new RecoveryEntryContext(new StageEntryId(Guid.NewGuid()), revalidated.ChapterKey,
                revalidated.NodeId, revalidated.StageKey);
            var entry = new RecoveryEntryPreparation(originChapter,
                originSelected, originStage, revalidated.NodeId, revalidated.StageKey,
                $"Stage {revalidated.StageKey.Value}", definition.Options.ToArray(), StageType.Event, mapRole, context, deck.MainId);
            return new RecoveryEntryOperationPreparation(this, _guard.ActiveScope, revalidated, entry);
        }

        private RecoveryEntryOperationPreparation PrepareEntryResult(ChapterProgressionResult result)
            => new RecoveryEntryOperationPreparation(this, _guard.ActiveScope, result);

        internal void InstallEntry(RecoveryEntryOperationPreparation prepared, StageEntryCommandResult? result)
        {
            _guard.RequireActive();
            if (prepared == null) throw new ArgumentNullException(nameof(prepared));
            if (!ReferenceEquals(prepared.Owner, this) || !ReferenceEquals(prepared.Scope, _guard.ActiveScope) || prepared.Consumed)
                throw new InvalidOperationException("회복 진입 제안은 같은 소유자와 처리 범위에서 한 번만 설치해야 합니다.");
            if (result.HasValue && (!ReferenceEquals(result.Value.Result, prepared.Result) ||
                result.Value.RequestNumber < 1 || result.Value.Kind != StageEntryCommandKind.Node || result.Value.Seed != 0 ||
                (prepared.Entry != null && (result.Value.ChapterKey != prepared.Entry.ExpectedChapterKey ||
                    result.Value.NodeId != prepared.Entry.TargetNodeId))))
                throw new InvalidOperationException("맵 결과가 준비된 회복 진입과 다릅니다.");

            if (prepared.Entry != null)
            {
                var current = _progression.RevalidateStageEntry(prepared.Entry.ExpectedChapterKey, prepared.Entry.TargetNodeId);
                if (!current.Accepted || current.StageKey != prepared.Entry.TargetStageKey)
                    throw new InvalidOperationException("회복 진입 설치 전에 대상 진행 사실이 변경되었습니다.");
            }
            _buffers.RecoveryOperations.InstallEntry(prepared.Entry, result, _buffers.StageCommandResultStore);
            prepared.Consumed = true;
        }
    }
}
