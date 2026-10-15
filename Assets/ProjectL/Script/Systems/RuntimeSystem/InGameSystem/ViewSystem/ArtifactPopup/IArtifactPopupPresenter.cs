namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>
    /// 아티팩트 팝업의 3D 보드를 그리는 쪽의 좁은 계약입니다. 받은 값으로 그리기만 하고 DB·Buffer·Core를 읽지 않습니다.
    /// </summary>
    /// <remarks>구현: <see cref="ArtifactPopupView"/>(씬), 테스트 가짜. UI 목록·문구는 <see cref="IArtifactPopupScreen"/>이 맡습니다.</remarks>
    internal interface IArtifactPopupPresenter
    {
        /// <summary>3D 보드와 배경을 켭니다. SceneRoot는 항상 켜져 있습니다(D13).</summary>
        void Show();

        /// <summary>그린 것을 모두 풀에 돌려주고 3D 보드와 배경을 끕니다. 여러 번 불러도 안전합니다.</summary>
        void Hide();

        /// <summary>이전에 그린 것을 돌려주고 보드·배치 모델을 다시 그립니다. 도중 실패하면 빌린 것을 모두 돌려주고 예외를 전파합니다.</summary>
        void Render(ArtifactPopupBoard board);

        /// <summary>그린 보드·모델을 모두 돌려줍니다(초기화 전 표시 등).</summary>
        void ClearBoard();
    }
}
