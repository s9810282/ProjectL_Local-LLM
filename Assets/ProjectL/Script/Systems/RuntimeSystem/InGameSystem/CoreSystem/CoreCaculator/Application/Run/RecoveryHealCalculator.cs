using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Run
{
    public static class RecoveryHealCalculator
    {
        public static int Calculate(RecoveryHealSpec heal, HealthStateSnapshot health)
        {
            if (heal == null) throw new ArgumentNullException(nameof(heal));
            int deficit = health.MaxHealth - health.CurrentHealth;
            switch (heal.Mode)
            {
                case RecoveryHealMode.Full: return deficit;
                case RecoveryHealMode.Flat: return Math.Min(heal.Value, deficit);
                // 양수 정수 나눗셈으로 기존 소수점 버림 규칙을 유지한다.
                case RecoveryHealMode.PercentOfMax: return (int)Math.Min((long)health.MaxHealth * heal.Value / 100, deficit);
                default: throw new InvalidOperationException("Unknown recovery heal mode.");
            }
        }
    }
}
