using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules
{
    /// <summary>부여 특성과 독립적으로 수신 효과의 중첩·소비·수명을 적용합니다.</summary>
    public static class AppliedStatusRules
    {
        public static StatusEffectSet Apply(StatusEffectSet current, AppliedStatus incoming)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            if (!incoming.Origin.HasValue) throw new ArgumentException("새 부여에는 출처가 필요합니다.", nameof(incoming));
            if (incoming is WeaknessExposureStatus exposure) return WeaknessExposureRules.Apply(current, exposure);
            if (incoming is AttackDecreaseStatus && current.Entries.Any(s => s is AttackDecreaseStatus)) return current;
            IEnumerable<AppliedStatus> retained = current.Entries;
            if (incoming is ActionRestrictionStatus restriction)
            {
                // 같은 키 제한은 출처와 상관없이 하나다. 남은 횟수가 긴 쪽이 남고, 같으면 새 것으로 바꾼다.
                var existing = current.Entries.OfType<ActionRestrictionStatus>().SingleOrDefault(s => s.Key == incoming.Key);
                if (existing != null && existing.RemainingAffectedSideTurnEnds > restriction.RemainingAffectedSideTurnEnds) return current;
                retained = retained.Where(s => s is not ActionRestrictionStatus || s.Key != incoming.Key);
            }
            else if (incoming is ShieldStatus) retained = retained.Where(s => s is not ShieldStatus || s.Origin != incoming.Origin);
            // 재구성 중 상태는 하나다(전투당 한 번 발동).
            else if (incoming is ReconstructionStatus) retained = retained.Where(s => s is not ReconstructionStatus);
            else if (incoming is not AttackIncreaseStatus && incoming is not AttackDecreaseStatus && incoming is not DefenseDecreaseStatus)
                throw new ArgumentException("아직 지원하지 않는 효과 부여입니다.", nameof(incoming));
            var result = new StatusEffectSet(retained.Append(incoming).ToArray());
            _ = AttackIncrease(result);
            return current.SameValues(result) ? current : result;
        }

        public static StatusEffectSet EndControllerTurn(StatusEffectSet current, Owner affectedSide, Owner endedSide)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (affectedSide != Owner.Player && affectedSide != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(affectedSide));
            if (endedSide != Owner.Player && endedSide != Owner.Enemy) throw new ArgumentOutOfRangeException(nameof(endedSide));
            bool changed = false;
            var remaining = new List<AppliedStatus>(current.Entries.Count);
            foreach (var status in current.Entries)
            {
                if (status is AttackIncreaseStatus grant)
                {
                    changed = true;
                    if (grant.RemainingControllerTurnEnds > 1)
                        remaining.Add(new AttackIncreaseStatus(grant.Origin.Value, grant.Amount, grant.RemainingControllerTurnEnds - 1));
                }
                else if (status is WeaknessExposureStatus exposure && affectedSide == endedSide &&
                    exposure.LifetimeKind == WeaknessExposureLifetimeKind.AffectedSideTurnEnds)
                {
                    changed = true;
                    if (exposure.RemainingAffectedSideTurnEnds > 1)
                        remaining.Add(new WeaknessExposureStatus(exposure.Origin.Value, exposure.IncreasePercent, exposure.LifetimeKind,
                            exposure.RemainingAffectedSideTurnEnds - 1, exposure.Movement));
                }
                else if (status is ActionRestrictionStatus restriction && affectedSide == endedSide)
                {
                    changed = true;
                    var next = EndRestriction(restriction);
                    if (next != null) remaining.Add(next);
                }
                else remaining.Add(status);
            }
            return !changed ? current : remaining.Count == 0 ? StatusEffectSet.Empty : new StatusEffectSet(remaining);
        }

        internal static ActionRestrictionStatus EndRestriction(ActionRestrictionStatus status) =>
            status == null || status.RemainingAffectedSideTurnEnds == 1 ? null :
                new ActionRestrictionStatus(status.Key, status.Origin.Value, status.RemainingAffectedSideTurnEnds - 1);

        public static StatusEffectSet OnAreaChanged(StatusEffectSet current, CharacterRuntimeArea before, CharacterRuntimeArea after)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (!Enum.IsDefined(typeof(CharacterRuntimeArea), before)) throw new ArgumentOutOfRangeException(nameof(before));
            if (!Enum.IsDefined(typeof(CharacterRuntimeArea), after)) throw new ArgumentOutOfRangeException(nameof(after));
            return before != after && (after == CharacterRuntimeArea.Hand || after == CharacterRuntimeArea.EnemyWaiting)
                ? ClearForReserve(current) : current;
        }

        /// <summary>표시 효과는 비우되 방어막의 소모·초기화 이력은 같은 출처에 남깁니다.</summary>
        public static StatusEffectSet ClearForReserve(StatusEffectSet current) => ClearForReserve(current, false);

        /// <summary>표시 효과는 비우되 방어막의 소모·초기화 이력은 같은 출처에 남깁니다.</summary>
        /// <param name="keepShieldBudget">
        /// 거짓이면 남은 예산을 0으로 둡니다(밤 종료 회수 등 — 다시 놓아도 다음 초기화 전까지 비어 있음).
        /// 참이면 남은 예산은 그대로 두고 자격만 끕니다. 배치 단계에서 손으로 손패에 넣는 경우만 씁니다:
        /// 다시 놓으면 필드에서 끌어서 옮긴 것과 같은 예산으로 돌아옵니다(자격이 없는 동안은 흡수·표시 없음).
        /// </param>
        public static StatusEffectSet ClearForReserve(StatusEffectSet current, bool keepShieldBudget)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            var history = new StatusEffectSet(current.Entries.OfType<ShieldStatus>().Where(s => !s.ExpiresAtSideTurnStart.HasValue).Select(s =>
                new ShieldStatus(s.Origin.Value, keepShieldBudget ? s.RemainingBudget : 0, false, s.LastResetControllerTurnCount)).ToArray());
            return current.SameValues(history) ? current : history;
        }

        /// <summary>
        /// 도발당함 항목을 주어진 출처 집합으로 맞춥니다. 집합이 지금과 같으면 같은 참조를 돌려줍니다.
        /// 도발당함 항목은 늘 목록 맨 앞에 출처 순으로 둡니다. 다른 효과 규칙은 자기 것만 바꾸거나 뒤에 붙이므로 이 자리가 유지되고,
        /// 그래서 자리만으로 다시 계산해도(이동 되돌리기) 원래 목록과 순서까지 같습니다.
        /// </summary>
        public static StatusEffectSet ReplaceTaunted(StatusEffectSet current, IEnumerable<EffectOriginKey> origins)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (origins == null) throw new ArgumentNullException(nameof(origins));
            var wanted = origins.Distinct().OrderBy(o => o).ToArray();
            var existing = current.Entries.OfType<TauntedStatus>().Select(s => s.Origin.Value).OrderBy(o => o).ToArray();
            if (wanted.SequenceEqual(existing)) return current;
            var entries = wanted.Select(o => (AppliedStatus)new TauntedStatus(o))
                .Concat(current.Entries.Where(s => s is not TauntedStatus)).ToArray();
            return entries.Length == 0 ? StatusEffectSet.Empty : new StatusEffectSet(entries);
        }

        public static StatusEffectSet CompleteAttack(StatusEffectSet current)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            return current.Entries.Any(s => s is AttackDecreaseStatus)
                ? new StatusEffectSet(current.Entries.Where(s => s is not AttackDecreaseStatus).ToArray()) : current;
        }

        public static bool IsRestricted(StatusEffectSet current, StatusEffectKey key)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (key != StatusEffectKey.Disarm && key != StatusEffectKey.Bind) throw new ArgumentOutOfRangeException(nameof(key));
            return current.Entries.Any(s => s is ActionRestrictionStatus && s.Key == key);
        }

        public static StatusEffectSet ClearForDeathOrBattleEnd(StatusEffectSet current) =>
            current == null ? throw new ArgumentNullException(nameof(current)) : StatusEffectSet.Empty;

        public static int AttackIncrease(StatusEffectSet current)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            int total = 0;
            foreach (var status in current.Entries.OfType<AttackIncreaseStatus>()) total = checked(total + status.Amount);
            return total;
        }

        public static int ApplyAttackDecrease(int attackPower, StatusEffectSet current)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (attackPower < 0) throw new ArgumentOutOfRangeException(nameof(attackPower));
            int percent = current.Entries.OfType<AttackDecreaseStatus>().SingleOrDefault()?.ReductionPercent ?? 0;
            return checked((int)((long)attackPower * (100 - percent) / 100));
        }

        public static int ApplyDefenseDecrease(int defense, StatusEffectSet current)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (defense < 0) throw new ArgumentOutOfRangeException(nameof(defense));
            BigInteger numerator = defense;
            BigInteger denominator = BigInteger.One;
            foreach (var status in current.Entries.OfType<DefenseDecreaseStatus>())
            {
                numerator *= 100 - status.ReductionPercent;
                denominator *= 100;
                if (numerator.IsZero) return 0;
            }
            return (int)(numerator / denominator);
        }
    }
}
