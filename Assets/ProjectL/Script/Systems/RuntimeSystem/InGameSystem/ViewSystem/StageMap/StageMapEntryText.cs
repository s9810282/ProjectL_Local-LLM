using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 맵 진입 거절 안내 문구의 원본입니다(스테이지 선택 리팩토링 설계 §3.1 C-16, 로비 <c>LobbyDepartureText</c>와 같은 꼴).
    /// 기획 확인 전 임시 문구입니다. 진단 로그는 따로 남습니다.
    /// </summary>
    internal static class StageMapEntryText
    {
        /// <summary>출전 쪽이 연결 밖 전투 노드를 막을 때도 같은 문구를 씁니다.</summary>
        internal const string NotConnected = "연결된 노드로만 이동할 수 있습니다.";
        private const string AlreadyCleared = "이미 지나온 노드입니다.";
        private const string Fallback = "이동할 수 없습니다.";

        internal static string Rejected(ChapterProgressionResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            return result.FailureReason switch
            {
                ChapterProgressionFailureReason.NavigationRejected => Navigation(result.NavigationFailureReason),
                ChapterProgressionFailureReason.StageAlreadyCleared => AlreadyCleared,
                ChapterProgressionFailureReason.PendingTraitGrowth => "증강을 먼저 선택해야 합니다.",
                ChapterProgressionFailureReason.RunEnded => "종료된 Run에서는 이동할 수 없습니다.",
                ChapterProgressionFailureReason.EnhancementShopUnavailable => "아직 이용할 수 없는 시설입니다.",
                _ => Fallback
            };
        }

        private static string Navigation(ChapterSelectionFailureReason reason) => reason switch
        {
            ChapterSelectionFailureReason.NotConnected => NotConnected,
            ChapterSelectionFailureReason.CurrentStageUncleared => "현재 노드를 먼저 마쳐야 합니다.",
            ChapterSelectionFailureReason.AlreadyCleared => AlreadyCleared,
            _ => Fallback
        };
    }
}
