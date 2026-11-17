using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    /// <summary>
    /// 공격자-대상 거리에 맞는 피해 백분율을 정합니다.
    /// 목록은 원래 공격이 닿는 거리를 덮어야 합니다. 쓸기 때문에 목록 밖으로 밀린 대상에게는 마지막 값을 적용합니다.
    /// </summary>
    internal sealed class DistanceDamageScaleTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.DistanceDamageScale;

        internal int EvaluatePercent(DistanceDamageScaleTraitDefinition definition, int distance, int? preSweepDistance = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (distance < 1)
                throw new ArgumentOutOfRangeException(nameof(distance));
            int requiredDistance = preSweepDistance ?? distance;
            int count = definition.PercentByDistance.Count;
            if (requiredDistance > count)
            {
                throw new InvalidOperationException(
                    $"DistanceDamageScale 설정에 거리 {requiredDistance}의 피해 백분율이 없습니다. " +
                    $"목록 길이({count})가 공격이 닿는 최대 거리보다 짧습니다.");
            }

            return definition.PercentByDistance[Math.Min(distance, count) - 1];
        }
    }
}
