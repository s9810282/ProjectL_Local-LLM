using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits
{
    /// <summary>
    /// 포효 정의 값의 조건 원본이다. 정의 계약(<see cref="RoarTraitSpec"/>·RoarTraitDefinition)과 DB 데이터(RoarTraitData)가 같은 조건을 쓴다.
    /// </summary>
    public static class RoarCountRules
    {
        /// <summary>
        /// 임계값은 1 이상, 증가량은 0 이상이며 증가량 중 하나는 양수여야 한다(모두 0이면 포효하지 않는다).
        /// 조건을 어긴 첫 인자 이름을, 문제가 없으면 null을 돌려준다.
        /// </summary>
        public static string FindDefinitionError(int hitThreshold, int countPerHit, int countPerOwnSideTurnEnd, int countPerOpposingSideTurnEnd)
        {
            if (hitThreshold < 1) return nameof(hitThreshold);
            if (countPerHit < 0) return nameof(countPerHit);
            if (countPerOwnSideTurnEnd < 0) return nameof(countPerOwnSideTurnEnd);
            if (countPerOpposingSideTurnEnd < 0) return nameof(countPerOpposingSideTurnEnd);
            if (countPerHit == 0 && countPerOwnSideTurnEnd == 0 && countPerOpposingSideTurnEnd == 0) return nameof(countPerHit);
            return null;
        }

        /// <summary><see cref="FindDefinitionError"/>가 문제를 찾으면 그 인자 이름으로 <see cref="ArgumentOutOfRangeException"/>을 던진다.</summary>
        public static void RequireDefinition(int hitThreshold, int countPerHit, int countPerOwnSideTurnEnd, int countPerOpposingSideTurnEnd)
        {
            string error = FindDefinitionError(hitThreshold, countPerHit, countPerOwnSideTurnEnd, countPerOpposingSideTurnEnd);
            if (error != null) throw new ArgumentOutOfRangeException(error);
        }
    }
}
