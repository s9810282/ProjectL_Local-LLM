using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class ThornBombRules : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.ThornBomb;

        /// <summary>폭발이 맞히는 칸 거리다. 폭심지와 인접 8칸(설계 R08).</summary>
        internal const int BlastRadius = 1;

        /// <summary>폭심지 바깥 유닛을 미는 칸 수다(설계 R08).</summary>
        internal const int KnockbackDistance = 1;

        internal static bool IsInBlast(CellCoord cell, CellCoord center) =>
            Math.Abs(cell.X - center.X) <= BlastRadius && Math.Abs(cell.Y - center.Y) <= BlastRadius;

        /// <summary>폭발이 맞히는 칸 중 판 안의 칸이다. 행 우선(Y, X) 순서다. 미리보기 범위 표시가 쓴다.</summary>
        internal static CellCoord[] BlastCells(CellCoord center, int boardWidth, int boardHeight)
        {
            var cells = new List<CellCoord>((BlastRadius * 2 + 1) * (BlastRadius * 2 + 1));
            for (int y = center.Y - BlastRadius; y <= center.Y + BlastRadius; y++)
                for (int x = center.X - BlastRadius; x <= center.X + BlastRadius; x++)
                    if (x >= 0 && y >= 0 && x < boardWidth && y < boardHeight)
                        cells.Add(new CellCoord(x, y));
            return cells.ToArray();
        }

        internal static int CalculateBaseDamage(int capturedAttackPower, int damagePercent)
        {
            if (capturedAttackPower < 0) throw new ArgumentOutOfRangeException(nameof(capturedAttackPower));
            if (damagePercent < 0) throw new ArgumentOutOfRangeException(nameof(damagePercent));
            return checked((int)((long)capturedAttackPower * damagePercent / 100));
        }

        internal static long CalculateDueSequence(long attachedControllerTurnSequence)
        {
            if (attachedControllerTurnSequence < 1)
                throw new ArgumentOutOfRangeException(nameof(attachedControllerTurnSequence));
            return checked(attachedControllerTurnSequence + 2);
        }
    }
}
