using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    internal readonly struct MoveSucceededEffectContext
    {
        public int Distance { get; }
        public bool AnchorChanged { get; }
        public int ActionCostSpent { get; }
        /// <summary>방어 Facing 갱신에 쓰는 이동 방향입니다.</summary>
        public Rotation4 MoveDirection { get; }

        public MoveSucceededEffectContext(int distance, bool anchorChanged, int actionCostSpent)
            : this(distance, anchorChanged, actionCostSpent, Rotation4.R0)
        {
        }

        public MoveSucceededEffectContext(
            int distance,
            bool anchorChanged,
            int actionCostSpent,
            Rotation4 moveDirection)
        {
            if (distance < 0) throw new ArgumentOutOfRangeException(nameof(distance));
            if (actionCostSpent < 0) throw new ArgumentOutOfRangeException(nameof(actionCostSpent));
            if (Enum.IsDefined(typeof(Rotation4), moveDirection) == false)
                throw new ArgumentOutOfRangeException(nameof(moveDirection));

            Distance = distance;
            AnchorChanged = anchorChanged;
            ActionCostSpent = actionCostSpent;
            MoveDirection = moveDirection;
        }
    }
}
