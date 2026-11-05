using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>같은 Depth의 가로 중심을 맵 중심선에 맞추는 로컬 좌표를 계산합니다.</summary>
    internal static class StageMapNodeLayout
    {
        internal static Vector3[] CalculateLocalPositions(
            IReadOnlyList<StageProgressData> nodes, float widthSpacing, float depthSpacing)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (!float.IsFinite(widthSpacing) || widthSpacing <= 0f ||
                !float.IsFinite(depthSpacing) || depthSpacing <= 0f)
                throw new InvalidOperationException("[StageMapNodeLayout] 노드 간격은 유한한 양수여야 합니다.");

            if (nodes.Count == 0) return Array.Empty<Vector3>();

            var ranges = new Dictionary<int, (int Min, int Max)>();
            for (int index = 0; index < nodes.Count; index++)
            {
                StageProgressData node = nodes[index];
                if (ranges.TryGetValue(node.Depth, out var range))
                {
                    ranges[node.Depth] = (Math.Min(range.Min, node.WidthIndex), Math.Max(range.Max, node.WidthIndex));
                }
                else
                {
                    ranges.Add(node.Depth, (node.WidthIndex, node.WidthIndex));
                }
            }

            var positions = new Vector3[nodes.Count];
            for (int index = 0; index < nodes.Count; index++)
            {
                StageProgressData node = nodes[index];
                var range = ranges[node.Depth];
                // 정수 합산·나눗셈과 큰 인덱스의 float 정밀도 손실을 피합니다.
                double center = ((double)range.Min + range.Max) / 2d;
                float x = (float)((node.WidthIndex - center) * widthSpacing);
                float z = (float)((double)node.Depth * depthSpacing);
                if (!float.IsFinite(x) || !float.IsFinite(z))
                    throw new InvalidOperationException("[StageMapNodeLayout] 노드 표시 좌표가 유한하지 않습니다.");
                positions[index] = new Vector3(x, 0f, z);
            }
            return positions;
        }
    }
}
