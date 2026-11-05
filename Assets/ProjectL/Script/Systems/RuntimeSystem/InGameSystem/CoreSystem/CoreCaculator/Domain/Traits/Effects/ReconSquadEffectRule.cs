using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal sealed class ReconSquadEffectRule : ICombatEffectRule
    {
        private readonly ReconSquadTraitDefinition _definition;
        private readonly ReconSquadTraitRule _rule;
        internal ReconSquadEffectRule(ReconSquadTraitDefinition definition, ReconSquadTraitRule rule)
        {
            _definition = definition ?? throw new ArgumentNullException(nameof(definition));
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
        internal WeaknessExposureStatus Grant(in EffectRuleInput input, CharacterCombatFacts actor,
            CharacterCombatFacts target, CellCoord cell, MovementEffectId? movement)
        {
            if (input.State is not StatelessEffectStateSnapshot || TraitEffectOrigin.GetRequiredTraitKey(input.Origin, actor.EntityId) != _rule.Key)
                throw new ArgumentException("특성 출처와 상태가 일치해야 합니다.", nameof(input));
            return _rule.CanApply(actor, target, cell, _definition.Radius)
                ? new WeaknessExposureStatus(input.Origin, _definition.IncreasePercent,
                    WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath, movement: movement) : null;
        }
    }
}
