using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>목적지에서 출발 노드를 향하는 연결선 위의 건물 앞 바닥점을 계산합니다.</summary>
    internal sealed class StageMarkerPlacement
    {
        private readonly float _frontClearance;

        internal StageMarkerPlacement(float frontClearance)
        {
            _frontClearance = frontClearance;
        }

        internal Vector3 GetFront(Transform mapRoot, Vector3 centerWorld, Vector3 originWorld,
            IReadOnlyList<Vector3> cornersInMap) =>
            CalculateFront(mapRoot, centerWorld, cornersInMap, originWorld, _frontClearance);

        /// <summary>클릭 상자 모서리를 직접 투영하며 결과는 맵 로컬 바닥 좌표입니다.</summary>
        internal static Vector3 CalculateFront(Transform mapRoot, Vector3 centerWorld,
            IReadOnlyList<Vector3> buildingCornersInMap, Vector3 originWorld, float clearance)
        {
            ValidateMapRoot(mapRoot);
            if (buildingCornersInMap == null) throw new ArgumentNullException(nameof(buildingCornersInMap));
            if (buildingCornersInMap.Count != 8 || !Finite(centerWorld) || !Finite(originWorld) ||
                !float.IsFinite(clearance) || clearance <= 0f)
                throw new InvalidOperationException("[StageMarkerPlacement] 유한한 출발/목적지 바닥점·양수 여백·건물 모서리 8개가 필요합니다.");

            Vector3 center = mapRoot.InverseTransformPoint(centerWorld);
            Vector3 direction = mapRoot.InverseTransformPoint(originWorld) - center;
            direction.y = 0f;
            float length = direction.magnitude;
            if (!float.IsFinite(length) || length < 0.001f)
                throw new InvalidOperationException("[StageMarkerPlacement] 출발 노드와 목적지 사이에 유효한 XZ 연결선이 필요합니다.");
            direction /= length;
            float reach = 0f;
            foreach (Vector3 corner in buildingCornersInMap)
            {
                if (!Finite(corner))
                    throw new InvalidOperationException("[StageMarkerPlacement] 건물 모서리는 유한해야 합니다.");
                reach = Mathf.Max(reach, Vector3.Dot(corner - center, direction));
            }
            float distance = reach + clearance;
            if (!float.IsFinite(distance) || distance >= length)
                throw new InvalidOperationException("[StageMarkerPlacement] 건물 크기와 앞 여백을 놓을 연결선 길이가 부족합니다.");
            Vector3 destination = center + direction * distance;
            if (!Finite(destination))
                throw new InvalidOperationException("[StageMarkerPlacement] 앞쪽 바닥점 계산 결과가 유한하지 않습니다.");
            return destination;
        }

        /// <summary>현재 효과 크기 계약은 단위 스케일 맵과 부모만 지원합니다.</summary>
        internal static void ValidateMapRoot(Transform mapRoot)
        {
            if (mapRoot == null) throw new ArgumentNullException(nameof(mapRoot));
            for (Transform current = mapRoot; current != null; current = current.parent)
            {
                if (!Finite(current.localScale) || Vector3.Distance(current.localScale, Vector3.one) > 0.001f ||
                    !Finite(current.position))
                    throw new InvalidOperationException($"[StageMarkerPlacement] 맵과 부모는 유한한 위치와 단위 스케일이어야 합니다: {current.name}");
            }
        }

        private static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
