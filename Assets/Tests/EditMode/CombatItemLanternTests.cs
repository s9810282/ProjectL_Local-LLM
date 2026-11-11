using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class CombatItemLanternTests
    {
        internal static readonly ItemDefinition Lantern = new ItemDefinition(4, new LanternItemSpec(5));

        [TestCase(0)]
        [TestCase(1)]
        public void LivingOrdinaryHit_ActivatesEvenAtZeroDamage(int damage)
        {
            var input = CombatItemDirectDamageTests.Input(damage, 100, null);
            var holder = CombatItemDirectDamageTests.Equip(input.Characters.Single(c => c.EntityId.Value == 2), 100, 4);
            var context = new ItemDamageContext(CombatDamageKind.OrdinaryAttack, input.Characters[0], holder, damage);
            var after = damage == 0 ? holder : CombatCommandCalculator.Hurt(holder, damage);
            var decision = CombatItemResolver.AfterDamage(Lantern, new ItemAfterDamageContext(context, after, damage));
            Assert.That(decision.Activated, Is.True);
            Assert.That(decision.TargetId, Is.EqualTo(input.ActorId));
            Assert.That(decision.TemporaryEffect.Stat, Is.EqualTo(ItemStatKind.PhysicalDefense));
        }

        [Test]
        public void Lantern_LethalHit_DoesNotActivate()
        {
            var input = CombatItemDirectDamageTests.Input(100, 100, null);
            var holder = CombatItemDirectDamageTests.Equip(input.Characters.Single(c => c.EntityId.Value == 2), 100, 4);
            var hit = new ItemDamageContext(CombatDamageKind.OrdinaryAttack, input.Characters[0], holder, 100);
            var decision = CombatItemResolver.AfterDamage(Lantern,
                new ItemAfterDamageContext(hit, CombatCommandCalculator.Hurt(holder, 100), 100));
            Assert.That(decision.Activated, Is.False);
        }

        [TestCase(CombatDamageKind.ExtraAttack)]
        [TestCase(CombatDamageKind.ReactionAttack)]
        [TestCase(CombatDamageKind.EnvironmentDamage)]
        [TestCase(CombatDamageKind.ReflectedDamage)]
        [TestCase(CombatDamageKind.DamageOverTime)]
        public void NonOrdinaryDamage_DoesNotActivate(CombatDamageKind cause)
        {
            var input = CombatItemDirectDamageTests.Input(0, 100, null);
            var holder = CombatItemDirectDamageTests.Equip(input.Characters.Single(c => c.EntityId.Value == 2), 100, 4);
            var hit = new ItemDamageContext(cause, input.Characters[0], holder, 0);
            Assert.That(CombatItemResolver.AfterDamage(Lantern, new ItemAfterDamageContext(hit, holder, 0)).Activated, Is.False);
        }

        [TestCase(3, 0, 3)]
        [TestCase(8, 3, 5)]
        public void CommonApplication_ConsumesBothHoldersButOnlyOneEffectAndActualStatDelta(int defense, int expected, int applied)
        {
            var input = CombatCommandTestData.Input();
            var current = input.Characters.ToDictionary(c => c.EntityId);
            current[input.ActorId] = Stats(current[input.ActorId], defense: defense);
            var events = new List<CombatEvent>();
            foreach (int id in new[] { 2, 3 })
            {
                var target = new EntityId(id);
                current[target] = CombatItemDirectDamageTests.Equip(current[target], 100, 4);
                // 적용 경계가 소지자와 공격자(효과 대상)의 최종 사실을 사전에 직접 반영한다.
                new CombatDamageApplication(new[] { Lantern }, current, events).Apply(target, CombatDamageKind.OrdinaryAttack,
                    new CombatDamageOrigin(CellCoord.Zero, input.ActorId, id), 0);
                Assert.That(current[target].Items.EquippedItemKey, Is.Null);
            }
            Assert.That(CombatEffectiveStatsCalculator.PhysicalDefense(current[input.ActorId]), Is.EqualTo(expected));
            Assert.That(current[input.ActorId].Stats.PhysicalDefense, Is.EqualTo(defense));
            Assert.That(current[input.ActorId].Items.Effects.Count, Is.EqualTo(1));
            Assert.That(events.OfType<CombatItemEvent>().Select(e => e.AppliedMagnitude), Is.EqualTo(new[] { applied, 0 }));
            Assert.That(events.OfType<CombatItemEvent>().Last().Reason, Is.EqualTo(ItemEventReason.DuplicateEffect));
        }

        [Test]
        public void RealAttack_LanternActivatesWhileDeferredRopeStaysInert()
        {
            var input = CombatItemDirectDamageTests.Input(20, 100, null);
            input = CombatItemDirectDamageTests.Items(CombatCommandTestData.With(input, input.Characters.Select(c =>
                c.EntityId == input.ActorId ? CombatItemDirectDamageTests.Equip(Stats(c, defense: 8), 100, 3)
                : c.EntityId.Value == 2 ? CombatItemDirectDamageTests.Equip(c, 100, 4) : c).ToArray()),
                new[] { Lantern, new ItemDefinition(3, new RopeItemSpec(10)) });
            var result = CombatCommandCalculator.Calculate(input, CombatItemDirectDamageTests.NoRules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(CombatItemDirectDamageTests.Final(result, 2).Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(CombatEffectiveStatsCalculator.PhysicalDefense(CombatItemDirectDamageTests.Final(result, 1)), Is.EqualTo(3));
            Assert.That(CombatItemDirectDamageTests.Final(result, 2).Items.Effects, Is.Empty);
            Assert.That(CombatItemDirectDamageTests.Final(result, 1).Items.EquippedItemKey, Is.EqualTo(3));
            Assert.That(result.Events.OfType<CombatItemEvent>().Count(), Is.EqualTo(1));
        }

        internal static CharacterCombatFacts Stats(CharacterCombatFacts c, int? defense = null, int? priority = null)
        {
            var s = c.Stats;
            var stats = new UnitStatsSnapshot(s.MoveMethod,s.MoveRange,s.AppointRange,s.AttackPower,                defense ?? s.PhysicalDefense,                priority ?? s.Priority,s.TraitStatuses.ToArray(),s.Category,s.ArtifactAttackBonus,                s.EffectiveAttackPower,s.SoulAttackBonus,s.SoulMaxHealthBonus,s.PositionAttackBonus);
            return CharacterSettlementCalculator.Copy(c, c.Health, stats);
        }
    }
}
