using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TutorialInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 튜토리얼 "다음"을 Continue 제출로 바꾸는 입력 어댑터입니다(튜토리얼 설계 §6.1·§10·§15.4.3).
    /// </summary>
    /// <remarks>
    /// 튜토리얼 Layer에서 누른 경우만 유효로 보고(Layer 밖의 입력은 라우팅되지 않음), 누른 순간 읽은 세션·revision을 뗀 순간 제출합니다.
    /// 누른 순간 확인을 받을 준비가 된 설명(<c>CanContinue</c>)일 때만 누름으로 봅니다(설계 §6). 표시·전환 중의 누름은 버립니다.
    /// Layer 전환으로 오는 취소는 뗌으로 보지 않습니다. 제출만 하고 Job 등록은 하지 않습니다(실행 소유자 Tick이 등록, 설계 §9.3).
    /// 세션 상태나 결과를 직접 바꾸지 않습니다. 예외는 잡아 오류 경계에 보고합니다(<c>CombatInputReceiver</c>와 같은 형태).
    /// </remarks>
    internal sealed class TutorialViewInputAdapter : ITutorialInputReceiver
    {
        private readonly ITutorialStateBufferReadSink _state;
        private readonly ITutorialRequestBufferWriteSink _requests;
        private readonly RuntimeFaultBoundary _faults;
        private bool _isPressed;
        private int _sessionId;
        private int _stepRevision;

        internal TutorialViewInputAdapter(ITutorialStateBufferReadSink state, ITutorialRequestBufferWriteSink requests,
            RuntimeFaultBoundary faults)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _requests = requests ?? throw new ArgumentNullException(nameof(requests));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        public void OnNextPressed()
        {
            _isPressed = false;
            if (_faults.IsFaulted)
                return;

            try
            {
                int sessionId = _state.Context.ActiveSessionId;
                if (sessionId == 0 || !_state.TryGetSession(sessionId, out TutorialSessionSnapshot session)
                    || !session.CanContinue)
                    return;

                _sessionId = sessionId;
                _stepRevision = session.StepRevision;
                _isPressed = true;
            }
            catch (Exception exception)
            {
                _faults.Report(exception);
            }
        }

        public void OnNextReleased()
        {
            if (!_isPressed)
                return;
            _isPressed = false;
            if (_faults.IsFaulted)
                return;

            try
            {
                // 늦은 확인이면 결과가 NotReady·StepChanged로 거절된다(설계 §6.1). 런타임 정지 뒤의 실패는 버린다.
                _requests.TrySubmit(TutorialCommandData.Continue(_sessionId, _stepRevision), out _, out _);
            }
            catch (Exception exception)
            {
                _faults.Report(exception);
            }
        }

        public void OnNextCanceled()
        {
            _isPressed = false;
        }
    }
}
