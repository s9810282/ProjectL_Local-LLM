using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class LeapTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Leap;
        internal static bool AllowsIntermediate(CharacterCombatFacts actor) => actor.Area == CharacterRuntimeArea.Field &&
            !actor.Health.IsDead && actor.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Leap);

        internal static bool SkipsMineEntry(bool leapActive, bool isFinalStep) => leapActive && !isFinalStep;
    }
}
