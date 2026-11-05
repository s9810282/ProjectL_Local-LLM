using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class CombatChangeEvidenceCoverageTests
    {
        [Test]
        public void EveryProductionEventTypeHasEvidenceCoverage()
        {
            var types = typeof(CombatEvent).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(CombatEvent).IsAssignableFrom(t));
            var samples = Samples().ToArray();
            Assert.That(samples.Select(s => s.Events[0].GetType()).Distinct(), Is.EquivalentTo(types));
            Assert.That(samples.Select(s => s.Events[0].Kind).Distinct(), Is.EquivalentTo(Enum.GetValues(typeof(CombatEventKind))));
            foreach (var sample in samples) AssertSample(sample);
        }

        [TestCaseSource(nameof(Cases))]
        public void ExactEventScope(Sample sample) => AssertSample(sample);

        [Test]
        public void ArtifactPurposeDistinguishesComputationFromMissingHealingCompanion()
        {
            var activation = Activation(new EntityId(1));
            var calculation = new CombatArtifactEvent(new[] { activation }, CombatArtifactPurpose.AttackPowerContribution);
            Assert.That(calculation.Purpose, Is.EqualTo(CombatArtifactPurpose.AttackPowerContribution));
            Assert.That(CombatChangeEvidenceReader.ReadAll(new[] { calculation }).Single().AffectedEntityIds, Is.Empty);
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadAll(new[] {
                new CombatArtifactEvent(new[] { activation }, CombatArtifactPurpose.Healing) }));
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadAll(new[] { new CombatArtifactEvent(new[] { activation }) }));
        }

        private static void AssertSample(Sample sample)
        {
            var evidence = CombatChangeEvidenceReader.ReadAll(sample.Events)[0];
            Assert.That(evidence.Kind, Is.EqualTo(sample.Kind), sample.Name);
            Assert.That(evidence.AffectedEntityIds, Is.EquivalentTo(sample.Actors.Select(id => new EntityId(id))), sample.Name);
            Assert.That(evidence.ChangedSourceCells, Is.EquivalentTo(sample.Cells), sample.Name);
        }

        private static IEnumerable<TestCaseData> Cases() => Samples().Select(s => new TestCaseData(s).SetName("Evidence_" + s.Name));

        public sealed class Sample
        {
            internal string Name;
            internal CombatEvent[] Events;
            internal CombatChangeEvidenceKind Kind;
            internal int[] Actors;
            internal CellCoord[] Cells;
        }

        private static Sample Row(CombatEvent value, CombatChangeEvidenceKind kind, int[] actors,
            CellCoord[] cells = null, params CombatEvent[] companions) => new Sample { Name = value.GetType().Name + "_" + value.Kind,
                Events = new[] { value }.Concat(companions).ToArray(), Kind = kind, Actors = actors, Cells = cells ?? Array.Empty<CellCoord>() };

        private static IEnumerable<Sample> Samples()
        {
            var stormId = new StageGimmickId(1);
            var stormBefore = new TornadoMemberState(31, CellCoord.Zero, 1, TornadoGimmickPhase.Cooldown, 1, null);
            var stormAfter = new TornadoMemberState(31, CellCoord.Zero, 1, TornadoGimmickPhase.Warning, 2, new CellCoord(1, 1));
            yield return Row(new CombatTornadoGimmickEvent(new TornadoGimmickState(stormId, new[] { stormBefore }),
                new TornadoGimmickState(stormId, new[] { stormAfter }), new StageGimmickBoundary(Owner.Player, 1),
                new[] { new TornadoMemberAdvance(stormBefore, stormAfter, 1, new[] { CellCoord.Zero }) }),
                CombatChangeEvidenceKind.Effect, Array.Empty<int>(), new[] { CellCoord.Zero });
            yield return Row(new CombatTornadoCenterEvent(new TornadoContactOrigin(stormId, 31, CellCoord.Zero, 0)),
                CombatChangeEvidenceKind.Observation, Array.Empty<int>());
            yield return Row(new CombatTornadoEntryEvent(stormId, new EntityId(1), CellCoord.Zero),
                CombatChangeEvidenceKind.Observation, Array.Empty<int>());
            yield return Row(new CombatTornadoMemberEvent(stormId, new TornadoMemberAdvance(stormBefore, stormAfter, 1, new[] { CellCoord.Zero })),
                CombatChangeEvidenceKind.Observation, Array.Empty<int>());
            yield return Row(new CombatHaboobEvent(HaboobGimmickStep.Warning,
                new HaboobGimmickState(new StageGimmickId(1), HaboobGimmickPhase.Active, 1, new[] { CellCoord.Zero }),
                new HaboobGimmickState(new StageGimmickId(1), HaboobGimmickPhase.RelocationWarning, 2, new[] { CellCoord.Zero })),
                CombatChangeEvidenceKind.Effect, Array.Empty<int>(), new[] { CellCoord.Zero });
            yield return Row(new CombatRockfallEvent(RockfallGimmickStep.Warning,
                new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Cooldown, 2, Array.Empty<CellCoord>()),
                new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Warning, 4, new[] { CellCoord.Zero }),
                Array.Empty<EntityId>()), CombatChangeEvidenceKind.Effect, Array.Empty<int>(), new[] { CellCoord.Zero });
            var a = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 1), Owner.Player);
            var moved = a.WithPlacement(a.Area, null, new CellCoord(2, 1), a.Rotation, new[] { new CellCoord(2, 1) });
            var hurt = a.WithHealth(new HealthStateSnapshot(90, 100));
            var dead = a.WithHealth(new HealthStateSnapshot(0, 100));
            var origin = new CombatDamageOrigin(new CellCoord(7, 7), new EntityId(7), 1);
            foreach (var kind in new[] { CombatEventKind.Moved, CombatEventKind.Placed, CombatEventKind.Returned,
                CombatEventKind.HealthChanged, CombatEventKind.HealthNormalized, CombatEventKind.Died, CombatEventKind.Relocated })
            {
                var final = kind == CombatEventKind.Died ? dead : kind == CombatEventKind.HealthChanged || kind == CombatEventKind.HealthNormalized ? hurt : moved;
                yield return Row(new CombatCharacterEvent(kind, a, final, tornadoSourceId: kind == CombatEventKind.Relocated ? 1 : (long?)null),
                    kind == CombatEventKind.Moved ? CombatChangeEvidenceKind.OrdinaryMove : kind == CombatEventKind.Returned ? CombatChangeEvidenceKind.Return : CombatChangeEvidenceKind.Effect, new[] { 1 });
            }
            var spec = new StructureSpec(EntityKey.Structure_PowderBarrel, 1, 1, new[] { CellCoord.Zero }, 10, false, 0);
            var structure = new StructureCombatFacts(new EntityId(5), spec, new HealthStateSnapshot(10, 10), new CellCoord(4, 4), Rotation4.R0, new[] { new CellCoord(4, 4) });
            foreach (var hp in new[] { 5, 0 })
                yield return Row(new CombatStructureEvent(structure, new StructureCombatFacts(structure.EntityId, spec,
                    new HealthStateSnapshot(hp, 10), structure.Anchor, structure.Rotation, structure.OccupiedCells)), CombatChangeEvidenceKind.Effect, new[] { 5 });
            var attack = AttackResult.FromFacts(a.EntityId, default, Rotation4.R0,
                new UnitActionUseSettlementSnapshot(a.EntityId, UnitActionUse.Available, UnitActionUse.Attacked),
                new[] { new CellCoord(6, 6) }, Array.Empty<AttackHitResult>(), new AttackCellSelection(new CellCoord(6, 6), new[] { new CellCoord(6, 6) }, Array.Empty<CellCoord>()));
            yield return Row(new CombatAttackEvent(attack), CombatChangeEvidenceKind.Action, new[] { 1 });
            yield return Row(new CombatRoarEvent(new EntityId(7), null, new[] { new CellCoord(1, 1) }), CombatChangeEvidenceKind.Effect, new[] { 7 });
            yield return Row(new CombatEffectsEvent(new[] { new TraitActivationSnapshot(a.EntityId, null, TraitKey.Leap,
                TraitActivationKind.MovementChanged, 1, new[] { a.EntityId }, new[] { a.Anchor.Value }) }, Array.Empty<CategoryActivationSnapshot>()),
                CombatChangeEvidenceKind.Observation, Array.Empty<int>(), companions: new CombatCharacterEvent(CombatEventKind.Moved, a, moved));
            yield return Row(new CombatTraitStateEvent(a, a.With(stats: a.Stats.With(moveRange: 4))), CombatChangeEvidenceKind.Effect, new[] { 1 });
            yield return Row(new CombatHeldEnvironmentEvent(a, a.With(heldEnvironment: new CharacterHeldEnvironmentState(1, true))), CombatChangeEvidenceKind.Effect, new[] { 1 });
            yield return Row(new CombatHuntersMarkEvent(a.EntityId, null, new HuntersMarkState(new EntityId(7), Owner.Enemy, 20)), CombatChangeEvidenceKind.Effect, new[] { 1 });
            yield return Row(new CombatPullEvent(new AttackPullSnapshot(a.EntityId, a.Anchor.Value, moved.Anchor.Value, new[] { moved.Anchor.Value })), CombatChangeEvidenceKind.Effect, new[] { 1 });
            yield return Row(new CombatFallEvent(a.EntityId, 1, a.Anchor.Value), CombatChangeEvidenceKind.Effect, new[] { 1 });
            var mine = new MineSourceFact(1, new CellCoord(3, 3), 1, 1, true, new[] { Owner.Player });
            var source = new CombatMovementSourceEvent(mine, mine.WithActive(false));
            yield return Row(source, CombatChangeEvidenceKind.Effect, Array.Empty<int>(), new[] { mine.Cell });
            yield return Row(new CombatObjectActivationEvent(mine, a.EntityId), CombatChangeEvidenceKind.Effect, new[] { 1 }, new[] { mine.Cell }, source);
            yield return Row(new CombatObjectCreatedEvent(mine), CombatChangeEvidenceKind.Effect, Array.Empty<int>(), new[] { mine.Cell });
            var tornado = new TornadoSourceFact(2, new CellCoord(3, 3), new[] { Owner.Player });
            yield return Row(new CombatTornadoEvent(tornado, tornado.AfterActivation(new CellCoord(4, 3))), CombatChangeEvidenceKind.Effect,
                Array.Empty<int>(), new[] { new CellCoord(3, 3), new CellCoord(4, 3) });
            yield return Row(new CombatThornConsumedEvent(new ThornFieldCellSnapshot(new CellCoord(4, 4), 1, 2)), CombatChangeEvidenceKind.Effect, Array.Empty<int>(), new[] { new CellCoord(4, 4) });
            var soul = new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Trait, 7, (int)TraitKey.CallOfBlood, "main"), new CellCoord(6, 6), 1, 1);
            var added = new SoulFieldChange(SoulFieldSnapshot.Empty, new SoulFieldSnapshot(2, new[] { soul }));
            yield return Row(new CombatSoulEvent(added), CombatChangeEvidenceKind.Effect, Array.Empty<int>(), new[] { soul.Cell });
            var consumed = new SoulFieldChange(added.Final, new SoulFieldSnapshot(2, Array.Empty<SoulFieldFacts>()));
            var collection = new SoulActionChanges(new CharacterChanges(new[] { new CharacterStateChange(a, a) },
                new[] { new SoulCountChange(a.OwnedId.Value, 0, 1) }), consumed, new long[] { 1 });
            yield return Row(new CombatSoulEvent(consumed, a.EntityId, collection), CombatChangeEvidenceKind.Effect, new[] { 1 }, new[] { soul.Cell });
            yield return Row(new CombatArtifactEvent(new[] { Activation(a.EntityId) }, CombatArtifactPurpose.Healing), CombatChangeEvidenceKind.Effect,
                new[] { 1 }, companions: new CombatCharacterEvent(CombatEventKind.HealthChanged, hurt, a));
            var structureHurt = new CombatStructureEvent(structure, new StructureCombatFacts(structure.EntityId, spec, new HealthStateSnapshot(5, 10),
                structure.Anchor, structure.Rotation, structure.OccupiedCells), new[] { origin });
            yield return Row(new CombatObstacleReactionEvent(structure.EntityId, ObstacleContentKind.PowderBarrel, ObstacleReactionKind.Hit, origin, 1),
                CombatChangeEvidenceKind.Effect, new[] { 5 }, companions: structureHurt);
            yield return Row(new CombatGimmickEvent(new RailwayGimmickActivationSnapshot(new StageGimmickId(1), new[] { a.Anchor.Value, new CellCoord(7, 7) }, new[] { a.EntityId })),
                CombatChangeEvidenceKind.Effect, new[] { 1 }, companions: new CombatCharacterEvent(CombatEventKind.Died, a, dead));
            yield return Row(new CombatSummonEvent(new CombatSummonRecord(a.EntityId, new EntityId(7), TraitKey.Necromancy), a), CombatChangeEvidenceKind.Effect, new[] { 1 });
            yield return Row(new CombatItemEvent(1, a.EntityId, a.EntityId, ItemEventStage.AfterDamage, ItemEventReason.DuplicateEffect, 0, 1, null, CombatDamageKind.OrdinaryAttack, ItemActivationKind.TemporaryEffect), CombatChangeEvidenceKind.Effect, new[] { 1 });
            yield return Row(new CombatItemEvent(1, a.EntityId, null, ItemEventStage.Equipment, ItemEventReason.Equipped, 0, null, 1), CombatChangeEvidenceKind.Effect, new[] { 1 });
            yield return Row(new CombatItemEvent(1, a.EntityId, null, ItemEventStage.Lifecycle, ItemEventReason.Death, 0, 1, null), CombatChangeEvidenceKind.Effect, new[] { 1 });
            yield return Row(CombatExperienceEvent.Earned(new EntityId(7), a.EntityId, 10), CombatChangeEvidenceKind.Effect, new[] { 1 });
            var owned = new OwnedCharacterRecord(a.OwnedId.Value, a.CharacterKey, a.OwnershipRole.Value);
            var grown = new OwnedCharacterRecord(a.OwnedId.Value, a.CharacterKey, a.OwnershipRole.Value, 1, 10);
            yield return Row(CombatExperienceEvent.Applied(a.EntityId, new OwnedCharacterChange(owned, grown), 10, 0), CombatChangeEvidenceKind.Effect, new[] { 1 });
            foreach (var kind in new[] { CombatEventKind.CombatEntered, CombatEventKind.ControllerTurnStarted, CombatEventKind.PlayerTurnEnded, CombatEventKind.EnemyPhaseEnded, CombatEventKind.NightEnded })
                yield return Row(new CombatTurnEvent(kind, default, new CombatTurnFacts(StagePhase.Deployment),
                    new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Player, controllerTurnSequence: 1)), CombatChangeEvidenceKind.Lifecycle, Array.Empty<int>());
            yield return Row(new CombatOutcomeEvent(new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed)), CombatChangeEvidenceKind.Lifecycle, Array.Empty<int>());
        }

        private static ArtifactActivationSnapshot Activation(EntityId target) => new ArtifactActivationSnapshot(
            new EffectOriginKey(EffectOwnerKind.Artifact, 1, 1, "authored-entry"), target, 10, 10);
    }
}
