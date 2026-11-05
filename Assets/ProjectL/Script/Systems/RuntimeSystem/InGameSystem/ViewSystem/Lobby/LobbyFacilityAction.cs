using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 팝업 컨트롤러·출발 의도처럼 이미 있는 "열 수 있나 / 열기" 한 쌍을 시설 실행 항목으로 감쌉니다.
    /// 아티팩트 팝업이 로비 계약에 기대지 않게 로비 쪽에서 감쌉니다.
    /// </summary>
    internal sealed class LobbyFacilityAction : ILobbyFacilityAction
    {
        private readonly Func<bool> _canOpen;
        private readonly Action _open;

        internal LobbyFacilityAction(Func<bool> canOpen, Action open)
        {
            _canOpen = canOpen ?? throw new ArgumentNullException(nameof(canOpen));
            _open = open ?? throw new ArgumentNullException(nameof(open));
        }

        public bool CanOpen => _canOpen();

        public void Open() => _open();
    }
}
