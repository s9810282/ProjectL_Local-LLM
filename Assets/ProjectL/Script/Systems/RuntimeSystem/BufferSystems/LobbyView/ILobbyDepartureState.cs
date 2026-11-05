using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView
{
    /// <summary>
    /// View가 보는 출발 요청의 읽기 계약입니다. 질문만 있고 단계 변경자는 없습니다.
    /// 단계는 실행 계층 Job만 구체 요청(<see cref="LobbyDepartureRequest"/>)으로 바꿉니다(로비 리팩토링 2차 설계 N-75).
    /// </summary>
    public interface ILobbyDepartureState
    {
        int Number { get; }

        /// <summary>처리 중이라 출발·시설 입력을 막아야 하는지입니다.</summary>
        bool IsPending { get; }

        /// <summary>거절·취소로 끝나 다시 제출할 수 있는지입니다.</summary>
        bool CanResubmit { get; }

        /// <summary>새 Run 호출이 정상 거절됐는지입니다. 사유는 <see cref="Result"/>에 있습니다.</summary>
        bool IsRejected { get; }

        RosterAcquireResult Result { get; }
    }
}
