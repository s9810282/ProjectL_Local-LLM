using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>현재 화면 좌표의 UI 가림을 검사합니다. 입력 갱신 시점의 오래된 포인터 상태를 사용하지 않습니다.</summary>
    internal sealed class StageMapPointerBlocker : IStageMapPointerBlocker
    {
        private readonly ScreenUIPointerBlocker _blocker = new();

        public bool IsBlocked(float screenX, float screenY)
        {
            return _blocker.IsBlocked(screenX, screenY);
        }
    }
}
