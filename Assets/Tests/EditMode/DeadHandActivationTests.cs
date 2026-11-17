using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Tests.Core
{
    public sealed class DeadHandActivationTests
    {
        [TestCase(TargetingChoiceMode.Single, 1)]
        [TestCase(TargetingChoiceMode.Direction, 2)]
        [TestCase(TargetingChoiceMode.All, 2)]
        public void PreparationCapturesOnlyActualEnemiesWithoutMutatingInput(TargetingChoiceMode mode, int count)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, mode, TargetingSplash.None, 0);
            var aim = mode == TargetingChoiceMode.Single ? AttackAim.ForCell(new CellCoord(3, 2))
                : mode == TargetingChoiceMode.Direction ? AttackAim.ForDirection(AttackDirection.Up) : AttackAim.ForAll();
            var input = DeadHandTestData.Input(new[] { new CellCoord(3, 2), new CellCoord(2, 2) }, active: true, method: method, aim: aim);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(7, EntityKey.Unit_Demon, new CellCoord(4, 2), Owner.Player)).ToArray());
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var before = input.Characters.ToArray();
            var selection = CombatAttackQueries.Selection(input, actor, CombatCommandCalculator.RequiredProfile(input, actor));
            Assert.That(selection.FailureReason, Is.EqualTo(AttackFailureReason.None));
            Assert.That(selection.Sweep.TargetIds.Count, Is.EqualTo(count));
            Assert.That(selection.Sweep.TargetIds, Has.No.Member(new EntityId(7)));
            Assert.That(input.Characters, Is.EqualTo(before));
            Assert.That(actor.ActionUse, Is.EqualTo(UnitActionUse.Available));
        }

        [Test]
        public void SweepIgnoresLineOfSightButMovementStillStopsAtWall()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(3, 3) }, active: true);
            input = StaticTerrainIntegrationTests.Terrain(input,
                new StaticTerrainFact(71, new CellCoord(3, 2), StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy }),
                new StaticTerrainFact(73, new CellCoord(4, 2), StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy }),
                new StaticTerrainFact(72, new CellCoord(4, 3), StaticTerrainKind.Wall, true, new[] { Owner.Player, Owner.Enemy }));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Sweep.Targets.Single().EnteredCells, Is.Empty);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(6));
            Assert.That(result.Attack.Value.BlockedContacts.Select(c => c.Contact), Has.No.Member(new CellCoord(3, 3)));
            Assert.That(result.Attack.Value.BlockedContacts.Select(c => c.Contact),
                Is.EquivalentTo(new[] { new CellCoord(3, 2), new CellCoord(4, 2), new CellCoord(4, 3) }));
        }

        [Test]
        public void ReactionExecutionAndPreviewDoNotSweepOrScale()
        {
            var input = DeadHandTestData.Input(active: true, percent: 200);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var result = CombatAttackCalculator.Reaction(input, Array.Empty<IMovementTileRule>(), actor, input.Aim);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(94));
            Assert.That(result.Events.OfType<CombatSweepEvent>(), Is.Empty);
            using var preview = new CombatAttackCalculation(input, Array.Empty<IMovementTileRule>(), true, actor, input.Aim);
            while (!preview.IsComplete) preview.Step();
            Assert.That(preview.Preview.Sweep, Is.Null);
            Assert.That(preview.Preview.Targets.ToArray().Single().ExpectedDamage, Is.EqualTo(6));
        }

        [Test]
        public void EmptyDirectionDoesNotConsumeAttack()
        {
            var input = DeadHandTestData.Input(Array.Empty<CellCoord>(), active: true);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Input.Characters.Single(c => c.EntityId == input.ActorId).ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(result.Events.OfType<CombatSweepEvent>(), Is.Empty);
        }

        [TestCase(CombatDamageKind.OrdinaryAttack, true)]
        [TestCase(CombatDamageKind.ExtraAttack, false)]
        [TestCase(CombatDamageKind.ReactionAttack, false)]
        [TestCase(CombatDamageKind.EnvironmentDamage, false)]
        public void ContributionUsesActualKindAndPreservesOriginAndState(CombatDamageKind kind, bool active)
        {
            var origin = TraitEffectOrigin.Create(new EntityId(1), TraitKey.DeadHand);
            var state = new DeadHandEffectStateSnapshot();
            var binding = new EffectRuleBinding(origin, new DeadHandEffectRule(new DeadHandTraitDefinition(150)), state);
            var result = new CombatEffectEngine().EvaluateAttackSweep(new[] { binding }, new AttackSweepEffectContext(kind));
            Assert.That(result.Contribution.HasValue, Is.EqualTo(active));
            Assert.That(result.Expectations.Count, Is.EqualTo(active ? 1 : 0));
            if (!active) return;
            Assert.That(result.Contribution.Value.Origin, Is.EqualTo(origin));
            Assert.That(result.Contribution.Value.DamagePercent, Is.EqualTo(150));
            Assert.That(result.Expectations[0].ExpectedState, Is.SameAs(state));
        }

        [Test]
        public void InvalidBoundOriginIsRejectedEvenWhenKindIsInactive()
        {
            var binding = new EffectRuleBinding(TraitEffectOrigin.Create(new EntityId(1), TraitKey.Knockback),
                new DeadHandEffectRule(new DeadHandTraitDefinition(100)), new DeadHandEffectStateSnapshot());
            Assert.Throws<ArgumentException>(() => new CombatEffectEngine().EvaluateAttackSweep(new[] { binding },
                new AttackSweepEffectContext(CombatDamageKind.ExtraAttack)));
        }

        [Test]
        public void DefaultContextIsRejectedAtEvaluationBoundary()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatEffectEngine()
                .EvaluateAttackSweep(Array.Empty<EffectRuleBinding>(), default));
        }
    }
}
