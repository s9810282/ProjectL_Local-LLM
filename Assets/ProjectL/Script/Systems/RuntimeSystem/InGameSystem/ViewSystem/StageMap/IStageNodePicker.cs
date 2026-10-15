namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 화면 좌표가 가리키는 맵 노드를 알려 주는 좁은 계약입니다.
    /// <see cref="StageMapInteraction"/>이 카메라·Transform을 모르고 순수 C#으로 남도록 씁니다.
    /// </summary>
    /// <remarks>구현: <see cref="StageNodePicker"/>(씬 카메라), 테스트 가짜.</remarks>
    internal interface IStageNodePicker
    {
        /// <summary>화면 좌표 아래의 노드 ID입니다. 노드가 없으면 false입니다.</summary>
        bool TryPickNode(float screenX, float screenY, out int nodeId);
    }
}
