using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 처음 닿는 시각의 순수 기하입니다(히트 타이밍 설계 H5). 모든 값은 필드 로컬 XZ 평면(<see cref="CombatBody"/>)입니다.
    /// 접촉 시각 식은 이 클래스 한 곳에만 둡니다.
    /// </summary>
    /// <remarks>
    /// "접촉 없음"은 기하 결과일 뿐 Core 명중을 취소하거나 피해 사건을 버릴 근거가 아닙니다.
    /// 경고·대체 시각·오류 정책은 부르는 쪽(공격 재생의 접촉 시각 결정 경계, 설계 §6 Q3)이 정합니다.
    /// </remarks>
    internal static class CombatContactGeometry
    {
        // 월드 ↔ 필드 로컬 변환의 반올림으로 칸 중심선 위 점 몸이 경로에서 빗나가지 않게, 경계를 이만큼 포함합니다.
        private const float Tolerance = .0001f;

        /// <summary>
        /// 움직이는 쪽(<paramref name="movingHalf"/>, 점이면 0)이 <paramref name="from"/> → <paramref name="to"/>를 지날 때
        /// 정지한 <paramref name="body"/>와 처음 겹치는 정규화 시각 <c>t ∈ [0, 1]</c>입니다. 시작부터 겹치면 0, 닿지 않으면 false입니다.
        /// </summary>
        /// <remarks>Minkowski 확장 + slab 방식입니다. 경계에 닿는 것도 접촉입니다.</remarks>
        internal static bool TrySegmentContact(Vector2 from, Vector2 to, Vector2 movingHalf, CombatBody body, out float time)
        {
            RequireFinite(from, nameof(from));
            RequireFinite(to, nameof(to));
            if (!float.IsFinite(movingHalf.x) || !float.IsFinite(movingHalf.y) || movingHalf.x < 0f || movingHalf.y < 0f)
                throw new ArgumentOutOfRangeException(nameof(movingHalf), "움직이는 몸의 반 크기는 유한한 0 이상이어야 합니다.");
            Vector2 half = body.HalfSize + movingHalf;
            Vector2 delta = to - from;
            float enter = 0f;
            float exit = 1f;
            if (!Slab(from.x, delta.x, body.Center.x, half.x, ref enter, ref exit) ||
                !Slab(from.y, delta.y, body.Center.y, half.y, ref enter, ref exit))
            {
                time = 0f;
                return false;
            }
            time = enter;
            return true;
        }

        /// <summary>
        /// 진행 방향 <paramref name="direction"/>으로 움직이는 앞면이 <paramref name="start"/>에서 출발할 때 <paramref name="body"/>의
        /// 가장 가까운 면에 닿기까지의 진행 거리 <c>min dot(p − start, d)</c>입니다. 음수면 시작 때 이미 지났습니다.
        /// </summary>
        /// <remarks>기차 앞면(<c>RailwayTrainVisualBinding.FrontContact</c>)의 접촉에 씁니다. 거리를 시간으로 바꾸는 것은 경로가 합니다.</remarks>
        internal static float PlaneContactDistance(Vector2 start, Vector2 direction, CombatBody body)
        {
            RequireFinite(start, nameof(start));
            RequireFinite(direction, nameof(direction));
            return Vector2.Dot(body.Center - start, direction) - Reach(direction, body);
        }

        /// <summary>
        /// 같은 앞면이 <paramref name="body"/>의 가장 먼 면을 지나기까지의 진행 거리 <c>max dot(p − start, d)</c>입니다.
        /// 기차 앞면이 레일 칸을 빠져나가는 순간에 씁니다(카메라 연출 조각 설계 K10). 닿는 거리는 <see cref="PlaneContactDistance"/>입니다.
        /// </summary>
        internal static float PlaneLeaveDistance(Vector2 start, Vector2 direction, CombatBody body)
        {
            RequireFinite(start, nameof(start));
            RequireFinite(direction, nameof(direction));
            return Vector2.Dot(body.Center - start, direction) + Reach(direction, body);
        }

        // 몸 중심에서 진행 방향으로 가장 먼 면까지의 거리입니다(축 정렬 사각형의 지지 거리).
        private static float Reach(Vector2 direction, CombatBody body) =>
            body.HalfSize.x * Mathf.Abs(direction.x) + body.HalfSize.y * Mathf.Abs(direction.y);

        /// <summary>
        /// 점 <paramref name="point"/>를 <paramref name="from"/> → <paramref name="to"/>에 수직 투영한 정규화 시각(0~1로 자름)입니다.
        /// 경로가 거의 0이면 1(종점)입니다. 몸과 궤적이 닿지 않을 때 Core가 준 접촉 칸으로 대신하는 시각입니다(설계 §6 Q3 B).
        /// </summary>
        internal static float ProjectOnSegment(Vector2 from, Vector2 to, Vector2 point)
        {
            RequireFinite(from, nameof(from));
            RequireFinite(to, nameof(to));
            RequireFinite(point, nameof(point));
            Vector2 path = to - from;
            float length = path.sqrMagnitude;
            return length > .000001f ? Mathf.Clamp01(Vector2.Dot(point - from, path) / length) : 1f;
        }

        private static bool Slab(float start, float delta, float center, float half, ref float enter, ref float exit)
        {
            float min = center - half - Tolerance;
            float max = center + half + Tolerance;
            if (Mathf.Abs(delta) <= float.Epsilon)
                return start >= min && start <= max;
            float first = (min - start) / delta;
            float second = (max - start) / delta;
            if (first > second)
                (first, second) = (second, first);
            enter = Math.Max(enter, first);
            exit = Math.Min(exit, second);
            return enter <= exit;
        }

        private static void RequireFinite(Vector2 value, string name)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y))
                throw new ArgumentOutOfRangeException(name, "접촉 좌표는 유한해야 합니다.");
        }
    }
}
