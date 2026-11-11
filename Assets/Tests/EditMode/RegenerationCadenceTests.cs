using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class RegenerationCadenceTests
    {
        [TestCase(0)]
        [TestCase(-1)]
        public void Configuration_RejectsNonpositiveIntervals(int interval)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegenerationTraitSpec(10, interval));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegenerationTraitDefinition(10, interval));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegenerationTraitStatusSnapshot(10, interval));
        }

        [Test]
        public void RegistryProviderAndCodec_RoundTripProgressWithoutChangingOldSnapshots()
        {
            var registry = TraitRegistry.CreateDefault();
            var spec = CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 100, CategoryKey.Main, new RegenerationTraitSpec(10, 2));
            var definition = new UnitCatalog(new[] { spec }, registry).GetRequired(spec.Key);
            var configured = (RegenerationTraitDefinition)definition.TraitDefinitions.Span[0];
            var registration = registry.GetRequiredRegistration(TraitKey.Regeneration);
            var fresh = (RegenerationRuntimeState)registration.TraitRuntimeStateFactory.BuildRuntimeState(configured);
            Assert.That(configured.HealAmount, Is.EqualTo(10));
            Assert.That(configured.HealEveryOwnSideTurnEnds, Is.EqualTo(2));
            Assert.That(fresh.CompletedEligibleTurnEnds, Is.Zero);

            var state = new RegenerationRuntimeState(1);
            var captured = (RegenerationEffectStateSnapshot)TraitEffectStateCodec.Capture(state);
            var restored = (RegenerationRuntimeState)TraitEffectStateCodec.Restore(captured);
            var clone = (RegenerationRuntimeState)restored.Clone();
            Assert.That(restored.CompletedEligibleTurnEnds, Is.EqualTo(1));
            Assert.That(clone.CompletedEligibleTurnEnds, Is.EqualTo(1));
            var statuses = new TraitEffectProvider(registry).CreateStatuses(definition, new UnitTraitStateSet(new[] { restored }));
            var status = statuses.OfType<RegenerationTraitStatusSnapshot>().Single();
            Assert.That((status.HealAmount, status.HealEveryOwnSideTurnEnds, status.CompletedEligibleTurnEnds), Is.EqualTo((10, 2, 1)));
            restored.CopyFinalValuesFrom(new RegenerationRuntimeState());
            Assert.That(restored.CompletedEligibleTurnEnds, Is.Zero);
            Assert.That(captured.CompletedEligibleTurnEnds, Is.EqualTo(1));
            Assert.That(status.CompletedEligibleTurnEnds, Is.EqualTo(1));
            Assert.That(clone.CompletedEligibleTurnEnds, Is.EqualTo(1));
            Assert.That(captured.ValueEquals(TraitEffectStateCodec.Capture(restored)), Is.False);
        }

        [Test]
        public void CadenceEqualityAndUnrelatedStatusApplication_PreserveProducerProgress()
        {
            var progress = new RegenerationTraitStatusSnapshot(10, 2, 1);
            Assert.That(progress.SameValues(new RegenerationTraitStatusSnapshot(10, 2, 1)), Is.True);
            Assert.That(progress.SameValues(new RegenerationTraitStatusSnapshot(10, 2, 0)), Is.False);
            Assert.That(progress.SameValues(new RegenerationTraitStatusSnapshot(10, 3, 1)), Is.False);
            var actor = CombatCommandTestData.Input().Characters[0];
            actor = actor.With(stats: actor.Stats.With(traitStatuses: new TraitStatusSnapshot[] { progress }));
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, actor.EntityId.Value, (int)TraitKey.Vanguard, "grant");
            var changed = actor.WithStatusEffects(AppliedStatusRules.Apply(actor.StatusEffects, new AttackIncreaseStatus(origin, 3, 2)));
            Assert.That(changed.Stats.TraitStatuses.Span[0], Is.SameAs(progress));
            Assert.That(changed.Stats.TraitStatuses.Span[0].SameValues(progress), Is.True);
            Assert.That(actor.StatusEffects.Entries, Is.Empty);
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegenerationRuntimeState(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegenerationTraitStatusSnapshot(10, 2, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegenerationTraitStatusSnapshot(10, 2, 2));
        }

        [Test]
        public void IntervalTwo_HealsOnlyOnEligibleEndsTwoAndFour()
        {
            var input = RegenerationTurnTests.Regeneration(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, 10, 40, 100, 2);
            for (int ending = 1; ending <= 4; ending++)
            {
                var result = EndPlayer(input);
                var actor = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId.Value == 1);
                Assert.That(actor.Health.CurrentHealth, Is.EqualTo(40 + ending / 2 * 10));
                Assert.That(Progress(actor), Is.EqualTo(ending % 2));
                Assert.That(result.Events.OfType<CombatTraitStateEvent>().Any(e => e.Final.EntityId == actor.EntityId), Is.True);
                input = CombatCommandComposition.Project(result);
                var opposite = CombatTurnCalculator.EndEnemyPhase(EnemyControllerPhaseTests.Input(input, CombatControllerPhase.Enemy), Array.Empty<IMovementTileRule>());
                Assert.That(Progress(CombatCommandComposition.Project(opposite).Characters.Single(c => c.EntityId == actor.EntityId)), Is.EqualTo(ending % 2));
            }
        }

        [Test]
        public void FullHealthDueEnd_ConsumesTheOpportunityWithoutBanking()
        {
            var source = RegenerationTurnTests.Regeneration(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, 10, 100, 100, 2, 1);
            var due = EndPlayer(source);
            var input = CombatCommandComposition.Project(due);
            var full = input.Characters.Single(c => c.EntityId.Value == 1);
            Assert.That(Progress(full), Is.Zero);
            Assert.That(due.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.HealthChanged), Is.Empty);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == full.EntityId
                ? c.WithHealth(new HealthStateSnapshot(40, 100)) : c).ToArray());
            var first = CombatCommandComposition.Project(EndPlayer(input));
            Assert.That(first.Characters.Single(c => c.EntityId == full.EntityId).Health.CurrentHealth, Is.EqualTo(40));
            Assert.That(Progress(first.Characters.Single(c => c.EntityId == full.EntityId)), Is.EqualTo(1));
            var second = CombatCommandComposition.Project(EndPlayer(first));
            Assert.That(second.Characters.Single(c => c.EntityId == full.EntityId).Health.CurrentHealth, Is.EqualTo(50));
            Assert.That(Progress(source.Characters.Single(c => c.EntityId == full.EntityId)), Is.EqualTo(1));
        }

        [Test]
        public void FieldExitDeathAndBattleCleanup_ResetOnlyCadence()
        {
            var input = RegenerationTurnTests.Regeneration(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, 10, 40, 100, 2, 1);
            var actor = input.Characters.Single(c => c.EntityId.Value == 1);
            var returned = CombatPlacementCalculator.Location(actor, CharacterRuntimeArea.Hand, 0, null, null);
            Assert.That(Progress(returned), Is.Zero);
            var reentered = CombatPlacementCalculator.Location(returned, CharacterRuntimeArea.Field, null, actor.Anchor, actor.Rotation);
            Assert.That(Progress(reentered), Is.Zero);
            var relocated = CombatPlacementCalculator.Location(actor, CharacterRuntimeArea.Field, null, actor.Anchor.Value + CellCoord.Right, actor.Rotation);
            Assert.That(Progress(relocated), Is.EqualTo(1));
            var cleared = CombatTemporaryStateCleanup.Clear(actor);
            Assert.That(Progress(cleared), Is.Zero);
            Assert.That(cleared.With(stats: actor.Stats).SameValues(actor), Is.True);
            Assert.That(Progress(CombatTemporaryStateCleanup.Clear(actor.WithHealth(new HealthStateSnapshot(0, 100)))), Is.Zero);
            Assert.That(Progress(actor), Is.EqualTo(1));
        }

        [Test]
        public void PreviewAndStaleEnd_CannotAdvanceProgress()
        {
            var input = RegenerationTurnTests.Regeneration(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, 10, 40, 100, 2, 1);
            CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var rejected = CombatTurnCalculator.EndPlayerTurn(EnemyControllerPhaseTests.Input(input, CombatControllerPhase.Player, expectedSequence: 9), Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Characters, Is.Empty);
            Assert.That(Progress(input.Characters.Single(c => c.EntityId.Value == 1)), Is.EqualTo(1));
        }

        private static int Progress(CharacterCombatFacts character) =>
            character.Stats.TraitStatuses.ToArray().OfType<RegenerationTraitStatusSnapshot>().Single().CompletedEligibleTurnEnds;

        private static CombatCommandTransition EndPlayer(CombatCommandInput input) =>
            CombatTurnCalculator.EndPlayerTurn(EnemyControllerPhaseTests.Input(input, CombatControllerPhase.Player), Array.Empty<IMovementTileRule>());
    }
}
