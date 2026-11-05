using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback
{
    /// <summary>
    /// 공격 미리보기의 넉백 기록을 도착 칸·표지로 바꿔 필드에 넘깁니다(넉백 미리보기 설계 KB1·KB4, 계획 TP-1).
    /// "멈춘 이유 → 표지 종류"와 "점유 칸 → 도착 칸·화살표 자리"의 원본입니다.
    /// </summary>
    /// <remarks>
    /// 방향·거리·막힘·면역은 계산하지 않습니다(원본은 Core 넉백 기록 <see cref="AttackKnockbackSnapshot"/>). 점유 칸은 같은 응답의
    /// 대상 미리보기(<see cref="AttackTargetPreview.TargetOccupiedCells"/>)에서 읽어 Buffer를 따로 보지 않습니다(설계 P1).
    /// 언제 보일지는 공격 조준이, 어느 층에 칠하고 어떻게 그릴지는 캔버스가 정합니다. 응답마다 한 번 부르며 작업 목록은 다시 씁니다.
    /// </remarks>
    internal sealed class CombatKnockbackPreview
    {
        private readonly ICombatKnockbackCanvas _canvas;
        private readonly List<CellCoord> _destinations = new();
        private readonly List<FieldKnockbackMark> _marks = new();
        // 기록 하나의 대상 점유 칸 작업 목록입니다(SurfaceHeightOf가 목록을 받음).
        private readonly List<CellCoord> _occupied = new();

        internal CombatKnockbackPreview(ICombatKnockbackCanvas canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }

        /// <summary>
        /// 기록마다 도착 칸(경로가 있을 때만, 점유 칸 + (최종 − 시작))과 화살표 하나를 만들어 캔버스에 넘깁니다.
        /// 기록이 없으면 빈 목록이라 지운 것과 같습니다. 필드를 구성하기 전이면 아무것도 하지 않습니다(그때는 칠한 층도 표지도 없음).
        /// </summary>
        internal void Show(in AttackPreview preview)
        {
            FieldGridMapper mapper = _canvas.Mapper;
            if (mapper == null)
                return;

            _destinations.Clear();
            _marks.Clear();
            IReadOnlyList<AttackKnockbackSnapshot> knockbacks = preview.Knockbacks;
            ReadOnlySpan<AttackTargetPreview> targets = preview.Targets.Span;
            for (int i = 0; i < knockbacks.Count; i++)
            {
                AttackKnockbackSnapshot knockback = knockbacks[i];
                FillOccupied(targets, knockback);
                if (knockback.Path.Count > 0)
                {
                    CellCoord offset = knockback.FinalAnchor - knockback.StartAnchor;
                    for (int c = 0; c < _occupied.Count; c++)
                        _destinations.Add(_occupied[c] + offset);
                }
                _marks.Add(MarkOf(mapper, knockback));
            }

            _canvas.ShowKnockbacks(_destinations, _marks);
        }

        /// <summary>도착 칸과 표지를 지웁니다. 여러 번 불려도 안전합니다.</summary>
        internal void Clear() => _canvas.ClearKnockbacks();

        /// <summary>Core 멈춤 이유 → 표지 종류입니다. 이 바꾸기의 원본은 여기 한 곳입니다(결정 Q-KB2 A). 사망은 HUD 체력 예상이 보입니다.</summary>
        internal static FieldKnockbackMarkKind KindOf(AttackKnockbackStop stop)
        {
            switch (stop)
            {
                case AttackKnockbackStop.Completed:
                case AttackKnockbackStop.Interrupted:
                    return FieldKnockbackMarkKind.Push;
                case AttackKnockbackStop.Blocked:
                    return FieldKnockbackMarkKind.Blocked;
                case AttackKnockbackStop.Immune:
                    return FieldKnockbackMarkKind.Immune;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stop), stop, null);
            }
        }

        /// <summary>
        /// 기록 대상의 점유 칸을 같은 응답의 대상 미리보기에서 채웁니다. 점유 칸이 비어 있으면 시작 칸 하나입니다.
        /// 대상이 없으면 Core 계약(기록 대상은 미리보기 대상 안) 위반이라 예외입니다(설계 P2).
        /// </summary>
        private void FillOccupied(ReadOnlySpan<AttackTargetPreview> targets, AttackKnockbackSnapshot knockback)
        {
            _occupied.Clear();
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].TargetId != knockback.TargetId)
                    continue;

                ReadOnlySpan<CellCoord> cells = targets[i].TargetOccupiedCells.Span;
                for (int c = 0; c < cells.Length; c++)
                    _occupied.Add(cells[c]);
                if (_occupied.Count == 0)
                    _occupied.Add(knockback.StartAnchor);
                return;
            }

            throw new InvalidOperationException(
                $"[CombatKnockbackPreview] 넉백 기록의 대상이 미리보기 대상에 없습니다: {knockback.TargetId}");
        }

        /// <summary>
        /// 화살표 하나입니다. 자리는 점유 칸 중 밀리는 쪽으로 가장 앞선 칸들의 중심에서 반 칸 더 간 곳(칸 가장자리, 대각선이면 꼭짓점)이고,
        /// 높이는 점유 칸과 화살표 앞 절반이 걸치는 칸(판 안일 때만) 표면 중 가장 높은 값입니다. 걸치는 칸은 앞 칸이고, 대각선이면
        /// 꼭짓점에서 만나는 양옆 칸 둘도 더합니다 — 고지대 쪽으로 밀릴 때 블록에 묻히지 않게 합니다(결정 Q-KBb3 A, 일괄 검토 뒤 대각선 옆 칸).
        /// 회전은 밀리는 방향을 봅니다(설계 KB2).
        /// </summary>
        private FieldKnockbackMark MarkOf(FieldGridMapper mapper, AttackKnockbackSnapshot knockback)
        {
            CellCoord step = knockback.Step;
            Vector3 forward = mapper.StepToLocal(step);
            long front = long.MinValue;
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int i = 0; i < _occupied.Count; i++)
            {
                CellCoord cell = _occupied[i];
                long along = ((long)cell.X * step.X) + ((long)cell.Y * step.Y);
                if (along > front)
                {
                    front = along;
                    sum = Vector3.zero;
                    count = 0;
                }
                if (along == front)
                {
                    sum += mapper.CellCenterToLocal(cell);
                    count++;
                }
            }

            bool diagonal = step.X != 0 && step.Y != 0;
            float height = mapper.SurfaceHeightOf(_occupied, knockback.StartAnchor);
            for (int i = 0; i < _occupied.Count; i++)
            {
                CellCoord cell = _occupied[i];
                if (((long)cell.X * step.X) + ((long)cell.Y * step.Y) != front)
                    continue;
                height = HigherSurface(mapper, cell + step, height);
                if (diagonal)
                {
                    height = HigherSurface(mapper, new CellCoord(cell.X + step.X, cell.Y), height);
                    height = HigherSurface(mapper, new CellCoord(cell.X, cell.Y + step.Y), height);
                }
            }

            Vector3 position = (sum / count) + (forward * 0.5f);
            position.y = height;
            return new FieldKnockbackMark(position, Quaternion.LookRotation(forward, Vector3.up), KindOf(knockback.Stop));
        }

        /// <summary>칸이 판 안이면 그 표면 높이와 지금 높이 중 높은 값, 판 밖이면 지금 높이입니다.</summary>
        private static float HigherSurface(FieldGridMapper mapper, CellCoord cell, float height) =>
            mapper.IsInside(cell) ? Mathf.Max(height, mapper.Surface.HeightAt(cell)) : height;
    }
}
