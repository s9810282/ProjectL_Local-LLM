using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    public enum AttackSweepSide
    {
        Left = 1,
        Right = 2,
    }

    public static class AttackSweepSideValidation
    {
        public static void Validate(AttackSweepSide? side)
        {
            if (side.HasValue && side != AttackSweepSide.Left && side != AttackSweepSide.Right)
                throw new ArgumentOutOfRangeException(nameof(side));
        }
    }
}
