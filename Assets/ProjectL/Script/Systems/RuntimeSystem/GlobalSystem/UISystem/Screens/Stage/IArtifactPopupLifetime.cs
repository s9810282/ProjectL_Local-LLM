using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>아티팩트 UI의 동기 수명 계약입니다. 열림 상태·닫기 연결·닫기 허용만 둡니다(로비 리팩토링 2차 설계 N-57).</summary>
    public interface IArtifactPopupLifetime
    {
        bool IsOpen { get; }
        event Action<bool> OpenStateChanged;

        /// <summary>닫기 버튼을 받을 수신자를 바꿉니다. null이면 닫기 입력을 버립니다.</summary>
        void BindClose(IArtifactPopupCloseReceiver receiver);

        void SetCanClose(bool canClose);
    }
}
