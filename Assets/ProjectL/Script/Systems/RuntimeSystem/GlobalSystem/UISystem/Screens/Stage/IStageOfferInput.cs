namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>공통 레이아웃이 넘기는 입력 의도입니다. 카드 번호를 무엇으로 읽을지와 제출·판단은 받는 쪽이 합니다.</summary>
    public interface IStageOfferInput
    {
        /// <summary>마지막으로 그린 카드 중 <paramref name="index"/>번을 골랐습니다.</summary>
        void SelectCard(int index);
        void Exit();
    }
}
