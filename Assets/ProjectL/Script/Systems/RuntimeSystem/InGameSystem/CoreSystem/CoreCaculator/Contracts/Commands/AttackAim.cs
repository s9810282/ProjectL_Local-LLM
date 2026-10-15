using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public readonly struct AttackAim
    {
        public AttackAimKind Kind { get; }
        public CellCoord? Cell { get; }
        public AttackDirection Direction { get; }

        private AttackAim(
            AttackAimKind kind,
            CellCoord? cell,
            AttackDirection direction)
        {
            if (Enum.IsDefined(typeof(AttackDirection), direction) == false)
                throw new ArgumentOutOfRangeException(nameof(direction));

            Kind = kind;
            Cell = cell;
            Direction = direction;
        }

        public static AttackAim ForCell(CellCoord cell)
        {
            return new AttackAim(AttackAimKind.Cell, cell, AttackDirection.None);
        }

        /// <summary>방향만 전달합니다. 지정 모양에 따른 방향 허용 여부는 계산 계층이 판단합니다.</summary>
        public static AttackAim ForDirection(AttackDirection direction)
        {
            if (direction == AttackDirection.None)
                throw new ArgumentOutOfRangeException(nameof(direction));

            return new AttackAim(AttackAimKind.Direction, null, direction);
        }

        /// <summary>전체 지정 의도입니다. 임의의 대상이나 셀을 요구하지 않습니다.</summary>
        public static AttackAim ForAll()
        {
            return new AttackAim(AttackAimKind.All, null, AttackDirection.None);
        }
    }
}
