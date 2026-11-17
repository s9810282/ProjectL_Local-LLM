using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class CombatChangeEvidenceTests
    {
        [Test]
        public void ItemHealingRequiresActualRecoveryCompanion()
        {
            var item = new CombatItemEvent(1, new EntityId(1), new EntityId(1), ItemEventStage.AfterDamage,
                ItemEventReason.Activated, 5, 1, null, CombatDamageKind.EnvironmentDamage, ItemActivationKind.Healing);
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadEventShapes(new[] { item }));
            var actor = CombatCommandTestData.Input(empty: true).Characters[0];
            var events = new CombatEvent[] { item, new CombatCharacterEvent(CombatEventKind.HealthChanged,
                actor.WithHealth(new HealthStateSnapshot(95, 100)), actor) };
            Assert.That(CombatChangeEvidenceReader.ReadEventShapes(events)[0].AffectedEntityIds, Is.EqualTo(new[] { actor.EntityId }));
        }

        [Test]
        public void ZeroMagnitudeTemporaryItemEffectStillAffectsItsRecipient()
        {
            var item = new CombatItemEvent(1, new EntityId(1), new EntityId(2), ItemEventStage.AfterDamage,
                ItemEventReason.Activated, 0, 1, null, CombatDamageKind.OrdinaryAttack, ItemActivationKind.TemporaryEffect);
            Assert.That(CombatChangeEvidenceReader.ReadEventShapes(new[] { item })[0].AffectedEntityIds,
                Is.EquivalentTo(new[] { new EntityId(1), new EntityId(2) }));
        }

        [Test]
        public void UnrelatedTraitStateCannotReplaceRequiredCompanion()
        {
            var actor = CombatCommandTestData.Input(empty: true).Characters[0];
            var occurrence = new CombatEffectsEvent(new[] { new TraitActivationSnapshot(actor.EntityId, actor.EntityId,
                TraitKey.Acceleration, TraitActivationKind.AttackGranted, 2) }, Array.Empty<CategoryActivationSnapshot>());
            var unrelated = new CombatTraitStateEvent(actor, actor.With(stats: actor.Stats.With(moveRange: 4)));
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadEventShapes(new CombatEvent[] { occurrence, unrelated }));
        }

        [Test]
        public void ActualTargetsNotDisplayTargets()
        {
            var attacker = new EntityId(1);
            var protectedId = new EntityId(2);
            var guardian = new EntityId(3);
            var origin = new CombatDamageOrigin(new CellCoord(1, 1), attacker, 1);
            var contact = new AttackHitContactSnapshot(0, protectedId, AttackTargetRole.Primary, 100, origin);
            var interception = new TraitActivationSnapshot(guardian, protectedId, TraitKey.Sacrifice,
                TraitActivationKind.DamageRedirected, 20, resultOrigin: origin)
                .WithInterception(new AttackInterceptionSnapshot(guardian, protectedId, guardian, 0));
            var hit = new AttackHitResult(guardian, 100, 80, AttackTargetRole.Primary, 100, 0, 0, new[] { contact });
            var attack = AttackResult.FromFacts(attacker, protectedId, Rotation4.R0,
                new UnitActionUseSettlementSnapshot(attacker, UnitActionUse.Available, UnitActionUse.Attacked), new[] { new CellCoord(4, 4) },
                new[] { hit }, new AttackCellSelection(new CellCoord(4, 4), new[] { new CellCoord(4, 4) }, Array.Empty<CellCoord>()),
                traitActivations: new[] { interception }, attackGroup: 1);
            var before = CombatCommandTestData.Character(3, EntityKey.Unit_Akimbo, new CellCoord(5, 4), Owner.Player);
            var health = new CombatCharacterEvent(CombatEventKind.HealthChanged, before, before.WithHealth(new HealthStateSnapshot(80, 100)), new[] { origin });
            var evidence = CombatChangeEvidenceReader.ReadEventShapes(new CombatEvent[] { new CombatAttackEvent(attack), health });
            Assert.That(evidence.SelectMany(e => e.AffectedEntityIds).Distinct(), Is.EquivalentTo(new[] { attacker, guardian }));
            Assert.That(evidence.SelectMany(e => e.ChangedSourceCells), Is.Empty);
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadEventShapes(new CombatEvent[] { new CombatAttackEvent(attack) }));
        }

        [Test]
        public void ChildAttackUsesActualRecipientAndRequiresCompanions()
        {
            var origin = new CombatDamageOrigin(new CellCoord(1, 1), new EntityId(1), 2);
            var contact = new AttackHitContactSnapshot(0, new EntityId(2), AttackTargetRole.Primary, 100, origin);
            var child = new AttackOutcomeSnapshot(new[] { new AttackHitResult(new EntityId(2), 100, 80,
                AttackTargetRole.Primary, 100, 0, 0, new[] { contact }) }, Array.Empty<TraitActivationSnapshot>());
            var occurrence = new TraitActivationSnapshot(new EntityId(1), new EntityId(2), TraitKey.KillChainAttack,
                TraitActivationKind.KillChainAttack, 20, resultOrigin: origin).WithChildAttack(child);
            var before = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 4), Owner.Enemy);
            var events = new CombatEvent[] { new CombatEffectsEvent(new[] { occurrence }, Array.Empty<CategoryActivationSnapshot>()),
                new CombatCharacterEvent(CombatEventKind.HealthChanged, before, before.WithHealth(new HealthStateSnapshot(80, 100)), new[] { origin }) };
            Assert.That(CombatChangeEvidenceReader.ReadEventShapes(events)[0].AffectedEntityIds, Is.EqualTo(new[] { new EntityId(2) }));
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadEventShapes(new[] { events[0] }));
        }

        [Test]
        public void NativeOrdinaryMoveHasActorScopeWithoutChangedSourceCells()
        {
            var result = CombatMovementCalculator.Calculate(MovementTestData.SafeRoute(new CellCoord(1, 1), new CellCoord(2, 1)), Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var evidence = CombatChangeEvidenceReader.ReadEventShapes(result.Events);
            Assert.That(evidence, Is.Not.Empty);
            Assert.That(evidence.All(e => e.Kind == CombatChangeEvidenceKind.OrdinaryMove), Is.True);
            Assert.That(evidence.SelectMany(e => e.AffectedEntityIds).Distinct(), Is.EqualTo(new[] { new EntityId(1) }));
            Assert.That(evidence.SelectMany(e => e.ChangedSourceCells), Is.Empty);
        }

        [Test]
        public void WrapperCompanionFactsCoverEffects()
        {
            var result = NativeAttack();
            var evidence = CombatChangeEvidenceReader.ReadEventShapes(result.Events);
            Assert.That(evidence.SelectMany(e => e.AffectedEntityIds).Distinct(), Is.EquivalentTo(new[] {
                new EntityId(1), new EntityId(2), new EntityId(3), new EntityId(4) }));
            Assert.That(evidence.SelectMany(e => e.ChangedSourceCells), Is.Empty);
        }

        [Test]
        public void MissingRequiredCompanionCannotPublish()
        {
            var events = NativeAttack().Events;
            var removed = events.OfType<CombatCharacterEvent>().First(e => e.Kind == CombatEventKind.HealthChanged);
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadEventShapes(events.Where(e => !ReferenceEquals(e, removed)).ToArray()));
        }

        [Test]
        public void WrapperAreaIsNotChangedSourceScope()
        {
            var evidence = CombatChangeEvidenceReader.ReadEventShapes(new CombatEvent[] {
                new CombatRoarEvent(new EntityId(8), null, new[] { new CellCoord(1, 1), new CellCoord(2, 1) }) });
            Assert.That(evidence.Single().AffectedEntityIds, Is.EqualTo(new[] { new EntityId(8) }));
            Assert.That(evidence.Single().ChangedSourceCells, Is.Empty);
        }

        [Test]
        public void NetZeroIntermediateEffectRetainsAffectedScope()
        {
            var actor = CombatCommandTestData.Input(empty: true).Characters[0];
            var hurt = actor.WithHealth(new HealthStateSnapshot(90, 100));
            var evidence = CombatChangeEvidenceReader.ReadEventShapes(new CombatEvent[] {
                new CombatCharacterEvent(CombatEventKind.HealthChanged, actor, hurt),
                new CombatCharacterEvent(CombatEventKind.HealthChanged, hurt, actor) });
            Assert.That(evidence.Count, Is.EqualTo(2));
            Assert.That(evidence.All(e => e.Kind == CombatChangeEvidenceKind.Effect && e.AffectedEntityIds.Contains(actor.EntityId)), Is.True);
        }

        [Test]
        public void SourceAddChangeAndRemovalPreserveOnlyActualCells()
        {
            var first = new DestinationTileModifierFact(1, new CellCoord(1, 1), true, new[] { Owner.Player }, 2, 0);
            var second = new DestinationTileModifierFact(1, new CellCoord(2, 1), true, new[] { Owner.Player }, 2, 0);
            var evidence = CombatChangeEvidenceReader.ReadEventShapes(new CombatEvent[] { new CombatMovementSourceEvent(first, second) });
            Assert.That(evidence.Single().ChangedSourceCells, Is.EquivalentTo(new[] { new CellCoord(1, 1), new CellCoord(2, 1) }));
            Assert.That(evidence.Single().AffectedEntityIds, Is.Empty);
        }

        [Test]
        public void ObjectActivationRequiresSameSourceCompanion()
        {
            var source = new MineSourceFact(1, new CellCoord(2, 1), 1, 5, true, new[] { Owner.Player });
            var activation = new CombatObjectActivationEvent(source, new EntityId(1));
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadEventShapes(new[] { activation }));
            var facts = CombatChangeEvidenceReader.ReadEventShapes(new CombatEvent[] { activation, new CombatMovementSourceEvent(source, source.WithActive(false)) });
            Assert.That(facts[0].AffectedEntityIds, Is.EqualTo(new[] { new EntityId(1) }));
            Assert.That(facts[0].ChangedSourceCells, Is.EqualTo(new[] { new CellCoord(2, 1) }));
        }

        [Test]
        public void RemoteSoulChangeExcludesUnrelatedOrigin()
        {
            var first = Soul(1, new CellCoord(1, 1));
            var remote = Soul(2, new CellCoord(6, 6));
            var change = new SoulFieldChange(new SoulFieldSnapshot(2, new[] { first }), new SoulFieldSnapshot(3, new[] { first, remote }));
            var evidence = CombatChangeEvidenceReader.ReadChanges(change);
            Assert.That(evidence.ChangedSourceCells, Is.EqualTo(new[] { new CellCoord(6, 6) }));
            Assert.That(evidence.AffectedEntityIds, Is.Empty);
        }

        [Test]
        public void NoOpCharacterChangesHaveEmptyScope()
        {
            var source = CombatCommandTestData.Input(empty: true).Characters;
            var evidence = CombatChangeEvidenceReader.ReadCharacterChanges(source, source.Select(c => c.With()).ToArray());
            Assert.That(evidence.AffectedEntityIds, Is.Empty);
        }

        [Test]
        public void MainOnlyPersistentChangeAffectsPublishedActor()
        {
            var source = CombatCommandTestData.Input(empty: true).Characters;
            var final = source.Select(c => c.EntityId == new EntityId(10) ? c.WithHealth(new HealthStateSnapshot(90, 100)) : c).ToArray();
            Assert.That(CombatChangeEvidenceReader.ReadCharacterChanges(source, final).AffectedEntityIds, Is.EqualTo(new[] { new EntityId(10) }));
        }

        [Test]
        public void UnsupportedOrMalformedEvidenceCannotPublish()
        {
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadEventShapes(new CombatEvent[] { null }));
            var reserved = new TraitActivationSnapshot(new EntityId(1), null, TraitKey.Acceleration, TraitActivationKind.FocusReset, 0);
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadEventShapes(new CombatEvent[] {
                new CombatEffectsEvent(new[] { reserved }, Array.Empty<CategoryActivationSnapshot>()) }));
        }

        private static CombatCommandTransition NativeAttack()
        {
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.Input(), Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        private static SoulFieldFacts Soul(long id, CellCoord cell) => new SoulFieldFacts(id,
            new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.CallOfBlood, "main"), cell, 2, 1);

    }
}
