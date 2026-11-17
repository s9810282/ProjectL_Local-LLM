using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Feedback
{
    /// <summary>
    /// 공격의 범위·영향·차단 벽·궤적·넉백과 체력 미리보기, 호버 범위 표시 의도를 적용합니다.
    /// 공격 조준·유닛 호버가 쓰는 계약 넷과, 넉백 미리보기를 함께 쓰는 포효 예고 계약의 주인입니다(씬 표시 나누기 설계 DS2·계획 TP-4, 포효 예고 계획 TP-19).
    /// </summary>
    /// <remarks>
    /// 입력 응답 판단과 공격 애니메이션은 맡지 않으며, 표시 상태는 Field와 유닛 HUD 일시 피드백이 소유합니다(연출 조립 설계 §9 I-2).
    /// 궤적은 경로 구성(<see cref="CombatAttackTrajectory"/>)에 넘기기만 합니다(공격 궤적 설계 TJ2).
    /// 넉백은 구성(<see cref="CombatKnockbackPreview"/>)에 넘기기만 합니다(넉백 미리보기 설계 KB1).
    /// 해제 뒤 규칙(D9)은 공유 수명(<see cref="CombatDisplayLifetime"/>)이 해제되면 아무것도 하지 않는 것으로 지킵니다.
    /// 호버 범위 원점 유닛은 완료 표시 목록(<see cref="CombatDisplaySnapshot"/>)에서 찾습니다.
    /// </remarks>
    internal sealed class CombatAttackFeedback : ICombatAttackRangeFeedback, ICombatUnitRangeFeedback, ICombatAttackTrajectoryFeedback,
        ICombatKnockbackPreviewFeedback, ICombatRoarForecastFeedback, ICombatPlayerSkillFeedback
    {
        private readonly FieldView _field;
        private readonly CombatUnitHudFeedback _hud;
        private readonly CombatAttackTrajectory _trajectory;
        private readonly CombatKnockbackPreview _knockback;
        private readonly CombatDisplaySnapshot _snapshot;
        private readonly CombatDisplayLifetime _lifetime;
        // 접촉별 벽 칸을 모으는 작업 목록입니다. 실제 층과 중복 처리는 Field가 소유합니다.
        private readonly List<CellCoord> _blockedWallCells = new();
        private readonly List<CellCoord> _extensionCells = new();
        private readonly HashSet<CellCoord> _basicCells = new();
        // 호버 범위로 마지막에 그린 층들입니다(공격 층·이동 층, 없으면 None). 지울 때 그 층만 지웁니다.
        private FieldHighlightLayer _hoverRangeLayers;
        // 스킬 선택 선을 다시 그리려고 둔 마지막 선택 값입니다. 예보가 경로 선을 그리는 동안은 선택 선 대신 경로 선입니다(전투 스킬 View 설계 SK2).
        private readonly List<CellCoord> _skillPoints = new();
        private EntityId _skillCaster;
        private CombatAttackPresentationData _skillSettings;
        private bool _skillPathShown;

        /// <param name="snapshot">호버 범위 원점 유닛을 찾는 완료 표시 목록입니다.</param>
        internal CombatAttackFeedback(FieldView field, CombatUnitHudFeedback hud, CombatAttackTrajectory trajectory,
            CombatDisplaySnapshot snapshot, CombatDisplayLifetime lifetime)
        {
            _field = field != null ? field : throw new ArgumentNullException(nameof(field));
            _hud = hud ?? throw new ArgumentNullException(nameof(hud));
            _trajectory = trajectory ?? throw new ArgumentNullException(nameof(trajectory));
            _snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
            // 넉백 미리보기는 필드만 필요한 공격 표시의 한 부분이라 여기서 만듭니다(넉백 미리보기 설계 P4, Factory 무변경).
            _knockback = new CombatKnockbackPreview(field);
        }

        #region 공격 궤적 (ICombatAttackTrajectoryFeedback, 공격 궤적 설계 TJ2)

        void ICombatAttackTrajectoryFeedback.ShowAttackTrajectory(EntityId attackerId, in AttackPreview preview)
        {
            if (!_lifetime.IsReleased) _trajectory.Show(attackerId, preview);
        }

        void ICombatAttackTrajectoryFeedback.ShowBlockedAttackTrajectory(EntityId attackerId, CellCoord target,
            IReadOnlyList<AttackBlockedContact> contacts)
        {
            if (!_lifetime.IsReleased) _trajectory.ShowBlocked(attackerId, target, contacts);
        }

        void ICombatAttackTrajectoryFeedback.ClearAttackTrajectory()
        {
            if (!_lifetime.IsReleased) _trajectory.Clear();
        }

        #endregion

        #region 넉백 미리보기 (ICombatKnockbackPreviewFeedback, 넉백 미리보기 설계 KB3)

        void ICombatKnockbackPreviewFeedback.ShowKnockbackPreview(in AttackPreview preview)
        {
            if (!_lifetime.IsReleased) _knockback.ShowAim(preview);
        }

        void ICombatKnockbackPreviewFeedback.ClearKnockbackPreview()
        {
            if (!_lifetime.IsReleased) _knockback.ClearAim();
        }

        #endregion

        #region 플레이어 스킬 (ICombatPlayerSkillFeedback, 전투 스킬 View 설계 SK2)

        void ICombatPlayerSkillFeedback.ShowSkillSelection(EntityId caster, IReadOnlyList<CellCoord> candidates, IReadOnlyList<CellCoord> points,
            CombatAttackPresentationData settings)
        {
            if (_lifetime.IsReleased) return;
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (points == null) throw new ArgumentNullException(nameof(points));
            _skillCaster = caster;
            _skillSettings = settings;
            _skillPoints.Clear();
            for (int i = 0; i < points.Count; i++)
                _skillPoints.Add(points[i]);
            _field.SetLayerCells(FieldHighlightLayer.SkillCandidate, candidates);
            _field.SetLayerCells(FieldHighlightLayer.SkillPoint, _skillPoints);
            if (!_skillPathShown)
                DrawSkillLine();
        }

        void ICombatPlayerSkillFeedback.ClearSkillSelection()
        {
            if (_lifetime.IsReleased) return;
            _field.ClearLayer(FieldHighlightLayer.SkillCandidate | FieldHighlightLayer.SkillPoint);
            _skillPoints.Clear();
            _skillCaster = EntityId.Empty;
            _skillSettings = null;
            if (!_skillPathShown)
                _trajectory.Clear();
        }

        // 영향 칸은 닫힌 테두리로 그립니다. 열린 변의 원점은 선택 유닛 칸인데 시전자(메인)가 선택 유닛과 다를 수 있어서입니다(계획 TP-1 정정과 같은 까닭).
        // 넉백은 공격 조준과 같은 출처(조준 출처)로 넉백 미리보기에 넘깁니다. 스킬 모드와 공격 모드는 전경이 배타라 함께 그리지 않습니다(계획 TP-4).
        void ICombatPlayerSkillFeedback.ShowSkillForecast(EntityId caster, CombatPlayerSkillForecastView forecast, CombatAttackPresentationData settings)
        {
            if (_lifetime.IsReleased) return;
            if (forecast == null) throw new ArgumentNullException(nameof(forecast));
            _field.SetAffectedCells(forecast.AffectedCells, closeOutline: true);
            if (forecast.Path.Count > 0)
            {
                _skillPathShown = true;
                _trajectory.ShowPath(caster, forecast.Path, settings);
            }
            else if (_skillPathShown)
            {
                _skillPathShown = false;
                DrawSkillLine();
            }
            _hud.ShowHealthPreview(forecast.Healths);
            _knockback.ShowSkill(forecast.Knockbacks, forecast.OccupiedOf);
        }

        void ICombatPlayerSkillFeedback.ClearSkillForecast()
        {
            if (_lifetime.IsReleased) return;
            _field.ClearLayer(FieldHighlightLayer.Affected);
            _hud.ClearAttackHealthPreview();
            _knockback.ClearAim();
            if (_skillPathShown)
            {
                _skillPathShown = false;
                DrawSkillLine();
            }
        }

        // 시전자 → 고른 점을 차례로 잇는 선입니다. 점이 없거나 연출 값이 없으면 선을 지웁니다.
        private void DrawSkillLine()
        {
            if (_skillPoints.Count == 0 || _skillSettings == null)
                _trajectory.Clear();
            else
                _trajectory.ShowPath(_skillCaster, _skillPoints, _skillSettings);
        }

        #endregion

        #region 포효 예고 (ICombatRoarForecastFeedback, 포효 예고 설계 RF4·RF5)

        // 범위 칸은 포효 예고 층에, 넉백은 공격 조준과 같은 넉백 미리보기에 넘깁니다. 어느 넉백 출처를 그릴지는 넉백 미리보기가 정합니다(계획 TP-18·TP-19).
        void ICombatRoarForecastFeedback.ShowRoarForecast(IReadOnlyList<CellCoord> cells, IReadOnlyList<AttackPreview> attacks)
        {
            if (_lifetime.IsReleased) return;
            if (cells == null) throw new ArgumentNullException(nameof(cells));
            if (attacks == null) throw new ArgumentNullException(nameof(attacks));
            _field.SetLayerCells(FieldHighlightLayer.RoarForecast, cells);
            _knockback.ShowRoar(attacks);
        }

        void ICombatRoarForecastFeedback.ClearRoarForecast()
        {
            if (_lifetime.IsReleased) return;
            _field.ClearLayer(FieldHighlightLayer.RoarForecast);
            _knockback.ClearRoar();
        }

        // 전경 입력 사실은 넉백 미리보기에 넘기기만 합니다. 넉백 출처를 고르는 규칙은 넉백 미리보기 한 곳입니다(새 SSOT 설계 SN3).
        void ICombatRoarForecastFeedback.SetForegroundInput(bool active)
        {
            if (!_lifetime.IsReleased) _knockback.SetForegroundInput(active);
        }

        #endregion

        #region 공격 범위·영향·막힘·체력 (ICombatAttackRangeFeedback)

        void ICombatAttackRangeFeedback.ShowRangeCells(IReadOnlyList<CellCoord> cells, IReadOnlyList<CellCoord> basicCells)
        {
            if (_lifetime.IsReleased) return;
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

        void ICombatAttackRangeFeedback.ClearRangeCells()
        {
            if (_lifetime.IsReleased) return;
            _field.ClearLayer(FieldHighlightLayer.AttackRange | FieldHighlightLayer.PullRange);
            _basicCells.Clear();
            _extensionCells.Clear();
        }

        void ICombatAttackRangeFeedback.ShowAffectedCells(IReadOnlyList<CellCoord> cells, AttackPullSnapshot pull, bool closeOutline)
        {
            if (_lifetime.IsReleased) return;
            _field.SetAffectedCells(cells, closeOutline);
            _field.SetLayerCells(FieldHighlightLayer.PullDestination,
                pull == null ? Array.Empty<CellCoord>() : new[] { pull.FinalAnchor });
        }

        void ICombatAttackRangeFeedback.ClearAffectedCells()
        {
            if (_lifetime.IsReleased) return;
            _field.ClearLayer(FieldHighlightLayer.Affected | FieldHighlightLayer.PullDestination);
        }

        void ICombatAttackRangeFeedback.ShowAttackHealthPreview(in AttackPreview attack)
        {
            if (!_lifetime.IsReleased) _hud.ShowAttackHealthPreview(attack);
        }

        void ICombatAttackRangeFeedback.ClearAttackHealthPreview()
        {
            if (!_lifetime.IsReleased) _hud.ClearAttackHealthPreview();
        }

        void ICombatAttackRangeFeedback.ShowAttackBlockers(IReadOnlyList<AttackBlockedContact> contacts)
        {
            if (_lifetime.IsReleased) return;
            if (contacts == null) throw new ArgumentNullException(nameof(contacts));
            _blockedWallCells.Clear();
            foreach (var contact in contacts)
                foreach (var cell in contact.WallCells)
                    _blockedWallCells.Add(cell);
            _field.SetLayerCells(FieldHighlightLayer.BlockedWall, _blockedWallCells);
        }

        void ICombatAttackRangeFeedback.ClearAttackBlockers()
        {
            if (_lifetime.IsReleased) return;
            _blockedWallCells.Clear();
            _field.ClearLayer(FieldHighlightLayer.BlockedWall);
        }

        #endregion

        #region 호버 범위 (ICombatUnitRangeFeedback)

        /// <summary>
        /// 호버 범위의 원점 유닛을 완료 표시 목록에서 찾고 그 점유 칸과 함께 그립니다. 환경 위험(Hazard)이 아니면 원점이 목록에 있어야 합니다.
        /// 검사 차례와 문구는 옛 표시와 같고 태그만 이 객체입니다(설계 P8).
        /// </summary>
        void ICombatUnitRangeFeedback.ShowHoverRange(IReadOnlyList<CellCoord> attackCells, IReadOnlyList<CellCoord> moveCells,
            CombatRangeTone tone, EntityId source)
        {
            if (_lifetime.IsReleased) return;
            CharacterData character = source.IsEmpty ? null : CombatCharacterQueries.Find(_snapshot.Characters, source);
            if (tone != CombatRangeTone.Hazard && character == null)
                throw new InvalidOperationException($"[CombatAttackFeedback] 호버 범위 원점 유닛 {source}이 완료 표시 목록에 없습니다.");
            DrawHoverRange(attackCells, moveCells, tone, character?.OccupiedCells ?? Array.Empty<CellCoord>());
        }

        void ICombatUnitRangeFeedback.ClearHoverRange()
        {
            if (!_lifetime.IsReleased) ClearHoverRangeLayers();
        }

        /// <summary>
        /// 호버 범위입니다(호버 공격 범위 설계 Q-UR3 B·§8 HZ5, 호버 이동 범위 설계 EM2). 아군은 아군 호버 범위 층(공격 범위와 같은 색)·아군 호버 이동 층,
        /// 적은 적 공격 범위 층·적 이동 층, 환경 위험은 위험 범위 층(이동 층 없음)에 그리고, 지난번에 그린 층이 다르면 먼저 지웁니다.
        /// 공격 모드 범위·이동 모드 범위와 층이 달라 호버를 지워도 그 범위는 남습니다(View 2라운드 W5-6).
        /// </summary>
        private void DrawHoverRange(IReadOnlyList<CellCoord> attackCells, IReadOnlyList<CellCoord> moveCells, CombatRangeTone tone,
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
        private void ClearHoverRangeLayers()
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

        #endregion
    }
}
