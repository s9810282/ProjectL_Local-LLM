using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class TraitRestrictionTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [Test]
        public void ExistingFailureReasonNumbers_RemainStable()
        {
            Assert.That((int)AttackFailureReason.WrongPhase, Is.EqualTo(1));
            Assert.That((int)AttackFailureReason.Dead, Is.EqualTo(5));
            Assert.That((int)MovementFailureReason.EntityNotFound, Is.EqualTo(1));
            Assert.That((int)MovementFailureReason.Dead, Is.EqualTo(5));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void AppliedDuringEnemyTurn_ExpiresAtThatEnd(int duration)
        {
            var input = EnemyControllerPhaseTests.Input(Source(duration), CombatControllerPhase.Enemy)
                .WithActor(new EntityId(1)).WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(4, 3)), default, null, null);
            var hit = CombatCommandTestData.Reaction(input, Rules);
            Assert.That(hit, Is.Not.Null);
            var world = CombatCommandComposition.Project(hit);
            world = End(world, Owner.Enemy);
            Assert.That(Remaining(world, "Disarm"), Is.EqualTo(duration - 1));
            Assert.That(Remaining(world, "AnkleBreaker"), Is.EqualTo(duration - 1));
            if (duration == 2) Assert.That(Remaining(End(world, Owner.Enemy), "Disarm"), Is.Zero);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void AppliedDuringAlliedTurn_CoversNextEnemyTurn(int duration)
        {
            var world = CombatCommandComposition.Project(TraitCombatFixtures.Attack(Source(duration)));
            world = End(world, Owner.Player);
            Assert.That(Remaining(world, "Disarm"), Is.EqualTo(duration));
            Assert.That(Remaining(world, "AnkleBreaker"), Is.EqualTo(duration));
            for (int n = duration - 1; n >= 0; n--)
            {
                world = End(world, Owner.Enemy);
                Assert.That(Remaining(world, "Disarm"), Is.EqualTo(n));
                Assert.That(Remaining(world, "AnkleBreaker"), Is.EqualTo(n));
            }
        }

        [Test]
        public void Reapply_RefreshesWithoutAdding()
        {
            var input = Source(2);
            var second = CombatCommandTestData.Character(5, EntityKey.Unit_Akimbo, new CellCoord(1, 2), Owner.Player, 20)
                .With(stats: input.Characters.Single(c => c.EntityId.Value == 1).Stats);
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] { second }).ToArray());
            var first = CombatCommandComposition.Project(TraitCombatFixtures.Attack(input));
            first = End(first, Owner.Enemy);
            var refreshed = CombatCommandComposition.Project(TraitCombatFixtures.Attack(first.WithActor(second.EntityId)));
            Assert.That(Remaining(refreshed, "Disarm"), Is.EqualTo(2));
            var state = Status(refreshed.Characters.Single(c => c.EntityId.Value == 2), "Disarm");
            var origin = state.GetType().GetProperty("Origin").GetValue(state);
            Assert.That(origin.GetType().GetProperty("OwnerInstanceId").GetValue(origin), Is.EqualTo(5L));
        }

        [TestCase(TargetingChoiceMode.Direction)]
        [TestCase(TargetingChoiceMode.All)]
        public void DirectionAndAll_DoNotApply(TargetingChoiceMode mode)
        {
            var method = new TargetingMethodSpec(mode == TargetingChoiceMode.Direction ? TargetingShape.CardinalLine : TargetingShape.RadialSquare,
                mode, TargetingSplash.None, 0);
            var aim = mode == TargetingChoiceMode.Direction ? AttackAim.ForDirection(AttackDirection.Right) : AttackAim.ForAll();
            var input = Source(1, CombatCommandTestData.Input(method, aim));
            var hit = TraitCombatFixtures.Attack(input);
            Assert.That(hit.Accepted, Is.True, hit.Reason);
            foreach (var character in CombatCommandComposition.Project(hit).Characters)
            {
                Assert.That(Status(character, "Disarm"), Is.Null);
                Assert.That(Status(character, "AnkleBreaker"), Is.Null);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StructureOrSplashPrimary_DoesNotApply(bool structure)
        {
            var input = Source(1, CombatCommandTestData.Input(category: CategoryKey.Cannon));
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 2).ToArray());
            if (structure)
            {
                var cell = new CellCoord(4, 3);
                var spec = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0);
                input = CombatCommandTestData.With(input, state: input.State.With(structures: new[]
                {
                    new StructureCombatFacts(new EntityId(100), spec, new HealthStateSnapshot(100, 100), cell, Rotation4.R0, new[] { cell })
                }));
            }
            var hit = TraitCombatFixtures.Attack(input);
            Assert.That(hit.Accepted, Is.True, hit.Reason);
            var splash = TraitCombatFixtures.Final(hit, 3);
            Assert.That(splash.Health.CurrentHealth, Is.LessThan(100));
            Assert.That(Status(splash, "Disarm"), Is.Null);
            Assert.That(Status(splash, "AnkleBreaker"), Is.Null);
        }

        [Test]
        public void AdmissionPreviewAndAttempt_DoNotConsumeRestrictions()
        {
            var hit = CombatCommandComposition.Project(TraitCombatFixtures.Attack(Source(1)));
            var input = EnemyControllerPhaseTests.Input(hit, CombatControllerPhase.Enemy).WithActor(new EntityId(2))
                .WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(1, 3)), default, null, null);
            Assert.That(CombatCommandCalculator.GetAttackRange(input).FailureReason.ToString(), Is.EqualTo("Disarmed"));
            Assert.That(CombatMovementCalculator.Range(input).FailureReason.ToString(), Is.EqualTo("AnkleBroken"));
            var availability = CombatCommandCalculator.GetActionAvailability(input);
            Assert.That(availability.CanAttack, Is.False);
            Assert.That(availability.CanMove, Is.False);
            Assert.That(CombatCommandCalculator.Preview(input, Rules).CanAttack, Is.False);
            var rejected = TraitCombatFixtures.Attack(input);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(CombatCommandTestData.Reaction(input, Rules), Is.Null);
            Assert.That(Remaining(input, "Disarm"), Is.EqualTo(1));
            Assert.That(Remaining(input, "AnkleBreaker"), Is.EqualTo(1));
        }

        [Test]
        public void DisarmOnly_EnemyCanStillPursue()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, new DisarmTraitSpec(1));
            var hit = CombatCommandComposition.Project(TraitCombatFixtures.Attack(input));
            var enemy = hit.Characters.Single(c => c.EntityId.Value == 2);
            hit = CombatCommandTestData.With(hit, characters: hit.Characters.Select(c => c.EntityId == enemy.EntityId
                ? c.With(stats: c.Stats.With(appointRange: 1)) : c).ToArray());
            var turn = EnemyControllerPhaseTests.Input(hit, CombatControllerPhase.Enemy).WithActor(enemy.EntityId);
            Assert.That(CombatCommandCalculator.GetAttackRange(turn).FailureReason.ToString(), Is.EqualTo("Disarmed"));
            Assert.That(CombatMovementCalculator.Range(turn).Succeeded, Is.True);
            Assert.That(EnemyMovementPathfinder.TryFind(turn, new EntityId(1), out _), Is.True);
        }

        [Test]
        public void ForcedMovement_IgnoresVoluntaryMoveLock()
        {
            var input = Source(1);
            var hit = CombatCommandComposition.Project(TraitCombatFixtures.Attack(input));
            var source = CombatCommandTestData.Character(5, EntityKey.Unit_ShotGun, new CellCoord(2, 3), Owner.Player, 20);
            source = source.With(stats: source.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) }));
            hit = TraitCombatFixtures.Characters(hit, hit.Characters.Where(c => c.EntityId.Value != 4).Concat(new[] { source }).ToArray());
            hit = CombatCommandTestData.With(hit, profiles: hit.Profiles.Concat(new[] { new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_ShotGun, 20, CategoryKey.Main, new KnockbackTraitSpec(1)),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)) }).ToArray());
            var forced = TraitCombatFixtures.Attack(hit.WithActor(source.EntityId));
            Assert.That(forced.Accepted, Is.True, forced.Reason);
            Assert.That(TraitCombatFixtures.Final(forced, 2).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(Status(TraitCombatFixtures.Final(forced, 2), "AnkleBreaker"), Is.Not.Null);
        }

        [Test]
        public void ControllerEnd_ExpiresOnlyDueGrantsAndPreservesHunter()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, new VanguardTraitSpec(3, 1));
            input = input.WithPersistentFacts(input.Owned.Select(o => o.Role == CharacterOwnershipRole.Main
                ? new OwnedCharacterRecord(o.Id, o.CharacterKey, o.Role, hunterAttackBonus: 7) : o).ToArray(),
                input.ItemInventory, input.GoldBalance, input.FameBalance);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.OwnershipRole == CharacterOwnershipRole.Main
                ? CombatEffectiveStatsCalculator.ProjectTraitAttack(c, 7) : c).ToArray());
            var hit = CombatCommandComposition.Project(TraitCombatFixtures.Attack(input));
            Assert.That(hit.Characters.Single(c => c.EntityId.Value == 10).Stats.TraitAttackBonus, Is.EqualTo(10));
            var ended = CombatTurnCalculator.EndPlayerTurn(CombatCommandTestData.FreshCommand(hit, CombatCommandKind.EndPlayerTurn), Rules);
            Assert.That(ended.Accepted, Is.True, ended.Reason);
            Assert.That(TraitCombatFixtures.Final(ended, 10).TraitEffects.AttackGrants, Is.Empty);
            Assert.That(TraitCombatFixtures.Final(ended, 10).Stats.TraitAttackBonus, Is.EqualTo(7));
        }

        [Test]
        public void OtherDebuffChangesAndExpiry_PreserveIndependentStatuses()
        {
            var input = CombatCommandComposition.Project(TraitCombatFixtures.Attack(Source(1)));
            var victim = input.Characters.Single(c => c.EntityId.Value == 2);
            var state = new CharacterDebuffState(new IntimidationDebuffState(20), new DefenseReductionDebuffState(30, 2),
                new HuntersMarkState(new EntityId(10), Owner.Player, 25), victim.Debuffs.Disarm, victim.Debuffs.AnkleBreaker);
            var changes = new[]
            {
                CombatDebuffCalculator.ApplyIntimidation(state, new IntimidationTraitSpec(20)),
                CombatDebuffCalculator.ClearIntimidation(state),
                CombatDebuffCalculator.ApplyDefenseReduction(state, 10, false),
                new DefenseReductionStatusRule().OnTurnEnded(state)
            };
            foreach (var changed in changes)
            {
                Assert.That(changed.Disarm.SameValues(state.Disarm), Is.True);
                Assert.That(changed.AnkleBreaker.SameValues(state.AnkleBreaker), Is.True);
                Assert.That(changed.HuntersMark.SameValues(state.HuntersMark), Is.True);
            }
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == victim.EntityId
                ? c.With(debuffs: state) : c).ToArray());
            var expired = End(input, Owner.Enemy).Characters.Single(c => c.EntityId == victim.EntityId).Debuffs;
            Assert.That(expired.Disarm, Is.Null);
            Assert.That(expired.AnkleBreaker, Is.Null);
            Assert.That(expired.Intimidation.SameValues(state.Intimidation), Is.True);
            Assert.That(expired.DefenseReduction.SameValues(state.DefenseReduction), Is.True);
            Assert.That(expired.HuntersMark.SameValues(state.HuntersMark), Is.True);
        }

        [Test]
        public void DisarmedRoar_RemainsPendingWithoutAttackingOrResetting()
        {
            var input = CombatCommandComposition.Project(TraitCombatFixtures.Attack(Source(1)));
            var enemy = input.Characters.Single(c => c.EntityId.Value == 2);
            var roar = new RoarTraitStatusSnapshot(3, 3);
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == enemy.EntityId
                    ? c.With(stats: c.Stats.With(traitStatuses: new TraitStatusSnapshot[] { roar })) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == enemy.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(enemy.CharacterKey, 1, CategoryKey.Main, new RoarTraitSpec(3)), p.Method) : p).ToArray());
            Assert.That(BossRoarCalculator.PendingBosses(input), Is.Empty);
            Assert.That(input.Characters.Single(c => c.EntityId == enemy.EntityId).Stats.TraitStatuses.ToArray().Single().SameValues(roar), Is.True);
        }

        private static CombatCommandInput Source(int duration, CombatCommandInput input = null) =>
            TraitCombatFixtures.Equip(input ?? CombatCommandTestData.Input(), EntityKey.Unit_Akimbo,
                new DisarmTraitSpec(duration), new AnkleBreakerTraitSpec(duration));

        internal static object Status(CharacterCombatFacts character, string name)
        {
            var property = typeof(CharacterDebuffState).GetProperty(name);
            Assert.That(property, Is.Not.Null, name);
            return property.GetValue(character.Debuffs);
        }

        private static int Remaining(CombatCommandInput input, string name)
        {
            var status = Status(input.Characters.Single(c => c.EntityId.Value == 2), name);
            return status == null ? 0 : (int)status.GetType().GetProperty("RemainingAffectedSideTurnEnds").GetValue(status);
        }

        private static CombatCommandInput End(CombatCommandInput input, Owner owner)
        {
            var type = typeof(CombatCommandCalculator).Assembly.GetType(typeof(CombatCommandCalculator).Namespace + ".TraitTurnLifecycle");
            Assert.That(type, Is.Not.Null);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            type.GetMethod("EndControllerTurn", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { input, projected, owner });
            return CombatCommandTestData.With(input, characters: projected.Values.ToArray());
        }
    }
}
