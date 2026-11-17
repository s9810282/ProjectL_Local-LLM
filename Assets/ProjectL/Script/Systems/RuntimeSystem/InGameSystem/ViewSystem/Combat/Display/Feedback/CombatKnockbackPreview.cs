using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback
{
    /// <summary>
    /// 공격 미리보기의 강제 이동 기록(넉백·끌어오기·죽음의 손 휩쓸기)을 도착 칸·표지로 바꿔 필드에 넘깁니다(넉백 미리보기 설계 KB1·KB4, 계획 TP-1,
    /// 끌어오기는 궤적 대상별 선 설계 PL1·PL4, 휩쓸기는 관통·죽음의 손 View 설계 §3.6). "멈춘 이유 → 표지 종류"와 "점유 칸 → 도착 칸·화살표 자리"의 원본입니다.
    /// </summary>
    /// <remarks>
    /// 방향·거리·막힘·면역은 계산하지 않습니다(원본은 Core 넉백 기록 <see cref="AttackKnockbackSnapshot"/>·끌어오기 기록 <see cref="AttackPullSnapshot"/>).
    /// 끌어오기는 화살표만 더하고, 도착 칸은 공격 영향 칸 표시의 끌어오기 도착 칸 층 하나가 원본입니다(PL2). 점유 칸은 같은 응답의
    /// 대상 미리보기(<see cref="AttackTargetPreview.TargetOccupiedCells"/>)에서 읽어 Buffer를 따로 보지 않습니다(설계 P1).
    /// 언제 보일지는 공격 조준·포효 예고가, 어느 층에 칠하고 어떻게 그릴지는 캔버스가 정합니다. 응답마다 한 번 부르며 작업 목록은 다시 씁니다.
    /// <para>
    /// 출처는 둘입니다(포효 예고 설계 RF5, 계획 TP-18). 규칙 = 조준 넉백·끌어오기가 있으면 조준 &gt; 전경 입력 중이면 없음 &gt; 포효 예고입니다.
    /// 포효 예고도 같은 <c>Append</c>를 지나지만 포효에는 끌어오기가 없어 끌어오기 화살표가 나오지 않습니다(나오게 되면 도착 칸 층이 없음, 궤적 대상별 선 설계 §9).
    /// 캔버스가 한 벌뿐이라 "어느 출처를 그릴지"는 이 객체 한 곳이 정합니다(새 SSOT 설계 SN3). 포효 예고가 없으면 조준만 있던 때와 똑같습니다.
    /// 전경 입력 사실(<see cref="SetForegroundInput"/>)을 받는 것은, 공격 조준이 호버 칸을 옮길 때마다 자기 넉백을 지우는 사이에
    /// 포효 넉백이 끼어 깜빡이지 않게 하려는 것입니다(포효 예고 RF-d 검토 M1).
    /// </para>
    /// </remarks>
    internal sealed class CombatKnockbackPreview
    {
        private readonly ICombatKnockbackCanvas _canvas;
        private readonly List<CellCoord> _destinations = new();
        private readonly List<FieldKnockbackMark> _marks = new();
        // 기록 하나의 대상 점유 칸 작업 목록입니다(SurfaceHeightOf가 목록을 받음).
        private readonly List<CellCoord> _occupied = new();
        // 포효 예고의 공격 미리보기입니다(대상이 있는 보스마다 하나). 조준을 지운 뒤 다시 그리려고 복사해 둡니다(계획 TP-20).
        private readonly List<AttackPreview> _roar = new();
        // 공격 조준의 넉백·끌어오기 기록이 그려져 있는지입니다. 참이면 포효 예고는 그리지 않습니다.
        private bool _aimShown;
        // 전경 입력(공격 모드·이동·배치·아이템 드래그) 중인지입니다. 참이면 포효 예고의 넉백은 그리지 않습니다(포효 예고 RF-d 검토 M1).
        private bool _foreground;

        internal CombatKnockbackPreview(ICombatKnockbackCanvas canvas)
        {
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }

        /// <summary>
        /// 공격 조준 응답의 넉백·끌어오기를 그립니다. 둘 다 없으면 조준 출처가 없는 것으로 보고 포효 예고를 그립니다
        /// (전경 입력 중이거나 포효 예고도 없으면 지움, 계획 TP-18, 끌어오기는 PL4).
        /// </summary>
        internal void ShowAim(in AttackPreview preview)
        {
            // 죽음의 손 휩쓸기도 조준 출처입니다(관통·죽음의 손 View 설계 §3.6).
            _aimShown = preview.Knockbacks.Count > 0 || preview.Pull != null || preview.Sweep != null ||
                preview.ThornBombExplosions.Count > 0;
            if (_aimShown)
                Draw(preview);
            else
                DrawRoar();
        }

        /// <summary>공격 조준의 넉백·끌어오기를 지웁니다. 전경 입력 중이 아니고 포효 예고가 있으면 그것이 다시 보입니다. 여러 번 불려도 안전합니다.</summary>
        internal void ClearAim()
        {
            _aimShown = false;
            DrawRoar();
        }

        /// <summary>
        /// 플레이어 스킬 예보의 넉백을 그립니다(전투 스킬 View 설계 SK2, 계획 TP-4). 공격 조준과 같은 조준 출처라, 그리는 동안 포효 예고는 보이지 않습니다.
        /// 스킬 사건에는 공격 미리보기 대상 행이 없어 점유 칸은 <paramref name="occupiedOf"/>(Buffer 캐릭터의 지금 점유 칸)가 주고, 없으면 시작 칸 하나입니다.
        /// 기록이 없으면 조준 출처가 없는 것으로 보고 포효 예고를 그립니다. 지우기는 <see cref="ClearAim"/>입니다.
        /// </summary>
        internal void ShowSkill(IReadOnlyList<AttackKnockbackSnapshot> knockbacks, Func<EntityId, IReadOnlyList<CellCoord>> occupiedOf)
        {
            if (knockbacks == null) throw new ArgumentNullException(nameof(knockbacks));
            if (occupiedOf == null) throw new ArgumentNullException(nameof(occupiedOf));
            _aimShown = knockbacks.Count > 0;
            if (!_aimShown)
            {
                DrawRoar();
                return;
            }
            FieldGridMapper mapper = _canvas.Mapper;
            if (mapper == null)
                return;
            _destinations.Clear();
            _marks.Clear();
            for (int i = 0; i < knockbacks.Count; i++)
            {
                AttackKnockbackSnapshot knockback = knockbacks[i];
                _occupied.Clear();
                IReadOnlyList<CellCoord> cells = occupiedOf(knockback.TargetId);
                if (cells != null)
                {
                    for (int c = 0; c < cells.Count; c++)
                        _occupied.Add(cells[c]);
                }
                if (_occupied.Count == 0)
                    _occupied.Add(knockback.StartAnchor);
                AddKnockback(mapper, knockback);
            }
            _canvas.ShowKnockbacks(_destinations, _marks);
        }

        /// <summary>포효 예고의 공격 미리보기를 교체합니다. 공격 조준이 넉백을 그리는 중이면 기억만 합니다. 목록은 복사합니다.</summary>
        internal void ShowRoar(IReadOnlyList<AttackPreview> attacks)
        {
            if (attacks == null)
                throw new ArgumentNullException(nameof(attacks));
            _roar.Clear();
            for (int i = 0; i < attacks.Count; i++)
                _roar.Add(attacks[i]);
            if (!_aimShown)
                DrawRoar();
        }

        /// <summary>포효 예고를 지웁니다. 공격 조준이 그리는 중이면 그대로 둡니다. 여러 번 불려도 안전합니다.</summary>
        internal void ClearRoar()
        {
            _roar.Clear();
            if (!_aimShown)
                _canvas.ClearKnockbacks();
        }

        /// <summary>
        /// 전경 입력 중인지를 받습니다(새 SSOT 설계 SN3, 계획 TP-18). 값이 같으면 아무것도 하지 않습니다. 바뀌었을 때 공격 조준이 넉백을 그리는 중이면
        /// 기억만 하고, 아니면 포효 예고의 넉백을 다시 그리거나 지웁니다. 부르는 쪽은 매 Tick 넘겨도 됩니다.
        /// </summary>
        internal void SetForegroundInput(bool active)
        {
            if (_foreground == active)
                return;
            _foreground = active;
            if (!_aimShown)
                DrawRoar();
        }

        // 포효 예고의 기록을 모두 모아 그립니다. 전경 입력 중이거나 기록이 없으면 지웁니다.
        private void DrawRoar()
        {
            if (_foreground || _roar.Count == 0)
            {
                _canvas.ClearKnockbacks();
                return;
            }
            FieldGridMapper mapper = _canvas.Mapper;
            if (mapper == null)
                return;
            _destinations.Clear();
            _marks.Clear();
            for (int i = 0; i < _roar.Count; i++)
                Append(mapper, _roar[i]);
            _canvas.ShowKnockbacks(_destinations, _marks);
        }

        /// <summary>
        /// 기록마다 도착 칸(경로가 있을 때만, 점유 칸 + (최종 − 시작))과 화살표 하나를 만들어 캔버스에 넘깁니다.
        /// 필드를 구성하기 전이면 아무것도 하지 않습니다(그때는 칠한 층도 표지도 없음).
        /// </summary>
        private void Draw(in AttackPreview preview)
        {
            FieldGridMapper mapper = _canvas.Mapper;
            if (mapper == null)
                return;

            _destinations.Clear();
            _marks.Clear();
            Append(mapper, preview);
            _canvas.ShowKnockbacks(_destinations, _marks);
        }

        // 응답 하나의 기록을 작업 목록 둘에 더합니다. 끌어오기는 화살표만 더합니다(도착 칸은 끌어오기 도착 칸 층, PL2).
        private void Append(FieldGridMapper mapper, in AttackPreview preview)
        {
            IReadOnlyList<AttackKnockbackSnapshot> knockbacks = preview.Knockbacks;
            ReadOnlySpan<AttackTargetPreview> targets = preview.Targets.Span;
            for (int i = 0; i < knockbacks.Count; i++)
            {
                AttackKnockbackSnapshot knockback = knockbacks[i];
                FillOccupied(targets, knockback.TargetId, knockback.StartAnchor);
                AddKnockback(mapper, knockback);
            }

            // 각 폭발의 시작점은 이전 폭발이 밀어낸 뒤의 위치입니다. 점유 칸은 Core가 밀기 직전 사실로 줍니다(여러 칸 유닛).
            var explosions = preview.ThornBombExplosions;
            for (int e = 0; e < explosions.Count; e++)
            {
                var explosion = explosions[e];
                for (int k = 0; k < explosion.Knockbacks.Count; k++)
                {
                    AttackKnockbackSnapshot knockback = explosion.Knockbacks[k];
                    IReadOnlyList<CellCoord> cells = explosion.KnockbackOccupiedCellsOf(knockback.TargetId);
                    _occupied.Clear();
                    for (int c = 0; c < cells.Count; c++)
                        _occupied.Add(cells[c]);
                    AddKnockback(mapper, knockback);
                }
            }

            AttackPullSnapshot pull = preview.Pull;
            if (pull != null)
            {
                // 방향은 Core 첫 걸음입니다. Core가 한 방향으로만 당겨 경로 전체 방향과 같습니다(설계 P-PL2).
                AttackPullStepSnapshot first = pull.Steps[0];
                FillOccupied(targets, pull.TargetId, pull.StartAnchor);
                _marks.Add(MarkOf(mapper, first.To - first.From, pull.StartAnchor, FieldKnockbackMarkKind.Pull));
            }

            // 죽음의 손 휩쓸기는 넉백과 같은 화살표·도착 칸입니다(관통·죽음의 손 View 설계 §3.6, 결정 D5 넉백 색 재사용).
            // 대상 행은 한 축으로만 움직여(Core 검사) 목표 칸 방향의 부호가 한 걸음입니다. 점유 칸은 Core 보조 행이 보장합니다.
            AttackSweepSnapshot sweep = preview.Sweep;
            if (sweep != null)
            {
                for (int i = 0; i < sweep.Targets.Count; i++)
                {
                    AttackSweepTargetSnapshot target = sweep.Targets[i];
                    if (!TryKindOf(target.Stop, out FieldKnockbackMarkKind kind))
                        continue;
                    FillOccupied(targets, target.TargetId, target.StartAnchor);
                    if (target.EnteredCells.Count > 0 && target.FinalAnchor.HasValue)
                    {
                        CellCoord offset = target.FinalAnchor.Value - target.StartAnchor;
                        for (int c = 0; c < _occupied.Count; c++)
                            _destinations.Add(_occupied[c] + offset);
                    }
                    _marks.Add(MarkOf(mapper, StepOf(target), target.StartAnchor, kind));
                }
            }
        }

        /// <summary>
        /// 휩쓸기 대상 행의 멈춘 이유 → 표지 종류입니다(넉백 <see cref="KindOf"/>와 같은 규칙, 이 바꾸기의 원본은 여기 한 곳). 미처리 대상(전투가 끝나 멈춤)은 표지가 없습니다.
        /// </summary>
        internal static bool TryKindOf(AttackSweepStop stop, out FieldKnockbackMarkKind kind)
        {
            switch (stop)
            {
                case AttackSweepStop.Completed:
                case AttackSweepStop.Interrupted:
                    kind = FieldKnockbackMarkKind.Push;
                    return true;
                case AttackSweepStop.Blocked:
                    kind = FieldKnockbackMarkKind.Blocked;
                    return true;
                case AttackSweepStop.Immune:
                    kind = FieldKnockbackMarkKind.Immune;
                    return true;
                case AttackSweepStop.NotProcessed:
                    kind = default;
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stop), stop, null);
            }
        }

        // 대상 행의 한 걸음(목표 칸 쪽 단위 칸 차이)입니다. View는 거리·막힘을 계산하지 않고 방향만 읽습니다.
        private static CellCoord StepOf(AttackSweepTargetSnapshot target)
        {
            long dx = (long)target.IntendedEndpoint.X - target.StartAnchor.X;
            long dy = (long)target.IntendedEndpoint.Y - target.StartAnchor.Y;
            return new CellCoord(Math.Sign(dx), Math.Sign(dy));
        }

        /// <summary>
        /// 넉백 기록 → 표지 종류입니다. 이 바꾸기의 원본은 여기 한 곳입니다(결정 Q-KB2 A). 사망은 HUD 체력 예상이 보입니다.
        /// 부딪힘 사실(<see cref="AttackKnockbackSnapshot.Collision"/>, 작렬)이 있으면 부딪힘이고, 없으면 Core 멈춤 이유를 따릅니다(작렬 View 설계 CV6).
        /// </summary>
        internal static FieldKnockbackMarkKind KindOf(AttackKnockbackSnapshot knockback)
        {
            if (knockback == null) throw new ArgumentNullException(nameof(knockback));
            if (knockback.Collision != null)
                return FieldKnockbackMarkKind.Collision;
            AttackKnockbackStop stop = knockback.Stop;
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

        // 점유 칸(_occupied)을 채운 기록 하나의 도착 칸(경로가 있을 때만, 점유 칸 + (최종 − 시작))과 화살표 하나를 더합니다. 공격 조준·포효·스킬이 함께 씁니다.
        private void AddKnockback(FieldGridMapper mapper, AttackKnockbackSnapshot knockback)
        {
            if (knockback.Path.Count > 0)
            {
                CellCoord offset = knockback.FinalAnchor - knockback.StartAnchor;
                for (int c = 0; c < _occupied.Count; c++)
                    _destinations.Add(_occupied[c] + offset);
            }
            _marks.Add(MarkOf(mapper, knockback.Step, knockback.StartAnchor, KindOf(knockback)));
        }

        /// <summary>
        /// 기록 대상의 점유 칸을 같은 응답의 대상 미리보기에서 채웁니다. 점유 칸이 비어 있으면 시작 칸 하나입니다.
        /// 대상이 없으면 Core 계약(기록 대상은 미리보기 대상 안) 위반이라 예외입니다(설계 P2). 끌어오기 대상도 맞지 않을 때 Core가
        /// 이동 대상 보조 행을 늘 만들어(<c>AttackResultFactory</c>) 같은 규칙입니다(궤적 대상별 선 설계 P-PL1).
        /// </summary>
        private void FillOccupied(ReadOnlySpan<AttackTargetPreview> targets, EntityId targetId, CellCoord startAnchor)
        {
            _occupied.Clear();
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i].TargetId != targetId)
                    continue;

                ReadOnlySpan<CellCoord> cells = targets[i].TargetOccupiedCells.Span;
                for (int c = 0; c < cells.Length; c++)
                    _occupied.Add(cells[c]);
                if (_occupied.Count == 0)
                    _occupied.Add(startAnchor);
                return;
            }

            throw new InvalidOperationException(
                $"[CombatKnockbackPreview] 강제 이동 기록의 대상이 미리보기 대상에 없습니다: {targetId}");
        }

        /// <summary>
        /// 화살표 하나입니다. 자리는 점유 칸 중 밀리는 쪽으로 가장 앞선 칸들의 중심에서 반 칸 더 간 곳(칸 가장자리, 대각선이면 꼭짓점)이고,
        /// 높이는 점유 칸과 화살표 앞 절반이 걸치는 칸(판 안일 때만) 표면 중 가장 높은 값입니다. 걸치는 칸은 앞 칸이고, 대각선이면
        /// 꼭짓점에서 만나는 양옆 칸 둘도 더합니다 — 고지대 쪽으로 밀릴 때 블록에 묻히지 않게 합니다(결정 Q-KBb3 A, 일괄 검토 뒤 대각선 옆 칸).
        /// 회전은 밀리는 방향을 봅니다(설계 KB2). 끌어오기도 같은 규칙으로 끌려오는 쪽 가장자리에 둡니다(궤적 대상별 선 설계 PL1·PL4).
        /// </summary>
        /// <param name="step">한 걸음 칸 차이(넉백 걸음, 끌어오기 첫 걸음)입니다.</param>
        /// <param name="startAnchor">점유 칸 높이 계산의 기준 칸(기록의 시작 칸)입니다.</param>
        private FieldKnockbackMark MarkOf(FieldGridMapper mapper, CellCoord step, CellCoord startAnchor, FieldKnockbackMarkKind kind)
        {
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
            float height = mapper.SurfaceHeightOf(_occupied, startAnchor);
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
            return new FieldKnockbackMark(position, Quaternion.LookRotation(forward, Vector3.up), kind);
        }

        /// <summary>칸이 판 안이면 그 표면 높이와 지금 높이 중 높은 값, 판 밖이면 지금 높이입니다.</summary>
        private static float HigherSurface(FieldGridMapper mapper, CellCoord cell, float height) =>
            mapper.IsInside(cell) ? Mathf.Max(height, mapper.Surface.HeightAt(cell)) : height;
    }
}
