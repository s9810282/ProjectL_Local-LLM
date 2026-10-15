using System;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>맵의 입력 기록을 기존 전투 진입 서비스에 연결하고 요청 수명을 관리합니다.</summary>
    internal sealed class StageMapEntryFlow
    {
        private readonly StageMapViewReader _reader;
        private readonly IStageViewBufferReadSink _requests;
        private readonly IStageViewBufferWriteSink _writer;
        private readonly ICombatEntryService _combat;
        private readonly Action<string> _report;
        private CombatEntryRequest _pending;
        private int _commandNumber;

        internal StageMapEntryFlow(StageMapViewReader reader, IStageViewBufferReadSink requests,
            IStageViewBufferWriteSink writer, ICombatEntryService combat, Action<string> report)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _report = report ?? throw new ArgumentNullException(nameof(report));
        }

        internal void Tick(bool canSubmit)
        {
            if (_pending != null)
            {
                switch (_pending.Status)
                {
                    case CombatEntryStatus.Rejected:
                        _report($"전투 진입 거절: {_pending.Result.FailureReason}, {_pending.Result.NavigationFailureReason}");
                        Finish();
                        return;
                    case CombatEntryStatus.Cancelled:
                    case CombatEntryStatus.Closed:
                    case CombatEntryStatus.Faulted:
                    case CombatEntryStatus.Active:
                        Finish();
                        return;
                    default:
                        return;
                }
            }

            if (!canSubmit || !_requests.TryGetNextCommand(0, out StageViewCommand command) ||
                !_reader.TryRead(out int mapKey, out var nodes))
                return;

            // 종류の振り分けのみ行い、到達可否・クリア状態・戦闘準備はCoreに委ねます。
            foreach (var node in nodes)
            {
                if (node.NodeId != command.Data.NodeId.Value) continue;
                if (node.StageKey == 0 || node.StageType != StageType.Combat)
                {
                    _writer.RemoveCommand(command.Number);
                    _report("이 노드는 전투 진입 대상이 아닙니다.");
                    return;
                }

                _commandNumber = command.Number;
                _pending = _combat.RequestEntry(mapKey, command.Data.NodeId);
                return;
            }
            _writer.RemoveCommand(command.Number);
            _report("선택한 노드가 현재 맵에 없습니다.");
        }

        /// <summary>씬 이탈 때 실행 전 요청은 취소하고 View가 기록한 요청만 정리합니다.</summary>
        internal void Detach()
        {
            if (_pending != null) _combat.CancelEntry(_pending);
            Finish();
            while (_requests.TryGetNextCommand(0, out StageViewCommand command))
                _writer.RemoveCommand(command.Number);
        }

        private void Finish()
        {
            if (_commandNumber != 0) _writer.RemoveCommand(_commandNumber);
            _commandNumber = 0;
            _pending = null;
        }
    }
}
