using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback
{
    /// <summary>공격의 범위·영향·차단 벽·궤적·넉백과 체력 미리보기 표시 의도를 적용합니다.</summary>
    /// <remarks>
    /// 입력 응답 판단과 공격 애니메이션은 맡지 않으며, 표시 상태는 Field와 유닛 HUD 일시 피드백이 소유합니다(연출 조립 설계 §9 I-2).
    /// 궤적은 경로 구성(<see cref="CombatAttackTrajectory"/>)에 넘기기만 합니다(공격 궤적 설계 TJ2).
    /// 넉백은 구성(<see cref="CombatKnockbackPreview"/>)에 넘기기만 합니다(넉백 미리보기 설계 KB1).
    /// </remarks>
    internal sealed class CombatAttackFeedback
    {
        private readonly FieldView _field;
        private readonly CombatUnitHudFeedback _hud;
        private readonly CombatAttackTrajectory _trajectory;
        private readonly CombatKnockbackPreview _knockback;
        // 접촉별 벽 칸을 모으는 작업 목록입니다. 실제 층과 중복 처리는 Field가 소유합니다.
        private readonly List<CellCoord> _blockedWallCells = new();
        private readonly List<CellCoord> _extensionCells = new();
        private readonly HashSet<CellCoord> _basicCells = new();
        // 호버 범위로 마지막에 그린 층들입니다(공격 층·이동 층, 없으면 None). 지울 때 그 층만 지웁니다.
        private FieldHighlightLayer _hoverRangeLayers;

        internal CombatAttackFeedback(FieldView field, CombatUnitHudFeedback hud, CombatAttackTrajectory trajectory)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _trajectory = trajectory ?? throw new ArgumentNullException(nameof(trajectory));
            // 넉백 미리보기는 필드만 필요한 공격 표시의 한 부분이라 여기서 만듭니다(넉백 미리보기 설계 P4, Factory 무변경).
            _knockback = new CombatKnockbackPreview(field);
        }

        internal void ShowAttackTrajectory(EntityId attackerId, in AttackPreview preview) =>
            _trajectory.Show(attackerId, preview);

        internal void ShowBlockedAttackTrajectory(EntityId attackerId, CellCoord target, IReadOnlyList<AttackBlockedContact> contacts) =>
            _trajectory.ShowBlocked(attackerId, target, contacts);

        internal void ClearAttackTrajectory() => _trajectory.Clear();

        internal void ShowKnockbackPreview(in AttackPreview preview) => _knockback.Show(preview);

        internal void ClearKnockbackPreview() => _knockback.Clear();

        internal void ShowRangeCells(IReadOnlyList<CellCoord> cells, IReadOnlyList<CellCoord> basicCells = null)
        {
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            _basicCells.Clear();
            _extensionCells.Clear();
            if (basicCells != null)
            {
                foreach (CellCoord cell in basicCells) _basicCells.Add(cell);
                foreach (CellCoord cell in cells)
                    if (!_basicCells.Contains(cell)) _extensionCells.Add(cell);
            }
            // 집합의 표시 층만 나눕니다. 거리·공격 가능 여부는 Core 응답을 그대로 따릅니다.
            _field.SetLayerCells(FieldHighlightLayer.AttackRange, basicCells ?? cells);
            _field.SetLayerCells(FieldHighlightLayer.PullRange, _extensionCells);
        }

        internal void ClearRangeCells()
        {
            _field.ClearLayer(FieldHighlightLayer.AttackRange | FieldHighlightLayer.PullRange);
            _basicCells.Clear();
            _extensionCells.Clear();
        }

        /// <summary>
        /// 호버 범위입니다(호버 공격 범위 설계 Q-UR3 B·§8 HZ5, 호버 이동 범위 설계 EM2). 아군은 아군 호버 범위 층(공격 범위와 같은 색)·아군 호버 이동 층,
        /// 적은 적 공격 범위 층·적 이동 층, 환경 위험은 위험 범위 층(이동 층 없음)에 그리고, 지난번에 그린 층이 다르면 먼저 지웁니다.
        /// 공격 모드 범위·이동 모드 범위와 층이 달라 호버를 지워도 그 범위는 남습니다(View 2라운드 W5-6).
        /// </summary>
        internal void ShowHoverRange(IReadOnlyList<CellCoord> attackCells, IReadOnlyList<CellCoord> moveCells, CombatRangeTone tone,
            IReadOnlyList<CellCoord> sourceCells)
        {
            if (attackCells == null) throw new ArgumentNullException(nameof(attackCells));
            if (moveCells == null) throw new ArgumentNullException(nameof(moveCells));
            if (sourceCells == null) throw new ArgumentNullException(nameof(sourceCells));
            FieldHighlightLayer attackLayer = AttackLayerOf(tone);
            FieldHighlightLayer moveLayer = MoveLayerOf(tone);
            if (moveLayer == FieldHighlightLayer.None && moveCells.Count > 0)
                throw new ArgumentException($"[CombatAttackFeedback] {tone} 호버 범위에는 이동 칸이 없습니다.", nameof(moveCells));

            FieldHighlightLayer layers = attackLayer | moveLayer;
            FieldHighlightLayer stale = _hoverRangeLayers & ~layers;
            if (stale != FieldHighlightLayer.None)
                _field.ClearLayer(stale);
            _hoverRangeLayers = layers;
            _field.SetHoverRangeSourceCells(sourceCells);
            _field.SetLayerCells(attackLayer, attackCells);
            if (moveLayer != FieldHighlightLayer.None)
                _field.SetLayerCells(moveLayer, moveCells);
        }

        /// <summary>호버 범위로 그린 층만 지웁니다.</summary>
        internal void ClearHoverRange()
        {
            _field.SetHoverRangeSourceCells(Array.Empty<CellCoord>());
            if (_hoverRangeLayers == FieldHighlightLayer.None)
                return;
            FieldHighlightLayer layers = _hoverRangeLayers;
            _hoverRangeLayers = FieldHighlightLayer.None;
            _field.ClearLayer(layers);
        }

        private static FieldHighlightLayer AttackLayerOf(CombatRangeTone tone)
        {
            switch (tone)
            {
                case CombatRangeTone.Ally: return FieldHighlightLayer.AllyHoverRange;
                case CombatRangeTone.Enemy: return FieldHighlightLayer.EnemyAttackRange;
                case CombatRangeTone.Hazard: return FieldHighlightLayer.HazardRange;
                default: throw new ArgumentOutOfRangeException(nameof(tone), tone, null);
            }
        }

        // 색조의 이동 칸 층입니다. 환경 위험은 움직이지 않아 None입니다.
        private static FieldHighlightLayer MoveLayerOf(CombatRangeTone tone)
        {
            switch (tone)
            {
                case CombatRangeTone.Ally: return FieldHighlightLayer.AllyHoverMoveRange;
                case CombatRangeTone.Enemy: return FieldHighlightLayer.EnemyMoveRange;
                case CombatRangeTone.Hazard: return FieldHighlightLayer.None;
                default: throw new ArgumentOutOfRangeException(nameof(tone), tone, null);
            }
        }

        internal void ShowAffectedCells(IReadOnlyList<CellCoord> cells, AttackPullSnapshot pull = null, bool closeOutline = false)
        {
            _field.SetAffectedCells(cells, closeOutline);
            _field.SetLayerCells(FieldHighlightLayer.PullDestination,
                pull == null ? Array.Empty<CellCoord>() : new[] { pull.FinalAnchor });
        }

        internal void ClearAffectedCells()
        {
            _field.ClearLayer(FieldHighlightLayer.Affected | FieldHighlightLayer.PullDestination);
        }

        internal void ShowAttackHealthPreview(ReadOnlyMemory<AttackTargetPreview> targets) =>
            _hud.ShowAttackHealthPreview(targets);

        internal void ClearAttackHealthPreview() => _hud.ClearAttackHealthPreview();

        internal void ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts)
        {
            if (contacts == null) throw new ArgumentNullException(nameof(contacts));
            _blockedWallCells.Clear();
            foreach (var contact in contacts)
                foreach (var cell in contact.WallCells)
                    _blockedWallCells.Add(cell);
            _field.SetLayerCells(FieldHighlightLayer.BlockedWall, _blockedWallCells);
        }

        internal void ClearAttackBlockers()
        {
            _blockedWallCells.Clear();
            _field.ClearLayer(FieldHighlightLayer.BlockedWall);
        }
    }
}
