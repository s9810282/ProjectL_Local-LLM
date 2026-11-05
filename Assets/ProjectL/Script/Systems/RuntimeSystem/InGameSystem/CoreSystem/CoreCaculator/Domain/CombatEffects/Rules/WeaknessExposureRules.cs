using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules
{
    /// <summary>최종 증가율과 양수 최댓값에 참여한 출처를 함께 보존합니다. 동률 출처도 모두 포함합니다.</summary>
    public sealed class WeaknessExposureEvaluation
    {
        public int EffectivePercent { get; }
        public IReadOnlyList<WeaknessExposureStatus> UsedSources { get; }

        internal WeaknessExposureEvaluation(int effectivePercent, IReadOnlyList<WeaknessExposureStatus> usedSources)
        {
            EffectivePercent = effectivePercent;
            UsedSources = Array.AsReadOnly(usedSources.ToArray());
        }

        public int Amplify(int damage)
        {
            if (damage < 0) throw new ArgumentOutOfRangeException(nameof(damage));
            return checked((int)((long)damage * (100L + EffectivePercent) / 100));
        }
    }

    /// <summary>특성별 최대 증가율을 더합니다. 같은 논리 출처의 시간제 효과는 교체하고 같은 이동의 중복 접촉은 무시합니다.</summary>
    public static class WeaknessExposureRules
    {
        public static StatusEffectSet Apply(StatusEffectSet current, WeaknessExposureStatus incoming)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));
            var entries = new List<AppliedStatus>(current.Entries.Count + 1);
            bool replaced = false;
            foreach (var status in current.Entries)
            {
                if (status is WeaknessExposureStatus exposure && SameSource(exposure.Origin.Value, incoming.Origin.Value) &&
                    exposure.Movement == incoming.Movement)
                {
                    if (incoming.Movement.HasValue) return current;
                    if (!replaced) entries.Add(incoming);
                    replaced = true;
                }
                else entries.Add(status);
            }
            if (!replaced) entries.Add(incoming);
            var result = new StatusEffectSet(entries);
            _ = Evaluate(result);
            return current.SameValues(result) ? current : result;
        }

        public static WeaknessExposureEvaluation Evaluate(StatusEffectSet current)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            var sources = current.Entries.OfType<WeaknessExposureStatus>().ToArray();
            var maxima = sources.GroupBy(s => (s.Origin.Value.Kind, s.Origin.Value.DefinitionKey))
                .ToDictionary(g => g.Key, g => g.Max(s => s.IncreasePercent));
            int total = 0;
            foreach (int maximum in maxima.Values) total = checked(total + maximum);
            return new WeaknessExposureEvaluation(total, sources.Where(s => s.IncreasePercent > 0 &&
                s.IncreasePercent == maxima[(s.Origin.Value.Kind, s.Origin.Value.DefinitionKey)]).ToArray());
        }

        public static int EffectivePercent(StatusEffectSet current) => Evaluate(current).EffectivePercent;

        public static int Amplify(int damage, StatusEffectSet current) => Evaluate(current).Amplify(damage);

        public static StatusEffectSet RemoveMovement(StatusEffectSet current, MovementEffectId movement)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (!movement.IsValid) throw new ArgumentException("제거할 이동 출처가 유효해야 합니다.", nameof(movement));
            var retained = current.Entries.Where(s => s is not WeaknessExposureStatus exposure || exposure.Movement != movement).ToArray();
            return retained.Length == current.Entries.Count ? current : retained.Length == 0 ? StatusEffectSet.Empty : new StatusEffectSet(retained);
        }

        private static bool SameSource(EffectOriginKey left, EffectOriginKey right) =>
            left.Kind == right.Kind && left.OwnerInstanceId == right.OwnerInstanceId && left.DefinitionKey == right.DefinitionKey;
    }
}
