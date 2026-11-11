using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class NexusReinforcementGeometryTests
    {
        [Test]
        public void TwoByTwoNexus_PaddingTwoHas32Cells()
        {
            var nexus = Nexus();
            var area = NexusReinforcementGeometry.Area(nexus, 2, 12, 12);

            Assert.That(area.Count, Is.EqualTo(32));
            Assert.That(area.Distinct().Count(), Is.EqualTo(32));
            Assert.That(area.Intersect(nexus.OccupiedCells), Is.Empty);
            Assert.That(area, Is.EqualTo(area.OrderBy(cell => cell.Y).ThenBy(cell => cell.X)));
            Assert.That((area.Min(c => c.X), area.Max(c => c.X), area.Min(c => c.Y), area.Max(c => c.Y)),
                Is.EqualTo((2, 7, 2, 7)));
            Assert.That(NexusReinforcementGeometry.Area(nexus, 1, 12, 12).Count, Is.EqualTo(12));
            Assert.That(NexusReinforcementGeometry.Area(Nexus(CellCoord.Zero), 2, 12, 12).Count, Is.EqualTo(12));
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)area).Clear());
        }

        [Test]
        public void RotatedFootprintAndLargePadding_UseActualCellsAndClipBeforeIteration()
        {
            var cells = new[] { new CellCoord(5, 4), new CellCoord(5, 5) };
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 2, 1,
                new[] { CellCoord.Zero, new CellCoord(1, 0) }, 10, false, 0);
            var nexus = new StructureCombatFacts(new EntityId(30), definition, new HealthStateSnapshot(10, 10),
                cells[0], Rotation4.R90, cells);
            var rotated = NexusReinforcementGeometry.Area(nexus, 1, 12, 12);
            Assert.That(rotated.Count, Is.EqualTo(10));
            Assert.That(rotated, Has.Member(new CellCoord(4, 6)));
            Assert.That(rotated, Has.No.Member(new CellCoord(7, 4)));

            var clipped = NexusReinforcementGeometry.Area(nexus, int.MaxValue, 7, 8);
            Assert.That(clipped.Count, Is.EqualTo(54));
            Assert.That(clipped.All(c => c.X >= 0 && c.X < 7 && c.Y >= 0 && c.Y < 8), Is.True);
            Assert.That(clipped.Intersect(cells), Is.Empty);
        }

        [Test]
        public void Candidates_ExcludeActualBlockersButNotUnitInfluence()
        {
            var source = CombatCommandTestData.Input(empty: true);
            var characters = source.Characters.Select(c => c.EntityId == new EntityId(1)
                ? CombatCommandTestData.With(c, traits: new TraitStatusSnapshot[] { new TauntTraitStatusSnapshot(), new SoloActionTraitStatusSnapshot() })
                : c).Append(CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(10, 1), Owner.Enemy)).ToArray();

            var hidden = new[] { Owner.Enemy };
            var staticTerrain = ((StaticTerrainKind[])Enum.GetValues(typeof(StaticTerrainKind)))
                .Select((kind, index) => new StaticTerrainFact(index + 1, new CellCoord(index, 0), kind,
                    true, hidden, entryThreshold: kind == StaticTerrainKind.WeakGround ? 2 : 0)).ToArray();
            var mine = new MineSourceFact(10, new CellCoord(0, 1), 1, 1, true, hidden);
            var consumedMine = new MineSourceFact(11, new CellCoord(1, 1), 2, 1, false, hidden);
            var flag = new FlagSourceFact(12, new CellCoord(2, 1), 3, 1, true, hidden);
            var badge = new SheriffBadgeSourceFact(13, new CellCoord(3, 1), 4, true, hidden);
            var consumedBadge = new SheriffBadgeSourceFact(14, new CellCoord(4, 1), 5, false, hidden);
            var trail = new TraitTrailFact(15, new CellCoord(5, 0), new EffectOriginKey(EffectOwnerKind.Trait, 1,
                (int)TraitKey.GroundCompaction, "main"), Owner.Player, 1, 0, 0, 1, true, hidden);
            var expiredTrail = new TraitTrailFact(16, new CellCoord(5, 1), trail.Origin, Owner.Player, 1, 0, 0, 0, false, hidden);
            var modifier = new DestinationTileModifierFact(17, new CellCoord(6, 0), true, hidden, 2, 3);
            var legacyTornado = new TornadoSourceFact(18, new CellCoord(7, 0), hidden);
            var boundTerrain = CellEnvironmentTestData.Generic(19, new CellCoord(10, 2));
            var tiles = staticTerrain.Cast<MovementTileFact>().Concat(new MovementTileFact[]
            {
                mine, consumedMine, flag, badge, consumedBadge, trail, expiredTrail, modifier, legacyTornado, boundTerrain
            }).ToArray();
            var environment = new MovementEnvironmentFacts(tiles);
            var barrel = Structure(31, EntityKey.Structure_PowderBarrel, new CellCoord(11, 1), Owner.Neutral);
            var supplies = Structure(32, EntityKey.Structure_Supplies, new CellCoord(11, 4), Owner.Neutral);
            var playerStructure = Structure(33, EntityKey.Structure_Headquarters, new CellCoord(11, 3), Owner.Player);
            var deadStructure = Structure(34, EntityKey.Structure_PowderBarrel, new CellCoord(10, 0), Owner.Neutral, hp: 0);
            var bindings = new EnvironmentSemanticBindings(staticTerrain.Select(t => t.Id).Append(boundTerrain.Id).ToArray(),
                new[] { mine.Id, consumedMine.Id, flag.Id, badge.Id, consumedBadge.Id }, new[] { barrel.EntityId, supplies.EntityId, deadStructure.EntityId });

            var center = new CellCoord(8, 4);
            var movingTornado = new TornadoGimmickSpec(new[] { new TornadoMemberSpec(20, hidden, center, 1, diagonalDistance: 1) });
            var tornado = new TornadoGimmickState(new StageGimmickId(1), new[]
            {
                new TornadoMemberState(20, center, 1, TornadoGimmickPhase.Warning, 1, new CellCoord(1, 1))
            });
            var haboob = new HaboobGimmickState(new StageGimmickId(2), HaboobGimmickPhase.RelocationWarning, 1,
                new[] { new CellCoord(1, 8), new CellCoord(2, 8) });
            var warning = new CellCoord(8, 2);
            var rockfall = new RockfallGimmickState(new StageGimmickId(3), RockfallGimmickPhase.Warning, 1, new[] { warning });
            var gimmicks = new StageGimmickStateSet(new StageGimmickState[] { tornado, haboob, rockfall });
            var thorn = new ThornFieldCellSnapshot(new CellCoord(8, 0), 1, 1);
            var rail = new CellCoord(9, 0);

            var input = Input(characters, environment, bindings,
                new[] { barrel, supplies, playerStructure, deadStructure }, new[] { thorn }, new[] { rail }, gimmicks,
                new StageGimmickSpec[] { movingTornado, new HaboobGimmickSpec(), new RockfallGimmickSpec() });
            var candidates = NexusReinforcementCalculator.Candidates(input);

            var blocked = staticTerrain.Select(t => t.Cell)
                .Concat(new[] { mine.Cell, flag.Cell, badge.Cell, trail.Cell, modifier.Cell, legacyTornado.Cell, boundTerrain.Cell,
                    barrel.Anchor, supplies.Anchor, playerStructure.Anchor, thorn.Cell, rail, new CellCoord(10, 1) })
                .Concat(TornadoGeometry.Footprint(center, 3)).Concat(haboob.Cells);
            Assert.That(candidates.Intersect(blocked), Is.Empty);
            var allowed = new[] { consumedMine.Cell, consumedBadge.Cell, expiredTrail.Cell, deadStructure.Anchor,
                warning, new CellCoord(10, 6), new CellCoord(0, 2), new CellCoord(11, 0), new CellCoord(2, 2) };
            Assert.That(candidates, Is.SupersetOf(allowed));
            Assert.That(candidates, Is.EqualTo(candidates.OrderBy(cell => cell.Y).ThenBy(cell => cell.X)));
            Assert.That(input.State.Reinforcements.RandomState, Is.EqualTo(77u));
            Assert.That(input.State.Reinforcements.Records, Is.Empty);
        }

        [Test]
        public void Candidates_ExcludeAllFriendlyCrosses()
        {
            var source = CombatCommandTestData.Input(empty: true);
            var sub = source.Characters.Single(c => c.EntityId == new EntityId(1));
            var main = source.Characters.Single(c => c.EntityId != sub.EntityId);
            var dead = CombatCommandTestData.Character(11, EntityKey.Unit_Akimbo, new CellCoord(6, 1), Owner.Player)
                .WithHealth(new HealthStateSnapshot(0, 100));
            var hand = CombatCommandTestData.Character(12, EntityKey.Unit_Akimbo, new CellCoord(7, 1), Owner.Player)
                .WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>());
            var waiting = CombatCommandTestData.Character(13, EntityKey.Unit_Demon, new CellCoord(8, 1), Owner.Enemy)
                .WithPlacement(CharacterRuntimeArea.EnemyWaiting, null, null, null, Array.Empty<CellCoord>());
            var candidates = NexusReinforcementCalculator.Candidates(Input(new[] { sub, main, dead, hand, waiting }));

            var crosses = new[] { new CellCoord(1, 3), new CellCoord(0, 3), new CellCoord(2, 3), new CellCoord(1, 2), new CellCoord(1, 4),
                new CellCoord(7, 7), new CellCoord(6, 7), new CellCoord(8, 7), new CellCoord(7, 6), new CellCoord(7, 8) };
            Assert.That(candidates.Intersect(crosses), Is.Empty);
            Assert.That(candidates, Is.SupersetOf(new[] { new CellCoord(2, 2), new CellCoord(6, 6),
                new CellCoord(6, 1), new CellCoord(7, 1), new CellCoord(8, 1), CellCoord.Zero }));
        }

        private static StructureCombatFacts Nexus(CellCoord? start = null)
        {
            var anchor = start ?? new CellCoord(4, 4);
            var offsets = new[] { CellCoord.Zero, new CellCoord(1, 0), new CellCoord(0, 1), new CellCoord(1, 1) };
            var definition = new StructureSpec(EntityKey.Structure_Headquarters, 2, 2, offsets, 10, false, 0);
            return new StructureCombatFacts(new EntityId(30), definition, new HealthStateSnapshot(10, 10),
                anchor, Rotation4.R0, offsets.Select(c => new CellCoord(c.X + anchor.X, c.Y + anchor.Y)).ToArray());
        }

        private static StructureCombatFacts Structure(int id, EntityKey key, CellCoord cell, Owner owner, int hp = 10) =>
            new(new EntityId(id), new StructureSpec(key, 1, 1, new[] { CellCoord.Zero }, 10, false, 0),
                new HealthStateSnapshot(hp, 10), cell, Rotation4.R0, new[] { cell }, owner);

        private static CombatCommandInput Input(CharacterCombatFacts[] characters, MovementEnvironmentFacts environment = null,
            EnvironmentSemanticBindings bindings = null, StructureCombatFacts[] structures = null,
            ThornFieldCellSnapshot[] thorns = null, CellCoord[] railCells = null, StageGimmickStateSet gimmicks = null,
            StageGimmickSpec[] definitions = null)
        {
            var basis = CombatCommandTestData.Input(empty: true);
            var nexus = Nexus();
            var progress = new NexusReinforcementState(nexus.EntityId, 77, Array.Empty<NexusReinforcementRecord>());
            var state = new CombatCommandState(12, 12, CombatFlowPhase.Automatic,
                new[] { nexus }.Concat(structures ?? Array.Empty<StructureCombatFacts>()).ToArray(), thorns ?? Array.Empty<ThornFieldCellSnapshot>(),
                new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: 1),
                environment, gimmicks: gimmicks, reinforcements: progress);
            var spec = new NexusReinforcementSpec(new[] { new NexusReinforcementEntry(EntityKey.Unit_Demon, 1) }, 1, rangePadding: 12);
            var battle = new CombatBattleFacts(8, 42, PlayerDeploymentZone.Rows(4), Array.Empty<CharacterInstanceId>(), 0,
                railCells, definitions, nexusReinforcements: spec);
            return new CombatCommandInput(basis.Number, basis.Kind, basis.ActorId, basis.Aim, state, characters, basis.Profiles,
                basis.Main, basis.Owned, basis.SoulCounts, basis.Souls, basis.Artifacts, battle: battle, environmentBindings: bindings);
        }
    }
}
