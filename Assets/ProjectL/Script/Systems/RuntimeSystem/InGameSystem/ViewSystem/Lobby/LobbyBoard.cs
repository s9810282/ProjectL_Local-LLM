using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 연결 시 DB 칸을 월드 자세로 계산하며 씬 Anchor에는 쓰지 않습니다. 칸 ↔ 위치 식의 유일한 자리입니다(로비 설계 §5.3).
    /// <c>WorldRoot</c>의 변환은 연결 때 한 번 잡습니다. 연결 동안 <c>WorldRoot</c>는 움직이지 않습니다.
    /// </summary>
    internal sealed class LobbyBoard
    {
        // 칸 중심에서 칸 크기의 이 비율보다 더 벗어나면 칸 위가 아닙니다(로비 계획 TP-HM8).
        private const float CellTolerance = .001f;

        private readonly LobbyBoardData _layout;
        private readonly Matrix4x4 _localToWorld;
        private readonly Matrix4x4 _worldToLocal;
        private readonly Quaternion _rotation;
        private readonly Dictionary<LobbyFacilityKind, Pose> _arrivals = new();

        internal Pose Start { get; }

        internal LobbyBoard(LobbyBoardData layout, Transform worldRoot)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (worldRoot == null) throw new ArgumentNullException(nameof(worldRoot));
            // 판 값은 DB 생성(LobbyVisualTable) 때 검사가 끝났습니다(로비 리팩토링 설계 LR5-3).
            _layout = layout;
            _localToWorld = worldRoot.localToWorldMatrix;
            _worldToLocal = worldRoot.worldToLocalMatrix;
            _rotation = worldRoot.rotation;
            Start = Calculate(layout.Start);
            foreach (LobbyFacilityKind kind in LobbyFacilityKinds.All)
                _arrivals.Add(kind, Calculate(layout.ArrivalOf(kind)));
        }

        internal Pose GetArrival(LobbyFacilityKind kind) => _arrivals.TryGetValue(kind, out Pose pose)
            ? pose
            : throw new InvalidOperationException($"[Lobby] 도착 자세를 대응할 시설 종류가 올바르지 않습니다({kind}).");

        /// <summary>칸 중심의 월드 위치입니다. 칸은 배치판 안이어야 합니다.</summary>
        internal Vector3 PositionOf(CellCoord cell)
        {
            if (!_layout.Contains(cell))
                throw new ArgumentOutOfRangeException(nameof(cell), cell, "[Lobby] 배치판 밖의 칸입니다.");
            Vector3 local = _layout.Center + new Vector3(
                (cell.X - (_layout.Columns - 1) / 2f) * _layout.CellSize.x, 0f,
                (cell.Y - (_layout.Rows - 1) / 2f) * _layout.CellSize.y);
            return _localToWorld.MultiplyPoint3x4(local);
        }

        /// <summary>월드 위치가 서 있는 칸입니다. 칸 중심이 아니거나 배치판 밖이면 예외입니다.</summary>
        internal CellCoord CellAt(Vector3 world)
        {
            Vector3 local = _worldToLocal.MultiplyPoint3x4(world) - _layout.Center;
            float column = local.x / _layout.CellSize.x + (_layout.Columns - 1) / 2f;
            float row = local.z / _layout.CellSize.y + (_layout.Rows - 1) / 2f;
            if (!float.IsFinite(column) || !float.IsFinite(row))
                throw new InvalidOperationException("[Lobby] 캐릭터 위치가 유한하지 않아 칸을 구할 수 없습니다.");
            var cell = new CellCoord(Mathf.RoundToInt(column), Mathf.RoundToInt(row));
            if (Mathf.Abs(column - cell.X) > CellTolerance || Mathf.Abs(row - cell.Y) > CellTolerance || !_layout.Contains(cell))
                throw new InvalidOperationException($"[Lobby] 캐릭터 위치가 배치판 칸 중심이 아닙니다: ({column}, {row}).");
            return cell;
        }

        /// <summary>수평 방향이 배치판 Z축(행 방향, R0·R180)에 더 가까우면 참입니다. 같으면 참입니다.</summary>
        internal bool IsAlongBoardZ(Vector3 worldDirection)
        {
            Vector3 local = _worldToLocal.MultiplyVector(worldDirection);
            return Mathf.Abs(local.z) >= Mathf.Abs(local.x);
        }

        private Pose Calculate(LobbyBoardData.Arrival arrival)
        {
            Vector3 position = PositionOf(arrival.Cell);
            Quaternion rotation = _rotation * BoardRotation.Yaw(arrival.Facing);
            // DB는 입력 값만 검사합니다. 칸 크기·월드 변환을 곱한 계산 결과는 넘칠 수 있어 여기서 봅니다(R2-d Codex 검수, N-19 중 이 검사는 남김).
            if (!ViewPrefabRules.IsFinite(position) || !ViewPrefabRules.IsFinite(rotation))
                throw new InvalidOperationException("[LobbyBoard] 배치판에서 계산한 월드 자세는 유한해야 합니다.");
            return new Pose(position, rotation);
        }
    }
}
