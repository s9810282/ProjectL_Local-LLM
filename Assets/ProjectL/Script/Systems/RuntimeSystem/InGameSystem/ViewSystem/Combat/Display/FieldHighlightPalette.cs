using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 칸 강조 색 묶음입니다. 원본은 <see cref="FieldView"/>의 씬 직렬화 색이고, 표시기는 이 값으로만 색을 고릅니다.
    /// </summary>
    internal sealed class FieldHighlightPalette
    {
        internal FieldHighlightPalette(
            Color playerZone,
            Color enemyZone,
            Color rail,
            Color hover,
            Color hoverValid,
            Color hoverInvalid,
            Color affected,
            Color movementDraft,
            Color range,
            Color activeActor)
        {
            PlayerZone = playerZone;
            EnemyZone = enemyZone;
            Rail = rail;
            Hover = hover;
            HoverValid = hoverValid;
            HoverInvalid = hoverInvalid;
            Affected = affected;
            MovementDraft = movementDraft;
            Range = range;
            ActiveActor = activeActor;
        }

        internal Color PlayerZone { get; }
        internal Color EnemyZone { get; }
        internal Color Rail { get; }
        internal Color Hover { get; }
        internal Color HoverValid { get; }
        internal Color HoverInvalid { get; }
        internal Color Affected { get; }
        internal Color MovementDraft { get; }
        internal Color Range { get; }
        internal Color ActiveActor { get; }

        /// <summary>강조 색 종류의 색입니다. 강조가 없으면(<see cref="FieldHighlightColor.None"/>) false입니다.</summary>
        internal bool TryGet(FieldHighlightColor slot, FieldStaticZone zone, out Color color)
        {
            switch (slot)
            {
                case FieldHighlightColor.HoverNeutral: color = Hover; return true;
                case FieldHighlightColor.HoverValid: color = HoverValid; return true;
                case FieldHighlightColor.HoverInvalid: color = HoverInvalid; return true;
                case FieldHighlightColor.Affected: color = Affected; return true;
                case FieldHighlightColor.MovementDraft: color = MovementDraft; return true;
                case FieldHighlightColor.Range: color = Range; return true;
                case FieldHighlightColor.ActiveActor: color = ActiveActor; return true;
                case FieldHighlightColor.Static: return TryGetZone(zone, out color);
                default: color = default; return false;
            }
        }

        internal bool TryGetZone(FieldStaticZone zone, out Color color)
        {
            switch (zone)
            {
                case FieldStaticZone.PlayerZone: color = PlayerZone; return true;
                case FieldStaticZone.EnemyZone: color = EnemyZone; return true;
                case FieldStaticZone.Rail: color = Rail; return true;
                default: color = default; return false;
            }
        }
    }
}
