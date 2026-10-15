using System;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Map
{
    /// <summary>맵 키별 클리어 여부를 보관하는 진행 기록입니다.</summary>
    [Serializable]
    public readonly struct MapProgressData
    {
        public int MapKey { get; }
        public bool IsClear { get; }

        public MapProgressData(int mapKey, bool isClear = false)
        {
            if (mapKey < 1) throw new ArgumentOutOfRangeException(nameof(mapKey));
            MapKey = mapKey;
            IsClear = isClear;
        }
    }
}
