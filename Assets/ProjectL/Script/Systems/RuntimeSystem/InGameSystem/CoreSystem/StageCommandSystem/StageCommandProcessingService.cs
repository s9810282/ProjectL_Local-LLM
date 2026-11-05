using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ShopSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageCommandSystem
{
    /// <summary>Core가 기대 번호의 입력을 직접 읽고 결과를 게시합니다. 입력 정리는 View가 맡습니다.</summary>
    public interface IStageCommandProcessingService : IServiceModule
    {
        bool ProcessNextNodeCommand(int expectedRequestNumber, Func<int> nextSeed);
        bool ProcessNextShopCommand(int expectedRequestNumber);
        bool TryProcessNextBattleCommand(int expectedRequestNumber, out ChapterProgressionResult result);
    }

    /// <summary>맵·상점 입력을 조회하고 기존 동기 Core 거래와 처리 결과를 함께 완료합니다.</summary>
    internal sealed class StageCommandProcessingService : IStageCommandProcessingService,
        ISystemPostBindService, IExportSystemServices
    {
        private readonly ChapterProgressionCoordinator _progression;
        private readonly IShopService _shop;
        private readonly IBattleEntryService _combat;
        private readonly RecoveryStageService _recovery;
        private IBufferQuery _buffers;
        private IStageViewBufferReadSink _mapInput;
        private readonly ShopCommandExecutor _shopCommands;
        private IStageCommandResultReadSink _results;
        private IStageCommandResultStore _resultStore;
        private bool _mapExecuting;

        internal StageCommandProcessingService(ChapterProgressionCoordinator progression,
            IShopService shop, IBattleEntryService combat, RecoveryStageService recovery, ShopCommandExecutor shopCommands)
        {
            _progression = progression ?? throw new ArgumentNullException(nameof(progression));
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
            _shopCommands = shopCommands ?? throw new ArgumentNullException(nameof(shopCommands));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            _buffers = resolver.GetService(BufferSystemKey.Buffer);
            _mapInput = _buffers.StageViewReadSink;
            _results = _buffers.StageCommandResults;
            _resultStore = _buffers.StageCommandResultStore;
            _shopCommands.Bind(_buffers.ShopViewReadSink, _results, _resultStore);
        }

        public bool ProcessNextNodeCommand(int expectedRequestNumber, Func<int> nextSeed)
        {
            if (nextSeed == null) throw new ArgumentNullException(nameof(nextSeed));
            ValidateRequest(expectedRequestNumber, _mapExecuting);
            if (!TryReadMap(expectedRequestNumber, out var command)) return false;
            _mapExecuting = true;
            try
            {
                if (IsRecoveryNode(command.Data)) return ProcessRecoveryEntry(command);
                // Run seed 인수 전 임시 공급입니다. 취소된 Job은 seed를 소비하지 않습니다.
                int seed = nextSeed();
                if (!TryReadMap(expectedRequestNumber, out _)) return false;
                var data = command.Data;
                var result = EvaluateNodeEntry(data.ChapterKey, data.NodeId);
                if (result.Accepted) result = _shop.EnterShop(data.ChapterKey, data.NodeId, seed);
                // 동기 거래가 끝난 뒤 입력 Clear를 취소로 해석하면 상태와 처리 기록이 분리됩니다.
                _resultStore.InstallMapResult(new StageEntryCommandResult(
                    command.Number, data.ChapterKey, data.NodeId, StageEntryCommandKind.Node, seed, result));
                return true;
            }
            finally
            {
                _mapExecuting = false;
            }
        }

        private bool IsRecoveryNode(StageViewCommandData command)
        {
            if (_buffers.CurrentProgressReadSink.CurrentMapKey != command.ChapterKey) return false;
            foreach (var node in _buffers.StageProgressReadSink.GetStages(command.ChapterKey))
            {
                if (node.NodeId == command.NodeId.Value)
                    return node.StageKey != 0 && node.StageType == StageType.Event && node.MapRole != StageMapRole.Boss;
            }
            return false;
        }

        private bool ProcessRecoveryEntry(StageViewCommand command)
        {
            using (_recovery.BeginOperation())
            {
                RecoveryEntryOperationPreparation prepared;
                try
                {
                    prepared = _recovery.PrepareEntry(command.Data.ChapterKey, command.Data.NodeId);
                }
                catch (InvalidOperationException) when (!IsSamePendingMap(command))
                {
                    // 조회 중 교체로 원본이 사라졌다면 취소된 번호의 결과를 새 Run에 남기지 않는다.
                    return false;
                }
                if (!IsSamePendingMap(command)) return false;
                var result = new StageEntryCommandResult(command.Number, command.Data.ChapterKey,
                    command.Data.NodeId, StageEntryCommandKind.Node, 0, prepared.Result);
                _recovery.InstallEntry(prepared, result);
                return true;
            }
        }

        private bool IsSamePendingMap(StageViewCommand expected)
        {
            return TryReadMap(expected.Number, out var current) && current.Data.Kind == expected.Data.Kind &&
                current.Data.ChapterKey == expected.Data.ChapterKey && current.Data.NodeId == expected.Data.NodeId;
        }

        public bool TryProcessNextBattleCommand(int expectedRequestNumber, out ChapterProgressionResult result)
        {
            result = null;
            ValidateRequest(expectedRequestNumber, _mapExecuting);
            if (!TryReadMap(expectedRequestNumber, out var command)) return false;
            _mapExecuting = true;
            try
            {
                var data = command.Data;
                result = _combat.EnterBattle(data.ChapterKey, data.NodeId);
                _resultStore.InstallMapResult(new StageEntryCommandResult(
                    command.Number, data.ChapterKey, data.NodeId, StageEntryCommandKind.Battle, 0, result));
                return true;
            }
            finally
            {
                _mapExecuting = false;
            }
        }

        public bool ProcessNextShopCommand(int expectedRequestNumber)
            => _shopCommands.ProcessNext(expectedRequestNumber);

        private bool TryReadMap(int expectedRequestNumber, out StageViewCommand command) =>
            _mapInput.TryGetNextCommand(_results.HandledMapCommandNumber, out command) &&
            command.Number == expectedRequestNumber;

        private ChapterProgressionResult EvaluateNodeEntry(int chapterKey, StageNodeId nodeId)
        {
            if (_buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded);
            if (_buffers.CurrentProgressReadSink.CurrentMapKey != chapterKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.ChapterMismatch);
            var result = _progression.RevalidateStageEntry(chapterKey, nodeId);
            if (!result.Accepted) return result;
            foreach (var node in _buffers.StageProgressReadSink.GetStages(chapterKey))
            {
                if (node.NodeId != nodeId.Value) continue;
                return node.StageType == StageType.Shop && node.MapRole == StageMapRole.Regular
                    ? result : ChapterProgressionResult.Reject(ChapterProgressionFailureReason.UnsupportedStageType);
            }
            return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageNotFound);
        }

        private void ValidateRequest(int expectedRequestNumber, bool executing)
        {
            if (expectedRequestNumber < 1) throw new ArgumentOutOfRangeException(nameof(expectedRequestNumber));
            if (_buffers == null) throw new InvalidOperationException("스테이지 요청 처리 서비스는 바인딩이 필요합니다.");
            if (executing) throw new InvalidOperationException("같은 도메인의 요청이 이미 실행 중입니다.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageCommandProcessingService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
