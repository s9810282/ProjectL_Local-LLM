using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 연결 한 번의 출발 요청을 소유합니다. 제출·취소와 "출발이 입력을 붙잡는지"를 답하고, 거절은 공용 안내로 한 번 알립니다.
    /// 처리 단계는 저장하지 않고 요청(Buffer)에서 읽습니다(검사 후속 정리 설계 §12.2 B4, TP-R7).
    /// </summary>
    internal sealed class LobbyDepartureFlow
    {
        // 거절 안내 시간(초)입니다. 기획 확인 전 임시 값입니다(검사 후속 정리 계획 TP-R7).
        private const float RejectionNoticeSeconds = 3f;
        // 출발 창구는 읽기 계약만 주고받습니다. 단계 변경은 실행 계층 Job만 합니다(로비 리팩토링 2차 설계 N-75).
        private readonly ILobbyViewBufferWriteSink _write;
        private readonly IUITextPresentation _text;
        private readonly Func<bool> _canWork;
        private ILobbyDepartureState _owned;

        internal LobbyDepartureFlow(ILobbyViewBufferWriteSink write, IUITextPresentation text, Func<bool> canWork)
        {
            _write = write ?? throw new ArgumentNullException(nameof(write));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _canWork = canWork ?? throw new ArgumentNullException(nameof(canWork));
        }

        /// <summary>
        /// 출발 처리가 입력을 붙잡고 있는지입니다. 처리 중인 요청이 있거나, 이 연결의 요청이 거절·취소로 끝나지 않았으면 참입니다
        /// (성공해 Stage로 넘어가는 동안과 오류로 끝난 뒤에도 로비를 떠날 때까지 막습니다).
        /// </summary>
        internal bool IsHolding => _write.Current?.IsPending == true || (_owned != null && !_owned.CanResubmit);

        /// <summary>출발을 제출합니다. 열어도 되는지는 부르는 쪽(시설 실행)이 이미 확인했습니다.</summary>
        internal void Submit()
        {
            ILobbyDepartureState request = _write.Submit();
            _owned = request;
            // 반환 전에 교체·폐기돼도 반환된 자기 요청 참조만 취소합니다.
            if (!_canWork()) _write.Cancel(request);
        }

        /// <summary>이 연결의 요청이 거절됐으면 사유를 한 번 알립니다. 알림이 없는 Buffer 결과라 Tick마다 이 값 하나만 봅니다.</summary>
        internal void Tick()
        {
            ILobbyDepartureState owned = _owned;
            if (owned == null || !owned.IsRejected) return;
            _owned = null;
            _text.ShowNotice(LobbyDepartureText.Rejected(owned.Result), RejectionNoticeSeconds);
        }

        internal void CancelOwnedRequest()
        {
            ILobbyDepartureState request = _owned;
            _owned = null;
            if (request != null) _write.Cancel(request);
        }
    }
}
