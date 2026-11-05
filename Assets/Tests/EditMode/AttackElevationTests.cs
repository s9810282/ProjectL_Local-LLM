using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    /// <summary>고지대 → 저지대 직격의 Wall 무시 규칙(고지대 Wall 무시 설계 D1~D4, D8)의 순수 단위 시험이다.</summary>
    public sealed class AttackElevationTests
    {
        private static readonly CellCoord Attacker = new CellCoord(1, 3);
        private static readonly CellCoord WallCell = new CellCoord(3, 3);
        private static readonly CellCoord Target = new CellCoord(5, 3);
        private static readonly Owner[] Both = { Owner.Player, Owner.Enemy };

        [TestCase(true, false, true)]
        [TestCase(true, true, false)]
        [TestCase(false, true, false)]
        [TestCase(false, false, false)]
        public void BypassesWalls_OnlyFromHighToLow(bool fromHigh, bool toHigh, bool expected)
        {
            var elevation = Elevation(fromHigh, toHigh);
            Assert.That(elevation.BypassesWalls(Attacker, Target), Is.EqualTo(expected));
        }

        [Test]
        public void From_KeepsOnlyActiveClassifiedHighGround()
        {
            var environment = new MovementEnvironmentFacts(new MovementTileFact[]
            {
                High(11, new CellCoord(0, 0)),
                High(12, new CellCoord(1, 0), active: false),
                High(13, new CellCoord(2, 0)),
                new StaticTerrainFact(14, new CellCoord(3, 0), StaticTerrainKind.Sandstorm, true, Both)
            });
            var elevation = AttackElevation.From(environment, Bindings(11, 12, 14));
            Assert.That(elevation.IsHigh(new CellCoord(0, 0)), Is.True);
            Assert.That(elevation.IsHigh(new CellCoord(1, 0)), Is.False, "비활성");
            Assert.That(elevation.IsHigh(new CellCoord(2, 0)), Is.False, "지형 의미 분류 누락");
            Assert.That(elevation.IsHigh(new CellCoord(3, 0)), Is.False, "다른 지형 종류");
        }

        [Test]
        public void From_FollowsObserverProjection()
        {
            var environment = new MovementEnvironmentFacts(new MovementTileFact[]
            {
                High(11, Attacker),
                High(12, Target, Owner.Enemy)
            });
            var bindings = Bindings(11, 12);
            Assert.That(AttackElevation.From(environment, bindings).IsHigh(Target), Is.True);
            var player = AttackElevation.From(environment.VisibleTo(Owner.Player), bindings);
            Assert.That(player.IsHigh(Attacker), Is.True);
            Assert.That(player.IsHigh(Target), Is.False, "플레이어에게 숨은 고지대");
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            var environment = new MovementEnvironmentFacts(Array.Empty<MovementTileFact>());
            Assert.Throws<ArgumentNullException>(() => new AttackElevation(null));
            Assert.Throws<ArgumentNullException>(() => AttackElevation.From(null, Bindings()));
            Assert.Throws<ArgumentNullException>(() => AttackElevation.From(environment, null));
            var walls = Walls();
            Assert.Throws<ArgumentNullException>(() =>
                AttackOcclusionResolver.CheckDirect(Attacker, Target, walls, null, AttackOcclusionSpec.Default));
            Assert.Throws<ArgumentNullException>(() => AttackOcclusionResolver.ResolveCells(Attacker,
                new AttackCellSelection(Target, new[] { Target }, Array.Empty<CellCoord>()), walls, null, AttackOcclusionSpec.Default));
        }

        [Test]
        public void TerrainCells_AgreesWithHasTerrainOnEveryCell()
        {
            var environment = new MovementEnvironmentFacts(new MovementTileFact[]
            {
                High(11, new CellCoord(0, 0)),
                High(12, new CellCoord(1, 1), active: false),
                High(13, new CellCoord(2, 2)),
                new StaticTerrainFact(14, new CellCoord(3, 3), StaticTerrainKind.Sandstorm, true, Both),
                High(15, new CellCoord(3, 1))
            });
            var bindings = Bindings(11, 12, 14, 15);
            foreach (StaticTerrainKind kind in Enum.GetValues(typeof(StaticTerrainKind)))
            {
                var cells = new HashSet<CellCoord>(EnvironmentTerrainRules.TerrainCells(environment, bindings, kind));
                for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                {
                    var cell = new CellCoord(x, y);
                    Assert.That(cells.Contains(cell), Is.EqualTo(EnvironmentTerrainRules.HasTerrain(environment, bindings, cell, kind)),
                        $"{kind} {cell}");
                }
            }
        }

        [TestCase(true, false, false, true)]
        [TestCase(true, true, false, false)]
        [TestCase(false, true, false, false)]
        [TestCase(false, false, false, false)]
        [TestCase(false, false, true, true)]
        [TestCase(true, true, true, true)]
        public void CheckDirect_CombinesUnitPolicyAndElevation(bool fromHigh, bool toHigh, bool ignorePolicy, bool clear)
        {
            var policy = ignorePolicy
                ? new AttackOcclusionSpec(AttackWallDelivery.IgnoreWalls, AttackWallPropagation.FromImpact)
                : AttackOcclusionSpec.Default;
            var contact = AttackOcclusionResolver.CheckDirect(Attacker, Target, Walls(), Elevation(fromHigh, toHigh), policy);
            Assert.That(contact == null, Is.EqualTo(clear));
            if (!clear) Assert.That(contact.WallCells, Is.EqualTo(new[] { WallCell }));
        }

        [Test]
        public void ResolveCells_HighAttackerStillCannotPlaceOnWallCell()
        {
            var selection = new AttackCellSelection(null, new[] { WallCell, Target }, Array.Empty<CellCoord>());
            var result = AttackOcclusionResolver.ResolveCells(Attacker, selection, Walls(), Elevation(true, false),
                AttackOcclusionSpec.Default);
            Assert.That(result.Cells.DesignatedCells.ToArray(), Is.EqualTo(new[] { Target }));
            Assert.That(result.BlockedContacts.Single().Contact, Is.EqualTo(WallCell));
            Assert.That(result.BlockedContacts.Single().Phase, Is.EqualTo(AttackOcclusionPhase.Direct));
        }

        [Test]
        public void ResolveCells_PropagationIgnoresElevationAndChecksFromImpact()
        {
            var farWall = new CellCoord(6, 3);
            var behindFarWall = new CellCoord(7, 3);
            var open = new CellCoord(5, 4);
            var selection = new AttackCellSelection(Target, new[] { Target }, new[] { behindFarWall, open });
            var result = AttackOcclusionResolver.ResolveCells(Attacker, selection, Walls(WallCell, farWall),
                Elevation(true, false), AttackOcclusionSpec.Default);
            Assert.That(result.Cells.PrimaryCell, Is.EqualTo(Target), "직격은 고지대 → 저지대로 Wall을 무시");
            Assert.That(result.Cells.AdditionalCells.ToArray(), Is.EqualTo(new[] { open }));
            var blocked = result.BlockedContacts.Single();
            Assert.That(blocked.Phase, Is.EqualTo(AttackOcclusionPhase.Propagation));
            Assert.That(blocked.Origin, Is.EqualTo(Target));
            Assert.That(blocked.Contact, Is.EqualTo(behindFarWall));
        }

        [Test]
        public void CollectSingle_HighOriginPassesWallToLowCellsAndStopsAtHighCell()
        {
            var lowEnemy = new CellCoord(4, 3);
            var highEnemy = new CellCoord(6, 3);
            var targets = new[]
            {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, lowEnemy, Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, highEnemy, Owner.Enemy)
            };
            var trace = PierceTargetCollector.CollectSingle(Attacker, new CellCoord(2, 3), new[] { Attacker }, 8, 8, targets,
                Array.Empty<StructureCombatFacts>(), Array.Empty<StructureCombatFacts>(), default, Walls(),
                new AttackElevation(new[] { Attacker, highEnemy }), AttackOcclusionSpec.Default);
            Assert.That(trace.TraveledCells, Is.EqualTo(new[] { new CellCoord(2, 3), WallCell, lowEnemy, Target }),
                "Wall 칸은 경로에만 남는다");
            Assert.That(trace.Encounters.Select(e => e.Id.Value), Is.EqualTo(new[] { 2 }));
            Assert.That(trace.Endpoint, Is.EqualTo(highEnemy));
            Assert.That(trace.StopReason, Is.EqualTo(PiercingShotStopReason.Wall));
            Assert.That(trace.BlockedContacts.Single().Contact, Is.EqualTo(highEnemy));
        }

        private static AttackElevation Elevation(bool attackerHigh, bool targetHigh)
        {
            var cells = new List<CellCoord>();
            if (attackerHigh) cells.Add(Attacker);
            if (targetHigh) cells.Add(Target);
            return new AttackElevation(cells);
        }

        private static WallGeometry Walls(params CellCoord[] cells)
        {
            if (cells.Length == 0) cells = new[] { WallCell };
            var facts = cells.Select((cell, i) => (MovementTileFact)WallStateLifecycleTests.Wall(71 + i, cell)).ToArray();
            return WallGeometryBuilder.Build(8, 8, new MovementEnvironmentFacts(facts),
                Bindings(facts.Select(f => f.Id).ToArray()), new WallBlockingSpec(0));
        }

        private static StaticTerrainFact High(long id, CellCoord cell, Owner knownTo) =>
            new StaticTerrainFact(id, cell, StaticTerrainKind.HighGround, true, new[] { knownTo });

        private static StaticTerrainFact High(long id, CellCoord cell, bool active = true) =>
            new StaticTerrainFact(id, cell, StaticTerrainKind.HighGround, active, Both);

        private static EnvironmentSemanticBindings Bindings(params long[] ids) =>
            new EnvironmentSemanticBindings(ids, Array.Empty<long>(), Array.Empty<EntityId>());
    }
}
