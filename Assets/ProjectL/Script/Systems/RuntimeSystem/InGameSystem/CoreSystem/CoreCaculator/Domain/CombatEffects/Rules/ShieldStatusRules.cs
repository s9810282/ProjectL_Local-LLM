using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules
{
    /// <summary>출처별 방어막 예산·자격·초기화와 흡수를 부여 특성에 독립적으로 계산합니다.</summary>
    public static class ShieldStatusRules
    {
        public static ShieldStatus Reconcile(ShieldStatus prior, EffectOriginKey origin, int capacity,
            bool eligible, long completedControllerTurns, int resetInterval, bool resetDue)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (completedControllerTurns < 0) throw new ArgumentOutOfRangeException(nameof(completedControllerTurns));
            if (resetInterval < 1) throw new ArgumentOutOfRangeException(nameof(resetInterval));
            if (prior != null && prior.Origin != origin) throw new ArgumentException("방어막 출처가 일치하지 않습니다.", nameof(prior));
            if (prior == null && !eligible) return null;
            long boundary = completedControllerTurns - completedControllerTurns % resetInterval;
            if (prior != null && boundary < prior.LastResetControllerTurnCount) throw new InvalidOperationException("방어막 초기화 경계가 역행했습니다.");
            int remaining = prior == null || (resetDue && boundary > prior.LastResetControllerTurnCount) ? capacity : prior.RemainingBudget;
            return new ShieldStatus(origin, remaining, eligible, prior != null && !resetDue ? prior.LastResetControllerTurnCount : boundary);
        }

        public static StatusEffectSet Absorb(StatusEffectSet current, int incoming, out int remaining)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (incoming < 0) throw new ArgumentOutOfRangeException(nameof(incoming));
            var shields = current.Entries.OfType<ShieldStatus>().ToArray();
            if (shields.Select(s => s.Origin).Distinct().Count() != shields.Length)
                throw new ArgumentException("방어막 출처가 중복되었습니다.", nameof(current));
            remaining = incoming;
            var entries = current.Entries.ToArray();
            bool changed = false;
            foreach (var shield in shields.Where(s => s.Eligible && s.RemainingBudget > 0).OrderBy(s => s.Origin.Value))
            {
                int absorbed = Math.Min(remaining, shield.RemainingBudget);
                if (absorbed == 0) break;
                entries[Array.IndexOf(entries, shield)] = new ShieldStatus(shield.Origin.Value,
                    shield.RemainingBudget - absorbed, shield.Eligible, shield.LastResetControllerTurnCount, shield.ExpiresAtSideTurnStart);
                remaining -= absorbed;
                changed = true;
            }
            return changed ? new StatusEffectSet(entries) : current;
        }

        /// <summary>확인된 한 출처만 이전 예산·자격으로 돌립니다. 다른 출처의 소모 이력은 유지합니다.</summary>
        public static StatusEffectSet Restore(StatusEffectSet current, EffectOriginKey origin, ShieldStatus previous)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (previous != null && previous.Origin != origin) throw new ArgumentException("복원 출처가 다릅니다.", nameof(previous));
            var entries = current.Entries.ToList();
            int index = entries.FindIndex(s => s is ShieldStatus && s.Origin == origin);
            if (index >= 0)
            {
                if (previous == null) entries.RemoveAt(index);
                else entries[index] = previous;
            }
            else if (previous != null) entries.Add(previous);
            var result = new StatusEffectSet(entries);
            return current.SameValues(result) ? current : result;
        }
    }
}
