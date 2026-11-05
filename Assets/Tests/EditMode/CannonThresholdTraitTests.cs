using System;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Tests.Core
{
    public sealed class CannonThresholdTraitTests
    {
        [Test]
        public void ThreeFactors_SacrificeUsesExactScale()
        {
            var input = SacrificeTraitTests.Input();
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new StationaryFireTraitSpec(100),
                new TrackingShotTraitSpec(0, 100), new FocusedBombardmentTraitSpec(1, 50));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 10)) : c.EntityId.Value == 5
                ? c.With(health: new HealthStateSnapshot(1000, 1000), stats: c.Stats.With(attackPower: 1, physicalDefense: 0))
                : c.With(stats: c.Stats.With(attackPower: 1))).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(5));
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(940));
        }

        [TestCase(10, 60)]
        [TestCase(3, 18)]
        public void ThreeSurvivingFactors_PreserveExactScale(int attack, int damage)
        {
            var input = ThreeFactors(attack);
            var before = input.State;
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.ToArray().Single().TargetHealthAfter, Is.EqualTo(1000 - damage));
            Assert.That(input.State.SameValues(before), Is.True);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(damage));
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(1000 - damage));
        }

        internal static CombatCommandInput ThreeFactors(int attack = 10)
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo,
                new StationaryFireTraitSpec(100), new TrackingShotTraitSpec(50, 100),
                new FocusedBombardmentTraitSpec(1, 50));
            return CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: attack)) : c.EntityId.Value == 2
                ? c.With(health: new HealthStateSnapshot(1000, 1000), stats: c.Stats.With(attackPower: attack == 10 ? 5 : 1, physicalDefense: 0)) : c).ToArray());
        }

        [TestCase(19, true)]
        [TestCase(20, false)]
        [TestCase(30, false)]
        public void Finishing_UsesPreStrikeHpAndStrictThreshold(int health, bool instant)
        {
            var input = Finishing(health);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(instant ? 0 : health - 15));
            var hit = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Final.EntityId.Value == 2 &&
                (e.Kind == CombatEventKind.Died || e.Kind == CombatEventKind.HealthChanged));
            Assert.That(Cause(hit), Is.EqualTo(instant ? CombatDamageCause.InstantDefeat : CombatDamageCause.NumericDamage));
        }

        [Test]
        public void LaterStrike_RechecksHp()
        {
            var first = TraitCombatFixtures.Attack(Finishing(30));
            Assert.That(TraitCombatFixtures.Final(first, 2).Health.CurrentHealth, Is.EqualTo(15));
            var second = TraitCombatFixtures.Attack(CombatCommandTestData.NextPlayerTurn(MovementTestData.After(first)));
            Assert.That(TraitCombatFixtures.Final(second, 2).Health.IsDead, Is.True);
            Assert.That(Cause(second.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Died)), Is.EqualTo(CombatDamageCause.InstantDefeat));
        }

        [Test]
        public void Finishing_BypassesShieldWithoutSacrifice()
        {
            var input = SacrificeTraitTests.Input();
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new FinishingShotTraitSpec(20));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, new SacrificeTraitSpec(2, 1), new CohesionTraitSpec(0, 100, 1, 1));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 100)) : c.EntityId.Value == 2
                ? c.With(health: new HealthStateSnapshot(15, 100)) : c).ToArray());
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            TraitPresenceReconciler.Apply(input, characters);
            input = CombatCommandTestData.With(input, characters: characters.Values.ToArray());
            Assert.That(characters[new EntityId(2)].TraitEffects.ActiveShieldTotal, Is.EqualTo(100));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 5).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).Any(t => t.TraitKey == TraitKey.Sacrifice), Is.False);
            var death = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Died && e.Final.EntityId.Value == 2);
            Assert.That(Cause(death), Is.EqualTo(CombatDamageCause.InstantDefeat));
            Assert.That(death.Final.TraitEffects.ActiveShieldTotal, Is.EqualTo(100));
        }

        [TestCase(50, 20)]
        [TestCase(49, 10)]
        public void Tracking_UsesInclusiveThreshold(int health, int damage)
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo, new TrackingShotTraitSpec(50, 100));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 10)) : c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(health, 100)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(health - damage));
        }

        [Test]
        public void Tracking_RechecksEachIndependentStrike()
        {
            var input = Line(new TrackingShotTraitSpec(50, 100), new KillChainAttackTraitSpec());
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 10)) : c.EntityId.Value == 3
                ? c.WithHealth(new HealthStateSnapshot(50, 100)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(20));
        }

        [Test]
        public void Focused_SingleTargetKillChain_KeepsBonus()
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo,
                new FocusedBombardmentTraitSpec(1, 50), new KillChainAttackTraitSpec());
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(1, 100)) : c).Append(
                    CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(7, 0), Owner.Enemy)).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(70));
        }

        [Test]
        public void Focused_KillChainRecountsItsOwnSingleTarget()
        {
            // 두 적을 맞힌 공격은 적 2명이라 일점 포격이 없고(3: 100 → 80), 한 명이 죽어 생긴 추가 공격은
            // 대상 1명으로 다시 세어 일점 포격이 붙는다(20 × 1.5 = 30, 80 → 50).
            var input = Line(new FocusedBombardmentTraitSpec(1, 50), new KillChainAttackTraitSpec());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(50));
        }

        [Test]
        public void Focused_IndependentActionRecapturesRemainingEnemyCount()
        {
            var input = Line(new FocusedBombardmentTraitSpec(1, 50));
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(5, EntityKey.Unit_Akimbo, new CellCoord(3, 3), Owner.Player, 20)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new FocusedBombardmentTraitSpec(1, 50));
            var first = TraitCombatFixtures.Attack(input);
            Assert.That(first.Accepted, Is.True, first.Reason);
            Assert.That(TraitCombatFixtures.Final(first, 3).Health.CurrentHealth, Is.EqualTo(80));
            var result = TraitCombatFixtures.Attack(MovementTestData.After(first).WithActor(new EntityId(5)));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(50));
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(2));
        }

        [Test]
        public void DamageFactors_Multiply()
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo,
                new StationaryFireTraitSpec(100), new TrackingShotTraitSpec(50, 50), new FocusedBombardmentTraitSpec(1, 50));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(10));
        }

        [Test]
        public void Finishing_HostileStructureUsesSameDefeatCause()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(empty: true), EntityKey.Unit_Akimbo, new FinishingShotTraitSpec(20));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 100)) : c).ToArray(), state: input.State.With(structures: new[] {
                    new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                        new[] { CellCoord.Zero }, 1000, false, 0), new HealthStateSnapshot(15, 1000), new CellCoord(4, 3), Rotation4.R0,
                        new[] { new CellCoord(4, 3) }, Owner.Enemy) }));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.True);
            Assert.That(Cause(result.Events.OfType<CombatStructureEvent>().Single()), Is.EqualTo(CombatDamageCause.InstantDefeat));
        }

        [Test]
        public void Preview_ProjectsDefeatWithoutChangingCommittedShieldOrHealth()
        {
            var input = Finishing(15);
            var before = input.Characters.ToArray();
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.ToArray().Single().TargetHealthAfter, Is.Zero);
            Assert.That(input.Characters.Select((c, i) => c.SameValues(before[i])).All(same => same), Is.True);
        }

        [Test]
        public void RepeatedDeliveredHits_RecheckCurrentHpWithinOneAction()
        {
            var (plan, world, ledger) = CombatItemDirectDamageTests.Prepare(Finishing(30), duplicate: true);
            AttackDirectDamageCalculator.Apply(plan, world, ledger, new System.Collections.Generic.HashSet<EntityId>());
            Assert.That(world.Characters[new EntityId(2)].Health.IsDead, Is.True);
            Assert.That(world.Events.OfType<CombatCharacterEvent>().Where(e => e.Final.EntityId.Value == 2)
                .Select(e => e.Final.Health.CurrentHealth), Is.EqualTo(new[] { 15, 0 }));
            Assert.That(Cause(world.Events.OfType<CombatCharacterEvent>().Last()), Is.EqualTo(CombatDamageCause.InstantDefeat));
        }

        [Test]
        public void Defeat_SkipsDamageItemsAndDiscardsEquipmentOnce()
        {
            var input = TraitCombatFixtures.Equip(CombatItemDirectDamageTests.Input(100, 15, 1), EntityKey.Unit_Akimbo,
                new FinishingShotTraitSpec(20), new CallOfBloodTraitSpec(10));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.WithHealth(new HealthStateSnapshot(50, 100)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 1).Health.CurrentHealth, Is.EqualTo(60));
            var item = result.Events.OfType<CombatItemEvent>().Single();
            Assert.That(item.Stage, Is.EqualTo(ItemEventStage.Lifecycle));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(1));
        }

        [Test]
        public void MixedArea_InstantVictimSkipsSacrificeButNormalLethalVictimStillUsesIt()
        {
            var input = TraitCombatFixtures.Equip(SacrificeTraitTests.Input(splash: true), EntityKey.Unit_Akimbo, new FinishingShotTraitSpec(20));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 100)) : c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(15, 100)) : c.EntityId.Value == 3
                ? c.WithHealth(new HealthStateSnapshot(25, 100)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(25));
            Assert.That(result.Attack.Value.TraitActivations.ToArray()
                .Count(t => t.TraitKey == TraitKey.Sacrifice && t.Interception != null), Is.EqualTo(1));
        }

        private static CombatDamageCause Cause(object hit)
        {
            var property = hit.GetType().GetProperty("DamageCause");
            Assert.That(property, Is.Not.Null);
            return (CombatDamageCause)property.GetValue(hit);
        }
        private static CombatCommandInput Finishing(int health)
        {
            var input = TraitCombatFixtures.Equip(TraitMovementAttackTests.SingleTarget(), EntityKey.Unit_Akimbo, new FinishingShotTraitSpec(20));
            return CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.With(stats: c.Stats.With(attackPower: 100)) : c.EntityId.Value == 2
                ? c.With(health: new HealthStateSnapshot(health, 100), stats: c.Stats.With(physicalDefense: 85)) : c).ToArray());
        }
        private static CombatCommandInput Line(params TraitSpec[] traits)
        {
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy).WithHealth(new HealthStateSnapshot(1, 100))).Append(
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy)).ToArray());
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, traits);
        }
    }
}
