using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Terrain.Item
{
    /// <summary>
    /// 토네이도 방향 화살촉 점등의 규칙입니다. 한 주기는 원위치에서 켜짐 → 진행 방향으로 밀리며 어두워짐 → 꺼짐이고 반복합니다.
    /// 화살촉이 켜진 뒤 지난 초를 넣으면 그 순간의 모습을 돌려줍니다. 점등 모양의 유일한 원본입니다.
    /// </summary>
    internal sealed class TornadoArrowPulse
    {
        internal float Seconds { get; }
        internal float LitRatio { get; }
        internal float Distance { get; }
        internal float EndBrightness { get; }

        /// <summary>점등하지 않는지입니다. 이때는 늘 원위치에 원래 밝기로 켜져 있습니다.</summary>
        internal bool IsStill => Seconds <= 0f;

        internal TornadoArrowPulse(float seconds, float litRatio, float distance, float endBrightness)
        {
            if (!float.IsFinite(seconds) || seconds < 0f)
                throw new InvalidOperationException("토네이도 방향 화살촉 점등 주기는 유한한 0 이상 값이어야 합니다.");
            if (!float.IsFinite(litRatio) || litRatio <= 0f || litRatio > 1f)
                throw new InvalidOperationException("토네이도 방향 화살촉 점등 켜짐 비율은 0 초과 1 이하여야 합니다.");
            if (!float.IsFinite(distance) || distance < 0f)
                throw new InvalidOperationException("토네이도 방향 화살촉 밀림 거리는 유한한 0 이상 값이어야 합니다.");
            if (!float.IsFinite(endBrightness) || endBrightness < 0f || endBrightness > 1f)
                throw new InvalidOperationException("토네이도 방향 화살촉 끝 밝기는 0~1이어야 합니다.");
            Seconds = seconds;
            LitRatio = litRatio;
            Distance = distance;
            EndBrightness = endBrightness;
        }

        /// <summary>
        /// 켜진 뒤 <paramref name="elapsed"/>초의 모습입니다. 꺼져 있는 구간이면 false입니다.
        /// 켜진 동안 <paramref name="slide"/>는 0에서 <see cref="Distance"/>까지(처음 빠르고 끝 느림), <paramref name="brightness"/>는 1에서 <see cref="EndBrightness"/>까지입니다.
        /// </summary>
        internal bool TryEvaluate(float elapsed, out float slide, out float brightness)
        {
            slide = 0f;
            brightness = 1f;
            if (IsStill)
                return true;
            float cycle = Mathf.Repeat(elapsed, Seconds) / Seconds;
            if (cycle >= LitRatio)
                return false;
            float progress = cycle / LitRatio;
            float eased = 1f - ((1f - progress) * (1f - progress));
            slide = Distance * eased;
            brightness = Mathf.Lerp(1f, EndBrightness, progress);
            return true;
        }
    }
}
