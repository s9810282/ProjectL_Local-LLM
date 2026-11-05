using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 돌마다 출발 자리·크기·회전을 정합니다(낙석 View 설계 RG1·RG4). 출발 자리 계산과 화면용 난수는 여기에만 둡니다. 규칙에 닿지 않는 View 난수입니다.
    /// </summary>
    internal static class CombatRockfallDrops
    {
        // 회전축이 진행 방향에 수직인 축에서 벗어나는 정도입니다(계획 RGP-4).
        private const float SpinAxisWobble = .25f;
        // 이보다 짧은 옆 변위는 진행 방향을 구할 수 없어 곧게 떨어지는 것으로 봅니다.
        private const float MinSideSquared = .0001f;

        /// <summary>능선 구간에서 칸과 가장 가까운 점(가로 평면 기준) 근처에서 출발합니다. 궤적이 거의 나란해집니다.</summary>
        internal static CombatRockfallDrop FromRidge(Vector3 landing, Vector3 up, Vector3 ridgeStart, Vector3 ridgeEnd, RockfallVisualData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            Vector3 segment = Vector3.ProjectOnPlane(ridgeEnd - ridgeStart, up);
            Vector3 toLanding = Vector3.ProjectOnPlane(landing - ridgeStart, up);
            float lengthSquared = segment.sqrMagnitude;
            float along = lengthSquared > 0f ? Mathf.Clamp01(Vector3.Dot(toLanding, segment) / lengthSquared) : 0f;
            Vector3 origin = Vector3.Lerp(ridgeStart, ridgeEnd, along) + (UnityEngine.Random.insideUnitSphere * settings.DropStartScatter);

            Vector3 offset = origin - landing;
            // 능선이 칸보다 낮으면 돌이 아래에서 솟지 않게 옆으로만 옵니다(계획 RGP-3).
            float height = Mathf.Max(0f, Vector3.Dot(offset, up));
            return WithLook(landing, up, Vector3.ProjectOnPlane(offset, up), up * height, settings);
        }

        /// <summary>능선이 없는 맵입니다. 칸 위에서 곧게 떨어집니다.</summary>
        internal static CombatRockfallDrop Straight(Vector3 landing, Vector3 up, RockfallVisualData settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return WithLook(landing, up, Vector3.zero, up * settings.DropHeight, settings);
        }

        private static CombatRockfallDrop WithLook(Vector3 landing, Vector3 up, Vector3 side, Vector3 rise, RockfallVisualData settings)
        {
            // 돌은 칸 쪽(옆 변위의 반대)으로 가므로 그 방향으로 구르는 축을 씁니다. 곧게 떨어지면 구를 방향이 없어 무작위 축입니다.
            Vector3 axis = side.sqrMagnitude > MinSideSquared
                ? (Vector3.Cross(up, -side.normalized) + (UnityEngine.Random.insideUnitSphere * SpinAxisWobble)).normalized
                : UnityEngine.Random.onUnitSphere;

            return new CombatRockfallDrop(landing, side, rise,
                UnityEngine.Random.Range(settings.StoneScaleMin, settings.StoneScaleMax),
                axis,
                UnityEngine.Random.Range(settings.SpinDegreesMin, settings.SpinDegreesMax),
                UnityEngine.Random.rotation);
        }
    }
}
