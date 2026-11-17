using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using static ProjectL.Tests.Core.CombatItemDirectDamageTests;
namespace ProjectL.Tests.Core
{
    public sealed class CombatItemDamageSourceTests
    {
        [TestCase("ordinary")]
        [TestCase("environment")]
        [TestCase("reflection")]
        public void WhiskeyApplication_AcrossDamageSources_PreservesCausalState(string source)
        {
            List<CombatEvent> events;
            CharacterCombatFacts final;
            int id;
            if (source == "ordinary")
            {
                var (plan, world, ledger) = Prepare(Input(15, 60, 1), duplicate: true);
                AttackDirectDamageCalculator.Apply(plan, world, ledger, new HashSet<EntityId>());
                events = world.Events; id = 2; final = world.Characters[new EntityId(id)];
            }
            else if (source == "reflection")
            {
                var input = ReflectionInput(1);
                var (plan, world, ledger) = Prepare(input);
                AttackDirectDamageCalculator.Apply(plan, world, ledger, new HashSet<EntityId>(), Heart(15));
                Assert.That(world.Characters[input.ActorId].Health.CurrentHealth, Is.EqualTo(55));
                AttackDirectDamageCalculator.Apply(plan, world, new CommandAttackLedger(), new HashSet<EntityId>(), Heart(15));
                events = world.Events; id = input.ActorId.Value; final = world.Characters[input.ActorId];
            }
            else
            {
                var input = Input(15, 60, 1);
                var characters = input.Characters.ToDictionary(c => c.EntityId);
                events = new List<CombatEvent>(); id = 2;
                var movement = MovementProjection.ForArrival(input, characters, events);
                for (int i = 0; i < 2; i++)
                {
                    movement.ChangeHealth(new MovementHealthRequest(new EntityId(id), 15), false,
                        new CombatDamageOrigin(new CellCoord(4, 3), null, i + 1));
                    if (i == 0) Assert.That(characters[new EntityId(id)].Health.CurrentHealth, Is.EqualTo(55));
                }
                final = characters[new EntityId(id)];
            }
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(40));
            Assert.That(final.Items.EquippedItemKey, Is.Null);
            AssertSequence(events, id, new[] { 45, 55, 40 });
            Assert.That(events.OfType<CombatItemEvent>().Count(e => e.Consumed), Is.EqualTo(1));
            Assert.That(events.OfType<CombatItemEvent>().Single(e => e.Consumed).AppliedMagnitude, Is.EqualTo(10));
        }

        [TestCase(1, 50)]
        [TestCase(2, 40)]
        public void EnvironmentDamage_UsesWhiskeyButNotRoket(int key, int hp)
        {
            var input = Input(20, 60, key);
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            MovementProjection.ForArrival(input, characters, events).ChangeHealth(new MovementHealthRequest(new EntityId(2), 20),
                false, new CombatDamageOrigin(new CellCoord(4, 3), null, 1));
            Assert.That(characters[new EntityId(2)].Health.CurrentHealth, Is.EqualTo(hp));
            Assert.That(characters[new EntityId(2)].Items.EquippedItemKey, Is.EqualTo(key == 1 ? null : (int?)key));
            Assert.That(events.OfType<CombatItemEvent>().All(e => e.DamageKind == CombatDamageKind.EnvironmentDamage), Is.True);
        }

        [TestCase(1, 40)]
        [TestCase(2, 30)]
        public void ReflectedDamage_UsesWhiskeyButNotRoket(int key, int hp)
        {
            var input = ReflectionInput(key);
            var (plan, world, ledger) = Prepare(input);
            AttackDirectDamageCalculator.Apply(plan, world, ledger, new HashSet<EntityId>(), Heart(30));
            Assert.That(world.Characters[input.ActorId].Health.CurrentHealth, Is.EqualTo(hp));
            Assert.That(world.Characters[input.ActorId].Items.EquippedItemKey, Is.EqualTo(key == 1 ? null : (int?)key));
            var health = world.Events.OfType<CombatCharacterEvent>().First(e => e.Final.EntityId == input.ActorId);
            AssertCause(world.Events, key, CombatDamageKind.ReflectedDamage);
            Assert.That(world.Events.FindIndex(e => e is CombatArtifactEvent), Is.LessThan(world.Events.IndexOf(health)));
        }

