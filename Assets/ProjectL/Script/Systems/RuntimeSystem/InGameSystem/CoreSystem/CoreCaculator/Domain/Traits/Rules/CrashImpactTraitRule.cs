using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    /// <summary>작렬의 정체성이다. 부딪힘 피해 식은 <c>CrashImpactCollisionRules</c> 한 곳에 있다.</summary>
    internal sealed class CrashImpactTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.CrashImpact;
    }
}
