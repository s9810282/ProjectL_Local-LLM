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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class PullBoundaryRegressionTests
    {
        [Test]
        public void OrdinaryMovement_TwoValidMinesDoNotOverflowPullBookkeeping()
        {
            var input = MineEnvironmentTests.Input(MineEnvironmentTests.Mine(id: 11, ordinal: 1, damage: 1),
                MineEnvironmentTests.Mine(id: 12, ordinal: 2, damage: int.MaxValue));
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == input.ActorId).Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatObjectActivationEvent>().Count(), Is.EqualTo(2));
        }

        [TestCase(Owner.Enemy)]
        [TestCase(Owner.Player)]
        public void DestinationHealthNormalization_IsNotAttackDamage(Owner owner)
        {
            var input = WithModifiers(PullTestData.Input(owner: owner),
                new DestinationTileModifierFact(71, new CellCoord(8, 7), true, new[] { Owner.Player }, 0, -50));
            var preview = CombatCommandCalculator.Preview(input, PullTestData.NoRules, out var result);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(owner == Owner.Enemy ? 30 : 50));
            var target = preview.Targets.ToArray().Single();
            Assert.That(target.HealthNormalizationLoss, Is.EqualTo(50));
            Assert.That(target.ExpectedDamage, Is.EqualTo(owner == Owner.Enemy ? 20 : 0));
            if (owner == Owner.Enemy)
            {
                var hit = result.Attack.Value.Hits.ToArray().Single();
                Assert.That(hit.DamageDealt, Is.EqualTo(20));
                Assert.That(hit.HealthNormalizationLoss, Is.EqualTo(50));
            }
            else Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
        }

        [Test]
        public void DepartureAndArrivalNormalization_AccumulateWithoutOverwriting()
        {
            var input = PullTestData.Input();
            var victim = input.Characters.Single(c => c.EntityId == PullTestData.Target);
            victim = PositionTileModifierCalculator.Replace(victim,
                new PositionTileModifier(victim.Anchor.Value, new long[] { 71 }, 0, 50))
                .WithHealth(new HealthStateSnapshot(150, 150));
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == victim.EntityId ? victim : c).ToArray());
            input = WithModifiers(input,
                new DestinationTileModifierFact(71, new CellCoord(9, 7), true, new[] { Owner.Player }, 0, 50),
                new DestinationTileModifierFact(72, new CellCoord(8, 7), true, new[] { Owner.Player }, 0, -50));
            var preview = CombatCommandCalculator.Preview(input, PullTestData.NoRules, out var result);
            var hit = result.Attack.Value.Hits.ToArray().Single();
            Assert.That(hit.DamageDealt, Is.EqualTo(20));
            Assert.That(hit.HealthNormalizationLoss, Is.EqualTo(100));
            Assert.That(hit.TargetHealthAfter, Is.EqualTo(30));
            Assert.That(preview.Targets.ToArray().Single().HealthNormalizationLoss, Is.EqualTo(100));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PreHitObjectRecovery_IsRecordedBeforeDamageAndNormalization(bool normalize)
        {
            var input = PullTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == PullTestData.Target
                ? CombatCommandTestData.With(c, hp: normalize ? 20 : 50) : c).ToArray());
            input = CliffFallTests.WithEnvironment(input, new HealingObject(71, new CellCoord(8, 7), normalize ? 80 : 40));
            if (normalize)
                input = WithModifiers(input, new DestinationTileModifierFact(72, new CellCoord(8, 7), true, new[] { Owner.Player }, 0, -50));
            var rules = new IMovementTileRule[] { new HealingRule() };
            var result = PullTestData.Run(input, rules);
            var preview = CombatCommandCalculator.Preview(input, rules);
            var hit = result.Attack.Value.Hits.ToArray().Single();
            Assert.That(hit.HealthRecovery, Is.EqualTo(normalize ? 80 : 40));
            Assert.That(hit.HealthNormalizationLoss, Is.EqualTo(normalize ? 50 : 0));
            Assert.That(hit.DamageDealt, Is.EqualTo(20));
            Assert.That(hit.TargetHealthAfter, Is.EqualTo(normalize ? 30 : 70));
            Assert.That(preview.Targets.ToArray().Single().ExpectedDamage, Is.EqualTo(20));
            Assert.That(preview.Targets.ToArray().Single().TargetHealthAfter, Is.EqualTo(hit.TargetHealthAfter));
        }

        [Test]
        public void PullWithPiercing_IsRejectedBeforeFriendlySelectionCanBecomeDamage()
        {
            var definition = UnitSpecTestFactory.Create(EntityKey.Unit_Akimbo, 1, 1, new[] { CellCoord.Zero }, 100,
                MoveMethod.LiftAndPlace, 3, 20, 100, new TraitSpec[] { new PullTraitSpec(1), new PiercingFalloffTraitSpec(30, 10) });
            Assert.Throws<ArgumentException>(() => new CombatAttackProfile(definition,
                new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0)));
        }


        [TestCase(1, false)]
        [TestCase(2, true)]
        public void ExistingTargetPolicy_RejectsIllegalLandingBeforeMovement(int range, bool accepted)
        {
            var input = PullTestData.Input(range: range, distance: range + 1);
            var actor = input.Characters[0];
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == actor.EntityId
                    ? CombatCommandTestData.With(c, category: CategorySnapshot.CreateStateless(CategoryKey.Cannon)) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                    ? new CombatAttackProfile(UnitSpecTestFactory.Create(actor.CharacterKey, 1, 1, new[] { CellCoord.Zero },
                        100, MoveMethod.LiftAndPlace, range, 20, 100, new TraitSpec[] { new PullTraitSpec(1) },
                        categoryKey: CategoryKey.Cannon), p.Method) : p).ToArray());
            Assert.That(CombatCommandCalculator.GetAttackRange(input).AttackAims.ToArray().Contains(input.Aim), Is.EqualTo(accepted));
            var result = CombatCommandCalculator.Calculate(input, PullTestData.NoRules);
            Assert.That(result.Accepted, Is.EqualTo(accepted));
            if (!accepted)
            {
                Assert.That(result.Reason, Is.EqualTo("AppointCellRejected"));
                Assert.That(result.Events, Is.Empty);
                Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(new CellCoord(9, 7)));
            }
            else Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(new CellCoord(9, 7)));
        }

        private static CombatCommandInput WithModifiers(CombatCommandInput input, params DestinationTileModifierFact[] modifiers)
        {
            var environment = new MovementEnvironmentFacts(input.State.Environment.Tiles.Concat(modifiers).ToArray());
            return CombatCommandTestData.With(input, state: input.State.With(environment: environment,
                environmentProgress: EnvironmentProgressFacts.Initial(environment)));
        }

        // 기존 Object 확장 계약의 회복을 검증합니다. 운영 콘텐츠를 추가하는 규칙이 아닙니다.
        private sealed class HealingObject : ObjectSourceFact
        {
            internal const string HealingKey = "test-pull-healing";
            internal int Amount { get; }
            internal HealingObject(long id, CellCoord cell, int amount, bool active = true)
                : base(id, cell, HealingKey, 1, active, new[] { Owner.Player, Owner.Enemy }) => Amount = amount;
            public override bool SameValues(MovementTileFact other) => other is HealingObject value && SameObjectValues(value) && Amount == value.Amount;
            public override ObjectSourceFact WithActive(bool active) => new HealingObject(Id, Cell, Amount, active);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => this;
        }

        private sealed class HealingRule : IMovementTileRule
        {
            public string Key => HealingObject.HealingKey;
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
            {
                var source = (HealingObject)tile;
                return new MovementStepEffects(healing: new[] { new MovementHealthRequest(input.MoverId, source.Amount) },
                    expectedSource: source, finalSource: source.WithActive(false));
            }
        }
    }
}
