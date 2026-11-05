using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class PullTerrainTests
    {
        [TestCase(Owner.Enemy)]
        [TestCase(Owner.Player)]
        public void HazardDeath_HasNoDirectHit(Owner owner)
        {
            var input = StaticTerrainIntegrationTests.Terrain(PullTestData.Input(extra: 3, distance: 4, owner: owner),
                StaticTerrainIntegrationTests.Tile(71, new CellCoord(10, 7), StaticTerrainKind.Cliff));
            var result = PullTestData.Run(input);
            Assert.That(PullTestData.Final(result).Health.IsDead, Is.True);
            Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.Fell), Is.True);
        }

        [Test]
        public void ArrivalTerrainControlsDamage()
        {
            var input = StaticTerrainIntegrationTests.Terrain(PullTestData.Input(),
                StaticTerrainIntegrationTests.Tile(71, new CellCoord(8, 7), StaticTerrainKind.Sandstorm));
            var result = PullTestData.Run(input);
            Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(new CellCoord(8, 7)));
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(84));
        }

        [TestCase(Owner.Enemy, 73)]
        [TestCase(Owner.Player, 93)]
        public void TraversedThorns_ApplyBeforeAttackAndConsumeOnce(Owner owner, int health)
        {
            var input = PullTestData.Input(extra: 2, distance: 3, owner: owner);
            input = CombatCommandTestData.With(input, state: CombatCommandEffectTests.WithFields(input,
                new ThornFieldCellSnapshot(new CellCoord(9, 7), 3, 7)));
            var result = PullTestData.Run(input);
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(health));
            Assert.That(result.FinalState.ThornFields, Is.Empty);
            Assert.That(result.Events.OfType<CombatCharacterEvent>().First(e => e.Kind == CombatEventKind.HealthChanged)
                .Final.Anchor, Is.EqualTo(new CellCoord(9, 7)));
        }

        [Test]
        public void DestinationMine_ResolvesBeforeDirectHit()
        {
            var input = CliffFallTests.WithEnvironment(PullTestData.Input(),
                MineEnvironmentTests.Mine(id: 60, damage: 7, cell: new CellCoord(8, 7)));
            var result = PullTestData.Run(input, MineEnvironmentTests.Rules);
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(73));
            var events = result.Events.ToArray();
            int mine = System.Array.FindIndex(events, e => e is CombatObjectActivationEvent);
            int attack = System.Array.FindIndex(events, e => e is CombatAttackEvent);
            Assert.That(mine, Is.GreaterThanOrEqualTo(0));
            Assert.That(mine, Is.LessThan(attack));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TerrainKillsMainOrAttacker_StopsDirectHit(bool main)
        {
            var input = PullTestData.Input();
            input = CombatCommandTestData.With(input, input.Characters.Select(c =>
                main && c.OwnershipRole == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterOwnershipRole.Main
                    ? CombatCommandTestData.With(c, hp: 1, position: new CellCoord(7, 6))
                    : !main && c.EntityId == input.ActorId ? CombatCommandTestData.With(c, hp: 1) : c).ToArray());
            input = CliffFallTests.WithEnvironment(input, MineEnvironmentTests.Mine(id: 60, damage: 7, cell: new CellCoord(8, 7)));
            var result = PullTestData.Run(input, MineEnvironmentTests.Rules);
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
            if (main) Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
        }
    }
}
