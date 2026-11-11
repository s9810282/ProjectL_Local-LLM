namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion
{
    /// <summary>한 Tick에서 읽은 진행 사실과 외부 재생 조건입니다. 완료 사실을 보관하지 않습니다.</summary>
    internal readonly struct StageNodeClearFrame
    {
        internal StageNodeClearFrame(StageNodeClearKey key, bool isCleared, long preparationVersion,
            bool isPreparedContext, bool canPresent)
        {
            Key = key;
            IsCleared = isCleared;
            PreparationVersion = preparationVersion;
            IsPreparedContext = isPreparedContext;
            CanPresent = canPresent;
        }

        internal StageNodeClearKey Key { get; }
        internal bool IsCleared { get; }
        internal long PreparationVersion { get; }
        internal bool IsPreparedContext { get; }
        internal bool CanPresent { get; }
    }
}
