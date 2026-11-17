using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules
{
    public static class PeriodicStatusRules
    {
        public static StatusEffectSet ApplyBleeding(StatusEffectSet current, EffectOriginKey origin, int damagePerStack)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            var stack = new BleedingStack(origin, damagePerStack);
            var prior = current.Entries.OfType<BleedingStatus>().SingleOrDefault();
            var stacks = prior?.Stacks ?? Array.Empty<BleedingStack>();
            return Replace(current, new BleedingStatus(stacks.Count == 5 ? stacks : stacks.Append(stack).ToArray(), 3));
        }

        public static StatusEffectSet ApplyFire(StatusEffectSet current, EffectOriginKey origin, int remainingAffectedSideStarts = 2)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            var incoming = new FireStatus(origin, remainingAffectedSideStarts);
            var prior = current.Entries.OfType<FireStatus>().SingleOrDefault();
            return prior != null && prior.RemainingAffectedSideStarts > incoming.RemainingAffectedSideStarts
                ? current : Replace(current, incoming);
        }

        public static StatusEffectSet ApplySlow(StatusEffectSet current, EffectOriginKey origin, Owner affectedSide, CombatTurnFacts turn)
        {
            if (turn == null) throw new ArgumentNullException(nameof(turn));
            if (affectedSide != Owner.Player && affectedSide != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(affectedSide));
            if (!turn.Controller.HasValue) throw new ArgumentException("둔화 부여에는 진행 중인 진영 턴이 필요합니다.", nameof(turn));
            // 현재 진영 턴은 이미 시작했으므로 같은 진영에서 부여하면 다음 자기 턴 종료까지 기다립니다.
            long expiry = checked(turn.CompletedControllerTurnCount + (turn.Controller == affectedSide ? 3 : 2));
            return Replace(current, new MovementRangeDecreaseStatus(origin, expiry));
        }

        public static StatusEffectSet ConsumeStartTick(StatusEffectSet current, StatusEffectKey key)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (key != StatusEffectKey.Fire && key != StatusEffectKey.Bleeding) throw new ArgumentOutOfRangeException(nameof(key));
            var prior = current.Entries.SingleOrDefault(s => s.Key == key);
            if (prior == null) return current;
            AppliedStatus next = prior switch
            {
                FireStatus fire when fire.RemainingAffectedSideStarts > 1 => new FireStatus(fire.Origin.Value, fire.RemainingAffectedSideStarts - 1),
                BleedingStatus bleeding when bleeding.RemainingAffectedSideStarts > 1 => new BleedingStatus(bleeding.Stacks, bleeding.RemainingAffectedSideStarts - 1),
                _ => null
            };
            return new StatusEffectSet(current.Entries.Select(s => ReferenceEquals(s, prior) ? next : s).Where(s => s != null).ToArray());
        }

        public static StatusEffectSet ExpireSlow(StatusEffectSet current, long completedControllerTurnCount)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (completedControllerTurnCount < 0) throw new ArgumentOutOfRangeException(nameof(completedControllerTurnCount));
            var entries = current.Entries.Where(s => s is not MovementRangeDecreaseStatus slow ||
                slow.ExpiresAtCompletedControllerTurnCount > completedControllerTurnCount).ToArray();
            return entries.Length == current.Entries.Count ? current : new StatusEffectSet(entries);
        }

        private static StatusEffectSet Replace(StatusEffectSet current, AppliedStatus incoming)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            var prior = current.Entries.SingleOrDefault(s => s.Key == incoming.Key);
            // 같은 값의 재부여도 갱신입니다. 이미 포착한 시작 묶음과 새 수명을 구분할 불변 값을 남깁니다.
            return new StatusEffectSet(prior == null ? current.Entries.Append(incoming).ToArray() :
                current.Entries.Select(s => ReferenceEquals(s, prior) ? incoming : s).ToArray());
        }
    }
}
