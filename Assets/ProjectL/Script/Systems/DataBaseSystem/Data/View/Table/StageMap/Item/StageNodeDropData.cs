using System;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMap.Item
{
    /// <summary>챕터 최초 표시의 노드 낙하 설정입니다. 재생 이력은 포함하지 않습니다.</summary>
    internal sealed class StageNodeDropData
    {
        internal StageNodeDropData(bool enabled, float height, float seconds)
        {
            Enabled = enabled;
            Height = height;
            Seconds = seconds;
        }

        public bool Enabled { get; }
        public float Height { get; }
        public float Seconds { get; }

        internal void Validate()
        {
            RequireNonnegativeFinite(Height, nameof(Height));
            RequireNonnegativeFinite(Seconds, nameof(Seconds));
        }

        private static void RequireNonnegativeFinite(float value, string name)
        {
            if (!float.IsFinite(value) || value < 0f)
                throw new InvalidOperationException($"[StageNodeDropData] {name}은 0 이상의 유한한 값이어야 합니다.");
        }
    }
}
