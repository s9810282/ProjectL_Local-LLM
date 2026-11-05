using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts
{
    internal readonly struct MoveSucceededEffectContext
    {
        public int Distance { get; }
        public bool AnchorChanged { get; }
        /// <summary>성공한 이동의 방향입니다.</summary>
        public Rotation4 MoveDirection { get; }

        public MoveSucceededEffectContext(int distance,bool anchorChanged)
            : this(distance,anchorChanged,Rotation4.R0)
        {
        }

        public MoveSucceededEffectContext(
            int distance,            bool anchorChanged,            Rotation4 moveDirection)
        {
            if (distance < 0) throw new ArgumentOutOfRangeException(nameof(distance));
            if (Enum.IsDefined(typeof(Rotation4), moveDirection) == false)
                throw new ArgumentOutOfRangeException(nameof(moveDirection));

            Distance = distance;
            AnchorChanged = anchorChanged;

            MoveDirection = moveDirection;
        }
    }
}
