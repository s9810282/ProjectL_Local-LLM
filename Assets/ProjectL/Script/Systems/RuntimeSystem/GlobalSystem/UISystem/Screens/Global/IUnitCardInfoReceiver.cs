namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>카드의 상세 입력과 수명 종료를 공용 패널 한 곳으로 전달합니다.</summary>
    public interface IUnitCardInfoReceiver
    {
        void Toggle(UnitCardUI source);
        void Forget(UnitCardUI source);
    }
}
