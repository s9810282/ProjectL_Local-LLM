using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>같은 진입의 Core 결과·표시 착지·활성화 수명을 대조해 입력을 정리합니다.</summary>
    internal sealed class StageMapEntryObserver
    {
        private readonly IStageViewBufferReadSink _input;
        private readonly IStageViewBufferWriteSink _writer;
        private readonly IStageCommandResultReadSink _results;
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IStageEntryAttemptRead _attempts;
        private readonly Action<int> _restoreNoncombat;
        private readonly Action<string> _report;
        private readonly Action<ChapterProgressionResult> _rejected;
        private readonly IStageMapEntryPresentation _movement;
        private readonly Func<bool> _canWork;
        internal StageMapEntryObserver(IStageViewBufferReadSink input, IStageViewBufferWriteSink writer,
            IStageCommandResultReadSink results, ICurrentProgressBufferReadSink current,
            IStageEntryAttemptRead attempts, Action<int> restoreNoncombat, Action<string> report,
            Action<ChapterProgressionResult> rejected, IStageMapEntryPresentation movement, Func<bool> canWork)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _attempts = attempts ?? throw new ArgumentNullException(nameof(attempts));
            _restoreNoncombat = restoreNoncombat ?? throw new ArgumentNullException(nameof(restoreNoncombat));
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _rejected = rejected ?? throw new ArgumentNullException(nameof(rejected));
            _movement = movement ?? throw new ArgumentNullException(nameof(movement));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
        }
        internal void Tick()
        {
            if (!_canWork()) return;
            bool hasAttempt = _attempts.TryGetCombatEntry(out int number, out var request);
            if (!_canWork()) return;
            bool prepared = hasAttempt && request != null && request.IsPrepared;
            bool hasInput = _input.TryGetNextCommand(0, out var command);
            if (!_canWork()) return;
            if (prepared && (!hasInput || StageEntryIdentity.Of(command) != StageEntryIdentity.Of(request) ||
                number != command.Number))
                throw ProtocolError("준비 완료된 전투의 대응 입력이 없거나 일치하지 않습니다.");
            if (!hasInput) return;
            bool hasCombat = hasAttempt && request != null && number == command.Number;
            if (hasCombat && request.IsCancelled)
            {
                // 실행 전 취소는 게임 결과가 아닙니다. Flow가 같은 요청의 Job 배출을 계속 기다립니다.
                _writer.RemoveCommandsThrough(command.Number);
                return;
            }
            bool hasReceipt = _results.TryGetMapResult(out var receipt);
            if (!_canWork()) return;
            bool receiptMatches = hasReceipt && StageEntryIdentity.Of(receipt) == StageEntryIdentity.Of(command) &&
                receipt.Result != null;
            int handled = prepared ? _results.HandledMapCommandNumber : 0;
            if (!_canWork()) return;
            if (prepared && (!receiptMatches || handled != command.Number ||
                receipt.Kind != StageEntryCommandKind.Battle || request.Result == null ||
                !request.Result.Accepted || !ReferenceEquals(receipt.Result, request.Result) ||
                request.Result.ChapterKey != command.Data.ChapterKey || request.Result.NodeId != command.Data.NodeId ||
                !request.Result.SelectStage))
                throw ProtocolError("준비 완료된 전투의 처리 번호 또는 수락 결과가 일치하지 않습니다.");
            if (!_canWork() || !receiptMatches) return;
            if (receipt.Kind == StageEntryCommandKind.Battle)
            {
                if (!hasCombat) return;
                if (prepared)
                {
                    RequireCurrentChapter(receipt);
                    if (!_canWork()) return;
                    _movement.PresentAccepted(command, request);
                    return;
                }
                // Ready 게시 뒤에도 활성화 또는 거절까지 같은 입력을 보존합니다.
                if (!request.IsActive && !request.IsRejected) return;
            }
            else if (receipt.Result.Accepted)
            {
                RequireCurrentChapter(receipt);
                if (!_canWork() || !_movement.PresentAccepted(command, null) || !OwnsCommand(command)) return;
            }
            if (!_canWork()) return;
            _writer.RemoveCommandsThrough(command.Number);
            if (!_canWork()) return;
            if (receipt.ChapterKey != _current.CurrentMapKey) return;
            if (!_canWork()) return;
            if (!receipt.Result.Accepted)
            {
                _report($"스테이지 진입 거절: {receipt.Result.FailureReason}, {receipt.Result.NavigationFailureReason}");
                // 정상 거절도 플레이어에게 한 번 알립니다(평가 C-16). 진단 로그는 그대로 둡니다.
                if (_canWork()) _rejected(receipt.Result);
            }
            else if (receipt.Kind == StageEntryCommandKind.Node)
                _restoreNoncombat(receipt.ChapterKey);
        }

        private void RequireCurrentChapter(StageEntryCommandResult receipt)
        {
            if (_current.CurrentMapKey != receipt.ChapterKey && _canWork())
                throw ProtocolError("수락된 이동 도중 현재 챕터가 바뀌었습니다.");
        }

        private bool OwnsCommand(StageViewCommand command)
        {
            if (!_canWork() || !_input.TryGetNextCommand(0, out var current) || !_canWork()) return false;
            return StageEntryIdentity.Of(current) == StageEntryIdentity.Of(command);
        }

        private static InvalidOperationException ProtocolError(string detail) =>
            new($"[StageMap] 노드 진입 표시 계약 오류: {detail}");
    }
}
