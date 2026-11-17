using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Tests.Core
{
    public sealed class PillarResolutionTests
    {
        [TestCase(4, 0, 1, 0)]
        [TestCase(4, 4, 1, -1)]
        [TestCase(0, 4, 0, -1)]
        [TestCase(-4, 4, -1, -1)]
        [TestCase(-4, 0, -1, 0)]
        [TestCase(-4, -4, -1, 1)]
        [TestCase(0, -4, 0, 1)]
        [TestCase(4, -4, 1, 1)]
        [TestCase(4, 2, 1, -1)]
        [TestCase(4, 1, 1, 0)]
        [TestCase(-4, 2, -1, -1)]
        [TestCase(-4, 1, -1, 0)]
        [TestCase(-4, -2, -1, 1)]
        [TestCase(-4, -1, -1, 0)]
        [TestCase(4, -2, 1, 1)]
        [TestCase(4, -1, 1, 0)]
        public void EightDirectionMatchesHtmlCoordinates(int htmlDx, int htmlDy, int coreDx, int coreDy)
        {
            var cells = PillarFallCalculator.GetCells(new CellCoord(7, 7),
                new CellCoord(7 - htmlDx, 7 + htmlDy), 3, 15, 15);
            Assert.That(cells, Is.EqualTo(new[] { new CellCoord(7 + coreDx, 7 + coreDy),
                new CellCoord(7 + 2 * coreDx, 7 + 2 * coreDy), new CellCoord(7 + 3 * coreDx, 7 + 3 * coreDy) }));
        }

        [TestCase(1, 1)]
        [TestCase(3, 3)]
        [TestCase(5, 5)]
        [TestCase(100, 5)]
        public void LengthAndBoardEdgeClipWithoutExtending(int length, int count)
        {
            var cells = PillarFallCalculator.GetCells(new CellCoord(2, 3), new CellCoord(1, 3), length, 8, 8);
            Assert.That(cells.Count, Is.EqualTo(count));
            Assert.That(cells.First(), Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(cells.Last(), Is.EqualTo(new CellCoord(2 + count, 3)));
        }

        [Test]
        public void MissingOrZeroDirectionRejectsBeforeObstacleMutation()
        {
            var input = PillarTestData.Input();
            var world = new CommandAttackProjection(input, input.State.Environment, null);
            Assert.Throws<ArgumentException>(() => world.Obstacles.ApplyHit(new EntityId(51),
                new CombatDamageOrigin(null, input.ActorId)));
            Assert.Throws<ArgumentException>(() => world.Obstacles.ApplyHit(new EntityId(51),
                new CombatDamageOrigin(new CellCoord(4, 3), input.ActorId)));
            Assert.That(world.Structures[new EntityId(51)].IsDestroyed, Is.False);
            Assert.That(world.Events, Is.Empty);
        }

        [Test]
        public void DirectHitCrushesAndCreatesThreeWallsExcludingOriginalCell()
        {
            var input = PillarTestData.Input();
            var result = PillarTestData.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Structures.Single().IsDestroyed, Is.True);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(4)).Final.Health.IsDead, Is.True);
            Assert.That(result.FinalState.Environment.Tiles.OfType<StaticTerrainFact>().Select(t => t.Cell),
                Is.EqualTo(new[] { new CellCoord(5, 3), new CellCoord(6, 3), new CellCoord(7, 3) }));
            Assert.That(result.FinalState.Environment.Tiles.OfType<StaticTerrainFact>().All(t => t.Kind == StaticTerrainKind.Wall), Is.True);
            Assert.That(input.State.Environment.Tiles, Is.Empty);
        }

        [Test]
        public void WallDoesNotTruncateImpact()
        {
            var wall = PlatformTestData.Terrain(71, new CellCoord(6, 3), StaticTerrainKind.Wall);
            var result = PillarTestData.Attack(PillarTestData.Input(wall));
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == wall.Id).SameValues(wall), Is.True);
            Assert.That(result.FinalState.Environment.Tiles.Any(t => t.Cell == new CellCoord(7, 3)), Is.True);
        }

        [Test]
        public void CliffIsFilledUsingSameSourceId()
        {
            var cliff = PlatformTestData.Terrain(71, new CellCoord(6, 3));
            var result = PillarTestData.Attack(PillarTestData.Input(cliff));
            var filled = (StaticTerrainFact)result.FinalState.Environment.Tiles.Single(t => t.Id == cliff.Id);
            Assert.That(filled.Kind, Is.EqualTo(StaticTerrainKind.FilledCliff));
            Assert.That(filled.KnownTo, Is.EqualTo(cliff.KnownTo));
            Assert.That(result.FinalState.EnvironmentProgress.NextSourceId, Is.EqualTo(74));
        }

        [Test]
        public void PlatformPassengerDiesButPlatformSurvivesWithoutDebris()
        {
            var cell = new CellCoord(5, 3);
            var cliff = PlatformTestData.Terrain(71, cell);
            var platform = PlatformTestData.Platform(72, cell, new CellCoord(5, 6));
            var result = PillarTestData.Attack(PillarTestData.Input(cliff, platform));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(4)).Final.Health.IsDead, Is.True);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == cliff.Id).SameValues(cliff), Is.True);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == platform.Id).SameValues(platform), Is.True);
            Assert.That(result.FinalState.Environment.Tiles.Count(t => t.Cell == cell), Is.EqualTo(2));
        }

        [Test]
        public void CrushBypassesReconstructionAndProtection()
        {
            var input = ReconstructionTraitTests.Input(3);
            var victim = HeldEnvironmentEffects.WithHeldState(
                CombatCommandTestData.With(ReconstructionTraitTests.Holding(3), position: new CellCoord(5, 3)),
                new CharacterHeldEnvironmentState(71, true));
            victim = CombatItemDirectDamageTests.Equip(victim, 5, 1);
            input = ObstacleReactionTests.Copy(input,
                characters: input.Characters.Where(c => c.EntityId.Value != 3).Select(c => c.EntityId.Value == 2 ? victim : c).ToArray(),
                structures: new[] { PillarTestData.Pillar().Structure }, profiles: new[] { PillarTestData.Pillar().Profile });
            input = CombatItemDirectDamageTests.Items(input);
            var result = PillarTestData.Attack(input);
            var death = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Final.EntityId == victim.EntityId && e.Kind == CombatEventKind.Died);
            Assert.That(death.DamageCause, Is.EqualTo(CombatDamageCause.PillarCrush));
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.Reconstructed), Is.False);
            Assert.That(death.Final.Items.EquippedItemKey, Is.Null);
            Assert.That(result.Events.OfType<CombatItemEvent>().Single(e => e.HolderId == victim.EntityId).Reason,
                Is.EqualTo(ItemEventReason.Death));
            Assert.That(death.DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(input.ActorId));
            Assert.That(result.Events.OfType<CombatHeldEnvironmentEvent>().Any(e => e.CharacterId == victim.EntityId &&
                e.Expected.BadgeAvailable && !e.Final.BadgeAvailable), Is.False);
        }

        [Test]
        public void PillarBarrelPillarCycleResolvesOnceAndRetainsDestroyedSource()
        {
            var first = PillarTestData.Pillar();
            var barrel = ObstacleReactionTests.Barrel(52, new CellCoord(5, 3), 9, 0);
            var second = PillarTestData.Pillar(53, new CellCoord(5, 4));
            var input = ObstacleReactionTests.Barrels(first, barrel, second);
            input = CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId.Value != 4).ToArray());
            var result = PillarTestData.Attack(input);
            Assert.That(result.Events.OfType<CombatObstacleDurabilityEvent>().Where(e => e.Final.IsDestroyed)
                .Select(e => e.ObstacleId.Value), Is.EqualTo(new[] { 51, 52, 53 }));
            var falls = result.Events.OfType<CombatPillarFallEvent>().ToArray();
            Assert.That(falls.Length, Is.EqualTo(2));
            Assert.That(falls[0].Origin.PhysicalCell, Is.EqualTo(new CellCoord(1, 3)));
            Assert.That(falls[1].Origin.PhysicalCell, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(falls[1].Direction, Is.EqualTo(new CellCoord(0, 1)));
            Assert.That(falls.All(f => f.Origin.CreditedCharacterId == input.ActorId), Is.True);
            Assert.That(result.Events.OfType<CombatPillarTerrainEvent>().Select(e => e.Final.Id).Distinct().Count(),
                Is.EqualTo(result.Events.OfType<CombatPillarTerrainEvent>().Count()));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CrushChainDoesNotGrantSupplyBuff(bool indirect)
        {
            var cell = indirect ? new CellCoord(6, 2) : new CellCoord(5, 3);
            var id = new EntityId(54);
            var spec = new StructureSpec(EntityKey.Structure_Supplies, 1, 1, new[] { CellCoord.Zero }, 1, false, 0);
            var supply = (new StructureCombatFacts(id, spec, new ObstacleDurabilitySnapshot(5, 5), cell, Rotation4.R0, new[] { cell }),
                new ObstacleContentProfile(id, ObstacleContentKind.Supplies, 0, 3, 9));
            var input = ObstacleReactionTests.Barrels(PillarTestData.Pillar(),
                ObstacleReactionTests.Barrel(52, new CellCoord(6, 3), 9, 0), supply);
            input = CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId.Value != 4).ToArray());
            var result = PillarTestData.Attack(input);
            Assert.That(result.Events.OfType<CombatHeldEnvironmentEvent>().Any(e => e.Final.SuppliesAttackBonus >
                e.Expected.SuppliesAttackBonus), Is.False);
            var final = result.FinalState.Structures.Single(s => s.EntityId == id);
            Assert.That(final.ObstacleDurability.RemainingHits, Is.EqualTo(indirect ? 4 : 0));
            Assert.That(result.Events.OfType<CombatObstacleReactionEvent>().Single(e => e.ObstacleId == id).SuppressSupplyRewards, Is.True);
        }

        [Test]
        public void FilledCliffRepeatedImpactAndOrdinaryStructureDoNotReceiveWalls()
        {
            var input = PillarTestData.Input();
            var cell = new CellCoord(5, 3);
            var spec = new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0);
            var ordinary = new StructureCombatFacts(new EntityId(55), spec, new HealthStateSnapshot(20, 20), cell, Rotation4.R0, new[] { cell });
            input = ObstacleReactionTests.Copy(input, structures: input.State.Structures.Append(ordinary).ToArray(),
                characters: input.Characters.Where(c => c.EntityId.Value != 4).ToArray());
            var filled = PlatformTestData.Terrain(71, new CellCoord(6, 3), StaticTerrainKind.FilledCliff);
            var result = PillarTestData.Attack(PillarTestData.WithTerrain(input, filled));
            Assert.That(result.FinalState.Structures.Single(s => s.EntityId == ordinary.EntityId), Is.SameAs(ordinary));
            Assert.That(result.FinalState.Environment.Tiles.Any(t => t.Cell == cell), Is.False);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 71).SameValues(filled), Is.True);
            Assert.That(result.Events.OfType<CombatPillarTerrainEvent>().Single().Final.Cell, Is.EqualTo(new CellCoord(7, 3)));
        }

        [Test]
        public void GeneratedWallsImmediatelyBlockMovementAndBuildLineOfSightGeometry()
        {
            var result = PillarTestData.Attack(PillarTestData.Input());
            var bindings = result.EnvironmentBindings.Final;
            var cell = new CellCoord(5, 3);
            Assert.That(EnvironmentTerrainRules.BlocksVoluntaryEntry(result.FinalState.Environment, bindings, cell), Is.True);
            var walls = WallGeometryBuilder.Build(8, 8, result.FinalState.Environment, bindings, result.FinalState.WallBlocking);
            Assert.That(walls.Occupies(cell), Is.True);
            Assert.That(walls.Runs.Single().Cells, Does.Contain(new CellCoord(7, 3)));
            var next = CombatCommandComposition.Project(result);
            next = CombatCommandTestData.With(next, next.Characters.Select(c => c.EntityId == next.ActorId
                ? CombatCommandTestData.With(c, position: new CellCoord(4, 2), actionUse: UnitActionUse.Available)
                : c.EntityId.Value == 3 ? CombatCommandTestData.With(c, position: new CellCoord(6, 4)) : c).ToArray());
            var actor = next.Characters.Single(c => c.EntityId == next.ActorId);
            var selection = CombatAttackQueries.Selection(next, actor, CombatCommandCalculator.RequiredProfile(next, actor),
                AttackAim.ForCell(new CellCoord(6, 4)));
            Assert.That(selection.FailureReason, Is.EqualTo(AttackFailureReason.BlockedByWall));
        }

        [Test]
        public void TerminalCrushStillCompletesQueuedChainAndTerrain()
        {
            var input = ObstacleReactionTests.Barrels(PillarTestData.Pillar(),
                ObstacleReactionTests.Barrel(52, new CellCoord(5, 3), 9, 0), PillarTestData.Pillar(53, new CellCoord(5, 4)));
            input = CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId.Value != 4)
                .Select(c => c.EntityId.Value == 10 ? CombatCommandTestData.With(c, position: new CellCoord(6, 3)) : c).ToArray());
            var result = PillarTestData.Attack(input);
            Assert.That(result.Outcome.IsStageEnded, Is.True);
            Assert.That(result.FinalState.Structures.All(s => s.IsDestroyed), Is.True);
            Assert.That(result.Events.OfType<CombatPillarFallEvent>().Count(), Is.EqualTo(2));
            Assert.That(result.FinalState.Environment.Tiles.Any(t => t.Cell == new CellCoord(5, 7)), Is.True);
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.ControllerTurnStarted), Is.False);
        }

        [Test]
        public void DiagonalFallProducesUsableWallsAndKeepsOriginalCellEmpty()
        {
            var input = ObstacleReactionTests.Barrels(PillarTestData.Pillar(cell: new CellCoord(3, 3)));
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == input.ActorId
                ? CombatCommandTestData.With(c, position: new CellCoord(2, 2)) : c).ToArray());
            input = CombatCommandTestData.FreshCommand(input, CombatCommandKind.Attack, aim: AttackAim.ForCell(new CellCoord(3, 3)));
            var result = PillarTestData.Attack(input);
            var cells = result.FinalState.Environment.Tiles.OfType<StaticTerrainFact>().Select(t => t.Cell).ToArray();
            Assert.That(cells, Is.EqualTo(new[] { new CellCoord(4, 4), new CellCoord(5, 5), new CellCoord(6, 6) }));
            var walls = WallGeometryBuilder.Build(8, 8, result.FinalState.Environment, result.EnvironmentBindings.Final,
                result.FinalState.WallBlocking);
            Assert.That(walls.Runs.Count, Is.EqualTo(3));
            Assert.That(walls.Occupies(new CellCoord(3, 3)), Is.False);
        }

        [Test]
        public void MultiCellBossIsCrushedOnceWhenTwoOccupiedCellsAreHit()
        {
            var input = PillarTestData.Input();
            var anchor = new CellCoord(5, 2);
            var cells = new[] { anchor, new CellCoord(6, 2), new CellCoord(5, 3), new CellCoord(6, 3) };
            var boss = CombatCommandTestData.Character(6, EntityKey.Unit_Stage1Boss, anchor, Owner.Enemy);
            boss = boss.With(stats: boss.Stats.With(category: CategorySnapshot.CreateStateless(CategoryKey.Boss)))
                .WithPlacement(CharacterRuntimeArea.Field, null, anchor, Rotation4.R0, cells);
            input = CombatCommandTestData.With(input, input.Characters.Where(c => c.EntityId.Value != 4).Append(boss).ToArray());
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            // 다칸 공격 프로필은 아직 지원하지 않는다. 실제 낙하·압사 경계의 점유 칸 중복 제거를 직접 검증한다.
            var damage = new CombatDamageApplication(Array.Empty<ItemDefinition>(), characters, events);
            var environment = new CommandEnvironmentState(input.State.Environment, input.EnvironmentBindings, input.State.EnvironmentProgress);
            var obstacles = new ObstacleReactionCalculator(input, environment, damage,
                input.State.Structures.ToDictionary(s => s.EntityId), new Dictionary<EntityId, CellCoord>());
            obstacles.ApplyHit(new EntityId(51), new CombatDamageOrigin(new CellCoord(1, 3), input.ActorId));
            obstacles.ResolveExplosions();
            Assert.That(characters[boss.EntityId].Health.IsDead, Is.True);
            Assert.That(events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Died &&
                e.Final.EntityId == boss.EntityId), Is.EqualTo(1));
            Assert.That(events.OfType<CombatPillarFallEvent>().Single().CrushedUnitIds, Is.EqualTo(new[] { boss.EntityId }));
        }
    }
}
