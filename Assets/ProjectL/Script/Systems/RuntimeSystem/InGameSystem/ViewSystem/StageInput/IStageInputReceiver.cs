namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput
{
    /// <summary>
    /// 스테이지 내부 입력을 받는 View의 계약입니다. 입력 Controller는 플레이어가 무엇을 했는지만 전달합니다.
    /// 좌표는 화면 좌표이며, 그 위치가 무엇을 뜻하는지는 받는 View가 정합니다.
    /// </summary>
    public interface IStageInputReceiver
    {
        void PointerMoved(float screenX, float screenY);

        /// <summary>선택 버튼을 누른 순간입니다. 드래그의 시작이 될 수 있습니다.</summary>
        void SelectPressed(float screenX, float screenY);

        /// <summary>
        /// 선택 버튼을 정상적으로 뗀 순간입니다. 뗀 곳이 기준입니다.
        /// 선택 확정의 후보일 뿐이며, 명령으로 기록할지는 View가 현재 상태를 보고 정합니다.
        /// </summary>
        void SelectReleased(float screenX, float screenY);

        /// <summary>
        /// 진행 중인 선택이 강제로 끝났습니다(Layer 전환·해제, 종료, 연결 해제 등).
        /// 드래그·선택 표시만 정리하고 배치·이동 같은 명령은 기록하지 않습니다.
        /// </summary>
        void SelectCanceled();

        /// <summary>사용자가 취소 키를 눌렀습니다.</summary>
        void Cancel();

        /// <summary>배치 단계: 전투 시작을 요청했습니다.</summary>
        void EnterCombatRequested();


        /// <summary>전투 단계: 현재 유닛의 턴 종료를 요청했습니다.</summary>
        void EndTurnRequested();

        /// <summary>전투 단계: 공격 대상 선택으로 전환을 요청했습니다.</summary>
        void AttackModeRequested();

        /// <summary>전투 단계: 체크포인트 복귀를 요청했습니다.</summary>
        void RequestReturn();

        /// <summary>
        /// 배치·전투 단계: 카메라 이동 키의 방향이 바뀌었습니다(x = 오른쪽, y = 위, 각 −1~1). 모두 떼면 (0, 0)이 옵니다(카메라 조작 설계 CC2).
        /// 누르고 있는 동안 반복해서 오지 않으므로 받는 View가 마지막 값을 기억합니다.
        /// </summary>
        void CameraPanChanged(float x, float y);

        /// <summary>배치·전투 단계: 마우스 휠을 굴렸습니다. 위로 굴리면 양수입니다. 값의 크기는 기기마다 다르므로 부호만 봅니다(카메라 조작 계획 P1).</summary>
        void CameraZoomScrolled(float delta);
    }
}
