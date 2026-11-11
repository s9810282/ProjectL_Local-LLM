using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 칸 강조 색 묶음입니다. 원본은 <see cref="FieldView"/>의 씬 직렬화 색이고, 표시기는 이 값으로만 색을 고릅니다.
    /// </summary>
    internal sealed class FieldHighlightPalette
    {
        internal FieldHighlightPalette(
            Color playerZone,
            Color rail,
            Color hover,
            Color hoverValid,
            Color hoverInvalid,
            Color attackHover,
            Color attackHoverValid,
            Color attackHoverInvalid,
            Color affected,
            Color movementDraft,
            Color moveRange,
            Color attackRange,
            Color enemyAttackRange,
            Color hazardRange,
            Color activeActor,
            Color objectiveTarget,
            Color knockbackDestination,
            Color allyHoverMoveRange,
            Color enemyMoveRange,
            Color rockfallWarning,
            Color nexusReinforcementRange,
            float fillAlpha,
            Color gimmickHoverRange = default,
            Color roarForecast = default)
        {
            if (!float.IsFinite(fillAlpha) || fillAlpha < 0f || fillAlpha > 1f)
                throw new ArgumentOutOfRangeException(nameof(fillAlpha), fillAlpha, "채우기 알파는 [0, 1]이어야 합니다.");

            PlayerZone = playerZone;
            Rail = rail;
            Hover = hover;
            HoverValid = hoverValid;
            HoverInvalid = hoverInvalid;
            AttackHover = attackHover;
            AttackHoverValid = attackHoverValid;
            AttackHoverInvalid = attackHoverInvalid;
            Affected = affected;
            MovementDraft = movementDraft;
            MoveRange = moveRange;
            AttackRange = attackRange;
            EnemyAttackRange = enemyAttackRange;
            HazardRange = hazardRange;
            ActiveActor = activeActor;
            ObjectiveTarget = objectiveTarget;
            KnockbackDestination = knockbackDestination;
            AllyHoverMoveRange = allyHoverMoveRange;
            EnemyMoveRange = enemyMoveRange;
            RockfallWarning = rockfallWarning;
            NexusReinforcementRange = nexusReinforcementRange;
            GimmickHoverRange = gimmickHoverRange;
            RoarForecast = roarForecast;
            FillAlpha = fillAlpha;
        }

        internal Color PlayerZone { get; }
        internal Color Rail { get; }
        internal Color Hover { get; }
        internal Color HoverValid { get; }
        internal Color HoverInvalid { get; }
        /// <summary>공격 조준 호버의 판단 전 색입니다. 공격 호버 세 색은 다른 호버(배치·아이템·이동)와 나뉩니다.</summary>
        internal Color AttackHover { get; }
        internal Color AttackHoverValid { get; }
        /// <summary>공격 조준 호버의 불가 색입니다. 막힌 벽 층도 이 색을 씁니다.</summary>
        internal Color AttackHoverInvalid { get; }
        internal Color Affected { get; }
        internal Color MovementDraft { get; }
        internal Color MoveRange { get; }
        internal Color AttackRange { get; }
        /// <summary>호버한 적 유닛의 공격 범위 색입니다(아군·공격 모드는 <see cref="AttackRange"/>).</summary>
        internal Color EnemyAttackRange { get; }
        /// <summary>호버한 지뢰·화약통의 피해 칸 색입니다(진영을 가리지 않는 위험).</summary>
        internal Color HazardRange { get; }
        internal Color ActiveActor { get; }
        internal Color ObjectiveTarget { get; }
        /// <summary>공격 미리보기의 넉백 도착 칸 색입니다(테두리·채우기).</summary>
        internal Color KnockbackDestination { get; }
        /// <summary>호버한 아군 유닛의 이동 칸 색입니다(이동 모드 이동 범위는 <see cref="MoveRange"/>).</summary>
        internal Color AllyHoverMoveRange { get; }
        /// <summary>호버한 적 유닛의 이동 칸 색입니다.</summary>
        internal Color EnemyMoveRange { get; }
        /// <summary>낙석이 떨어질 칸의 색입니다(테두리·채우기).</summary>
        internal Color RockfallWarning { get; }
        /// <summary>호버한 토네이도·하붑·Rider 먼지 연막의 현재 영역 색입니다.</summary>
        internal Color GimmickHoverRange { get; }
        /// <summary>적 본부 증원 범위의 테두리 색입니다.</summary>
        internal Color NexusReinforcementRange { get; }
        /// <summary>포효가 임박한 보스의 포효 범위 색입니다(테두리·채우기, 포효 예고 설계 RF4).</summary>
        internal Color RoarForecast { get; }

        /// <summary>칸 채우기 불투명도입니다. 모든 채우기가 함께 쓰고, 강조 색 알파에 곱합니다.</summary>
        internal float FillAlpha { get; }

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
