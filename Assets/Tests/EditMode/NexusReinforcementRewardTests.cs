using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class NexusReinforcementRewardTests
    {
        [TestCase(false, 11, 13)]
        [TestCase(true, 0, 0)]
        public void SameType_InitialKillPaysAndReinforcementKillDoesNot(bool reinforced, int gold, int xp)
        {
            var input = CombatKillRewardTests.Input(new UnitKillRewardSpec(11, 13));
            var victim = input.Characters.First(c => c.Owner == Owner.Enemy);
            var nexus = new EntityId(30);
            if (reinforced)
                input = CombatCommandTestData.With(input, state: input.State.With(reinforcements:
                    new NexusReinforcementState(nexus, 19, new[] { new NexusReinforcementRecord(victim.EntityId, 0) }, 1)));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.RewardSettlement.KillGold, Is.EqualTo(gold));
            Assert.That(result.RewardSettlement.KillCount, Is.EqualTo(1));
            Assert.That(result.RewardChange?.FinalGold ?? 7, Is.EqualTo(7 + gold));
            var killer = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(killer.Experience.Earned, Is.EqualTo(xp));
            Assert.That(killer.StageRecord.Kills, Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatExperienceEvent>().Count(e => e.Kind == CombatEventKind.ExperienceEarned),
                Is.EqualTo(reinforced ? 0 : 1));
            Assert.That(input.Profiles.Single(p => p.Definition.Key == victim.CharacterKey).Definition.KillReward.Gold, Is.EqualTo(11));
        }

        [TestCase("uncredited")]
        [TestCase("indirect")]
        [TestCase("reflection")]
        public void CreateThenDie_UsesProjectedProvenance(string attribution)
        {
            var original = CombatKillRewardTests.Input(new UnitKillRewardSpec(11, 13));
            var template = original.Characters.First(c => c.Owner == Owner.Enemy);
            var anchor = new CellCoord(6, 6);
            var created = new CharacterCombatFacts(new EntityId(90), null, template.CharacterKey, Owner.Enemy, null,
                CharacterRuntimeArea.Field, template.Health, template.Stats, null, anchor, Rotation4.R0, new[] { anchor },
                UnitActionUse.Deferred, experience: template.Experience);
            var dead = created.WithPlacement(CharacterRuntimeArea.Excluded, null, null, null, Array.Empty<CellCoord>(),
                health: new HealthStateSnapshot(0, created.Health.MaxHealth));
            var input = new CombatCommandInput(0, CombatCommandKind.StartControllerTurn,
                EntityId.Empty, default, original.State, original.Characters, original.Profiles, original.Main, original.Owned,
                original.SoulCounts, original.Souls, original.Artifacts, battle: original.Battle,
                processingKind: CombatProcessingKind.Advance, goldBalance: 7, fameBalance: 0);
            var record = new NexusReinforcementRecord(created.EntityId, 0);
            var nexus = new EntityId(30);
            var proposed = input.State.With(reinforcements: new NexusReinforcementState(nexus, 19, new[] { record }, 1));
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected.Add(dead.EntityId, dead);
            var events = new List<CombatEvent>
            {
                new CombatReinforcementEvent(nexus, record, created)
            };
            if (attribution == "reflection")
                events.Add(new CombatArtifactEvent(new[] { new ArtifactActivationSnapshot(
                    new EffectOriginKey(EffectOwnerKind.Artifact, 13, 1002, "night_reflect_damage"), dead.EntityId, 3, 3) },
                    new ThornHeartReflectionContext(original.ActorId, 1, 2)));
            events.Add(new CombatCharacterEvent(CombatEventKind.Died, created, dead,
                new[] { new CombatDamageOrigin(anchor, attribution == "indirect" ? original.ActorId : (EntityId?)null, 2) }));
            var result = CombatCommandCalculator.Finish(input, projected, input.SoulCounts.ToDictionary(p => p.Key, p => p.Value),
                input.Souls, Array.Empty<StructureCombatFacts>(), new List<long>(),
                new Dictionary<EntityId, CellCoord>(), new SoulEffectCalculator(input.Artifacts), null, proposed, events: events);

            Assert.That(result.Characters.Created.Single().Health.IsDead, Is.True);
            Assert.That(result.Characters.Created.Single().ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            Assert.That(result.FinalState.Reinforcements.Records, Has.Count.EqualTo(1));
            Assert.That(result.RewardSettlement.KillGold, Is.Zero);
            Assert.That(result.RewardSettlement.KillCount, Is.EqualTo(attribution == "uncredited" ? 0 : 1));
            Assert.That(result.Events.OfType<CombatExperienceEvent>(), Is.Empty);
        }
    }
}
