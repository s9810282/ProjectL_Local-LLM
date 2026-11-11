using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    /// <summary>
    /// 한 유닛의 사망 특성 기여를 모은 결과입니다. 자폭은 기여 중 가장 큰 비율 한 번, 공포는 기여가 하나라도 있으면 한 번입니다.
    /// </summary>
    internal sealed class EffectDeathTraitEvaluation
    {
        public bool HasContribution => Contributions.Count > 0;
        public IReadOnlyList<EffectDeathTraitContribution> Contributions { get; }
        public int SelfDestructPercent { get; }
        public bool Fear { get; }
        /// <summary>자폭 비율을 낸 기여의 원본입니다. 자폭하지 않으면 없습니다.</summary>
        public EffectOriginKey? SelfDestructOrigin { get; }
        /// <summary>공포를 낸 첫 기여(원본 순)의 원본입니다. 공포가 없으면 없습니다.</summary>
        public EffectOriginKey? FearOrigin { get; }

        public EffectDeathTraitEvaluation(EffectDeathTraitContribution[] contributions)
        {
            if (contributions == null) throw new ArgumentNullException(nameof(contributions));
            var copied = (EffectDeathTraitContribution[])contributions.Clone();
            Array.Sort(copied, (left, right) => left.Origin.CompareTo(right.Origin));
            var origins = new HashSet<EffectOriginKey>();
            foreach (var contribution in copied)
                if (contribution.Origin.IsValid == false || origins.Add(contribution.Origin) == false)
                    throw new ArgumentException("유효하지 않거나 중복된 사망 특성 기여입니다.", nameof(contributions));

            Contributions = Array.AsReadOnly(copied);
            var selfDestruct = copied.Where(c => c.SelfDestructPercent > 0).OrderByDescending(c => c.SelfDestructPercent).ToArray();
            if (selfDestruct.Length > 0)
            {
                SelfDestructPercent = selfDestruct[0].SelfDestructPercent;
                SelfDestructOrigin = selfDestruct[0].Origin;
            }
            var fear = copied.Where(c => c.Fear).ToArray();
            Fear = fear.Length > 0;
            if (Fear) FearOrigin = fear[0].Origin;
        }
    }
}
