using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;

namespace ProjectL.Tests.CoreIntegration
{
    // 접근 진척(A)과 이동 뒤 공격 추정(B)을 고친 판단이 준비 실행과 동기 실행에서 같은 결과로 한 번만 게시되는지 본다.
    public sealed class EnemyEngagementFollowupPublicationTests
    {
        public enum Scenario
        {
            RailPrefix,
            WallDetour,
            RegenerationFollowup,
            LethalRailHold,
        }

        private static readonly CellCoord Hammer = new(2, 0);
        private static readonly IMovementTileRule[] Rules =
        {
            new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule()
        };

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void SafeRailPrefixMovesAndSurvivesTheTrain(int partition)
        {
            using var prepared = Create(Scenario.RailPrefix);
            using var synchronous = Create(Scenario.RailPrefix);
            var actor = prepared.Character(EntityKey.Unit_Demon);

            var move = PublishBoth(prepared, synchronous, partition);
            Assert.That(move.PathMovement, Is.Not.Null);
            var moved = prepared.Character(EntityKey.Unit_Demon);
            Assert.That(moved.Anchor, Is.EqualTo(move.PathMovement.FinalStop));
            Assert.That(moved.Anchor.Value.Y, Is.GreaterThan(RailY(Scenario.RailPrefix)), "철로 칸에 멈추면 안 된다.");
            Assert.That(Distance(moved.Anchor.Value, Hammer), Is.LessThan(Distance(actor.Anchor.Value, Hammer)));

            var boundary = PublishBoth(prepared, synchronous, partition);
            Assert.That(Railway(boundary), Is.Not.Null, "이번 적 턴 끝에 기차가 지나가야 한다.");
            Assert.That(Railway(boundary).KilledUnitIds.ToArray(), Has.No.Member(actor.EntityId));
            var survived = prepared.Character(EntityKey.Unit_Demon);
            Assert.That(survived.Health.CurrentHealth, Is.EqualTo(actor.Health.CurrentHealth));
            Assert.That(survived.Anchor, Is.EqualTo(moved.Anchor));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void WallDetourMovesAwayInManhattanButCloserInRoute(int partition)
        {
            using var prepared = Create(Scenario.WallDetour);
            using var synchronous = Create(Scenario.WallDetour);
            var actor = prepared.Character(EntityKey.Unit_Demon);

            var move = PublishBoth(prepared, synchronous, partition);
            Assert.That(move.PathMovement, Is.Not.Null);
            var moved = prepared.Character(EntityKey.Unit_Demon);
            // 벽 줄의 틈은 오른쪽 끝에만 있다. 맨해튼 거리는 늘지만 실제 경로 거리는 줄어든다.
            Assert.That(moved.Anchor.Value.X, Is.GreaterThan(actor.Anchor.Value.X));
            Assert.That(Distance(moved.Anchor.Value, Hammer), Is.GreaterThan(Distance(actor.Anchor.Value, Hammer)));
            Assert.That(moved.ActionUse, Is.EqualTo(UnitActionUse.Moved));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void RegenerationEnemyMovesThenAttacksInTheSameEnemyPhase(int partition)
        {
            using var prepared = Create(Scenario.RegenerationFollowup);
            using var synchronous = Create(Scenario.RegenerationFollowup);
            var actor = prepared.Character(EntityKey.Unit_Demon);
            Assert.That(actor.Stats.TraitStatuses.ToArray().Select(status => status.Key), Does.Contain(TraitKey.Regeneration));
            var playersBefore = Players(prepared).ToDictionary(unit => unit.EntityId, unit => unit.Health.CurrentHealth);

            var move = PublishBoth(prepared, synchronous, partition);
            Assert.That(move.PathMovement, Is.Not.Null);
            Assert.That(move.ActionUseSettlement?.ActorId, Is.EqualTo(actor.EntityId));
            Assert.That(prepared.Character(EntityKey.Unit_Demon).ActionUse, Is.EqualTo(UnitActionUse.Moved));

            var attack = PublishBoth(prepared, synchronous, partition);
            Assert.That(attack.Attack.HasValue, Is.True, "같은 적 턴 안에서 이동 뒤 공격이 이어져야 한다.");
            Assert.That(attack.ActionUseSettlement?.ActorId, Is.EqualTo(actor.EntityId));
            var playersAfter = Players(prepared).ToArray();
            Assert.That(playersAfter.Any(unit => unit.Health.CurrentHealth < playersBefore[unit.EntityId]), Is.True);
            // 비치명 조건이다. 피해 5는 체력 10을 넘지 않는다.
            Assert.That(playersAfter.All(unit => !unit.Health.IsDead), Is.True);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void LethalRailEndpointsKeepSafeHold(int partition)
        {
            using var prepared = Create(Scenario.LethalRailHold);
            using var synchronous = Create(Scenario.LethalRailHold);
            var actor = prepared.Character(EntityKey.Unit_Demon);

            var hold = PublishBoth(prepared, synchronous, partition);
            Assert.That(hold.PathMovement, Is.Null, "닿는 모든 칸이 기차 칸이면 대기가 맞다.");
            Assert.That(Railway(hold), Is.Not.Null);
            var held = prepared.Character(EntityKey.Unit_Demon);
            Assert.That(held.Anchor, Is.EqualTo(actor.Anchor));
            Assert.That(held.Health.CurrentHealth, Is.EqualTo(actor.Health.CurrentHealth));
        }

        private static RailwayGimmickActivationSnapshot Railway(CombatCommandTransition result) =>
            result.Events.OfType<CombatGimmickEvent>().Select(e => e.Activation).OfType<RailwayGimmickActivationSnapshot>()
                .SingleOrDefault();

        private static IEnumerable<CharacterCombatFacts> Players(ConfiguredMovementBattle battle) =>
            battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts())
                .Where(c => c.Owner == Owner.Player && c.Area == CharacterRuntimeArea.Field);

        private static int Distance(CellCoord a, CellCoord b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

        private static int RailY(Scenario scenario) => scenario == Scenario.RailPrefix ? 2 : 4;

        // 경계 시험(EnemyAssessmentBoundaryPublicationTests)과 같은 방식으로 준비 실행 한 번과 동기 계산 한 번을 비교한다.
        private static CombatCommandTransition PublishBoth(ConfiguredMovementBattle prepared, ConfiguredMovementBattle synchronous,
            int partition)
        {
            var buffers = prepared.Buffers;
            var direct = synchronous.Buffers;
            var publication = prepared.Publication();
            var directPublication = synchronous.Publication();
            int frame = 0;
            long ticks = 0;
            var service = new CombatAdvanceService(publication.Assembler, publication.Publisher, CombatMovementRules.Validated(Rules),
                () => frame, () => ++ticks, timestampFrequency: 1000L * partition, usePreparation: true);
            var builder = new SystemBuilderHelper();
            builder.AddService<IBufferQuery>(buffers);
            service.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            try
            {
                if (buffers.CombatViewReadSink.Commands.Count == 0)
                {
                    buffers.CombatViewWriteSink.Submit(prepared.EndPlayerTurn());
                    direct.CombatViewWriteSink.Submit(synchronous.EndPlayerTurn());
                }
                EnemyAdvancePreparationTests.AssertBattleValues(buffers, direct);
                var pending = buffers.CombatViewReadSink.Commands.ToArray();
                Assert.That(pending, Is.Not.Empty);
                var snapshot = prepared.CaptureStores();
                var directSnapshot = synchronous.CaptureStores();
                int sequence = buffers.CombatStateReadSink.LatestResultSequence;
                int handled = buffers.CombatStateReadSink.HandledCommandNumber;

                for (; frame < 100_000 && service.ReadPreparationState() != AdvancePreparationState.Ready; frame++)
                {
                    long version = service.PreparationWorkVersion;
                    service.PrepareAdvanceSlice();
                    Assert.That(service.PreparationWorkVersion - version, Is.LessThanOrEqualTo(partition));
                }
                Assert.That(service.ReadPreparationState(), Is.EqualTo(AdvancePreparationState.Ready));
                Assert.That(service.InputCaptureCount, Is.EqualTo(1));
                Assert.That(prepared.CaptureStores(), Is.EqualTo(snapshot), "준비는 저장소를 바꾸지 않는다.");

                var expected = CombatCommandCalculator.AdvanceUtility(directPublication.EnemyStep(), Rules);
                Assert.That(expected.Accepted, Is.True, expected.Reason);
                Assert.That(synchronous.CaptureStores(), Is.EqualTo(directSnapshot));
                Assert.That(directPublication.Publisher.Publish(expected), Is.True);
                Assert.That(service.AdvancePreparedCombat(), Is.EqualTo(PreparedAdvanceResult.Published));
                Assert.That(buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
                Assert.That(buffers.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(buffers.CombatViewReadSink.Commands, Is.EqualTo(pending));
                EnemyAdvancePreparationTests.AssertBattleValues(buffers, direct);
                Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var actualResult), Is.True);
                Assert.That(direct.CombatResultReadSink.TryGetLatest(out var expectedResult), Is.True);
                EnemyAdvancePreparationTests.AssertValues(actualResult, expectedResult, "engagement publication");
                return expected;
            }
            finally
            {
                service.CancelPreparation();
            }
        }

        private static ConfiguredMovementBattle Create(Scenario scenario)
        {
            var config = ConfiguredMovementBattle.Config;
            var database = new CoreIntegrationTestData(config);
            var traits = scenario == Scenario.RegenerationFollowup
                ? new UnitTraitData[] { new RegenerationTraitData(10) }
                : Array.Empty<UnitTraitData>();
            // 근접 1칸, 이동 3(메인 카테고리), 체력 100인 적 하나만 둔다.
            database.SetUnit(new UnitData(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                1, 1, 100, 0, 1, CategoryKey.Main, 5, 1, traits, 1, killReward: UnitKillRewardSpec.Excluded));
            // 아군 공격력을 0으로 두어 아군 위협 항이 판단을 흔들지 않게 한다. 위치·체력·사거리는 기본 시험 데이터 그대로다.
            database.SetUnit(PlayerUnit(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly, CategoryKey.Main, 1));
            database.SetUnit(PlayerUnit(EntityKey.Unit_Cowboy, UnitRole.Regular, UnitOwnerEligibility.Both, CategoryKey.Rider, 2));
            database.SetUnit(PlayerUnit(EntityKey.Unit_DynamiteHammer, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly, CategoryKey.Rider, 2));
            var battle = new ConfiguredMovementBattle(config, new Encounter(scenario), database: database, deploySniper: false);
            try
            {
                battle.Submit(battle.EndPlayerTurn());
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
                var turn = battle.Buffers.CombatStateReadSink.Current.CommandState.Turn;
                Assert.That(turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
                // 기차(2, 3)는 완료 턴 수가 2가 되는 이번 적 턴 끝에 처음 지나간다.
                Assert.That(turn.CompletedControllerTurnCount, Is.EqualTo(1));
                Assert.That(battle.Character(EntityKey.Unit_DynamiteHammer).Anchor, Is.EqualTo(Hammer));
                return battle;
            }
            catch
            {
                battle.Dispose();
                throw;
            }
        }

        private static UnitData PlayerUnit(EntityKey key, UnitRole role, UnitOwnerEligibility owner, CategoryKey category, int range) =>
            new(key, role, owner, 1, 1, 10, 0, range, category, 0, 1, Array.Empty<UnitTraitData>(), 1,
                killReward: UnitKillRewardSpec.Excluded);

        private sealed class Encounter : IBattleInitializationSource
        {
            private readonly Scenario _scenario;
            internal Encounter(Scenario scenario) => _scenario = scenario;

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var enemy = _scenario switch
                {
                    Scenario.RailPrefix => new CellCoord(2, 5),
                    Scenario.LethalRailHold => new CellCoord(7, 5),
                    _ => new CellCoord(2, 4),
                };
                var basis = new ConfiguredMovementBattle.EncounterSource(enemy).Read(database, chapterKey, stageKey);
                var stage = basis.Stage;
                var visibleTo = new[] { Owner.Player, Owner.Enemy };
                var wallCells = _scenario switch
                {
                    // 왼쪽 여섯 칸을 막고 오른쪽 끝 두 칸만 틈으로 남긴다.
                    Scenario.WallDetour => Enumerable.Range(0, 6).Select(x => new CellCoord(x, 3)).ToArray(),
                    // (7,5)에서 이동 3으로 닿는 칸이 모두 y=4 철로가 되도록 둘레를 막는다.
                    Scenario.LethalRailHold => new[] { new CellCoord(6, 5), new CellCoord(6, 3), new CellCoord(7, 3) },
                    _ => Array.Empty<CellCoord>(),
                };
                var walls = wallCells.Select((cell, index) => new StaticTerrainFact(100 + index, cell,
                    StaticTerrainKind.Wall, true, visibleTo)).ToArray();
                bool rail = _scenario == Scenario.RailPrefix || _scenario == Scenario.LethalRailHold;
                var rails = rail
                    ? Enumerable.Range(0, stage.MapWidth).Select(x => new CellCoord(x, RailY(_scenario))).ToArray()
                    : Array.Empty<CellCoord>();
                var gimmicks = rail ? new StageGimmickSpec[] { new RailwayGimmickSpec(2, 3) } : Array.Empty<StageGimmickSpec>();
                var configured = new StageConfig(stage.MapWidth, stage.MapHeight, stage.ReserveCapacity, stage.PlayerZone,
                    stage.TimelineSeed, stage.InitialEnemyPlacements.ToArray(), rails, gimmicks,
                    stage.InitialEnemyStructurePlacements.ToArray());
                return new BattleInitialization(configured, basis.MainAnchor, basis.MainRotation, basis.Units, basis.Structures,
                    basis.Categories, basis.AttackProfiles, new MovementEnvironmentFacts(walls.Cast<MovementTileFact>().ToArray()),
                    new EnvironmentSemanticBindings(walls.Select(wall => wall.Id).ToArray(), Array.Empty<long>(), Array.Empty<EntityId>()),
                    clearRewards: basis.ClearRewards);
            }
        }
    }
}
