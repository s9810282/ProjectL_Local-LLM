using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem
{
    /// <summary>조립 계층이 소유하는 단일 입력 정리 콜백입니다. 한 화면의 실패가 다음 정리를 막지 않습니다.</summary>
    internal sealed class InGameViewFaultCleanup
    {
        private RuntimeFaultBoundary _faults;
        private Action _combat;
        private Action _stageMap;
        private Action _lobby;
        private Action _recovery;
        private Action _tutorial;
        private bool _isStopping;

        internal InGameViewFaultCleanup(RuntimeFaultBoundary faults, Action combat, Action stageMap,
            Action lobby = null, Action recovery = null, Action tutorial = null)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _stageMap = stageMap ?? throw new ArgumentNullException(nameof(stageMap));
            _lobby = lobby;
            _recovery = recovery;
            _tutorial = tutorial;
            _faults.ConnectStopInput(StopInput);
        }

        internal void StopInput()
        {
            if (_faults == null || _isStopping) return;
            _isStopping = true;
            // 정리 중 종료가 연결을 해제해도 이번 정리의 나머지 화면까지 호출합니다.
            var combat = _combat;
            var stageMap = _stageMap;
            var lobby = _lobby;
            var recovery = _recovery;
            var tutorial = _tutorial;
            List<Exception> errors = null;
            try
            {
                if (combat != null)
                {
                    CleanupErrors.Try(combat, ref errors);
                }
                if (stageMap != null)
                {
                    CleanupErrors.Try(stageMap, ref errors);
                }
                if (lobby != null)
                {
                    CleanupErrors.Try(lobby, ref errors);
                }
                if (recovery != null)
                {
                    CleanupErrors.Try(recovery, ref errors);
                }
                if (tutorial != null)
                {
                    CleanupErrors.Try(tutorial, ref errors);
                }
            }
            finally
            {
                _isStopping = false;
            }
            CleanupErrors.ThrowIfAny(errors, "[InGameView] 입력 정리 중 실패했습니다.");
        }

        internal void Disconnect()
        {
            _faults?.DisconnectStopInput(StopInput);
            _faults = null;
            _combat = _stageMap = _lobby = null;
            _recovery = null;
            _tutorial = null;
        }
    }
}
