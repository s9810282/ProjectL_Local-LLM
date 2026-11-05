using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 이동 경로 띠·화살촉 이음 규칙의 원본입니다(이동 경로 화살표 설계 MA2·MA3). 그리기는 <see cref="FieldMovementPathLine"/>이 합니다.
    /// </summary>
    /// <remarks>
    /// 점은 필드 로컬이고, 한 구간 안의 점은 높이가 같습니다(높이 구간은 렌더러가 나눔). 방향은 가로(x·z) 성분으로만 봅니다.
    /// 꺾임 이음: 꺾인 점 C(들어온 방향 dIn, 나가는 방향 dOut, h = 띠 두께 절반)를 <c>C + dIn·h → C → C − dOut·h</c> 셋으로 바꿉니다.
    /// 들어온 띠가 C를 h만큼 지나고 나가는 띠가 C보다 h만큼 뒤에서 시작해, 바깥 모서리 홈이 메워진 직각 모서리가 됩니다.
    /// 경로는 상하좌우 한 칸 걸음이라 모든 구간이 가로·세로 방향이고, 되돌아가는 짧은 구간도 MultiLine 사각형의 감김이
    /// 방향 부호와 상관없어 위를 봅니다(설계 F7). 겹친 곳은 같은 재질·색의 불투명이라 보이지 않습니다.
    /// 화살촉 메시·자세는 공용 <see cref="FieldArrowhead"/>가 소유하며, 이 부품은 띠와 화살촉의 이음만 계산합니다.
    /// 할당하지 않습니다. 결과 목록은 부르는 쪽이 갖고 다시 씁니다.
    /// </remarks>
    internal static class FieldMovementPathShape
    {
        // 들어온 방향과 나가는 방향의 내적이 이보다 크면 같은 방향(직선)으로 봅니다.
        private const float StraightDot = 0.9999f;

        /// <summary>
        /// 구간 하나의 띠 점을 <paramref name="result"/>에 채웁니다. 방향이 같은 점은 빼고, 꺾인 점마다 이음 점을 넣습니다.
        /// <paramref name="trimEnd"/>면 마지막 점을 마지막 방향으로 <paramref name="trimAlong"/>만큼 옮깁니다(음수면 뒤로, 화살촉 안으로 줄임).
        /// 점이 둘 미만이면 받은 점을 그대로 옮겨 담습니다.
        /// </summary>
        internal static void Fill(IReadOnlyList<Vector3> run, bool trimEnd, float trimAlong, float halfThickness, List<Vector3> result)
        {
            if (run == null)
                throw new ArgumentNullException(nameof(run));
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            result.Clear();
            int count = run.Count;
            if (count < 2)
            {
                for (int i = 0; i < count; i++)
                    result.Add(run[i]);
                return;
            }

            result.Add(run[0]);
            for (int i = 1; i < count - 1; i++)
            {
                Vector3 enter = Flat(run[i] - run[i - 1]);
                Vector3 leave = Flat(run[i + 1] - run[i]);
                if (Vector3.Dot(enter, leave) > StraightDot)
                    continue;

                Vector3 corner = run[i];
                result.Add(corner + (enter * halfThickness));
                result.Add(corner);
                result.Add(corner - (leave * halfThickness));
            }

            Vector3 end = run[count - 1];
            if (trimEnd)
                end += Flat(end - run[count - 2]) * trimAlong;
            result.Add(end);
        }

        /// <summary>
        /// 마지막 구간의 띠 끝을 옮길 거리입니다. 띠 끝 = 화살촉 가운데 = 꼭지에서 길이 절반 뒤이고, 꼭지 = 도착 칸 가운데 + 꼭지 자리입니다(설계 MA3).
        /// </summary>
        internal static float TrimAlong(float tipOffset, float arrowLength) => tipOffset - (arrowLength * 0.5f);

        private static Vector3 Flat(Vector3 vector) => new Vector3(vector.x, 0f, vector.z).normalized;
    }
}
