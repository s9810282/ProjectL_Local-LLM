using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public enum StageSelectionFailureReason
    {
        None = 0,
        NodeNotFound = 1, // 요청한 노드 아이디가 현재 맵에 존재하지 않음
        NotConnected, // 노드는 존재하는데 현재 노드의 연결된 다음 노드 아이디가 없음 (고립됨)
        FinalNodeReached, // 이미 보스 노드까지 선택한 상태라 더 진행이 안됨
    }
}