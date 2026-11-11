using System;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item
{
    /// <summary>맵 진입 연출의 입력 설정입니다. 진행 상태와 파생 시각은 포함하지 않습니다.</summary>
    internal sealed class StageMapIntroData
    {
        internal StageMapIntroData(bool enabled, float startHoldSeconds, float revealSeconds,
            float bossHoldSeconds, float returnSeconds)
        {
            Enabled = enabled;
            StartHoldSeconds = startHoldSeconds;
            RevealSeconds = revealSeconds;
            BossHoldSeconds = bossHoldSeconds;
            ReturnSeconds = returnSeconds;
        }

        public bool Enabled { get; }
        public float StartHoldSeconds { get; }
        public float RevealSeconds { get; }
        public float BossHoldSeconds { get; }
        public float ReturnSeconds { get; }

        internal void Validate()
        {
            RequireTime(StartHoldSeconds, nameof(StartHoldSeconds), false);
            RequireTime(RevealSeconds, nameof(RevealSeconds), true);
            RequireTime(BossHoldSeconds, nameof(BossHoldSeconds), false);
            RequireTime(ReturnSeconds, nameof(ReturnSeconds), false);
        }

        private static void RequireTime(float value, string name, bool positive)
        {
            if (!float.IsFinite(value) || value < 0f || (positive && value == 0f))
                throw new InvalidOperationException($"[StageMapIntroData] {name}은 유한한 {(positive ? "양수" : "0 이상 값")}여야 합니다.");
        }
    }
}
