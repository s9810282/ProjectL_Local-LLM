using System;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item
{
    /// <summary>노드 이동과 맵 복귀 때 사용할 카메라 구도의 불변 설정입니다.</summary>
    internal sealed class StageMapCameraFocusData
    {
        internal StageMapCameraFocusData(bool enabled, float playerViewportY)
        {
            Enabled = enabled;
            PlayerViewportY = playerViewportY;
        }

        internal bool Enabled { get; }
        internal float PlayerViewportY { get; }

        internal void Validate()
        {
            if (!float.IsFinite(PlayerViewportY) || PlayerViewportY <= 0f || PlayerViewportY >= 1f)
                throw new InvalidOperationException("[StageMapCameraFocusData] Player Viewport Y는 유한한 0 초과 1 미만이어야 합니다.");
        }
    }
}
