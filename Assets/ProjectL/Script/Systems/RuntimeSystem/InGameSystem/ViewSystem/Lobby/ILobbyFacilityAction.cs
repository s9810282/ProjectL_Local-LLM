namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 시설 하나의 실행 항목입니다. 시설 실행 표가 종류마다 이 항목 하나를 받습니다(로비 리팩토링 2차 설계 M-13·N-54).
    /// </summary>
    internal interface ILobbyFacilityAction
    {
        /// <summary>지금 이 시설을 열 수 있는지입니다. 시설 실행 판정(막힘·Layer)은 실행 표가 따로 봅니다.</summary>
        bool CanOpen { get; }

        /// <summary>시설을 엽니다. 실행 표가 <see cref="CanOpen"/>을 확인한 뒤에만 부릅니다.</summary>
        void Open();
    }
}
