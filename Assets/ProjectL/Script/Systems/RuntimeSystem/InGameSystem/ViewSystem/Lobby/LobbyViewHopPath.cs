using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 로비 캐릭터가 도착 칸까지 한 칸씩 뛰는 경로입니다(로비 설계 §15 HM1·HM2). 가로·세로로만 걷고,
    /// 열과 행이 모두 다르면 도착 방향 축을 마지막에 걷습니다. 방향이 바뀌는 걸음 앞과 도착 뒤에 돕니다.
    /// 화면 표시 경로라 Core에 묻지 않고, 전투 이동 코드도 참조하지 않습니다.
    /// </summary>
    /// <remarks>위치는 월드 좌표입니다. 구간 목록은 <see cref="Plan"/>마다 비우고 다시 채우며, <see cref="Evaluate"/>는 할당하지 않습니다.</remarks>
    internal sealed class LobbyViewHopPath : ChessPiecePath
    {
        // 이 각도 이하의 차이는 같은 방향으로 보고 돌지 않습니다(로비 계획 TP-HM9). 로비가 정한 값이며 전투 코드와 묶지 않습니다(N-50).
        private const float SameFacingDegrees = .1f;

        /// <summary>회전 하나 또는 뛰기 하나입니다. 회전은 제자리(<see cref="From"/>)에서 돕니다.</summary>
        private readonly struct Segment
        {
            internal readonly bool IsHop;
            internal readonly float Seconds;
            internal readonly Vector3 From;
            internal readonly Vector3 To;
            internal readonly Quaternion FromRotation;
            internal readonly Quaternion ToRotation;

            private Segment(bool isHop, float seconds, Vector3 from, Vector3 to, Quaternion fromRotation, Quaternion toRotation)
            {
                IsHop = isHop;
                Seconds = seconds;
                From = from;
                To = to;
                FromRotation = fromRotation;
                ToRotation = toRotation;
            }

            internal static Segment Turn(Vector3 at, Quaternion from, Quaternion to, float seconds) =>
                new(false, seconds, at, at, from, to);

            internal static Segment Hop(Vector3 from, Vector3 to, Quaternion facing, float seconds) =>
                new(true, seconds, from, to, facing, facing);
        }

        private readonly LobbyBoard _board;
        private readonly LobbyMotionData _motion;
        private readonly List<Segment> _segments = new();

        internal LobbyViewHopPath(LobbyBoard board, LobbyMotionData motion)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
        }

        internal override float Plan(Vector3 origin, Quaternion originRotation, Vector3 destination, Vector3? facingPosition)
        {
            _segments.Clear();
            CellCoord from = _board.CellAt(origin);
            CellCoord to = _board.CellAt(destination);
            if (from == to)
                throw new InvalidOperationException("[Lobby] 출발과 도착이 같은 칸인데 위치가 다릅니다.");

            // 도착 방향이 Z축(R0·R180)이면 열(X)을 먼저 맞추고 행을 마지막에 걷습니다. X축이면 반대입니다(설계 HM1, Q-HM1 A).
            bool rowsLast = !facingPosition.HasValue || _board.IsAlongBoardZ(facingPosition.Value - destination);
            Vector3 position = origin;
            Quaternion facing = originRotation;
            CellCoord cell = from;
            if (rowsLast)
            {
                WalkColumns(ref cell, to, destination, ref position, ref facing);
                WalkRows(ref cell, to, destination, ref position, ref facing);
            }
            else
            {
                WalkRows(ref cell, to, destination, ref position, ref facing);
                WalkColumns(ref cell, to, destination, ref position, ref facing);
            }
            AddTurn(position, ref facing, FacingRotation(destination, facingPosition, facing));

            float seconds = 0f;
            for (int i = 0; i < _segments.Count; i++)
                seconds += _segments[i].Seconds;
            return seconds;
        }

        internal override void Evaluate(float elapsed, Quaternion currentRotation, out Vector3 position, out Quaternion rotation)
        {
            float remaining = elapsed;
            int last = _segments.Count - 1;
            for (int i = 0; i < last; i++)
            {
                if (remaining < _segments[i].Seconds)
                {
                    Sample(_segments[i], remaining, out position, out rotation);
                    return;
                }
                remaining -= _segments[i].Seconds;
            }
            Sample(_segments[last], remaining, out position, out rotation);
        }

        private void Sample(in Segment segment, float elapsed, out Vector3 position, out Quaternion rotation)
        {
            float p = Mathf.Clamp01(elapsed / segment.Seconds);
            if (!segment.IsHop)
            {
                position = segment.From;
                rotation = Quaternion.Slerp(segment.FromRotation, segment.ToRotation, p);
                return;
            }
            // 바닥이 평평해(정착 높이 하나) 높이는 포물선 한 줄입니다(설계 HM2). 꼭대기는 걸음 가운데입니다.
            position = Vector3.Lerp(segment.From, segment.To, p) + Vector3.up * (_motion.HopHeight * 4f * p * (1f - p));
            rotation = segment.ToRotation;
        }

        private void WalkColumns(ref CellCoord cell, CellCoord to, Vector3 destination, ref Vector3 position, ref Quaternion facing)
        {
            int step = Math.Sign(to.X - cell.X);
            while (cell.X != to.X)
            {
                cell = new CellCoord(cell.X + step, cell.Y);
                AddHop(ref position, ref facing, cell == to ? destination : _board.PositionOf(cell));
            }
        }

        private void WalkRows(ref CellCoord cell, CellCoord to, Vector3 destination, ref Vector3 position, ref Quaternion facing)
        {
            int step = Math.Sign(to.Y - cell.Y);
            while (cell.Y != to.Y)
            {
                cell = new CellCoord(cell.X, cell.Y + step);
                AddHop(ref position, ref facing, cell == to ? destination : _board.PositionOf(cell));
            }
        }

        private void AddHop(ref Vector3 position, ref Quaternion facing, Vector3 target)
        {
            AddTurn(position, ref facing, FacingRotation(position, target, facing));
            _segments.Add(Segment.Hop(position, target, facing, _motion.HopSeconds));
            position = target;
        }

        // 방향이 바뀌면 회전 구간을 넣습니다. 회전 시간이 0이면 구간 없이 다음 구간에서 바로 그 방향입니다.
        private void AddTurn(Vector3 position, ref Quaternion facing, Quaternion target)
        {
            if (_motion.TurnSeconds > 0f && Quaternion.Angle(facing, target) > SameFacingDegrees)
                _segments.Add(Segment.Turn(position, facing, target, _motion.TurnSeconds));
            facing = target;
        }
    }
}
