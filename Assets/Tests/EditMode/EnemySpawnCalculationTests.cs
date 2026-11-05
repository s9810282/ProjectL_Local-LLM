using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class EnemySpawnCalculationTests
    {
        [TestCase(UnitRole.MainCharacter, UnitOwnerEligibility.Both)]
        [TestCase(UnitRole.Regular, UnitOwnerEligibility.PlayerOnly)]
        public void IneligibleCandidate_IsRejectedBeforeRandomSelection(UnitRole role, UnitOwnerEligibility owner)
        {
            Assert.Throws<InvalidOperationException>(() => Calculate(new[] {
                new EnemySpawnCandidate(Unit(role: role, owner: owner), 1) }));
        }

        [Test]
        public void MultiCellCandidate_DoesNotUseAnchorOnlyPlacement()
        {
            var error = Assert.Throws<InvalidOperationException>(() => Calculate(new[] {
                new EnemySpawnCandidate(Unit(width: 2), 1) }));
            Assert.That(error.Message, Does.Contain("다칸"));
        }

        [Test]
        public void OverflowingWeightSum_IsNotWrappedIntoAnotherDistribution()
        {
            Assert.Throws<OverflowException>(() => Calculate(new[] {
                new EnemySpawnCandidate(Unit(), int.MaxValue), new EnemySpawnCandidate(Unit(), 1) }));
        }

        [Test]
        public void DefaultCandidate_DoesNotBecomeAnImplicitUnit()
        {
            Assert.Throws<InvalidOperationException>(() => Calculate(new[] { default(EnemySpawnCandidate) }));
        }

        [TestCase(8)]
        [TestCase(9)]
        public void Cliffs_AreExcludedBeforeCountingCapacity(int count)
        {
            // 아군 배치 구역은 y=0~3이라 적이 놓일 칸은 y=4~5이며, y=4 전체가 낭떠러지다.
            var cliffs = Enumerable.Range(0, 8).Select(x => new StaticTerrainFact(100 + x, new CellCoord(x, 4),
                StaticTerrainKind.Cliff, true, new[] { Owner.Player, Owner.Enemy })).ToArray();
            var environment = new MovementEnvironmentFacts(cliffs.Cast<MovementTileFact>().ToArray());
            var bindings = new EnvironmentSemanticBindings(cliffs.Select(c => c.Id).ToArray(),
                Array.Empty<long>(), Array.Empty<EntityId>());
            InitialEnemyPlacementSpec[] Spawn() => EnemySpawnCalculator.Calculate(new StageConfig(8, 6, 8, 4, 2),
                new[] { new EnemySpawnCandidate(Unit(), 1) }, count, 7, Array.Empty<StructureSpec>(), environment, bindings);
            if (count == 9)
            {
                Assert.That(Assert.Throws<InvalidOperationException>(() => Spawn()).Message, Does.Contain("부족"));
                return;
            }
            Assert.That(Spawn().All(p => p.Anchor.Y == 5), Is.True);
        }

        [TestCase(14)]
        [TestCase(15)]
        public void Obstacles_ReserveRotatedFootprintBeforeCountingCapacity(int count)
        {
            // 2x1 화약통을 R90으로 돌려 (3,4)·(3,5) 두 칸을 점유한다.
            var barrel = new StructureSpec(EntityKey.Structure_PowderBarrel, 2, 1,
                new[] { new CellCoord(0, 0), new CellCoord(1, 0) }, 10, false, 0);
            var obstacles = new[] { new InitialObstaclePlacementSpec(barrel, ObstacleContentKind.PowderBarrel,
                new CellCoord(3, 4), Rotation4.R90, 0) };
            InitialEnemyPlacementSpec[] Spawn() => EnemySpawnCalculator.Calculate(new StageConfig(8, 6, 8, 4, 2),
                new[] { new EnemySpawnCandidate(Unit(), 1) }, count, 7, Array.Empty<StructureSpec>(), obstacles: obstacles);
            if (count == 15)
            {
                Assert.That(Assert.Throws<InvalidOperationException>(() => Spawn()).Message, Does.Contain("부족"));
                return;
            }
            var anchors = Spawn().Select(p => p.Anchor).ToArray();
            Assert.That(anchors.Distinct().Count(), Is.EqualTo(count));
            Assert.That(anchors, Has.No.Member(new CellCoord(3, 4)));
            Assert.That(anchors, Has.No.Member(new CellCoord(3, 5)));
        }

        [TestCase(32)]
        [TestCase(33)]
        public void Candidates_AreEveryCellOutsidePlayerRows(int count)
        {
            // 적 배치 구역은 없다. 8×6에서 아군 2줄(y=0~1)을 뺀 32칸이 모두 후보다.
            InitialEnemyPlacementSpec[] Spawn() => EnemySpawnCalculator.Calculate(new StageConfig(8, 6, 8, 2, 2),
                new[] { new EnemySpawnCandidate(Unit(), 1) }, count, 7, Array.Empty<StructureSpec>());
            if (count == 33)
            {
                Assert.That(Assert.Throws<InvalidOperationException>(() => Spawn()).Message, Does.Contain("부족"));
                return;
            }
            var expected = Enumerable.Range(2, 4).SelectMany(y => Enumerable.Range(0, 8).Select(x => new CellCoord(x, y)));
            Assert.That(Spawn().Select(p => p.Anchor), Is.EquivalentTo(expected));
        }

        [TestCase(10)]
        [TestCase(11)]
        public void Candidates_SkipListedPlayerCells(int count)
        {
            // 4×3에서 떨어진 아군 칸 두 개를 뺀 10칸이 후보다.
            var zone = PlayerDeploymentZone.Of(new[] { CellCoord.Zero, new CellCoord(3, 2) });
            var stage = new StageConfig(4, 3, 8, zone, 0, Array.Empty<InitialEnemyPlacementSpec>(),
                Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(), Array.Empty<InitialEnemyStructurePlacementSpec>());
            InitialEnemyPlacementSpec[] Spawn() => EnemySpawnCalculator.Calculate(stage,
                new[] { new EnemySpawnCandidate(Unit(), 1) }, count, 7, Array.Empty<StructureSpec>());
            if (count == 11)
            {
                Assert.That(Assert.Throws<InvalidOperationException>(() => Spawn()).Message, Does.Contain("부족"));
                return;
            }
            var anchors = Spawn().Select(p => p.Anchor).ToArray();
            Assert.That(anchors.Distinct().Count(), Is.EqualTo(10));
            Assert.That(anchors, Has.No.Member(CellCoord.Zero));
            Assert.That(anchors, Has.No.Member(new CellCoord(3, 2)));
        }

        private static InitialEnemyPlacementSpec[] Calculate(EnemySpawnCandidate[] candidates) =>
            EnemySpawnCalculator.Calculate(new StageConfig(8, 6, 8, 2, 2), candidates, 2, 7,
                Array.Empty<StructureSpec>());

        private static UnitSpec Unit(int width = 1, UnitRole role = UnitRole.Regular,
            UnitOwnerEligibility owner = UnitOwnerEligibility.EnemyOnly) => new UnitSpec(
            EntityKey.Unit_Demon, role, width, 1,
            Enumerable.Range(0, width).Select(x => new CellCoord(x, 0)).ToArray(), 10,
            MoveMethod.LiftAndPlace, 1, CategoryKey.Main,
            5, 0, 1, Array.Empty<TraitSpec>(), owner);
    }
}
