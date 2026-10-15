namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput
{
    /// <summary>맵 View가 받는 입력입니다. 노드 판별과 명령 기록은 수신 View가 담당합니다.</summary>
    public interface IStageSelectionInputReceiver
    {
        void PointerMoved(float screenX, float screenY);
        void SelectPressed(float screenX, float screenY);
        void SelectReleased(float screenX, float screenY);

        /// <summary>누르던 선택의 강제 종료입니다. 선택 확정으로 처리하지 않습니다.</summary>
        void SelectCanceled();

        void ConfirmRequested();

        /// <summary>사용자가 취소 액션을 실행했습니다. 강제 종료와 구분합니다.</summary>
        void CancelRequested();
    }
}
