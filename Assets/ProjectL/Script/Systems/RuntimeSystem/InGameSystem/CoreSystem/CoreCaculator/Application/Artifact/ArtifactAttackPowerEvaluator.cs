using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact
{

    internal sealed class ArtifactAttackPowerEvaluator
    {

        internal static int Evaluate(CharacterCombatFacts attacker, int soulCount,
            ArtifactCombatFacts facts, CompiledArtifactLoadout loadout, ICombatEffectEngine engine, List<CombatEvent> events = null)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (soulCount < 0) throw new ArgumentOutOfRangeException(nameof(soulCount));
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            int power = checked(attacker.Stats.AttackPower + attacker.TemporaryAttackBonus +
                attacker.PositionModifier.AttackBonus + attacker.HeldEnvironment.SuppliesAttackBonus);
            if (attacker.Owner != Owner.Player) return power;
            if (!attacker.OwnedId.HasValue)
                throw new ArgumentException("Player attack requires an owned character link.", nameof(attacker));
            power = checked(power + soulCount * SoulGrowthRules.AttackBonusPerSoul);
            if (attacker.Health.IsDead || attacker.Area != CharacterRuntimeArea.Field) return power;
            EffectAttackDamageEvaluation evaluation = engine.EvaluateAttackDamage(
                loadout.CaptureBindings(), new AttackDamageEffectContext(
                    attacker.Stats.AttackPower, facts.PlayerUnitCount, facts.EnemyUnitCount));
            int lastStandBonus = checked(evaluation.EffectiveDamage - attacker.Stats.AttackPower);
            var activations = new List<ArtifactActivationSnapshot>();
            foreach (var contribution in evaluation.Contributions)
                if (contribution.FlatBonus > 0)
                    activations.Add(new ArtifactActivationSnapshot(contribution.Origin, attacker.EntityId,
                        contribution.FlatBonus, contribution.FlatBonus));
            if (activations.Count != 0) events?.Add(new CombatArtifactEvent(activations));
            return checked(power + lastStandBonus);
        }
    }
}
