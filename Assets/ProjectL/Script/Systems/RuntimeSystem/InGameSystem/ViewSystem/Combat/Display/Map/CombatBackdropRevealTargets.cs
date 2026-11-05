using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry;
using ProjectL.Tool.Runtime;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>배경에 명시한 구역을 가장 가까운 생성 타일의 등장 시점에 연결합니다. 자세와 표시는 변경하지 않습니다.</summary>
    internal static class CombatBackdropRevealTargets
    {
        internal static IReadOnlyList<CombatBoardRevealTarget> Read(Transform backdrop, Transform field,
            IReadOnlyList<Transform> tiles)
        {
            if (backdrop == null) throw new ArgumentNullException(nameof(backdrop));
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (tiles == null) throw new ArgumentNullException(nameof(tiles));
            // 생성 타일 없는 배경은 기존 진입 건너뛰기 정책을 따릅니다.
            if (tiles.Count == 0) return Array.Empty<CombatBoardRevealTarget>();
            Transform root = backdrop.Find(MapRevealAreaLayout.RootPath);
            // 해당 경로와 접두사가 구역 등록 규칙입니다. 등록하지 않은 배경도 정상입니다.
            if (root == null) return Array.Empty<CombatBoardRevealTarget>();
            var areas = new List<Transform>();
            for (int i = 0; i < root.childCount; i++)
            {
                Transform area = root.GetChild(i);
                if (area.name.StartsWith(MapRevealAreaLayout.AreaPrefix, StringComparison.Ordinal)) areas.Add(area);
            }
            if (areas.Count == 0) return Array.Empty<CombatBoardRevealTarget>();

            var positions = new Vector2[tiles.Count];
            for (int i = 0; i < tiles.Count; i++)
            {
                if (tiles[i] == null)
                    throw new InvalidOperationException($"[CombatBackdropRevealTargets] 생성 타일이 사라졌습니다. Index: {i}");
                positions[i] = FieldPosition(field, tiles[i]);
            }

            var targets = new List<CombatBoardRevealTarget>(areas.Count);
            foreach (Transform area in areas)
            {
                Vector2 position = FieldPosition(field, area);
                float closestDistance = float.PositiveInfinity;
                int closest = -1;
                for (int i = 0; i < tiles.Count; i++)
                {
                    if (tiles[i].IsChildOf(area) || area.IsChildOf(tiles[i]))
                        throw new InvalidOperationException($"[CombatBackdropRevealTargets] 배경 구역과 생성 타일을 부모·자식으로 겹칠 수 없습니다. Area: {area.name}, Tile: {tiles[i].name}");
                    float distance = (position - positions[i]).sqrMagnitude;
                    // 같은 거리면 생성 목록의 앞 타일을 유지합니다.
                    if (distance >= closestDistance) continue;
                    closestDistance = distance;
                    closest = i;
                }
                if (closest < 0)
                    throw new InvalidOperationException($"[CombatBackdropRevealTargets] 구역의 기준 타일을 찾을 수 없습니다. Area: {area.name}");
                targets.Add(new CombatBoardRevealTarget(area, tiles[closest]));
            }
            return targets.AsReadOnly();
        }

        private static Vector2 FieldPosition(Transform field, Transform target)
        {
            Vector3 position = field.InverseTransformPoint(target.position);
            if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.z) || float.IsInfinity(position.z))
                throw new InvalidOperationException($"[CombatBackdropRevealTargets] 유한한 X·Z 위치가 필요합니다. Object: {target.name}");
            return new Vector2(position.x, position.z);
        }
    }
}
