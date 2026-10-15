using System;

namespace ProjectL.Systems.RuntimeSystem.Common
{
    /// <summary>
    /// 런타임 하나의 최초 오류와 정지 여부를 소유하는 공유 경계입니다(F1, 설계 W1·W17 §10.1).
    /// Bootstrap이 만들어 소유하고, View와 CombatFlow는 같은 인스턴스를 주입받아 읽습니다.
    /// </summary>
    /// <remarks>
    /// 게임 상태를 저장하지 않으며 View·LifeCycle·Unity 구체 타입을 참조하지 않습니다.
    /// 로그는 생성 때 받은 콜백으로만 남기고, 입력 정리는 연결된 콜백 하나로만 요청합니다.
    /// </remarks>
    internal sealed class RuntimeFaultBoundary
    {
        private readonly Action<Exception> _log;
        private Action _stopInput;
        private bool _isStoppingInput;

        internal RuntimeFaultBoundary(Action<Exception> log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        /// <summary>최초 오류가 보고됐는지 여부입니다. 한 번 true가 되면 되돌리지 않습니다.</summary>
        internal bool IsFaulted { get; private set; }

        /// <summary>처음 보고된 원인입니다. 정리 실패는 여기에 섞지 않습니다.</summary>
        internal Exception FirstError { get; private set; }

        /// <summary>오류·종료 때 실행할 입력 정리 콜백을 연결합니다. 런타임당 하나만 연결합니다.</summary>
        internal void ConnectStopInput(Action stopInput)
        {
            if (stopInput == null)
                throw new ArgumentNullException(nameof(stopInput));
            if (_stopInput != null && _stopInput != stopInput)
                throw new InvalidOperationException("[RuntimeFaultBoundary] Input cleanup is already connected.");

            _stopInput = stopInput;
        }

        /// <summary>
        /// 연결한 쪽이 Shutdown에서 자기 정리 콜백 참조를 놓습니다. 연결된 콜백과 다르면 무시합니다.
        /// </summary>
        internal void DisconnectStopInput(Action stopInput)
        {
            if (stopInput == null)
                throw new ArgumentNullException(nameof(stopInput));
            if (_stopInput == stopInput)
                _stopInput = null;
        }

        /// <summary>
        /// 최초 오류를 고정하고 입력을 정리한 뒤 원인과 정리 실패를 한 번 보고합니다.
        /// 이미 오류 상태이면(정리 중 재진입 포함) 아무것도 하지 않습니다. 복구는 새 런타임으로만 합니다.
        /// </summary>
        internal void Report(Exception error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));
            if (IsFaulted)
                return;

            // 정지 상태를 먼저 고정해야 정리 중에 들어온 입력·완료·전환 요청을 거부할 수 있다.
            IsFaulted = true;
            FirstError = error;

            Exception cleanupFailure = TryStopInput();
            _log(cleanupFailure == null
                ? error
                : new AggregateException(
                    "[RuntimeFaultBoundary] Runtime stopped, and input cleanup also failed.",
                    error,
                    cleanupFailure));
        }

        /// <summary>
        /// 정상 종료 때 런타임 Shutdown 전에 입력부터 정리합니다. 오류 상태는 바꾸지 않습니다.
        /// 실패는 호출자가 종료 실패로 기록하도록 반환합니다.
        /// </summary>
        internal Exception StopInputForShutdown()
        {
            return TryStopInput();
        }

        private Exception TryStopInput()
        {
            if (_stopInput == null || _isStoppingInput)
                return null;

            _isStoppingInput = true;
            try
            {
                _stopInput();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
            finally
            {
                _isStoppingInput = false;
            }
        }
    }
}
