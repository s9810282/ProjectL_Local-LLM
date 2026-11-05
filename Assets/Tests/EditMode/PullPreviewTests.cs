using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class PullPreviewTests
    {
        [Test]
        public void RepeatedPreview_LeavesWorldAndExposureUnchanged()
        {
            var input = PullTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == PullTestData.Target
                ? HuntersMarkAttackTests.Expose(c, 25, 2, 90) : c).ToArray());
            var before = input.Characters.Single(c => c.EntityId == PullTestData.Target);
            for (int i = 0; i < 3; i++)
            {
                var preview = CombatCommandCalculator.Preview(input, PullTestData.NoRules, out var projection);
                Assert.That(preview.CanAttack, Is.True);
                Assert.That(preview.PrimaryCell, Is.EqualTo(input.Aim.Cell));
                Assert.That(preview.Targets.ToArray().Single().ExpectedDamage, Is.EqualTo(25));
                Assert.That(preview.Targets.ToArray().Single().TargetOccupiedCells.ToArray(), Is.EqualTo(new[] { new CellCoord(9, 7) }));
                Assert.That(Movement(preview).FinalAnchor, Is.EqualTo(new CellCoord(8, 7)));
                Assert.That(PullTestData.Final(projection).StatusEffects.SameValues(before.StatusEffects), Is.True);
                Assert.That(input.Characters.Single(c => c.EntityId == PullTestData.Target).SameValues(before), Is.True);
                Assert.That(new EnemyAiPolicy(new HostilityPolicy(), PullTestData.NoRules)
                    .TrySelectAttackAim(input, CombatCommandCalculator.GetAttackRange(input), out var aim), Is.True);
                Assert.That(aim, Is.EqualTo(input.Aim));
            }
        }

        [TestCase(Owner.Enemy, 27, 73)]
        [TestCase(Owner.Player, 7, 93)]
        public void PreviewArrivalAndDamage_MatchVisibleExecution(Owner owner, int damage, int health)
        {
            var input = CliffFallTests.WithEnvironment(PullTestData.Input(owner: owner),
                MineEnvironmentTests.Mine(id: 60, damage: 7, cell: new CellCoord(8, 7)));
            var preview = CombatCommandCalculator.Preview(input, MineEnvironmentTests.Rules);
            var result = PullTestData.Run(input, MineEnvironmentTests.Rules);
            var target = preview.Targets.ToArray().Single(t => t.TargetId == PullTestData.Target);
            Assert.That(target.ExpectedDamage, Is.EqualTo(damage));
            Assert.That(target.SecondaryDamage, Is.EqualTo(7));
            Assert.That(target.TargetHealthAfter, Is.EqualTo(health));
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(health));
            Assert.That(Movement(preview).Path, Is.EqualTo(Movement(result.Attack.Value).Path));
            Assert.That(result.Attack.Value.Hits.Length, Is.EqualTo(owner == Owner.Player ? 0 : 1));
        }

        [Test]
        public void FriendlyMove_PreviewHasTargetAndZeroDamageWithoutFakeHit()
        {
            var input = PullTestData.Input(owner: Owner.Player);
            var preview = CombatCommandCalculator.Preview(input, PullTestData.NoRules, out var result);
            Assert.That(preview.Targets.Length, Is.EqualTo(1));
            Assert.That(preview.Targets.ToArray()[0].TargetId, Is.EqualTo(PullTestData.Target));
            Assert.That(preview.Targets.ToArray()[0].ExpectedDamage, Is.Zero);
            Assert.That(preview.Targets.ToArray()[0].IsDirectHit, Is.False);
            Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
            Assert.That(Movement(preview).TargetId, Is.EqualTo(PullTestData.Target));
        }

        [Test]
        public void HazardDeath_ReportsOnlyActualPathAndNoDirectHit()
        {
            var input = StaticTerrainIntegrationTests.Terrain(PullTestData.Input(extra: 3, distance: 4),
                StaticTerrainIntegrationTests.Tile(71, new CellCoord(10, 7), StaticTerrainKind.Cliff));
            var preview = CombatCommandCalculator.Preview(input, PullTestData.NoRules, out var result);
            var movement = Movement(preview);
            Assert.That(movement.StartAnchor, Is.EqualTo(new CellCoord(11, 7)));
            Assert.That(movement.FinalAnchor, Is.EqualTo(new CellCoord(10, 7)));
            Assert.That(movement.Path, Is.EqualTo(new[] { new CellCoord(10, 7) }));
            Assert.That(preview.Targets.ToArray().Single().TargetKilled, Is.True);
            Assert.That(preview.Targets.ToArray().Single().ExpectedDamage, Is.Zero);
            Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
            Assert.That(result.Events.OfType<CombatPullEvent>().Single().Movement.Path, Is.EqualTo(movement.Path));
        }

        [Test]
        public void HiddenMine_IsNotExposedByPublicPreview()
        {
            var input = CliffFallTests.WithEnvironment(PullTestData.Input(),
                MineEnvironmentTests.Mine(id: 60, damage: 7, hidden: true, cell: new CellCoord(8, 7)));
            var preview = CombatCommandCalculator.Preview(input, MineEnvironmentTests.Rules, out var projection);
            Assert.That(preview.Targets.ToArray().Single().ExpectedDamage, Is.EqualTo(20));
            Assert.That(projection.Events.OfType<CombatObjectActivationEvent>(), Is.Empty);
            Assert.That(Movement(preview).FinalAnchor, Is.EqualTo(new CellCoord(8, 7)));
            Assert.That(PullTestData.Final(PullTestData.Run(input, MineEnvironmentTests.Rules)).Health.CurrentHealth, Is.EqualTo(73));
        }

        [Test]
        public void MovementSnapshot_DefensivelyCopiesAndValidatesActualPath()
        {
            var path = new[] { new CellCoord(8, 7) };
            var movement = new AttackPullSnapshot(PullTestData.Target, new CellCoord(9, 7), path[0], path);
            path[0] = CellCoord.Zero;
            Assert.That(movement.Path[0], Is.EqualTo(new CellCoord(8, 7)));
            Assert.Throws<ArgumentException>(() => new AttackPullSnapshot(PullTestData.Target, new CellCoord(9, 7), CellCoord.Zero, path));
        }

        private static AttackPullSnapshot Movement<T>(T result)
        {
            var property = typeof(T).GetProperty("Pull");
            Assert.That(property, Is.Not.Null, "Actual Pull movement must be exposed through the result contract.");
            var movement = (AttackPullSnapshot)property.GetValue(result);
            Assert.That(movement, Is.Not.Null);
            return movement;
        }
    }
}
