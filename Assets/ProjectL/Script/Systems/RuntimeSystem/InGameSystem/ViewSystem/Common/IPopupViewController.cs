namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>팝업 내용의 표시와 입력 수신을 연결합니다.</summary>
    internal interface IPopupViewController
    {
        /// <summary>내용을 표시하고 입력 수신을 연결합니다.</summary>
        void Show();

        /// <summary>입력 수신과 임시 표시를 정리합니다.</summary>
        void Hide(bool explicitClose);
    }
}
