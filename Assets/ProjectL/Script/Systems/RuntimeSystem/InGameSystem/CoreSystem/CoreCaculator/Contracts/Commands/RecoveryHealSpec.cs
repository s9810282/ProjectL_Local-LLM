using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class RecoveryHealSpec
    {
        public RecoveryHealMode Mode { get; }
        public int Value { get; }

        public RecoveryHealSpec(RecoveryHealMode mode, int value)
        {
            if (!Enum.IsDefined(typeof(RecoveryHealMode), mode))
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (mode == RecoveryHealMode.PercentOfMax && (value < 1 || value > 100))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (mode == RecoveryHealMode.Flat && value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));

            // Flat permits zero; Full does not use Value.
            Mode = mode;
            Value = value;
        }
    }
}
