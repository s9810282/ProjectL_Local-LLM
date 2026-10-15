using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>
    /// 공격자와 대상 사이 거리에 따라 공격 피해 배율을 바꾸는 특성의 입력입니다.
    /// </summary>
    public sealed class DistanceDamageScaleTraitSpec : TraitSpec
    {
        /// <summary>
        /// 거리별 피해 백분율입니다. 0번이 거리 1이며, 공격이 닿는 모든 거리를 덮어야 합니다(목록 밖 거리는 계산 때 예외).
        /// </summary>
        public IReadOnlyList<int> PercentByDistance { get; }

        public DistanceDamageScaleTraitSpec(IReadOnlyList<int> percentByDistance)
            : base(TraitKey.DistanceDamageScale)
        {
            PercentByDistance = DistanceDamageScalePercents.Copy(percentByDistance, nameof(percentByDistance));
        }
    }

    /// <summary>
    /// 거리별 피해 백분율 목록의 검증과 복사를 한 곳에서 맡습니다.
    /// </summary>
    public static class DistanceDamageScalePercents
    {
        /// <summary>
        /// 목록을 검증하고 변경할 수 없는 사본을 돌려줍니다. 길이는 1 이상, 각 값은 0 이상이어야 합니다.
        /// </summary>
        public static IReadOnlyList<int> Copy(IReadOnlyList<int> percents, string paramName)
        {
            if (percents == null)
                throw new ArgumentNullException(paramName);
            if (percents.Count == 0)
                throw new ArgumentException("거리별 피해 백분율은 하나 이상 필요합니다.", paramName);

            var copy = new int[percents.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                if (percents[i] < 0)
                    throw new ArgumentOutOfRangeException(paramName);
                copy[i] = percents[i];
            }

            return Array.AsReadOnly(copy);
        }

        /// <summary>
        /// 두 목록의 값이 순서까지 모두 같은지 비교합니다.
        /// </summary>
        public static bool SameValues(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            if (left == null || right == null || left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }

            return true;
        }
    }
}
