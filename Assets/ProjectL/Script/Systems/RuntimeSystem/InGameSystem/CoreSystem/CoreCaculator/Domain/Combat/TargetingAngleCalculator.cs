using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>진영의 고정 정면을 기준으로 지정 각도의 포함 여부를 계산합니다.</summary>
    internal static class TargetingAngleCalculator
    {
        internal static bool Contains(TargetingChoiceAngle angle, Owner owner, CellCoord origin, CellCoord target)
        {
            Validate(angle, owner);
            long x = (long)target.X - origin.X;
            long y = (long)target.Y - origin.Y;
            if (x == 0 && y == 0)
                return false;

            long forward = owner == Owner.Player ? y : -y;
            switch (angle)
            {
                case TargetingChoiceAngle.Unrestricted: return true;
                case TargetingChoiceAngle.Front: return forward > 0 && Math.Abs(x) <= forward;
                case TargetingChoiceAngle.FrontAndSides: return forward >= 0;
                case TargetingChoiceAngle.FrontAndRear: return Math.Abs(x) <= Math.Abs(forward);
                default: throw new ArgumentOutOfRangeException(nameof(angle));
            }
        }

        /// <summary>빈 마스크에서도 정의와 진영의 유효성을 먼저 검사합니다.</summary>
        internal static void Validate(TargetingChoiceAngle angle, Owner owner)
        {
            if ((uint)angle > (uint)TargetingChoiceAngle.FrontAndRear)
                throw new ArgumentOutOfRangeException(nameof(angle));
            if (owner != Owner.Player && owner != Owner.Enemy)
                throw new ArgumentOutOfRangeException(nameof(owner));
        }

        /// <summary>호출자가 새로 만든 오프셋 배열을 압축하며 원래 순서와 모양의 경계를 유지합니다.</summary>
        internal static CellCoord[] FilterOffsets(TargetingChoiceAngle angle, Owner owner, CellCoord[] offsets)
        {
            Validate(angle, owner);
            if (angle == TargetingChoiceAngle.Unrestricted)
                return offsets;

            int count = 0;
            for (int i = 0; i < offsets.Length; i++)
            {
                if (Contains(angle, owner, CellCoord.Zero, offsets[i]))
                    offsets[count++] = offsets[i];
            }
            if (count != offsets.Length)
                Array.Resize(ref offsets, count);
            return offsets;
        }
    }
}
