namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>아티팩트 UI의 닫기 버튼을 받는 좁은 수신 계약입니다(로비 리팩토링 2차 설계 N-57).</summary>
    public interface IArtifactPopupCloseReceiver
    {
        /// <summary>열려 있고 닫을 수 있을 때 닫기 버튼이 눌렸습니다.</summary>
        void CloseClicked();
    }
}
