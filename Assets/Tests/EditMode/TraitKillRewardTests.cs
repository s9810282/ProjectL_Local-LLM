using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class TraitKillRewardTests
    {
        [TestCase(CombatDamageKind.ExtraAttack)]
        [TestCase(CombatDamageKind.ReflectedDamage)]
        public void AdditionalAndReflectionKills_AreCredited(CombatDamageKind kind)
        {
            var input = Input().WithActor(new EntityId(10));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var damage = CombatDamageApplication.For(input, characters, new List<CombatEvent>());
            damage.Apply(new EntityId(2), kind, new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 1), 1);
            Assert.That(characters[new EntityId(1)].Health.CurrentHealth, Is.EqualTo(60));
            Assert.That(characters[new EntityId(1)].Stats.TraitAttackBonus, Is.EqualTo(3));
            Assert.That(characters[new EntityId(10)].Stats.TraitAttackBonus, Is.EqualTo(3));
        }

        [Test]
        public void DuplicateDeath_IsNotAwardedTwice()
        {
            var input = Input();
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var damage = CombatDamageApplication.For(input, characters, events);
            var origin = new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 1);
            damage.Apply(new EntityId(2), CombatDamageKind.ExtraAttack, origin, 1);
            damage.Apply(new EntityId(2), CombatDamageKind.ExtraAttack, origin, 1);
            Assert.That(characters[new EntityId(1)].Health.CurrentHealth, Is.EqualTo(60));
            Assert.That(characters[new EntityId(1)].TraitEffects.AttackGrants.Count, Is.EqualTo(1));
            Assert.That(events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ThornHeartEnemyAdvance_AwardsActualReflectorExactlyOnce(bool lethalOwnerHit)
        {
            var input = TraitCombatFixtures.Equip(ThornHeartTestData.Input(hp: 2), EntityKey.Unit_Demon,
                new CallOfBloodTraitSpec(10), new MoraleBoostTraitSpec(20, 1));
            if (lethalOwnerHit) input = ThornHeartTestData.Replace(input, 2, c => c.WithHealth(new HealthStateSnapshot(1, 100)));
            var hq = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100), new CellCoord(7, 0), Rotation4.R0, new[] { new CellCoord(7, 0) });
            input = CombatCommandTestData.With(input, state: input.State.With(structures: new[] { hq }));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 1).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(lethalOwnerHit ? 0 : 90));
            Assert.That(TraitCombatFixtures.Final(result, 2).Stats.TraitAttackBonus, Is.EqualTo(lethalOwnerHit ? 0 : 4));
            Assert.That(TraitCombatFixtures.Final(result, 3).Stats.TraitAttackBonus, Is.EqualTo(4));
            var main = TraitCombatFixtures.Final(result, 10);
            Assert.That(main.TraitEffects.AttackGrants.Count, Is.EqualTo(1));
            Assert.That(main.Stats.TraitAttackBonus, Is.EqualTo(4));
            Assert.That(main.TraitEffects.AttackGrants.Single().Origin.OwnerInstanceId, Is.EqualTo(2));
            var reflection = result.Events.OfType<CombatArtifactEvent>().Single(e => e.Reflection != null).Reflection;
            Assert.That(reflection.StruckAllyId, Is.EqualTo(new EntityId(2)));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died && e.Final.EntityId.Value == 1), Is.EqualTo(1));
        }

        [Test]
        public void Morale_UsesBaseAttack()
        {
            var input = Input();
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var victim = characters[new EntityId(2)];
            characters[victim.EntityId] = victim.With(stats: victim.Stats.With(artifactAttackBonus: 100));
            var damage = CombatDamageApplication.For(input, characters, new List<CombatEvent>());
            damage.Apply(victim.EntityId, CombatDamageKind.OrdinaryAttack, new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 1), 1);
            Assert.That(victim.Stats.AttackPower, Is.EqualTo(13));
            Assert.That(characters[new EntityId(1)].Stats.TraitAttackBonus, Is.EqualTo(3));
            Assert.That(characters[new EntityId(1)].Stats.AttackPower, Is.EqualTo(20));
        }

        [Test]
        public void PerVictimRounding_PrecedesSum()
        {
            var input = Input();
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var victim = characters[new EntityId(3)];
            characters[victim.EntityId] = victim.With(health: new HealthStateSnapshot(1, 100), stats: victim.Stats.With(attackPower: 13));
            var damage = CombatDamageApplication.For(input, characters, new List<CombatEvent>());
            var origin = new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 1);
            damage.Apply(new EntityId(2), CombatDamageKind.OrdinaryAttack, origin, 1);
            damage.Apply(new EntityId(3), CombatDamageKind.ExtraAttack, origin, 1);
            Assert.That(characters[new EntityId(1)].Stats.TraitAttackBonus, Is.EqualTo(6));
            Assert.That(characters[new EntityId(10)].Stats.TraitAttackBonus, Is.EqualTo(6));
            Assert.That(characters[new EntityId(1)].Health.CurrentHealth, Is.EqualTo(70));
        }

        [Test]
        public void DeadOrHandAllies_ReceiveNoGrant()
        {
            var input = Input();
            var hand = input.Characters.Single(c => c.EntityId.Value == 3);
            var dead = input.Characters.Single(c => c.EntityId.Value == 4);
            hand = CombatCommandTestData.Character(5, EntityKey.Unit_Akimbo, CellCoord.Zero, Owner.Player)
                .WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>());
            dead = CombatCommandTestData.Character(6, EntityKey.Unit_Akimbo, CellCoord.Zero, Owner.Player)
                .WithHealth(new HealthStateSnapshot(0, 100)).WithPlacement(CharacterRuntimeArea.Excluded, null, null, null, Array.Empty<CellCoord>());
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] { hand, dead }).ToArray());
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var damage = CombatDamageApplication.For(input, characters, new List<CombatEvent>());
            damage.Apply(new EntityId(2), CombatDamageKind.ExtraAttack, new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 1), 1);
            Assert.That(characters[hand.EntityId].TraitEffects.AttackGrants, Is.Empty);
            Assert.That(characters[dead.EntityId].TraitEffects.AttackGrants, Is.Empty);
            Assert.That(characters[new EntityId(10)].Stats.TraitAttackBonus, Is.EqualTo(3));
        }

        [Test]
        public void DeadOwner_IsNotResurrected()
        {
            var input = Input();
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            characters[new EntityId(1)] = characters[new EntityId(1)].WithHealth(new HealthStateSnapshot(0, 100));
            var damage = CombatDamageApplication.For(input, characters, new List<CombatEvent>());
            damage.Apply(new EntityId(2), CombatDamageKind.ReflectedDamage, new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 1), 1);
            Assert.That(characters[new EntityId(1)].Health.CurrentHealth, Is.Zero);
            Assert.That(characters[new EntityId(1)].TraitEffects.AttackGrants, Is.Empty);
        }

        [Test]
        public void Morale_KillChainLaterStrikeUsesProjectedAttack()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, empty: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy, 13).WithHealth(new HealthStateSnapshot(1, 100)),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 4), Owner.Enemy, 13).WithHealth(new HealthStateSnapshot(22, 100)) }).ToArray());
            var hq = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100), new CellCoord(7, 0), Rotation4.R0, new[] { new CellCoord(7, 0) });
            input = CombatCommandTestData.With(input, state: input.State.With(structures: new[] { hq }));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new KillChainAttackTraitSpec(), new MoraleBoostTraitSpec(20, 1));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.IsDead, Is.True,
                "The later strike must use 20 + the first kill's rounded 3 ATK.");
            // 처치 연쇄 추가 공격도 공격 행동으로 센다(2026-11-02 결정).
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount,
                Is.EqualTo(1 + TraitActivationOutcomeTests.Activations(result.Events, TraitKey.KillChainAttack).Count()));
            Assert.That(TraitCombatFixtures.Final(result, 10).Stats.TraitAttackBonus, Is.EqualTo(6));
        }

        [Test]
        public void Healing_RoundsEachVictimBeforeSumming()
        {
            var input = Input();
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            foreach (int id in new[] { 2, 3 }) characters[new EntityId(id)] = characters[new EntityId(id)].WithHealth(new HealthStateSnapshot(1, 13));
            var damage = CombatDamageApplication.For(input, characters, new List<CombatEvent>());
            var origin = new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 1);
            damage.Apply(new EntityId(2), CombatDamageKind.OrdinaryAttack, origin, 1);
            damage.Apply(new EntityId(3), CombatDamageKind.ExtraAttack, origin, 1);
            Assert.That(characters[new EntityId(1)].Health.CurrentHealth, Is.EqualTo(52));
        }

        [Test]
        public void AreaKills_HealAndGrantAttackWithSeparateRoundingForEachVictim()
        {
            var input = TraitCombatFixtures.Equip(Input(), EntityKey.Unit_Akimbo,
                new CallOfBloodTraitSpec(13), new MoraleBoostTraitSpec(30, 1));
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 2 || c.EntityId.Value == 3
                ? c.With(health: new HealthStateSnapshot(1, 50), stats: c.Stats.With(attackPower: 5)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(2));
            Assert.That(TraitCombatFixtures.Final(result, 1).Health.CurrentHealth, Is.EqualTo(64));
            Assert.That(TraitCombatFixtures.Final(result, 10).Stats.TraitAttackBonus, Is.EqualTo(4));
        }

        private static CombatCommandInput Input()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo,
                new CallOfBloodTraitSpec(10), new MoraleBoostTraitSpec(20, 1));
            return TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 1
                ? c.WithHealth(new HealthStateSnapshot(50, 100)) : c.EntityId.Value == 2
                ? c.With(health: new HealthStateSnapshot(1, 100), stats: c.Stats.With(attackPower: 13)) : c).ToArray());
        }
    }
}
