using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class CategoryEffectRetirementTests
    {
        private static readonly IMovementTileRule[] Rules = Array.Empty<IMovementTileRule>();

        [Test]
        public void DirectKill_DoesNotProduceAdditionalFrenzyAttack()
        {
            var input = CombatCommandTestData.Input(
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0),
                AttackAim.ForCell(new CellCoord(4, 3)), category: CategoryKey.Rider);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == new EntityId(2)
                ? CombatCommandTestData.With(c, hp: 5) : c).ToArray());
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatAttackEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Attack.Value.Hits.Length, Is.EqualTo(1));
            var final = CombatCommandComposition.Project(result);
            Assert.That(final.Characters.Single(c => c.EntityId == new EntityId(2)).Health.IsDead, Is.True);
            Assert.That(final.Characters.Single(c => c.EntityId == new EntityId(3)).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(final.Characters.Single(c => c.EntityId == new EntityId(4)).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [Test]
        public void HeavyWithoutOffsets_ConstructsDefaultCatalog()
        {
            var spec = new UnitSpec(EntityKey.Unit_ShotGun, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero },
                40, MoveMethod.LiftAndPlace, 2, CategoryKey.Heavy, 6, 5, 30, Array.Empty<TraitSpec>());
            var definition = new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault()).GetRequired(spec.Key);
            Assert.That(definition.Category.Key, Is.EqualTo(CategoryKey.Heavy));
            Assert.That(definition.Category.RuleDefinitions.Length, Is.Zero);
            Assert.That(definition.PhysicalDefense, Is.EqualTo(5));
            Assert.That(definition.MoveRange, Is.EqualTo(3));
            var enemy = BattleCharacterCalculator.CreateEnemy(new EntityId(91), definition, 1,
                new CellCoord(4, 3), Rotation4.R0, new[] { new CellCoord(4, 3) }, TraitRegistry.CreateDefault());
            Assert.That(enemy.Stats.Category.Key, Is.EqualTo(CategoryKey.Heavy));
        }

        [Test]
        public void RiderMoveThenAttack_AndNextOwnerTurnConsumeTheirOwnActions()
        {
            var input = CombatCommandTestData.Input(category: CategoryKey.Rider);
            var moved = CombatCommandCalculator.Calculate(MovementTestData.Route(input,
                new CellCoord(1, 3), new CellCoord(2, 3)), Rules);
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            var after = CombatCommandComposition.Project(moved);
            var followUp = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(after,
                CombatCommandKind.Attack, input.Aim), Rules);
            Assert.That(followUp.Accepted, Is.True, followUp.Reason);
            var next = CombatCommandTestData.NextPlayerTurn(after);
            var attack = CombatCommandCalculator.Calculate(CombatCommandTestData.FreshCommand(next,
                CombatCommandKind.Attack, input.ActorId, input.Aim), Rules);
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(MovementTestData.Actor(attack).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BehindHeavy_DamageMatchesUnprotectedControl(bool shield)
        {
            var input = CombatCommandTestData.Input();
            var provider = CombatCommandTestData.With(CombatCommandTestData.Character(90, EntityKey.Unit_ShotGun,
                new CellCoord(4, 2), Owner.Enemy), category: CategorySnapshot.CreateStateless(CategoryKey.Heavy));
            var victim = input.Characters.Single(c => c.EntityId == new EntityId(2));
            victim = victim.With(stats: victim.Stats.With(physicalDefense: 3));
            if (shield) victim = HeldEnvironmentEffects.AcquireBadge(victim, 41);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == victim.EntityId ? victim : c).ToArray());
            var protectedInput = CombatCommandTestData.With(input, input.Characters.Concat(new[] { provider }).ToArray(),
                profiles: input.Profiles.Concat(new[] { new CombatAttackProfile(
                    CombatCommandTestData.Spec(provider.CharacterKey, 1, CategoryKey.Heavy), input.Profiles[1].Method) }).ToArray());
            var control = CombatCommandCalculator.Calculate(input, Rules);
            var result = CombatCommandCalculator.Calculate(protectedInput, Rules);
            Assert.That(result.Accepted && control.Accepted, Is.True);
            var expected = control.Attack.Value.Hits.ToArray().Single(h => h.TargetId == victim.EntityId);
            var actual = result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == victim.EntityId);
            Assert.That(actual.DamageDealt, Is.EqualTo(expected.DamageDealt));
            Assert.That(actual.TargetHealthAfter, Is.EqualTo(expected.TargetHealthAfter));
            var preview = CombatCommandCalculator.Preview(protectedInput, Rules);
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == victim.EntityId).ExpectedDamage, Is.EqualTo(expected.DamageDealt));
            Assert.That(victim.Health.CurrentHealth, Is.EqualTo(100));
        }
    }
}
