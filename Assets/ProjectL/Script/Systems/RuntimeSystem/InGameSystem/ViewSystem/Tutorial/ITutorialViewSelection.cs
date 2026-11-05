namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 설명 구간 동안 UI 선택을 비우고 끝나면 되돌리는 좁은 계약입니다(튜토리얼 설계 §15.4.3 키보드로 눌리는 버튼).
    /// Unity UI의 Enter·Space는 우리 Layer와 화면 막을 우회해 선택된 버튼을 누르기 때문입니다. 옵션창과 같은 방식입니다.
    /// </summary>
    internal interface ITutorialViewSelection
    {
        /// <summary>지금 선택을 기억하고 비웁니다.</summary>
        void SaveAndClear();

        /// <summary>기억한 선택을 되돌립니다. 그 사이 다른 곳이 선택을 잡았거나 기억한 대상이 꺼졌으면 덮지 않습니다.</summary>
        void Restore();
    }
}
