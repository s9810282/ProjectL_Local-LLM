namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>화면 좌표를 UI가 차지하여 맵 입력을 차단하는지 알려 줍니다.</summary>
    internal interface IStageMapPointerBlocker
    {
        /// <summary>해당 화면 좌표에서 맵 노드와 배경 입력을 막으면 true입니다.</summary>
        bool IsBlocked(float screenX, float screenY);
    }
}
