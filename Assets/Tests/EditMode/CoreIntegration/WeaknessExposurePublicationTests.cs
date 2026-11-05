using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class WeaknessExposurePublicationTests
    {
        [Test]
        public void CheckedGroupSumOverflow_RejectsEveryStoreBeforePublication()
        {
            using var battle = HuntersMarkPublicationTests.Create();
            var target = battle.Character(EntityKey.Unit_Demon);
            var overflow = target.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] {
                new WeaknessExposureStatus(new EffectOriginKey(EffectOwnerKind.Trait, battle.Actor.EntityId.Value, (int)TraitKey.ReconSquad, "main"), int.MaxValue, 1),
                HuntersMarkPublicationTests.Exposure(battle.Main.EntityId, 1, 1) }));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<OverflowException>(() => battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(
                new[] { new CharacterStateChange(target, overflow) }, Array.Empty<SoulCountChange>())));
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void FullEventFacts_ReconstructPublishedExposureAndPreserveOldSnapshots()
        {
            using var battle = HuntersMarkPublicationTests.Create(30, 1);
            HuntersMarkPublicationTests.SetExposure(battle, HuntersMarkPublicationTests.Exposure(battle.Main.EntityId, 20, 2));
            var original = battle.Buffers.CharacterReadSink.Characters.Single(c => c.CharacterKey == EntityKey.Unit_Demon);
            var replay = battle.Buffers.CharacterReadSink.Characters.ToDictionary(c => c.EntityId, c => c.ToFacts());
            battle.Submit(battle.Attack(battle.Actor, original.Anchor.Value));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            foreach (var fact in result.Events)
            {
                if (fact is CombatCharacterEvent character) replay[character.Final.EntityId] = character.Final;
                else if (fact is CombatTraitStateEvent status) replay[status.Final.EntityId] = status.Final;
            }
            var published = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == original.EntityId);
            Assert.That(replay[original.EntityId].StatusEffects.SameValues(published.StatusEffects), Is.True);
            Assert.That(replay[original.EntityId].Health.CurrentHealth, Is.EqualTo(published.Health.CurrentHealth));
            var row = published.StatusEffectSnapshots.OfType<WeaknessExposureEffectSnapshot>().Single();
            Assert.That(row.EffectiveIncreasePercent, Is.EqualTo(30));
            Assert.That(row.Sources.Select(s => s.RemainingAffectedSideTurnEnds), Is.EqualTo(new int?[] { 2, 1 }));
            Assert.That(original.StatusEffectSnapshots.OfType<WeaknessExposureEffectSnapshot>().Single().EffectiveIncreasePercent, Is.EqualTo(20));
            Assert.That(result.Events.OfType<CombatHuntersMarkEvent>(), Is.Empty);
        }

        [Test]
        public void AffectedEnd_PublishesFallbackThenExpiryWithoutBankingOtherSide()
        {
            using var battle = HuntersMarkPublicationTests.Create();
            var target = battle.Character(EntityKey.Unit_Demon);
            HuntersMarkPublicationTests.SetExposure(battle, HuntersMarkPublicationTests.Exposure(battle.Main.EntityId, 20, 2),
                HuntersMarkPublicationTests.Exposure(battle.Actor.EntityId, 30, 1));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(ExposureRow(battle).EffectiveIncreasePercent, Is.EqualTo(30));
            battle.AdvanceToPlayerOrDay();
            Assert.That(ExposureRow(battle).EffectiveIncreasePercent, Is.EqualTo(20));
            Assert.That(ExposureRow(battle).Sources.Single().RemainingAffectedSideTurnEnds, Is.EqualTo(1));
            battle.Submit(battle.EndPlayerTurn());
            battle.AdvanceToPlayerOrDay();
            Assert.That(battle.Character(EntityKey.Unit_Demon).StatusEffects.Entries.OfType<WeaknessExposureStatus>(), Is.Empty);
        }

        [Test]
        public void StaleExpiryRefresh_InstallsNoPartialStateOrResult()
        {
            using var battle = HuntersMarkPublicationTests.Create();
            HuntersMarkPublicationTests.SetExposure(battle, HuntersMarkPublicationTests.Exposure(battle.Actor.EntityId, 30, 1));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var input = battle.Publication().EnemyStep();
            var transition = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            HuntersMarkPublicationTests.SetExposure(battle, HuntersMarkPublicationTests.Exposure(battle.Actor.EntityId, 30, 2));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void LatePrepareFailure_InstallsNeitherGrantedExposureNorHealthNorResult()
        {
            using var battle = HuntersMarkPublicationTests.Create();
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            input = input.WithWorld(input.State.With(traitTurn: new TraitTurnFacts(999, 0)), input.Characters, input.Main,
                input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(transition.Characters.Characters.Any(c => c.Final.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Any()), Is.True);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
        }

        private static WeaknessExposureEffectSnapshot ExposureRow(ConfiguredMovementBattle battle) =>
            battle.Buffers.CharacterReadSink.Characters.Single(c => c.CharacterKey == EntityKey.Unit_Demon)
                .StatusEffectSnapshots.OfType<WeaknessExposureEffectSnapshot>().Single();
    }
}
