using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 연결 한 번의 비전투 팝업 되살리기 대기입니다. 맵을 처음 그렸거나 비전투 노드에 착지하면 그 맵을 기억하고,
    /// 맵이 한가해지면 그 방문의 팝업(상점·회복)을 한 번 되살립니다(스테이지 선택 리팩토링 설계 §3.2 Scope 나누기).
    /// </summary>
    /// <remarks>
    /// 노드 종류 이름(<c>StageType</c>)은 읽지 않고, Buffer에 있는 그 방문의 상태로 고릅니다 —
    /// 상점 상태가 있으면 상점, 끝나지 않은 회복 상태가 있으면 회복입니다(검사 후속 정리 설계 §12.6 Q-R9).
    /// 상태가 선택한 노드의 것인지는 각 팝업이 자기 문맥을 잡을 때 확인합니다.
    /// </remarks>
    internal sealed class StageMapViewNoncombatRestore
    {
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly IRunProgressBufferReadSink _runProgress;
        private readonly IRecoveryStateReadSink _recoveryState;
        private readonly StageMapViewPopups _popups;
        private readonly RuntimeFaultBoundary _faults;
        private readonly Func<bool> _canWork;
        private readonly Func<bool> _canOpen;
        private RunId _run;
        private int _map;

        /// <param name="canOpen">맵이 한가하고 선택 화면 부모가 열려 팝업을 열 수 있는지입니다. 기억한 방문이 아직 유효할 때만 묻습니다.</param>
        internal StageMapViewNoncombatRestore(ICurrentProgressBufferReadSink current, IRunProgressBufferReadSink runProgress,
            IRecoveryStateReadSink recoveryState, StageMapViewPopups popups, RuntimeFaultBoundary faults,
            Func<bool> canWork, Func<bool> canOpen)
        {
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _runProgress = runProgress ?? throw new ArgumentNullException(nameof(runProgress));
            _recoveryState = recoveryState ?? throw new ArgumentNullException(nameof(recoveryState));
            _popups = popups ?? throw new ArgumentNullException(nameof(popups));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
            _canOpen = canOpen ?? throw new ArgumentNullException(nameof(canOpen));
        }

        /// <summary>지금 Run의 그 맵을 되살릴 대상으로 기억합니다. 지금 맵이 아니면 기억하지 않습니다.</summary>
        internal void Request(int chapter)
        {
            if (!_canWork() || chapter <= 0 || !_current.HasMap ||
                _current.CurrentMapKey != chapter || _runProgress.RunId.IsEmpty) return;
            _run = _runProgress.RunId;
            _map = chapter;
        }

        internal void Clear()
        {
            _run = default;
            _map = 0;
        }

        /// <summary>기억한 방문이 아직 지금 맵이고 팝업을 열 수 있으면 한 번 되살립니다. Run·맵이 바뀌었으면 잊습니다.</summary>
        internal void Tick()
        {
            if (_map == 0) return;
            if (!_canWork() || _runProgress.RunId != _run || !_current.HasMap || _current.CurrentMapKey != _map)
            {
                Clear();
                return;
            }
            if (!_canOpen()) return;
            int chapter = _map;
            // 외부 화면 콜백 전에 소비합니다. 복원할 방문 사실은 지금의 Buffer에서만 읽습니다.
            Clear();
            if (!_current.HasSelection) return;
            try
            {
                bool hasShop = _current.Shop != null;
                bool hasRecovery = _recoveryState.TryGetCurrent(out RecoveryStateSnapshot state) && !state.IsCompleted;
                if (hasShop && hasRecovery)
                    throw new InvalidOperationException("[StageMap] 상점과 회복 상태가 동시에 활성입니다.");
                if (hasShop) _popups.Shop.TryRestore(chapter);
                else if (hasRecovery) _popups.Recovery.TryRestore(chapter);
            }
            catch (Exception error)
            {
                _faults.Report(error);
            }
        }
    }
}
