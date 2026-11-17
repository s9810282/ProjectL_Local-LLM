using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills
{
    public static class PlayerSkillCalculator
    {
        public static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.Kind != CombatCommandKind.UsePlayerSkill)
                throw new ArgumentException("스킬 명령이 필요합니다.", nameof(input));
            return CombatCommandCalculator.Calculate(input, rules);
        }

        internal static CombatCommandTransition CalculateCast(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            var source = input.PlayerSkillRequest?.ExpectedSource;
            var availability = PlayerSkillQueries.Availability(input, source ?? input.PlayerSkillSource.GetValueOrDefault());
            if (!availability.CanUse) return CombatCommandCalculator.Reject(input, availability.FailureReason.ToString());
            if (!source.HasValue) return CombatCommandCalculator.Reject(input, nameof(PlayerSkillFailureReason.SourceChanged));

            var selection = PlayerSkillQueries.SelectionActual(input, source.Value);
            if (!selection.PrefixIsValid) return CombatCommandCalculator.Reject(input, selection.FailureReason.ToString());
            if (!selection.IsComplete)
                return CombatCommandCalculator.Reject(input, nameof(PlayerSkillFailureReason.WrongPointCount));

            MovementEnvironmentCalculator.RequireRules(rules);
            return availability.SkillKey == PlayerSkillKey.MagicBullet
                ? MagicBulletCalculator.Calculate(input, rules) : PowderBlossomCalculator.Calculate(input, rules);
        }

        internal static CombatCommandTransition Complete(CombatCommandInput input, MovementProjection arrival)
        {
            var original = input.State.PlayerSkill;
            var settlement = new PlayerSkillUseSettlement(input.ActorId, original.RemainingUses,
                original.LastUsedControllerTurnSequence, input.State.Turn.ControllerTurnSequence);
            var transition = arrival.Complete();
            var result = new PlayerSkillResult(original.Definition, original.UpgradeKey, transition.Events, settlement);
            var state = transition.FinalState.With(playerSkill: original.WithUsage(settlement.FinalRemainingUses,
                settlement.FinalLastUsedControllerTurnSequence));
            return transition.WithPlayerSkill(result, state);
        }
    }
}
