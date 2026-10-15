namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput
{
    /// <summary>맵 View가 받는 입력입니다. 아티팩트 판별과 명령 기록은 수신 View가 담당합니다.</summary>
    public interface IArtifactInputReceiver
    {
        void PointerMoved(float screenX, float screenY);
        void SelectPressed(float screenX, float screenY);
        void SelectReleased(float screenX, float screenY);

        /// <summary>누르던 선택의 강제 종료입니다. 선택 확정으로 처리하지 않습니다.</summary>
        void SelectCanceled();

        /// <summary>반시계 방향 90도 회전 요청입니다. 실제 회전은 View가 처리합니다.</summary>
        void RotateLeft();

        /// <summary>시계 방향 90도 회전 요청입니다. 실제 회전은 View가 처리합니다.</summary>
        void RotateRight();

        /// <summary>사용자의 우클릭 액션입니다. 팝업은 배치물 회수로 처리하며 강제 종료와 구분합니다.</summary>
        void Cancel();

    }
}
