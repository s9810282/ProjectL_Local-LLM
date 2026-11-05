using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Railway
{
    /// <summary>열차 이동과 접촉 예약이 공유하는 불변 경로입니다. 모든 위치는 필드 로컬 좌표입니다.</summary>
    internal sealed class RailwayTravelPath
    {
        internal IReadOnlyList<CellCoord> Cells { get; }
        internal Vector3 Start { get; }
        internal Vector3 End { get; }
        internal Vector3 FrontStart { get; }
        internal float Duration { get; }
        internal Vector3 Direction { get; }
        internal float Speed { get; }

        internal RailwayTravelPath(IReadOnlyList<CellCoord> cells, Vector3 start, Vector3 end,
            Vector3 frontStart, float duration)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (cells.Count == 0) throw new InvalidOperationException("열차 경로에 레일 칸이 필요합니다.");
            RequireFinite(start, nameof(start));
            RequireFinite(end, nameof(end));
            RequireFinite(frontStart, nameof(frontStart));
            float length = (end - start).magnitude;
            if (!float.IsFinite(duration) || duration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(duration));
            if (!float.IsFinite(length) || length <= 0f || !float.IsFinite(length / duration) || length / duration <= 0f)
                throw new InvalidOperationException("열차 경로 길이와 속도는 유한한 양수여야 합니다.");
            // 접촉 기하는 필드 로컬 XZ 평면에서 계산합니다(히트 타이밍 설계 H5). 높이가 변하는 경로는 그 계산과 맞지 않습니다.
            if (Mathf.Abs(end.y - start.y) > .0001f * length)
                throw new InvalidOperationException("열차 경로는 필드 평면(XZ)과 나란해야 합니다.");
            var copy = new CellCoord[cells.Count];
            var unique = new HashSet<CellCoord>();
            for (int i = 0; i < cells.Count; i++)
            {
                if (!unique.Add(cells[i])) throw new InvalidOperationException("열차 경로의 레일 칸이 중복됩니다.");
                copy[i] = cells[i];
            }
            Cells = Array.AsReadOnly(copy);
            Start = start;
            End = end;
            FrontStart = frontStart;
            Duration = duration;
            Direction = (end - start) / length;
            Speed = length / duration;
        }

        internal Vector3 PositionAt(float elapsed)
        {
            if (!float.IsFinite(elapsed)) throw new ArgumentOutOfRangeException(nameof(elapsed));
            return Vector3.Lerp(Start, End, Mathf.Clamp01(elapsed / Duration));
        }

        /// <summary>기준면이 이미 지난 대상은 시작 접촉입니다. 종료 뒤 미도달은 설정 오류입니다.</summary>
        internal float ContactTime(Vector3 position)
        {
            if (!TryGetContactTime(position, out float time))
                throw new InvalidOperationException("열차가 재생 시간 안에 대상 위치에 도달하지 못합니다.");
            return time;
        }

        /// <summary>앞면이 대상 위치(점 몸)에 닿는 시간입니다.</summary>
        internal bool TryGetContactTime(Vector3 position, out float time)
        {
            RequireFinite(position, nameof(position));
            return TryGetContactTime(CombatBody.Point(CombatBody.Flatten(position)), out time);
        }

        /// <summary>
        /// 앞면이 대상 몸에 처음 닿는 시간입니다(히트 타이밍 설계 H7). 면 접촉 식은 <see cref="CombatContactGeometry.PlaneContactDistance"/> 한 곳을 씁니다.
        /// 기준면이 이미 몸에 닿아 있으면 0, 재생 시간 안에 닿지 않으면 false입니다.
        /// </summary>
        internal bool TryGetContactTime(CombatBody body, out float time)
        {
            time = CombatContactGeometry.PlaneContactDistance(CombatBody.Flatten(FrontStart), CombatBody.Flatten(Direction), body) / Speed;
            if (!float.IsFinite(time)) throw new InvalidOperationException("접촉 시간이 유한하지 않습니다.");
            time = Mathf.Max(0f, time);
            return time <= Duration;
        }

        /// <summary>
        /// 앞면이 몸의 가장 먼 면을 지나는 시간입니다(기차가 칸을 빠져나가는 순간, 카메라 연출 조각 설계 K10).
        /// 시작 때 이미 지났으면 0, 재생 시간 안에 지나지 않으면 재생 시간(재생 끝)입니다. 면 거리 식은 <see cref="CombatContactGeometry.PlaneLeaveDistance"/>입니다.
        /// </summary>
        internal float LeaveTime(CombatBody body)
        {
            float time = CombatContactGeometry.PlaneLeaveDistance(CombatBody.Flatten(FrontStart), CombatBody.Flatten(Direction), body) / Speed;
            if (!float.IsFinite(time)) throw new InvalidOperationException("빠져나가는 시간이 유한하지 않습니다.");
            return Mathf.Clamp(time, 0f, Duration);
        }

        internal static void RequireFinite(Vector3 value, string name)
        {
            if (!float.IsFinite(value.x) || !float.IsFinite(value.y) || !float.IsFinite(value.z))
                throw new ArgumentOutOfRangeException(name, "위치는 유한해야 합니다.");
        }
    }
}
