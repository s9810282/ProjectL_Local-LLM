using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 현재 맵 입력 표현입니다. 진입 요청 기록만 에디터 로그로 남깁니다.
    /// 호버 이펙트·강조 색은 아직 넣지 않습니다(스테이지 선택 View 계획 §8.3 Q3).
    /// </summary>
    internal sealed class StageMapInteractionLog : IStageMapInteractionFeedback
    {
        public void HoverChanged(int previousNodeId, int currentNodeId)
        {
            // 이펙트 연결 자리입니다. 지금은 표시하지 않습니다.
        }

        public void EntryRequested(int nodeId, int requestNumber)
        {
#if UNITY_EDITOR
            Debug.Log($"[StageMap] 진입 요청 기록: 노드 {nodeId}, 요청 번호 {requestNumber}");
#endif
        }
    }
}
