using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Railway
{
    /// <summary>
    /// 이미 확정된 사망 대상의 몸(모양 = 프리팹 BoxCollider, 배치 = 사망 직전 사실)에 앞면이 처음 닿는 시간을 계산합니다(히트 타이밍 설계 H7).
    /// 판정·시계·씬 객체는 소유하지 않습니다.
    /// </summary>
    internal sealed class RailwayContactSchedule
    {
        internal IReadOnlyList<Entry> Entries { get; }

        private RailwayContactSchedule(Entry[] entries) => Entries = Array.AsReadOnly(entries);

        internal static RailwayContactSchedule Create(IReadOnlyList<RailwayTravelPath> paths, IReadOnlyList<Target> targets)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            foreach (RailwayTravelPath path in paths)
                if (path == null) throw new ArgumentException("경로에 null이 있습니다.", nameof(paths));
            var entries = new Entry[targets.Count];
            var ids = new HashSet<EntityId>();
            for (int i = 0; i < targets.Count; i++)
            {
                Target target = targets[i] ?? throw new ArgumentException("대상에 null이 있습니다.", nameof(targets));
                if (!ids.Add(target.Id)) throw new InvalidOperationException("접촉 대상 ID가 중복됩니다.");
                float earliest = float.PositiveInfinity;
                foreach (RailwayTravelPath path in paths)
                {
                    if (!Overlaps(path.Cells, target.OccupiedCells)) continue;
                    if (path.TryGetContactTime(target.Body, out float time)) earliest = Math.Min(earliest, time);
                }
                if (!float.IsFinite(earliest))
                    throw new InvalidOperationException($"접촉 대상 {target.Id}에 재생 시간 안에 도달하는 선로가 없습니다.");
                entries[i] = new Entry(target.Id, earliest);
            }
            Array.Sort(entries, (left, right) =>
            {
                int time = left.Time.CompareTo(right.Time);
                return time != 0 ? time : left.Id.CompareTo(right.Id);
            });
            return new RailwayContactSchedule(entries);
        }

        /// <summary>
        /// 기차가 필드를 지나는 구간입니다(카메라 연출 조각 설계 K9·K10). <paramref name="entry"/>는 앞면이 경로의 레일 칸 몸에 처음 닿는 가장 이른 시각,
        /// <paramref name="exit"/>는 앞면이 레일 칸 몸의 먼 쪽 면을 지나는 가장 늦은 시각입니다(재생 시간 안에 못 지나면 재생 끝).
        /// 닿는 시각은 사망 접촉과 같은 <see cref="RailwayTravelPath.TryGetContactTime(CombatBody, out float)"/>, 지나는 시각은 <see cref="RailwayTravelPath.LeaveTime"/>을 씁니다.
        /// 경로가 없거나 재생 시간 안에 닿는 레일 칸이 없으면 false입니다. 이 구간은 연출 알림에만 쓰므로 기차 재생을 막지 않습니다.
        /// </summary>
        /// <param name="cellBody">레일 칸의 몸입니다(운영은 <see cref="FieldGridMapper.CellBody"/>).</param>
        internal static bool TryGetFieldSpan(IReadOnlyList<RailwayTravelPath> paths, Func<CellCoord, CombatBody> cellBody,
            out float entry, out float exit)
        {
            if (paths == null) throw new ArgumentNullException(nameof(paths));
            if (cellBody == null) throw new ArgumentNullException(nameof(cellBody));
            entry = float.PositiveInfinity;
            exit = 0f;
            foreach (RailwayTravelPath path in paths)
            {
                if (path == null) throw new ArgumentException("경로에 null이 있습니다.", nameof(paths));
                foreach (CellCoord cell in path.Cells)
                {
                    CombatBody body = cellBody(cell);
                    if (path.TryGetContactTime(body, out float contact)) entry = Math.Min(entry, contact);
                    exit = Math.Max(exit, path.LeaveTime(body));
                }
            }
            return float.IsFinite(entry);
        }

        private static bool Overlaps(IReadOnlyList<CellCoord> rails, IReadOnlyList<CellCoord> occupied)
        {
            foreach (CellCoord rail in rails)
                foreach (CellCoord cell in occupied)
                    if (rail.Equals(cell)) return true;
            return false;
        }

        /// <summary>사망 Expected에서 얻은 점유 칸과 필드 로컬 몸입니다.</summary>
        internal sealed class Target
        {
            internal EntityId Id { get; }
            internal IReadOnlyList<CellCoord> OccupiedCells { get; }
            internal CombatBody Body { get; }

            /// <summary>표시 중심 한 점(필드 로컬)을 점 몸으로 쓰는 대상입니다. 옛 중심 기준과 같은 시각입니다.</summary>
            internal Target(EntityId id, IReadOnlyList<CellCoord> occupiedCells, Vector3 position)
                : this(id, occupiedCells, PointOf(position))
            {
            }

            internal Target(EntityId id, IReadOnlyList<CellCoord> occupiedCells, CombatBody body)
            {
                if (id.IsEmpty) throw new ArgumentException("대상 ID가 비어 있습니다.", nameof(id));
                if (occupiedCells == null) throw new ArgumentNullException(nameof(occupiedCells));
                if (occupiedCells.Count == 0) throw new InvalidOperationException("접촉 대상의 사망 전 점유 칸이 필요합니다.");
                var copy = new CellCoord[occupiedCells.Count];
                for (int i = 0; i < copy.Length; i++) copy[i] = occupiedCells[i];
                Id = id;
                OccupiedCells = Array.AsReadOnly(copy);
                Body = body;
            }

            private static CombatBody PointOf(Vector3 position)
            {
                RailwayTravelPath.RequireFinite(position, nameof(position));
                return CombatBody.Point(CombatBody.Flatten(position));
            }
        }

        internal readonly struct Entry
        {
            internal EntityId Id { get; }
            internal float Time { get; }
            internal Entry(EntityId id, float time)
            {
                Id = id;
                Time = time;
            }
        }
    }
}
