using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class LavaChainPublicationTests
    {
        [Test]
        public void StageAuthoringIsOptInAndKeepsExplicitSettings()
        {
            using var fixture = new CoreAuthoringFixture();
            var stage = fixture.Create<CombatStageData>();
            Assert.That(((CombatStageDetail)stage.GetData()).LavaChain, Is.Null);
            var settings = new LavaChainGimmickAuthoring();
            CoreAuthoringFixture.Set(settings, "enabled", true);
            CoreAuthoringFixture.Set(settings, "initialHeadCell", new UnityEngine.Vector2Int(2, 3));
            CoreAuthoringFixture.Set(settings, "cycleControllerTurns", 5);
            CoreAuthoringFixture.Set(stage, "lavaChain", settings);
            var data = ((CombatStageDetail)stage.GetData()).LavaChain;
            Assert.That(data, Is.Not.SameAs(((CombatStageDetail)stage.GetData()).LavaChain));
            Assert.That(data.InitialHeadCell, Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(data.CycleControllerTurns, Is.EqualTo(5));
            Assert.That(data.WarningLeadControllerTurns, Is.EqualTo(1));
            Assert.That(data.PushDistance, Is.EqualTo(1));
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1, Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(),
                    Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters, new CellCoord(1, 0), Rotation4.R0,
                    new CellCoord(3, 5), Rotation4.R180, null, lavaChain: data)));
            using var baseline = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config);
            var definition = CoreIntegrationTestData.DatabaseSource(baseline.Buffers).Read(database, 1, new StageKey(1101));
            var spec = definition.Stage.StageGimmicks.ToArray().OfType<LavaChainGimmickSpec>().Single();
            Assert.That(spec.InitialHeadCell, Is.EqualTo(data.InitialHeadCell));
            Assert.That(spec.CycleControllerTurns, Is.EqualTo(data.CycleControllerTurns));
            Assert.That(spec.WarningLeadControllerTurns, Is.EqualTo(data.WarningLeadControllerTurns));
            Assert.That(spec.PushDistance, Is.EqualTo(data.PushDistance));
        }

        [Test]
        public void PublishedWarningRetractionAndFinalOccupancyAgree()
        {
            using var battle = Open();
            var warning = Reach(battle, 2).Events.OfType<CombatLavaChainEvent>().Single();
            Assert.That(warning.Step, Is.EqualTo(LavaChainGimmickStep.Warning));
            Assert.That(Read(battle).SameValues(warning.Final), Is.True);
            var rng = Current(battle).EnvironmentProgress.RandomState;
            var result = Reach(battle, 3);
            var advance = result.Events.OfType<CombatLavaChainEvent>().Single();
            Assert.That(advance.Step, Is.EqualTo(LavaChainGimmickStep.Retracted));
            Assert.That(advance.HeadPath[0], Is.EqualTo(warning.Expected.HeadCell));
            Assert.That(advance.HeadPath.Last(), Is.EqualTo(Read(battle).HeadCell));
            for (int i = 1; i < advance.HeadPath.Count; i++)
                Assert.That(advance.HeadPath[i] - advance.HeadPath[i - 1], Is.EqualTo(warning.Final.Heading));
            Assert.That(Current(battle).EnvironmentProgress.RandomState, Is.EqualTo(rng));
            Assert.That(Read(battle).IsWarning, Is.False);
            Assert.That(Read(battle).NextBoundaryCount, Is.EqualTo(5));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ForgedWarningHeadingOrRngIsRejectedWithoutPublishing(bool alterRng)
        {
            using var battle = Open(cycle: 2);
            var input = HaboobPublicationTests.EndInput(battle);
            var valid = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var original = valid.Events.OfType<CombatLavaChainEvent>().Single();
            var heading = original.Final.Heading == new CellCoord(1, 0) ? new CellCoord(1, 1) : new CellCoord(1, 0);
            var final = new LavaChainGimmickState(original.Final.Id, original.Final.HeadCell, original.Final.NextBoundaryCount, true, heading);
            var changed = new CombatLavaChainEvent(original.Step, original.Boundary, original.Expected, final,
                original.HeadPath, original.ExpectedRandomState, original.FinalRandomState);
            var random = valid.FinalState.EnvironmentProgress;
            var forged = alterRng ? valid.WithFinalState(valid.FinalState.With(environmentProgress:
                new EnvironmentProgressFacts(random.NextObjectEntryOrdinal, random.NextSourceId, random.RandomState ^ 1))) :
                CombatCommandComposition.WithEvents(valid, valid.Events.Select(e => ReferenceEquals(e, original) ? changed : e).ToArray())
                    .WithFinalState(valid.FinalState.With(gimmicks: valid.FinalState.Gimmicks.Replace(final)));
            var stores = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(forged));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            battle.Buffers.BattleWriteSink.Apply(valid);
        }

        [Test]
        public void AlteredHeadPathIsRejectedWithoutPublishing()
        {
            using var battle = Open();
            Reach(battle, 2);
            battle.AdvanceToPlayerOrDay();
            var valid = CombatCommandCalculator.Calculate(HaboobPublicationTests.EndInput(battle), Array.Empty<IMovementTileRule>());
            var original = valid.Events.OfType<CombatLavaChainEvent>().Single();
            var changed = new CombatLavaChainEvent(original.Step, original.Boundary, original.Expected, original.Final,
                new[] { original.Expected.HeadCell }.Concat(original.HeadPath).ToArray());
            var forged = CombatCommandComposition.WithEvents(valid,
                valid.Events.Select(e => ReferenceEquals(e, original) ? changed : e).ToArray());
            var stores = battle.CaptureStores();
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(forged));
            Assert.That(battle.CaptureStores(), Is.EqualTo(stores));
            battle.Buffers.BattleWriteSink.Apply(valid);
        }

        internal static ConfiguredMovementBattle Open(int cycle = 3, CellCoord? head = null) =>
            new(ConfiguredMovementBattle.Config, new HaboobPublicationTests.Source(new StageGimmickSpec[]
                { new LavaChainGimmickSpec(head ?? new CellCoord(0, 2), cycle, 1, 1) }));

        [Test]
        public void MeteorImpactsBeforeChainPushesAnUnhitCharacterIntoNewFlames()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new FixedSource(meteor: true), deploySniper: false);
            var warning = Reach(battle, 2);
            Assert.That(warning.Events.OfType<CombatMeteorEvent>().Single().Final.Warnings.Single().Cells[0],
                Is.EqualTo(new CellCoord(1, 0)));
            Assert.That(Read(battle).Heading, Is.EqualTo(new CellCoord(0, -1)));
            var id = battle.Actor.EntityId;
            var result = Reach(battle, 3);
            Assert.That(Current(battle).Turn.CompletedControllerTurnCount, Is.EqualTo(3));
            var impact = result.Events.OfType<CombatMeteorImpactEvent>().Single();
            Assert.That(impact.RecipientIds, Has.No.Member(id));
            var contact = result.Events.OfType<CombatLavaChainContactEvent>().Single(c => c.RecipientId == id);
            Assert.That(contact.LandingCell, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(result.Events.ToList().IndexOf(impact), Is.LessThan(result.Events.ToList().IndexOf(contact)));
            Assert.That(battle.Actor.Anchor, Is.EqualTo(new CellCoord(1, 1)));
            Assert.That(battle.Actor.StatusEffects.Entries.OfType<FireStatus>().Single().RemainingAffectedSideStarts, Is.EqualTo(3));
        }

        internal sealed class FixedSource : IBattleInitializationSource
        {
            private readonly bool _meteor;
            private readonly bool _overlap;
            internal FixedSource(bool meteor = false, bool overlap = false) { _meteor = meteor; _overlap = overlap; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var head = new CellCoord(0, 4);
                var d = new ConfiguredMovementBattle.EncounterSource(_overlap ? head : (CellCoord?)null).Read(database, chapterKey, stageKey);
                var s = d.Stage;
                var chain = new LavaChainGimmickSpec(head, 3, 1, 1);
                var definitions = _meteor ? new StageGimmickSpec[] { chain, new MeteorGimmickSpec(3, 1, 1, 1, 3, 60, 3, 4) }
                    : new StageGimmickSpec[] { chain };
                var stage = new StageConfig(s.MapWidth, s.MapHeight, s.ReserveCapacity, s.PlayerZone, s.TimelineSeed,
                    s.InitialEnemyPlacements.ToArray(), Array.Empty<CellCoord>(), definitions, s.InitialEnemyStructurePlacements.ToArray());
                var environment = d.Environment;
                var bindings = d.EnvironmentBindings;
                if (_meteor)
                {
                    var cells = Enumerable.Range(0, 6).SelectMany(y => Enumerable.Range(0, 8).Select(x => new CellCoord(x, y)));
                    var walls = cells.Where(c => !(c.X >= 1 && c.X <= 3 && c.Y <= 2) && !(c.X == 0 && c.Y <= 4) && c != new CellCoord(3, 5));
                    var tiles = walls.Select((c, i) => (MovementTileFact)new StaticTerrainFact(i + 1, c, StaticTerrainKind.Wall,
                        true, new[] { Owner.Player, Owner.Enemy })).ToList();
                    tiles.Add(new StaticTerrainFact(tiles.Count + 1, new CellCoord(0, 2), StaticTerrainKind.Cliff, true, new[] { Owner.Player, Owner.Enemy }));
                    environment = new MovementEnvironmentFacts(tiles);
                    bindings = new EnvironmentSemanticBindings(tiles.Select(t => t.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>());
                }
                return new BattleInitialization(stage, d.MainAnchor, d.MainRotation, d.Units, d.Structures, d.Categories,
                    d.AttackProfiles, environment, bindings, d.ObstaclePlacements, d.WallBlocking, d.ObjectiveKind,
                    d.ItemDefinitions, d.Progression, d.InitialRandomTornadoPlacements, d.ClearRewards, d.TraitGrowth);
            }
        }
        internal static CombatCommandState Current(ConfiguredMovementBattle battle) => battle.Buffers.CombatStateReadSink.Current.CommandState;
        internal static LavaChainGimmickState Read(ConfiguredMovementBattle battle) => Current(battle).Gimmicks.Entries.OfType<LavaChainGimmickState>().Single();
        internal static CombatResult Reach(ConfiguredMovementBattle battle, int boundary)
        {
            for (int step = 0; Current(battle).Turn.CompletedControllerTurnCount < boundary; step++)
            {
                Assert.That(step, Is.LessThan(256));
                if (Current(battle).Turn.ControllerPhase == CombatControllerPhase.Player) battle.Submit(battle.EndPlayerTurn());
                else Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            }
            Assert.That(Current(battle).Turn.CompletedControllerTurnCount, Is.EqualTo(boundary));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            return result;
        }
    }
}
