namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TutorialInput
{
    /// <summary>튜토리얼 "다음"의 누름 수명을 받습니다. 정상 뗌과, 누른 채 맵이 꺼져 생기는 취소를 나눠 받습니다.</summary>
    internal interface ITutorialInputReceiver
    {
        void OnNextPressed();

        void OnNextReleased();

        void OnNextCanceled();
    }
}
