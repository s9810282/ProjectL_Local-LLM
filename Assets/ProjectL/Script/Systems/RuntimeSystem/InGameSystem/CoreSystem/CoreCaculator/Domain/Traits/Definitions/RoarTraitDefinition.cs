using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions
{
    /// <summary>포효 정의다. 값의 뜻과 조건은 <see cref="RoarTraitSpec"/>·<see cref="RoarCountRules"/>와 같다.</summary>
    public sealed class RoarTraitDefinition : TraitDefinition
    {
        public int HitThreshold { get; }
        public int CountPerHit { get; }
        public int CountPerOwnSideTurnEnd { get; }
        public int CountPerOpposingSideTurnEnd { get; }

        public RoarTraitDefinition(int hitThreshold, int countPerHit = 1, int countPerOwnSideTurnEnd = 0, int countPerOpposingSideTurnEnd = 0)
            : base(TraitKey.Roar)
        {
            RoarCountRules.RequireDefinition(hitThreshold, countPerHit, countPerOwnSideTurnEnd, countPerOpposingSideTurnEnd);
            HitThreshold = hitThreshold;
            CountPerHit = countPerHit;
            CountPerOwnSideTurnEnd = countPerOwnSideTurnEnd;
            CountPerOpposingSideTurnEnd = countPerOpposingSideTurnEnd;
        }
    }
}
