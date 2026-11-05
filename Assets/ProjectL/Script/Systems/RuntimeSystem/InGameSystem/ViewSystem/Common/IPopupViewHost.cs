namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>부모 연결의 현재 여부와 모달 입력 차단을 제공합니다.</summary>
    internal interface IPopupViewHost
    {
        bool IsCurrent { get; }

        /// <summary>복귀 Layer를 확인한 뒤 호스트 입력 차단을 해제합니다.</summary>
        void SetPopupBlocked(bool blocked);
    }
}
