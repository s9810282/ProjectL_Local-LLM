using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageField.Item;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 필드 색 묶음을 읽는 곳입니다. 원본은 View DB <see cref="StageFieldViewData.Colors"/>이고(필드 색 DB 설계 FC1),
    /// 칸 강조 표시기·궤적 선·넉백/끌어오기 화살표가 이 객체로만 색을 고릅니다(FC2, 설계 P-FC1).
    /// </summary>
    /// <remarks>
    /// 값을 복사하지 않고 묶음을 들고 있다가 읽을 때마다 그 칸을 봅니다. 필드는 매 구성과 에디터 값 바뀜 신호 때 <see cref="Use"/>로
    /// 지금 묶음을 다시 가리킵니다(계획 TP-3, FC3). 표시기는 이 객체를 계속 들고 있으므로 바꿔 끼울 필요가 없습니다.
    /// </remarks>
    internal sealed class FieldHighlightPalette
    {
        private FieldColorsData _colors;

        internal FieldHighlightPalette(FieldColorsData colors)
        {
            Use(colors);
        }

        /// <summary>읽을 색 묶음을 바꿉니다. 값 검사는 DB 생성 때 묶음이 합니다(<c>FieldColorsData.Validate</c>).</summary>
        internal void Use(FieldColorsData colors)
        {
            _colors = colors ?? throw new ArgumentNullException(nameof(colors));
        }

        internal Color PlayerZone => _colors.PlayerZoneColor;
        internal Color Rail => _colors.RailColor;
        internal Color Hover => _colors.HoverColor;
        internal Color HoverValid => _colors.HoverValidColor;
        internal Color HoverInvalid => _colors.HoverInvalidColor;
        /// <summary>공격 조준 호버의 판단 전 색입니다. 공격 호버 세 색은 다른 호버(배치·아이템·이동)와 나뉩니다.</summary>
        internal Color AttackHover => _colors.AttackHoverColor;
        internal Color AttackHoverValid => _colors.AttackHoverValidColor;
        /// <summary>공격 조준 호버의 불가 색입니다. 막힌 벽 층도 이 색을 씁니다.</summary>
        internal Color AttackHoverInvalid => _colors.AttackHoverInvalidColor;
        internal Color Affected => _colors.AffectedColor;
        internal Color MovementDraft => _colors.MovementDraftColor;
        internal Color MoveRange => _colors.MoveRangeColor;
        internal Color AttackRange => _colors.AttackRangeColor;
        /// <summary>호버한 적 유닛의 공격 범위 색입니다(아군·공격 모드는 <see cref="AttackRange"/>).</summary>
        internal Color EnemyAttackRange => _colors.EnemyAttackRangeColor;
        /// <summary>호버한 지뢰·화약통의 피해 칸 색입니다(진영을 가리지 않는 위험).</summary>
        internal Color HazardRange => _colors.HazardRangeColor;
        internal Color ActiveActor => _colors.ActiveActorColor;
        internal Color ObjectiveTarget => _colors.ObjectiveTargetColor;
        /// <summary>공격 미리보기의 넉백 도착 칸 색입니다(테두리·채우기).</summary>
        internal Color KnockbackDestination => _colors.KnockbackDestinationColor;
        /// <summary>호버한 아군 유닛의 이동 칸 색입니다(이동 모드 이동 범위는 <see cref="MoveRange"/>).</summary>
        internal Color AllyHoverMoveRange => _colors.AllyHoverMoveRangeColor;
        /// <summary>호버한 적 유닛의 이동 칸 색입니다.</summary>
        internal Color EnemyMoveRange => _colors.EnemyMoveRangeColor;
        /// <summary>낙석이 떨어질 칸의 색입니다(테두리·채우기).</summary>
        internal Color RockfallWarning => _colors.RockfallWarningColor;
        /// <summary>호버한 토네이도·하붑·Rider 먼지 연막의 현재 영역 색입니다.</summary>
        internal Color GimmickHoverRange => _colors.GimmickHoverRangeColor;
        /// <summary>적 본부 증원 범위의 테두리 색입니다.</summary>
        internal Color NexusReinforcementRange => _colors.NexusReinforcementRangeColor;
        /// <summary>포효가 임박한 보스의 포효 범위 색입니다(테두리·채우기, 포효 예고 설계 RF4).</summary>
        internal Color RoarForecast => _colors.RoarForecastColor;
        /// <summary>스킬 모드의 다음 점 후보 칸 색입니다(테두리·채우기, 전투 스킬 View 설계 SK2).</summary>
        internal Color SkillCandidate => _colors.SkillCandidateColor;
        /// <summary>스킬 모드에서 고른 점 색입니다(테두리·채우기).</summary>
        internal Color SkillPoint => _colors.SkillPointColor;

        /// <summary>공격 가능 궤적 점선 색입니다(공격 궤적 설계 TJ8). 칸 강조 색과 따로 궤적 선만 씁니다.</summary>
        internal Color Trajectory => _colors.TrajectoryColor;
        /// <summary>벽에 막힌 궤적 점선 색입니다.</summary>
        internal Color TrajectoryBlocked => _colors.TrajectoryBlockedColor;
        /// <summary>밀리는 대상의 화살표 색입니다(넉백 미리보기 설계 KB6).</summary>
        internal Color Knockback => _colors.KnockbackColor;
        /// <summary>막혀서 덜 가거나 못 가는 대상의 화살표 색입니다.</summary>
        internal Color KnockbackBlocked => _colors.KnockbackBlockedColor;
        /// <summary>넉백 면역 대상의 화살표 색입니다.</summary>
        internal Color KnockbackImmune => _colors.KnockbackImmuneColor;
        /// <summary>부딪힘 피해를 받는 대상(작렬)의 화살표 색입니다.</summary>
        internal Color KnockbackCollision => _colors.KnockbackCollisionColor;
        /// <summary>끌어오기로 끌려오는 대상의 화살표 색입니다(궤적 대상별 선 설계 PL3).</summary>
        internal Color PullArrow => _colors.PullArrowColor;

        /// <summary>칸 채우기 불투명도입니다. 모든 채우기가 함께 쓰고, 강조 색 알파에 곱합니다.</summary>
        internal float FillAlpha => _colors.FillAlpha;

        /// <summary>
        /// 강조 색의 채우기 색입니다. RGB는 그대로, 알파는 강조 색 알파 × <see cref="FillAlpha"/>입니다(겹친 강조 2단계 3판 RC9).
        /// 알파 1인 색은 <see cref="FillAlpha"/> 그대로입니다. 채우기 색을 따로 저장하지 않아 색 원본이 하나로 남습니다.
        /// </summary>
        internal Color ToFill(Color color) => new(color.r, color.g, color.b, color.a * FillAlpha);

        /// <summary>강조 색 종류의 색입니다. 강조가 없으면(<see cref="FieldHighlightColor.None"/>) false입니다.</summary>
        internal bool TryGet(FieldHighlightColor slot, FieldStaticZone zone, out Color color)
        {
            switch (slot)
            {
                case FieldHighlightColor.HoverNeutral: color = Hover; return true;
                case FieldHighlightColor.HoverValid: color = HoverValid; return true;
                case FieldHighlightColor.HoverInvalid: color = HoverInvalid; return true;
                case FieldHighlightColor.AttackHoverNeutral: color = AttackHover; return true;
                case FieldHighlightColor.AttackHoverValid: color = AttackHoverValid; return true;
                case FieldHighlightColor.AttackHoverInvalid: color = AttackHoverInvalid; return true;
                case FieldHighlightColor.Affected: color = Affected; return true;
                case FieldHighlightColor.MovementDraft: color = MovementDraft; return true;
                case FieldHighlightColor.MoveRange: color = MoveRange; return true;
                case FieldHighlightColor.AttackRange: color = AttackRange; return true;
                case FieldHighlightColor.EnemyAttackRange: color = EnemyAttackRange; return true;
                case FieldHighlightColor.HazardRange: color = HazardRange; return true;
                case FieldHighlightColor.ActiveActor: color = ActiveActor; return true;
                case FieldHighlightColor.ObjectiveTarget: color = ObjectiveTarget; return true;
                case FieldHighlightColor.KnockbackDestination: color = KnockbackDestination; return true;
                case FieldHighlightColor.AllyHoverMoveRange: color = AllyHoverMoveRange; return true;
                case FieldHighlightColor.EnemyMoveRange: color = EnemyMoveRange; return true;
                case FieldHighlightColor.RockfallWarning: color = RockfallWarning; return true;
                case FieldHighlightColor.GimmickHoverRange: color = GimmickHoverRange; return true;
                case FieldHighlightColor.NexusReinforcementRange: color = NexusReinforcementRange; return true;
                case FieldHighlightColor.RoarForecast: color = RoarForecast; return true;
                case FieldHighlightColor.SkillCandidate: color = SkillCandidate; return true;
                case FieldHighlightColor.SkillPoint: color = SkillPoint; return true;
                case FieldHighlightColor.Static: return TryGetZone(zone, out color);
                default: color = default; return false;
            }
        }

        internal bool TryGetZone(FieldStaticZone zone, out Color color)
        {
            switch (zone)
            {
                case FieldStaticZone.PlayerZone: color = PlayerZone; return true;
                case FieldStaticZone.Rail: color = Rail; return true;
                default: color = default; return false;
            }
        }
    }
}
