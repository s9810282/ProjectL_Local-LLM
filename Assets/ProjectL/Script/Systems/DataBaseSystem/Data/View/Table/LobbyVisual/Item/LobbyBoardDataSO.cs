using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>로비 배치판과 시작·시설 도착 칸을 입력합니다.</summary>
    [CreateAssetMenu(fileName = "NewLobbyBoardData", menuName = "ProjectL/Database/View/LobbyBoardData")]
    internal sealed class LobbyBoardDataSO : DatabaseItem
    {
        [Header("배치판")]
        [SerializeField] private int boardColumns = 4;
        [SerializeField] private int boardRows = 4;
        [SerializeField] private Vector2 cellSize = new(4.25f, 3.85f);
        [SerializeField] private Vector3 boardCenter = new(0f, 0.1f, -0.98f);
        [Header("시작 칸과 방향")]
        [SerializeField] private Vector2Int startCell = new(1, 2);
        [SerializeField] private Rotation4 startFacing = Rotation4.R0;
        [Header("시설 도착 칸과 방향")]
        [Tooltip("시설 종류마다 한 줄입니다. 모든 종류가 정확히 한 번씩 있어야 하고 칸은 서로 달라야 합니다.")]
        [SerializeField] private FacilityArrivalInput[] facilityArrivals =
        {
            new(LobbyFacilityKind.Artifact, new Vector2Int(0, 2), Rotation4.R270),
            new(LobbyFacilityKind.StageEntry, new Vector2Int(1, 3), Rotation4.R0),
            new(LobbyFacilityKind.Deck, new Vector2Int(2, 2), Rotation4.R90)
        };

        public override IData GetData()
        {
            var arrivals = new List<KeyValuePair<LobbyFacilityKind, LobbyBoardData.Arrival>>(facilityArrivals.Length);
            // 직렬화 배열과 [Serializable] 원소는 Unity가 늘 채우므로 null은 검사하지 않습니다(로비 리팩토링 2차 설계 N-31).
            foreach (FacilityArrivalInput input in facilityArrivals)
                arrivals.Add(input.GetData());
            return new LobbyBoardData(boardColumns, boardRows, cellSize, boardCenter,
                new LobbyBoardData.Arrival(new CellCoord(startCell.x, startCell.y), startFacing), arrivals);
        }

        /// <summary>Inspector의 시설 종류·칸·방향을 읽기 전용 도착 값으로 변환합니다.</summary>
        [Serializable]
        private sealed class FacilityArrivalInput
        {
            [SerializeField] private LobbyFacilityKind kind;
            [SerializeField] private Vector2Int cell;
            [SerializeField] private Rotation4 facing;

            internal FacilityArrivalInput(LobbyFacilityKind kind, Vector2Int cell, Rotation4 facing)
            {
                this.kind = kind;
                this.cell = cell;
                this.facing = facing;
            }

            internal KeyValuePair<LobbyFacilityKind, LobbyBoardData.Arrival> GetData() =>
                new(kind, new LobbyBoardData.Arrival(new CellCoord(cell.x, cell.y), facing));
        }
    }
}
