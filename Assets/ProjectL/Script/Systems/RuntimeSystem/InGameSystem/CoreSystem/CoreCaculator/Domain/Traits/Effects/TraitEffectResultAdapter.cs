using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Transitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal static class TraitEffectResultAdapter
    {
        internal static EffectRuleResult Convert(in EffectRuleInput input, TraitStateChange change)
        {
            EntityId unitId = GetRequiredUnitId(input);
            if (TraitEffectOrigin.GetRequiredTraitKey(input.Origin, unitId) != change.TraitKey)
                throw new InvalidOperationException("Trait origin mismatch");

            if (change.HasChange == false)
                return new EffectRuleResult(null, Array.Empty<EffectActivation>());

            var stateChange = new EffectStateChange(
                input.Origin,
                input.State,
                TraitEffectStateCodec.Capture(change.FinalState));

            if (change.HasActivation == false)
                return new EffectRuleResult(stateChange, Array.Empty<EffectActivation>());

            EntityId targetId = change.ActivationTargetId ?? EntityId.Empty;
            var activation = new EffectActivation(
                input.Origin,
                (int)change.ActivationKind,
                change.ActivationMagnitude,
                targetId);
            return new EffectRuleResult(stateChange, new[] { activation });
        }

        private static EntityId GetRequiredUnitId(in EffectRuleInput input)
        {
            if (input.Origin.OwnerInstanceId <= 0)
                throw new ArgumentException("Trait owner must be a valid unit.", nameof(input));
            return new EntityId(input.Origin.OwnerInstanceId);
        }
    }
}
