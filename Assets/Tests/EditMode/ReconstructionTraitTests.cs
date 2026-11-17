using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 적 전용 재구성 특성의 치명 피해 가로채기(Phase D)입니다.
    /// 기본 배치: 플레이어 공격자 1 (3,3) 공격력 20, 재구성 적 2 (4,3) 체력 5/100, 이웃 적 3 (5,3), 먼 적 4 (6,6). 플레이어 턴입니다.
    /// </summary>
    public sealed class ReconstructionTraitTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly TargetingMethodSpec SingleNoSplash =
            new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
        private static readonly CellCoord HolderCell = new CellCoord(4, 3);
        private static readonly EntityId Holder = new EntityId(2);

        [TestCase(1, 0)]
        [TestCase(2, 50)]
        [TestCase(3, 50)]
        public void LethalHit_RestoresFullHealthAndStartsReconstructing(int level, int shield)
        {
            var input = Input(level);
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            var holder = Final(result, input, 2);
            Assert.That(holder.Health.IsDead, Is.False);
            Assert.That(holder.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Reconstruction(holder).RemainingOwnTurnStarts, Is.EqualTo(2));
            Assert.That(TraitStatus(holder).Used, Is.True);
            Assert.That(Shield(holder), Is.EqualTo(shield));

            var started = Activations(result, TraitActivationKind.ReconstructionStarted).Single();
            Assert.That(started.SourceUnitId, Is.EqualTo(Holder));
            Assert.That(started.TargetIds, Is.EqualTo(new[] { Holder }));
            Assert.That(started.Magnitude, Is.EqualTo(5));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Died), Is.False);
            var events = result.Events.ToList();
            Assert.That(events.FindIndex(e => IsActivation(e, TraitActivationKind.ReconstructionStarted)),
                Is.LessThan(events.FindIndex(e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.Reconstructed)));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
        }

        [Test]
        public void Reconstruction_SuppressesDeathTraits_UntilTheNextLethalHitReallyKills()
        {
            // 재구성은 죽음이 아니므로 자폭·공포·처치 보상이 없다. 다시 치명 피해를 받으면 그때 죽고 자폭한다(설계 R4).
            var input = Input(1, selfDestruct: true);
            var first = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(first.Accepted, Is.True, first.Reason);
            Assert.That(Activations(first, TraitActivationKind.SelfDestructExploded), Is.Empty);
            Assert.That(Final(first, input, 1).Health.CurrentHealth, Is.EqualTo(100));

            var hurt = Final(first, input, 2).WithHealth(new HealthStateSnapshot(5, 100));
            var again = Replace(input, hurt);
            var second = CombatCommandCalculator.Calculate(again, NoRules);
            Assert.That(second.Accepted, Is.True, second.Reason);
            Assert.That(Final(second, again, 2).Health.IsDead, Is.True);
            Assert.That(Activations(second, TraitActivationKind.ReconstructionStarted), Is.Empty);
            Assert.That(Activations(second, TraitActivationKind.SelfDestructExploded).Single().SourceUnitId, Is.EqualTo(Holder));
            Assert.That(Final(second, again, 1).Health.CurrentHealth, Is.EqualTo(90));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(second.Events));
        }

        [Test]
        public void UsedReconstruction_DoesNotTriggerAgain()
        {
            var input = Input(1, used: true);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(Activations(result, TraitActivationKind.ReconstructionStarted), Is.Empty);
        }

        [Test]
        public void FinishingShotInstantDefeat_IsAlsoIntercepted_UntilReconstructionIsUsed()
        {
            // 즉사도 재구성이 버틴다(2026-11-10 사용자 결정, Q-ED1 바뀜). 즉사 발동은 그대로 남고 사망은 없다.
            var input = Input(2, finishingShot: true);
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var holder = Final(result, input, 2);
            Assert.That(holder.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Reconstruction(holder).RemainingOwnTurnStarts, Is.EqualTo(2));
            Assert.That(Shield(holder), Is.EqualTo(50));
            Assert.That(Activations(result, TraitActivationKind.InstantDefeat).Single().TargetIds, Is.EqualTo(new[] { Holder }));
            Assert.That(Activations(result, TraitActivationKind.ReconstructionStarted).Single().Magnitude, Is.EqualTo(5));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Died), Is.False);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));

            // 이미 쓴 재구성은 즉사를 버티지 못한다.
            var used = Input(2, used: true, finishingShot: true);
            var killed = CombatCommandCalculator.Calculate(used, NoRules);
            Assert.That(killed.Accepted, Is.True, killed.Reason);
            Assert.That(Final(killed, used, 2).Health.IsDead, Is.True);
            Assert.That(Activations(killed, TraitActivationKind.ReconstructionStarted), Is.Empty);
        }

        [Test]
        public void PublicPreview_ShowsReconstruction()
        {
            var input = Input(2);
            CombatCommandCalculator.Preview(input, NoRules, out var projection);
            Assert.That(projection.Accepted, Is.True, projection.Reason);
            var holder = Final(projection, input, 2);
            Assert.That(holder.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Reconstruction(holder), Is.Not.Null);
            Assert.That(Activations(projection, TraitActivationKind.ReconstructionStarted).Single().SourceUnitId, Is.EqualTo(Holder));
        }

        [Test]
        public void FallDeath_IsNotIntercepted()
        {
            var input = Input(1);
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            projection.ApplyFall(Holder, 1, HolderCell);
            var result = projection.Complete();
            Assert.That(Final(result, input, 2).Health.IsDead, Is.True);
            Assert.That(Activations(result, TraitActivationKind.ReconstructionStarted), Is.Empty);
        }

        [Test]
        public void ReconstructingUnit_IsStillDamagedAndKnockedBack()
        {
            var reconstructing = CombatCommandTestData.With(Holding(1, used: true), hp: 100)
                .WithStatusEffects(new StatusEffectSet(new AppliedStatus[]
                    { new ReconstructionStatus(TraitEffectOrigin.Create(Holder, TraitKey.Reconstruction), 2) }));
            var input = Input(1, holder: reconstructing, knockback: 2, neighbor: Enemy(3, new CellCoord(1, 6), 100),
                far: Enemy(4, new CellCoord(7, 3), 100));
            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            var holder = Final(result, input, 2);
            Assert.That(holder.Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(holder.Anchor, Is.EqualTo(new CellCoord(6, 3)));
            Assert.That(Reconstruction(holder).RemainingOwnTurnStarts, Is.EqualTo(2));
        }

        internal static CombatCommandInput Input(int level, bool used = false, bool selfDestruct = false, bool finishingShot = false,
            CharacterCombatFacts holder = null, int knockback = 0, CharacterCombatFacts neighbor = null, CharacterCombatFacts far = null)
        {
            var input = CombatCommandTestData.Input(SingleNoSplash, AttackAim.ForCell(HolderCell), empty: true);
            var actorTraits = new List<TraitStatusSnapshot>();
            var actorSpecs = new List<TraitSpec>();
            if (knockback > 0)
            {
                actorTraits.Add(new KnockbackTraitStatusSnapshot(knockback));
                actorSpecs.Add(new KnockbackTraitSpec(knockback));
            }
            if (finishingShot)
            {
                actorTraits.Add(new FinishingShotTraitStatusSnapshot());
                actorSpecs.Add(new FinishingShotTraitSpec(50));
            }
            var actor = CombatCommandTestData.With(input.Characters[0], position: new CellCoord(3, 3),
                traits: actorTraits.Count > 0 ? actorTraits.ToArray() : null);
            var characters = new[]
            {
                actor, input.Characters[1], holder ?? Holding(level, used, selfDestruct),
                neighbor ?? Enemy(3, new CellCoord(5, 3), 100),
                far ?? Enemy(4, new CellCoord(6, 6), 100),
            };
            var profiles = input.Profiles.ToArray();
            if (actorSpecs.Count > 0)
                profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main,
                    actorSpecs.ToArray()), SingleNoSplash);
            var holderSpecs = selfDestruct
                ? new TraitSpec[] { new ReconstructionTraitSpec(level), new SelfDestructTraitSpec(1) }
                : new TraitSpec[] { new ReconstructionTraitSpec(level) };
            var all = profiles.Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 20, CategoryKey.Main, holderSpecs), SingleNoSplash));
            return CombatCommandTestData.With(input, characters, profiles: all.ToArray());
        }

        internal static CharacterCombatFacts Holding(int level, bool used = false, bool selfDestruct = false)
        {
            var traits = selfDestruct
                ? new TraitStatusSnapshot[] { new ReconstructionTraitStatusSnapshot(level, used), new SelfDestructTraitStatusSnapshot(1) }
                : new TraitStatusSnapshot[] { new ReconstructionTraitStatusSnapshot(level, used) };
            return CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Gravekeeper, HolderCell, Owner.Enemy, 20),
                hp: 5, traits: traits);
        }

        internal static CombatCommandInput Replace(CombatCommandInput input, CharacterCombatFacts character) =>
            CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == character.EntityId ? character : c).ToArray());

        internal static CharacterCombatFacts Final(CombatCommandTransition result, CombatCommandInput input, int id)
        {
            var entityId = new EntityId(id);
            return result.Characters.Characters.SingleOrDefault(c => c.Final.EntityId == entityId)?.Final ??
                input.Characters.Single(c => c.EntityId == entityId);
        }

        internal static ReconstructionStatus Reconstruction(CharacterCombatFacts character) =>
            character.StatusEffects.Entries.OfType<ReconstructionStatus>().SingleOrDefault();

        internal static int Shield(CharacterCombatFacts character) => character.StatusEffects.Entries.OfType<ShieldStatus>()
            .Where(s => s.Origin == TraitEffectOrigin.Create(character.EntityId, TraitKey.Reconstruction)).Sum(s => s.RemainingBudget);

        internal static TraitActivationSnapshot[] Activations(CombatCommandTransition result, TraitActivationKind kind) =>
            result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Where(t => t.TraitActivationKind == kind).ToArray();

        private static ReconstructionTraitStatusSnapshot TraitStatus(CharacterCombatFacts character) =>
            character.Stats.TraitStatuses.ToArray().OfType<ReconstructionTraitStatusSnapshot>().Single();

        private static CharacterCombatFacts Enemy(int id, CellCoord cell, int hp) =>
            CombatCommandTestData.With(CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, Owner.Enemy), hp: hp);

        private static bool IsActivation(CombatEvent e, TraitActivationKind kind) =>
            e is CombatEffectsEvent effects && effects.Traits.Any(t => t.TraitActivationKind == kind);
    }
}
