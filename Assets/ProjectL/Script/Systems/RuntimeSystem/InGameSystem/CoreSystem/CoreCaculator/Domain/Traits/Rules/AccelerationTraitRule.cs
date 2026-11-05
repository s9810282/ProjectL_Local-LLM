using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    /// <summary>가속의 보유 원본과 그 공격력 투영을 함께 갱신합니다.</summary>
    internal sealed class AccelerationTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Acceleration;
        internal int Gain(int held, int completedSteps, int perCell) => checked(held + completedSteps * perCell);
        internal static int Held(UnitStatsSnapshot stats) => stats.TraitStatuses.ToArray().OfType<AccelerationTraitStatusSnapshot>().SingleOrDefault()?.HeldAttackBonus ?? 0;
        internal static UnitStatsSnapshot ProjectChange(UnitStatsSnapshot before, UnitStatsSnapshot after) =>
            after.With(traitAttackBonus: checked(before.TraitAttackBonus - Held(before) + Held(after)));
        internal static UnitStatsSnapshot UndoGain(UnitStatsSnapshot stats, int added) => WithHeld(stats, checked(Held(stats) - added));
        internal static UnitStatsSnapshot WithHeld(UnitStatsSnapshot stats, int held)
        {
            if (held < 0) throw new ArgumentOutOfRangeException(nameof(held));
            return ProjectChange(stats, stats.With(traitStatuses: stats.TraitStatuses.ToArray()
                .Select(t => t.Key == TraitKey.Acceleration ? new AccelerationTraitStatusSnapshot(held) : t).ToArray()));
        }
    }
}