        [TestCase(1, 50)]
        [TestCase(2, 40)]
        public void KillChain_UsesExtraAttackCause(int key, int hp)
        {
            var input = Input(20, 1, null);
            input = AddTrait(input, new KillChainAttackTraitSpec(), new KillChainAttackTraitStatusSnapshot());
            input = Items(CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId.Value != 4)
                .Select(c => c.EntityId.Value == 3 ? Equip(c, 60, key) : c).ToArray()));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(hp));
            AssertCause(result.Events, key, CombatDamageKind.ExtraAttack);
        }

        [Test]
        public void MovementHazards_ObserveLatestSlotAndHealth()
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1),new CellCoord(2, 1),new CellCoord(3, 1));
            var s = input.State;
            var state = EnvironmentDamageTests.WithDamageTiles(new CombatCommandState(8,8,s.Phase,s.Structures,                s.Turn,s.Environment), (new CellCoord(2, 1), 15), (new CellCoord(3, 1), 15));
            input = Items(CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId ? Equip(c, 60, 1) : c).ToArray(), state));
            var result = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new EnvironmentDamageTests.DamageRule() });
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, 1).Health.CurrentHealth, Is.EqualTo(40));
            AssertSequence(result.Events, 1, new[] { 45, 55, 40 });
        }
        [Test]
        public void BarrelExplosion_UsesWhiskeyThroughSharedDamageBoundary()
        {
            var input = ObstacleReactionTests.Barrels(ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 1, 20));
            input = Items(CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId.Value == 3 ? Equip(c, 60, 1) : c).ToArray()));
            var result = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, 3).Health.CurrentHealth, Is.EqualTo(50));
            Assert.That(result.Events.OfType<CombatItemEvent>().Single().DamageKind, Is.EqualTo(CombatDamageKind.EnvironmentDamage));
        }

        [Test]
        public void KnockbackArrivalHazard_ActivateAfterOrdinaryDamageWithoutRetestingOrdinaryRules()
        {
            var input = CliffFallTests.Knockback();
            var s = input.State;
            var state = EnvironmentDamageTests.WithDamageObjects(new CombatCommandState(8,8,s.Phase,s.Structures,                s.Turn,s.Environment), (new CellCoord(5, 3), 15));
            input = Items(CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId.Value == 2 ? Equip(c, 80, 1) : c).ToArray(), state));
            var result = CombatCommandCalculator.Calculate(input, EnvironmentDamageTests.DamageRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, 2).Health.CurrentHealth, Is.EqualTo(55));
            Assert.That(result.Events.OfType<CombatItemEvent>().Single().DamageKind, Is.EqualTo(CombatDamageKind.EnvironmentDamage));
        }

        [TestCase(1, 40)]
        [TestCase(2, 30)]
        public void ReactionAttack_UsesExplicitCauseAndOnlyWhiskey(int key, int hp)
        {
            var input = Input(30, 60, key);
            var result = CombatAttackCalculator.Reaction(input, NoRules, input.Characters[0], input.Aim);
            Assert.That(result, Is.Not.Null);
            Assert.That(Final(result, 2).Health.CurrentHealth, Is.EqualTo(hp));
            AssertCause(result.Events, key, CombatDamageKind.ReactionAttack);
        }

        [Test]
        public void RecoveryNormalizationAndFall_DoNotActivateWhiskey()
        {
            var input = Input(1, 40, 1);
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var movement = MovementProjection.ForArrival(input, characters, events);
            movement.ChangeHealth(new MovementHealthRequest(new EntityId(2), 1), true);
            Assert.That(characters[new EntityId(2)].Items.EquippedItemKey, Is.EqualTo(1));
            var normalized = PositionTileModifierCalculator.Replace(characters[new EntityId(2)],
                new PositionTileModifier(new CellCoord(4, 3), new long[] { 71 }, 0, -70));
            Assert.That(normalized.Items.EquippedItemKey, Is.EqualTo(1));
            movement.ApplyFall(new EntityId(2), 71, new CellCoord(4, 3));
            Assert.That(events.OfType<CombatItemEvent>().Any(e => e.Activated), Is.False);
        }

        /// <summary>
        /// 피해 경로가 적용 경계에 넘긴 분류를 아이템 결과로 확인한다. 위스키(1)는 발동 사건에 분류를 남기고,
        /// 로켓(2)은 일반 공격이 아니면 발동하지 않는다.
        /// </summary>
        private static void AssertCause(IEnumerable<CombatEvent> events, int key, CombatDamageKind expected)
        {
            var activated = events.OfType<CombatItemEvent>().Where(e => e.Activated).ToArray();
            if (key == 1) Assert.That(activated.Single().DamageKind, Is.EqualTo(expected));
            else Assert.That(activated, Is.Empty);
        }

        private static CombatCommandInput ReflectionInput(int key)
        {
            var input = ThornHeartTestData.Input(heart: false, hp: 60);
            return Items(ThornHeartTestData.Replace(input, 1, c => Equip(c, 60, key)));
        }

        private static ThornHeartAttackScope Heart(int amount)
        {
            var effect = ArtifactThornHeartRuleTests.HeartEffect(); effect.value.amount = amount;
            return new ThornHeartAttackScope(ArtifactEffectCompiler.Compile(new ArtifactLoadoutSnapshot(0,
                new[] { new ArtifactPlacedEffectSnapshot(1002, 13, effect) })), true, true);
        }

        private static CombatCommandInput AddTrait(CombatCommandInput input, TraitSpec spec, TraitStatusSnapshot status)
        {
            var actor = CombatCommandTestData.With(input.Characters[0], traits: new[] { status });
            return Items(CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, actor.Stats.AttackPower, CategoryKey.Main, spec), p.Method) : p).ToArray()));
        }
    }
}
