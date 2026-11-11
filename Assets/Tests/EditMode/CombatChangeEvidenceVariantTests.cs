using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class CombatChangeEvidenceVariantTests
    {
        private static readonly TraitActivationKind[] Reserved = { TraitActivationKind.None, TraitActivationKind.FollowUpAttack,
            TraitActivationKind.FocusGainScheduled, TraitActivationKind.FocusStackApplied, TraitActivationKind.FocusReset,
            TraitActivationKind.AttackDiscountArmed, TraitActivationKind.AttackCostReduced, TraitActivationKind.AttackDiscountExpired };

        [Test]
        public void EveryNestedTraitKindRequiresAnExplicitFixtureOrRejection()
        {
            var samples = Samples().ToArray();
            var testedSeparately = new[] { TraitActivationKind.KillChainAttack, TraitActivationKind.DamageRedirected };
            Assert.That(samples.Select(s => s.Kind).Concat(testedSeparately).Concat(Reserved), Is.EquivalentTo(Enum.GetValues(typeof(TraitActivationKind))));
            foreach (var kind in Reserved.Where(k => k != TraitActivationKind.None))
            {
                var occurrence = new TraitActivationSnapshot(new EntityId(1), null, TraitKey.Acceleration, kind, 0);
                Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadAll(new CombatEvent[] { Wrap(occurrence) }), kind.ToString());
            }
            foreach (var sample in samples)
            {
                var evidence = CombatChangeEvidenceReader.ReadAll(sample.Events)[0];
                Assert.That(evidence.AffectedEntityIds.Select(id => id.Value), Is.EquivalentTo(sample.Actors), sample.Kind.ToString());
                Assert.That(evidence.ChangedSourceCells, Is.EquivalentTo(sample.Cells), sample.Kind.ToString());
                if (sample.Kind is TraitActivationKind.TrailCreated or TraitActivationKind.TrailChanged or TraitActivationKind.TrailRemoved)
                {
                    Assert.That(evidence.Kind, Is.EqualTo(CombatChangeEvidenceKind.Observation));
                    var source = CombatChangeEvidenceReader.ReadAll(sample.Events)[1];
                    Assert.That(source.ChangedSourceCells, Is.Empty);
                    Assert.That(source.ChangedSourceIds, Is.EqualTo(new long[] { 1 }));
                }
                if (sample.RequiresCompanion)
                    Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadAll(new[] { sample.Events[0] }), sample.Kind.ToString());
            }
        }

        [Test]
        public void EveryItemStageAndReasonHasExactHolderScope()
        {
            var events = new List<CombatItemEvent>();
            foreach (var stage in new[] { ItemEventStage.BeforeDamage, ItemEventStage.AfterDamage, ItemEventStage.AttackCompleted })
            {
                events.Add(new CombatItemEvent(1, new EntityId(1), new EntityId(stage == ItemEventStage.BeforeDamage ? 1 : 2), stage, ItemEventReason.Activated, 3, 1, null,
                    stage == ItemEventStage.AttackCompleted ? null : CombatDamageKind.OrdinaryAttack,
                    stage == ItemEventStage.BeforeDamage ? ItemActivationKind.DamageMitigation : ItemActivationKind.TemporaryEffect));
                if (stage != ItemEventStage.BeforeDamage)
                    events.Add(new CombatItemEvent(1, new EntityId(1), new EntityId(2), stage, ItemEventReason.DuplicateEffect, 0, 1, null,
                        stage == ItemEventStage.AttackCompleted ? null : CombatDamageKind.OrdinaryAttack, ItemActivationKind.TemporaryEffect));
            }
            foreach (var reason in new[] { ItemEventReason.Equipped, ItemEventReason.Unequipped, ItemEventReason.Replaced, ItemEventReason.ReturnedToHand })
                events.Add(new CombatItemEvent(1, new EntityId(1), null, ItemEventStage.Equipment, reason, 0,
                    reason == ItemEventReason.Equipped ? null : reason == ItemEventReason.Replaced ? 2 : 1,
                    reason is ItemEventReason.Equipped or ItemEventReason.Replaced ? 1 : null));
            foreach (var reason in new[] { ItemEventReason.Death, ItemEventReason.BattleEnded })
                events.Add(new CombatItemEvent(1, new EntityId(1), null, ItemEventStage.Lifecycle, reason, 0, 1, null));
            Assert.That(events.Select(e => e.Stage).Distinct(), Is.EquivalentTo(Enum.GetValues(typeof(ItemEventStage))));
            Assert.That(events.Select(e => e.Reason).Distinct(), Is.EquivalentTo(Enum.GetValues(typeof(ItemEventReason))));
            foreach (var e in events)
            {
                var evidence = CombatChangeEvidenceReader.ReadAll(new[] { e }).Single();
                Assert.That(evidence.Kind, Is.EqualTo(CombatChangeEvidenceKind.Effect));
                Assert.That(evidence.AffectedEntityIds.Select(id => id.Value), Is.EquivalentTo(e.Reason == ItemEventReason.Activated && e.Stage != ItemEventStage.BeforeDamage ? new[] { 1, 2 } : new[] { 1 }));
                Assert.That(evidence.ChangedSourceCells, Is.Empty);
            }
        }

        private sealed class Sample
        {
            internal TraitActivationKind Kind;
            internal CombatEvent[] Events;
            internal int[] Actors = Array.Empty<int>();
            internal CellCoord[] Cells = Array.Empty<CellCoord>();
            internal bool RequiresCompanion;
        }

        private static CombatEffectsEvent Wrap(TraitActivationSnapshot occurrence) => new CombatEffectsEvent(new[] { occurrence }, Array.Empty<CategoryActivationSnapshot>());
        private static EffectOriginKey Origin(TraitKey key) => new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)key, "main");

        private static IEnumerable<Sample> Samples()
        {
            var a = CombatCommandTestData.Input(empty: true).Characters[0];
            Sample State(TraitActivationKind kind, TraitKey key, CharacterCombatFacts before, CharacterCombatFacts after) => new Sample {
                Kind = kind, Actors = new[] { 1 }, RequiresCompanion = true,
                Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(a.EntityId, a.EntityId, key, kind, 1)), new CombatTraitStateEvent(before, after) } };
            var disarmed = a.With(debuffs: new CharacterDebuffState(disarm: new DisarmState(Origin(TraitKey.Disarm), 2)));
            yield return State(TraitActivationKind.StatusApplied, TraitKey.Disarm, a, disarmed);
            yield return State(TraitActivationKind.StatusConsumed, TraitKey.Disarm, disarmed,
                a.With(debuffs: new CharacterDebuffState(disarm: new DisarmState(Origin(TraitKey.Disarm), 1))));
            foreach (var kind in new[] { TraitActivationKind.StatusRemoved, TraitActivationKind.StatusExpired }) yield return State(kind, TraitKey.Disarm, disarmed, a);
            var stationary = a.With(stats: a.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new StationaryFireTraitStatusSnapshot(true) }));
            yield return State(TraitActivationKind.StatusReset, TraitKey.StationaryFire, stationary,
                a.With(stats: a.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new StationaryFireTraitStatusSnapshot(false) })));
            CharacterCombatFacts Shield(int budget, bool eligible, long reset) => a.With(traitEffects: new CharacterTraitEffects(Array.Empty<TraitAttackGrant>(),
                new[] { new TraitShieldGrant(Origin(TraitKey.SoloAction), budget, eligible, reset) }));
            yield return State(TraitActivationKind.ShieldGranted, TraitKey.SoloAction, a, Shield(5, true, 0));
            yield return State(TraitActivationKind.ShieldAbsorbed, TraitKey.SoloAction, Shield(5, true, 0), Shield(3, true, 0));
            yield return State(TraitActivationKind.ShieldRemoved, TraitKey.SoloAction, Shield(5, true, 0), a);
            yield return State(TraitActivationKind.ShieldReset, TraitKey.SoloAction, Shield(3, true, 0), Shield(5, true, 1));
            yield return State(TraitActivationKind.DefenseChanged, TraitKey.Cohesion, a,
                a.With(traitEffects: new CharacterTraitEffects(Array.Empty<TraitAttackGrant>(), Array.Empty<TraitShieldGrant>(), 3)));
            yield return State(TraitActivationKind.AttackGranted, TraitKey.Acceleration, a,
                a.With(stats: a.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new AccelerationTraitStatusSnapshot(2) })));
            yield return State(TraitActivationKind.ArmorBroken, TraitKey.ArmorBreaker, a,
                a.With(debuffs: new CharacterDebuffState(armorBreak: new ArmorBreakState(new[] { new ArmorBreakStack(Origin(TraitKey.ArmorBreaker), 20) }))));
            var hunter = State(TraitActivationKind.HunterTargetSelected, TraitKey.Hunter, a,
                a.With(stats: a.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new HunterTraitStatusSnapshot(new EntityId(2), 0) })));
            hunter.Events[0] = Wrap(new TraitActivationSnapshot(a.EntityId, new EntityId(2), TraitKey.Hunter, hunter.Kind, 0));
            yield return hunter;
            foreach (var pair in new[] { (TraitActivationKind.DamageModified, TraitKey.Vanguard), (TraitActivationKind.DefenseBypassed, TraitKey.CleanSweep),
                (TraitActivationKind.KnockbackPrevented, TraitKey.SteadfastWill), (TraitActivationKind.TauntSelected, TraitKey.Taunt) })
                yield return new Sample { Kind = pair.Item1, Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(a.EntityId, new EntityId(2), pair.Item2, pair.Item1, 1)) } };
            var moved = a.WithPlacement(a.Area, null, a.Anchor.Value + new CellCoord(1, 0), a.Rotation, new[] { a.Anchor.Value + new CellCoord(1, 0) });
            foreach (var pair in new[] { (TraitActivationKind.MovementChanged, TraitKey.Leap), (TraitActivationKind.RangeChanged, TraitKey.GroundCompaction),
                (TraitActivationKind.KnockbackApplied, TraitKey.Knockback) })
                yield return new Sample { Kind = pair.Item1, RequiresCompanion = true,
                    Actors = pair.Item1 == TraitActivationKind.KnockbackApplied ? new[] { 1 } : Array.Empty<int>(),
                    Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(a.EntityId, a.EntityId, pair.Item2, pair.Item1, 1)),
                        new CombatCharacterEvent(CombatEventKind.Moved, a, moved) } };
            yield return new Sample { Kind = TraitActivationKind.PullApplied, Actors = new[] { 1 }, RequiresCompanion = true,
                Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(new EntityId(2), a.EntityId, TraitKey.Pull, TraitActivationKind.PullApplied, 1)),
                    new CombatPullEvent(new AttackPullSnapshot(a.EntityId, a.Anchor.Value, moved.Anchor.Value, new[] { moved.Anchor.Value })) } };
            foreach (var kind in new[] { TraitActivationKind.HealthRecovered, TraitActivationKind.InstantDefeat })
                yield return new Sample { Kind = kind, Actors = new[] { 1 }, RequiresCompanion = true,
                    Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(a.EntityId, a.EntityId,
                        kind == TraitActivationKind.HealthRecovered ? TraitKey.CallOfBlood : TraitKey.FinishingShot, kind, 1)),
                        kind == TraitActivationKind.HealthRecovered
                            ? new CombatCharacterEvent(CombatEventKind.HealthChanged, a.WithHealth(new HealthStateSnapshot(90, 100)), a)
                            : new CombatCharacterEvent(CombatEventKind.Died, a, a.WithHealth(new HealthStateSnapshot(0, 100))) } };
            var roar = State(TraitActivationKind.RoarTriggered, TraitKey.Roar,
                a.With(stats: a.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new RoarTraitStatusSnapshot(3, 3) })),
                a.With(stats: a.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new RoarTraitStatusSnapshot(0, 3) })));
            roar.Events = roar.Events.Append(new CombatRoarEvent(a.EntityId, null)).ToArray(); yield return roar;
            var trail = new TraitTrailFact(1, a.Anchor.Value, Origin(TraitKey.GroundCompaction), Owner.Player, 1, 0, 0, 2, true, new[] { Owner.Player });
            foreach (var kind in new[] { TraitActivationKind.TrailCreated, TraitActivationKind.TrailChanged, TraitActivationKind.TrailRemoved })
                yield return new Sample { Kind = kind, RequiresCompanion = true,
                    Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(a.EntityId, null, TraitKey.GroundCompaction, kind, 1, targetCells: new[] { trail.Cell })),
                        new CombatMovementSourceEvent(kind == TraitActivationKind.TrailCreated ? null : trail,
                            kind == TraitActivationKind.TrailCreated ? trail : trail.WithRemainingOpposingTurnEnds(kind == TraitActivationKind.TrailRemoved ? 0 : 1)) } };
            // 자폭 폭발: 실제로 맞은 대상의 체력 사건이 함께 있어야 한다.
            yield return new Sample { Kind = TraitActivationKind.SelfDestructExploded, Actors = new[] { 1 }, RequiresCompanion = true,
                Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(new EntityId(2), null, TraitKey.SelfDestruct,
                        TraitActivationKind.SelfDestructExploded, 5, new[] { a.EntityId })),
                    new CombatCharacterEvent(CombatEventKind.HealthChanged, a, a.WithHealth(new HealthStateSnapshot(95, 100))) } };
            // 작렬 부딪힘: 체력 사건이 있는 받는 쪽만 실제 변화다. 피해가 모두 막혀도 부딪힘 자체는 관찰로 읽는다.
            yield return new Sample { Kind = TraitActivationKind.CrashImpactCollided, Actors = new[] { 1 },
                Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(new EntityId(2), null, TraitKey.CrashImpact,
                        TraitActivationKind.CrashImpactCollided, 10, new[] { a.EntityId }, new[] { a.Anchor.Value + new CellCoord(1, 0) })),
                    new CombatCharacterEvent(CombatEventKind.HealthChanged, a, a.WithHealth(new HealthStateSnapshot(95, 100))) } };
            // 재구성 시작: 재구성 사건이 함께 있어야 한다.
            yield return new Sample { Kind = TraitActivationKind.ReconstructionStarted, Actors = new[] { 1 }, RequiresCompanion = true,
                Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(a.EntityId, null, TraitKey.Reconstruction,
                        TraitActivationKind.ReconstructionStarted, 10, new[] { a.EntityId })),
                    new CombatCharacterEvent(CombatEventKind.Reconstructed, a.WithHealth(new HealthStateSnapshot(10, 100)), a) } };
            // 재구성 끝(부활): 재구성 중 상태가 풀린 특성 상태 사건이 함께 있어야 한다.
            var reconstructing = a.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { new ReconstructionStatus(Origin(TraitKey.Reconstruction), 1) }));
            yield return new Sample { Kind = TraitActivationKind.ReconstructionEnded, Actors = new[] { 1 }, RequiresCompanion = true,
                Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(a.EntityId, null, TraitKey.Reconstruction,
                        TraitActivationKind.ReconstructionEnded, 0, new[] { a.EntityId })),
                    new CombatTraitStateEvent(reconstructing, a) } };
            // 시체 밀기: 위치 사건 없이 경로만 남기며 출처의 사망 사실이 함께 있어야 한다.
            yield return new Sample { Kind = TraitActivationKind.CorpseDisplaced, RequiresCompanion = true,
                Events = new CombatEvent[] { Wrap(new TraitActivationSnapshot(a.EntityId, null, TraitKey.SelfDestruct,
                        TraitActivationKind.CorpseDisplaced, 1, targetCells: new[] { a.Anchor.Value, a.Anchor.Value + new CellCoord(1, 0) })),
                    new CombatCharacterEvent(CombatEventKind.Died, a, a.WithHealth(new HealthStateSnapshot(0, 100))) } };
        }
    }
}
