using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>로비 배치판·정착 칸의 읽기 전용 값입니다.</summary>
    internal sealed class LobbyBoardData : IData
    {
        internal readonly struct Arrival
        {
            internal CellCoord Cell { get; }
            internal Rotation4 Facing { get; }

            internal Arrival(CellCoord cell, Rotation4 facing)
            {
                Cell = cell;
                Facing = facing;
            }
        }

        private readonly Dictionary<LobbyFacilityKind, Arrival> _arrivals;

        internal int Columns { get; }
        internal int Rows { get; }
        internal Vector2 CellSize { get; }
        internal Vector3 Center { get; }
        internal Arrival Start { get; }

        /// <summary>
        /// 시설 종류 → 도착 칸·방향 목록 하나입니다. 같은 종류가 두 번 들어오면 만들 때 예외입니다.
        /// 모든 종류가 있는지·칸이 겹치지 않는지는 <see cref="Validate"/>가 봅니다(로비 리팩토링 2차 설계 M-13).
        /// </summary>
        internal LobbyBoardData(int columns, int rows, Vector2 cellSize, Vector3 center,
            Arrival start, IEnumerable<KeyValuePair<LobbyFacilityKind, Arrival>> arrivals)
        {
            if (arrivals == null) throw new ArgumentNullException(nameof(arrivals));
            Columns = columns;
            Rows = rows;
            CellSize = cellSize;
            Center = center;
            Start = start;
            _arrivals = new Dictionary<LobbyFacilityKind, Arrival>();
            foreach (KeyValuePair<LobbyFacilityKind, Arrival> pair in arrivals)
            {
                if (!_arrivals.TryAdd(pair.Key, pair.Value))
                    throw Invalid("facilityArrivals", $"시설 '{pair.Key}'의 도착 칸이 두 번 있습니다.");
            }
        }

        /// <summary>시설의 도착 칸·방향입니다. <see cref="Validate"/>를 지난 값이면 모든 종류가 있습니다.</summary>
        internal Arrival ArrivalOf(LobbyFacilityKind kind)
        {
            if (!_arrivals.TryGetValue(kind, out Arrival arrival))
                throw new KeyNotFoundException($"[LobbyBoardData] 시설 '{kind}'의 도착 칸이 없습니다.");
            return arrival;
        }

        /// <summary>칸이 배치판 안인지입니다. DB 검사와 View 판 계산이 함께 쓰는 원본입니다(로비 리팩토링 2차 설계 N-41).</summary>
        internal bool Contains(CellCoord cell) =>
            cell.X >= 0 && cell.X < Columns && cell.Y >= 0 && cell.Y < Rows;

        internal void Validate()
        {
            if (Columns < 1 || Rows < 1)
                throw Invalid("boardColumns/boardRows", "열과 행은 1 이상이어야 합니다.");
            if (!float.IsFinite(CellSize.x) || !float.IsFinite(CellSize.y) || CellSize.x <= 0f || CellSize.y <= 0f)
                throw Invalid("cellSize", "칸 크기는 유한한 양수여야 합니다.");
            if (!ViewPrefabRules.IsFinite(Center)) throw Invalid("boardCenter", "중심은 유한해야 합니다.");
            ValidateArrival(Start, "startCell/startFacing");
            foreach (LobbyFacilityKind kind in _arrivals.Keys)
            {
                if (!LobbyFacilityKinds.IsDefined(kind))
                    throw Invalid("facilityArrivals", $"정의되지 않은 시설 종류({(int)kind})입니다.");
            }
            var cells = new HashSet<CellCoord>();
            foreach (LobbyFacilityKind kind in LobbyFacilityKinds.All)
            {
                if (!_arrivals.TryGetValue(kind, out Arrival arrival))
                    throw Invalid("facilityArrivals", $"시설 '{kind}'의 도착 칸이 필요합니다.");
                ValidateArrival(arrival, $"facilityArrivals({kind})");
                if (!cells.Add(arrival.Cell))
                    throw Invalid("facilityArrivals", "시설 도착 칸은 서로 달라야 합니다.");
            }
        }

        private void ValidateArrival(Arrival arrival, string field)
        {
            if (!Contains(arrival.Cell))
                throw Invalid(field, "칸은 배치판 안에 있어야 합니다.");
            if (!System.Enum.IsDefined(typeof(Rotation4), arrival.Facing))
                throw Invalid(field, "방향은 정의된 Rotation4 값이어야 합니다.");
        }

        private static InvalidOperationException Invalid(string field, string reason) =>
            new($"[LobbyBoardData] {field}: {reason}");
    }
}
