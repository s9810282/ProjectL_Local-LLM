namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput
{
    /// <summary>로비 시설 선택에 필요한 화면 좌표와 누름 수명을 받습니다.</summary>
    public interface ILobbyInputReceiver
    {
        void OnPointer(float x, float y);

        void OnSelectPressed();

        void OnSelectReleased();

        void OnSelectCanceled();
    }
}
