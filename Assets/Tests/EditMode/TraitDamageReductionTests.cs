using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Tests.Core
{
    public sealed class TraitDamageReductionTests
    {
        [TestCase(100, 40)]
        [TestCase(93, 37)]
        public void ReceivedPercentages_AreSeparateAndMultiply(int incoming, int expected)
        {
            var rules = new[] { 50, 20 }.Select((percent, index) => new EffectRuleBinding(
                new EffectOriginKey(EffectOwnerKind.Trait, index + 1, (int)TraitKey.Entrenchment, "fixture-percent"),
                new PercentRule(percent), StatelessEffectStateSnapshot.Instance)).ToArray();
            var result = new CombatEffectEngine().EvaluateIncomingDamage(rules, new IncomingDamageEffectContext(incoming));
            Assert.That(result.FinalDamage, Is.EqualTo(expected));
            Assert.That(result.EffectiveReduction, Is.Zero);
        }

        [Test]
        public void ExistingItemAndTerrainHooks_ExecuteOnce()
        {
            var input = CombatItemDirectDamageTests.Input(100, 60, 1);
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[]
            {
                CombatCommandTestData.Character(6, EntityKey.Unit_Akimbo, new CellCoord(3, 4), Owner.Enemy)
            }).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new EntrenchmentTraitSpec(1, 50));
            var terrain = new StaticTerrainFact(71, new CellCoord(4, 3), StaticTerrainKind.Sandstorm, true, new[] { Owner.Player, Owner.Enemy });
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { terrain });
            input = input.WithWorld(input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment)),
                input.Characters, input.Main, input.SoulCounts, input.Souls, new EnvironmentSemanticBindings(new[] { 71L }, Array.Empty<long>(), Array.Empty<EntityId>()));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(30));
            Assert.That(result.Events.OfType<CombatItemEvent>().Count(e => e.Stage == ItemEventStage.AfterDamage), Is.EqualTo(1));
        }
        [Test]
        public void Entrenchment_ExcludesCenter()
        {
            var input = Input();
            var attack = Enemy(input, new EntityId(1));
            var result = TraitCombatFixtures.Attack(attack);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 1).Health.IsDead, Is.True);
        }

        [TestCase(2, 4, 56)]
        [TestCase(3, 4, 0)]
        public void Entrenchment_RankTwoCoversDiagonalWithinOneCell(int x, int y, int remainingHp)
        {
            var input = TraitCombatFixtures.Equip(Input(), EntityKey.Unit_Akimbo, new EntrenchmentTraitSpec(1, 56));
            var cell = new CellCoord(x, y);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 5
                ? c.WithPlacement(CharacterRuntimeArea.Field, null, cell, Rotation4.R0, new[] { cell }) : c).ToArray());
            Assert.That(Receive(input, CombatDamageKind.EnvironmentDamage).Health.CurrentHealth, Is.EqualTo(remainingHp));
        }

        [TestCase(CombatDamageKind.OrdinaryAttack)]
        [TestCase(CombatDamageKind.EnvironmentDamage)]
        [TestCase(CombatDamageKind.ReflectedDamage)]
        public void Entrenchment_ReducesAttackEnvironmentAndReflection(CombatDamageKind kind)
        {
            var input = Input();
            Assert.That(Receive(input, kind).Health.CurrentHealth, Is.EqualTo(50));
        }

        [Test]
        public void MultipleAuras_Multiply()
        {
            Assert.That(Receive(Input(2), CombatDamageKind.OrdinaryAttack).Health.CurrentHealth, Is.EqualTo(75));
            Assert.That(Receive(Input(2), CombatDamageKind.EnvironmentDamage).Health.CurrentHealth, Is.EqualTo(75));
        }

        [Test]
        public void SourceLeavesField_StopsReductionImmediately()
        {
            var input = Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 1
                ? c.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>()) : c).ToArray());
            Assert.That(Receive(input, CombatDamageKind.EnvironmentDamage).Health.IsDead, Is.True);
        }

        private static CharacterCombatFacts Receive(CombatCommandInput input, CombatDamageKind kind)
        {
            if (kind == CombatDamageKind.OrdinaryAttack)
            {
                var result = TraitCombatFixtures.Attack(Enemy(input, new EntityId(5)));
                Assert.That(result.Accepted, Is.True, result.Reason);
                return TraitCombatFixtures.Final(result, 5);
            }
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            CombatDamageApplication.For(input, characters, new List<CombatEvent>()).Apply(new EntityId(5), kind,
                new CombatDamageOrigin(new CellCoord(4, 3), new EntityId(2), 1), 100);
            return characters[new EntityId(5)];
        }

        internal static CombatCommandInput Input(int sourceCount = 1)
        {
            var input = CombatCommandTestData.Input(empty: true);
            var characters = input.Characters.Concat(new[]
            {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy, 100),
                CombatCommandTestData.Character(5, EntityKey.Unit_Shielder, new CellCoord(2, 3), Owner.Player)
            }).ToList();
            if (sourceCount == 2) characters.Add(CombatCommandTestData.Character(6, EntityKey.Unit_Akimbo, new CellCoord(2, 2), Owner.Player, 20));
            input = TraitCombatFixtures.Characters(input, characters.ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Concat(new[]
            {
                new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Shielder, 1),
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))
            }).ToArray());
            return TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new EntrenchmentTraitSpec(1, 50));
        }

        private static CombatCommandInput Enemy(CombatCommandInput input, EntityId victim) =>
            EnemyControllerPhaseTests.Input(input, CombatControllerPhase.Enemy).WithActor(new EntityId(2))
                .WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(input.Characters.Single(c => c.EntityId == victim).Anchor.Value), default, null, null);

        private sealed class PercentRule : IEffectIncomingDamageRule
        {
            private readonly int _percent;
            internal PercentRule(int percent)
            {
                _percent = percent;
            }

            public EffectIncomingDamageContribution EvaluateIncomingDamage(in EffectRuleInput input, in IncomingDamageEffectContext context)
            {
                var constructor = typeof(EffectIncomingDamageContribution).GetConstructor(new[] { typeof(EffectOriginKey), typeof(int), typeof(int) });
                Assert.That(constructor, Is.Not.Null);
                return (EffectIncomingDamageContribution)constructor.Invoke(new object[] { input.Origin, 0, 100 - _percent });
            }
        }
    }
}
