namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    /// <summary>덱 화면의 출전 표시와 입력 연결입니다. 일반 덱 표시 계약과 분리합니다.</summary>
    public interface IDeckCombatDisplay
    {
        bool IsDepartureConfirmationOpen { get; }
        /// <summary>화면에 마련된 덱 칸 수입니다. 빈 칸 출전 안내에 사용합니다.</summary>
        int DeckSlotCount { get; }
        void BindDeparture(IDeckCombatInput input);
        void RenderDeparture(bool visible, bool canStart);
    }

    /// <summary>출전과 변경 확인의 입력 의도입니다.</summary>
    public interface IDeckCombatInput
    {
        void StartCombat();
        void AcceptDeparture();
        void CancelDepartureConfirmation();
    }
}
