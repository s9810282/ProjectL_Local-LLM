using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    /// <summary>
    /// 공격자-대상 거리에 맞는 피해 백분율을 정합니다.
    /// 목록은 이 유닛이 맞힐 수 있는 모든 거리를 덮어야 하며, 목록 밖 거리는 설정 오류로 처리합니다.
    /// </summary>
    internal sealed class DistanceDamageScaleTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.DistanceDamageScale;

        internal int EvaluatePercent(DistanceDamageScaleTraitDefinition definition, int distance)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (distance < 1)
                throw new ArgumentOutOfRangeException(nameof(distance));
            if (distance > definition.PercentByDistance.Count)
            {
                throw new InvalidOperationException(
                    $"DistanceDamageScale 설정에 거리 {distance}의 피해 백분율이 없습니다. " +
                    $"목록 길이({definition.PercentByDistance.Count})가 공격이 닿는 최대 거리보다 짧습니다.");
            }

            return definition.PercentByDistance[distance - 1];
        }
    }
}
