namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>카메라 핸들을 중복 대여하지 않고 한 정렬 작업의 축 이동만 허용합니다.</summary>
    internal interface IStageMapCameraFocusControl
    {
        bool IsReady { get; }
        StageMapCameraFocusFrame BeginFocus();
        bool IsFocusCurrent(long generation);
        bool TrySetFocusOffset(long generation, float offset);
        void EndFocus(long generation, bool restoreStart);
    }
}
