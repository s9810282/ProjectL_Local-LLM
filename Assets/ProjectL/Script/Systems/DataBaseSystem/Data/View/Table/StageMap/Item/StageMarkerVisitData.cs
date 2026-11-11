using System;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item
{
    /// <summary>건물 앞 여백과 클리어 후 작은 점프의 불변 표시 설정입니다.</summary>
    internal sealed class StageMarkerVisitData
    {
        internal StageMarkerVisitData(float frontClearance, float settleJumpHeight, float settleJumpSeconds)
        {
            FrontClearance = frontClearance;
            SettleJumpHeight = settleJumpHeight;
            SettleJumpSeconds = settleJumpSeconds;
        }

        public float FrontClearance { get; }
        public float SettleJumpHeight { get; }
        public float SettleJumpSeconds { get; }

        internal void Validate()
        {
            if (!float.IsFinite(FrontClearance) || FrontClearance <= 0f ||
                !float.IsFinite(SettleJumpHeight) || SettleJumpHeight < 0f ||
                !float.IsFinite(SettleJumpSeconds) || SettleJumpSeconds <= 0f)
                throw new InvalidOperationException("[StageMarkerVisitData] 앞 여백과 작은 점프 시간은 유한한 양수, 높이는 유한한 0 이상이어야 합니다.");
        }
    }
}
