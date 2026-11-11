namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>선 View가 소유한 표시를 빌려 조작합니다. 이전 대여의 호출은 새 준비에 영향을 주지 않습니다.</summary>
    internal interface IStageLinkDrawTarget
    {
        int FromNodeId { get; }
        int ToNodeId { get; }
        void Prepare();
        void SetProgress(float progress);
        void Restore();
    }
}
