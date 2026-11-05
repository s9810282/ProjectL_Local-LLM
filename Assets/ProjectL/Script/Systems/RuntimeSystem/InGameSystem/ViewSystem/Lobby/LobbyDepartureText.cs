using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>로비 출발 거절 사유의 안내 문구 원본입니다(검사 후속 정리 설계 Q-R6, 문구는 기획 확인 전 임시).</summary>
    internal static class LobbyDepartureText
    {
        internal static string Rejected(RosterAcquireResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            return result.FailureReason switch
            {
                RosterAcquireFailureReason.InvalidRequest => "출발 요청이 올바르지 않습니다.",
                RosterAcquireFailureReason.InvalidDefinition => "출발 데이터가 올바르지 않습니다.",
                RosterAcquireFailureReason.InvalidMainState => "메인 캐릭터 상태를 확인할 수 없습니다.",
                RosterAcquireFailureReason.IdentityOverflow => "캐릭터 등록 번호를 발급할 수 없습니다.",
                RosterAcquireFailureReason.RunEnded => "종료된 Run에서는 출발할 수 없습니다.",
                RosterAcquireFailureReason.OwnershipLimitReached => "보유 한도에 도달했습니다.",
                RosterAcquireFailureReason.InvalidInitialDeck => "시작 덱 데이터가 올바르지 않습니다.",
                _ => "출발을 완료하지 못했습니다."
            };
        }
    }
}
