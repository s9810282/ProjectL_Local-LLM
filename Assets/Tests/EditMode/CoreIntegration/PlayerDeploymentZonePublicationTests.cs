using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;

namespace ProjectL.Tests.CoreIntegration
{
    // 아군 배치 구역이 DB → StageConfig → CombatPreparation → Buffer → CombatBattleFacts를 거쳐 그대로 판정에 닿는지 확인한다.
    // 칸 구역의 옛 줄 수 속성은 0이라, 중간에서 줄 수로 되돌리면 아군 구역이 통째로 사라진다.
    public sealed class PlayerDeploymentZonePublicationTests
    {
        private static readonly CellCoord MainCell = new CellCoord(1, 0);
        private static readonly CellCoord MiddleCell = new CellCoord(4, 3);
        private static readonly CellCoord BottomRowCell = new CellCoord(2, 0);

        [Test]
        public void ListedCells_ReachDefinitionBufferAndPlacementJudgement()
        {
            using var battle = new Battle(new[] { MainCell, MiddleCell });
            Assert.That(battle.Definition.Stage.PlayerZone.ListedCells, Is.EqualTo(new[] { MainCell, MiddleCell }));
            AssertBufferZone(battle);

            Assert.That(battle.Process(CombatViewCommandData.PlaceMainCharacter(MainCell, Rotation4.R0)).Accepted, Is.True);
            Assert.That(battle.Process(CombatViewCommandData.PlaceUnit(battle.Sub(EntityKey.Unit_Cowboy), MiddleCell, Rotation4.R0)).Accepted,
                Is.True, "옛 줄 밖이지만 아군 칸");
            var rejected = battle.Process(CombatViewCommandData.PlaceUnit(battle.Sub(EntityKey.Unit_Sniper), BottomRowCell, Rotation4.R0));
            Assert.That(rejected.Accepted, Is.False, "옛 줄 안이지만 아군 칸 아님");
            Assert.That(rejected.Reason, Is.EqualTo("InvalidPlacement"));

            // 명령 반영 뒤 Buffer를 다시 만든 뒤에도 구역이 그대로다.
            AssertBufferZone(battle);

            static void AssertBufferZone(Battle b)
            {
                var zone = b.Buffers.CombatStateReadSink.Current.PlayerZone;
                Assert.That(zone.RowDepth, Is.Null);
                Assert.That(zone.ListedCells, Is.EqualTo(new[] { MainCell, MiddleCell }));
            }
        }

        [Test]
        public void EmptyCells_FallBackToFieldPlayerRows()
        {
            using var battle = new Battle(null);
            Assert.That(battle.Definition.Stage.PlayerZone.RowDepth, Is.EqualTo(2));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.PlayerZone.RowDepth, Is.EqualTo(2));

            Assert.That(battle.Process(CombatViewCommandData.PlaceMainCharacter(MainCell, Rotation4.R0)).Accepted, Is.True);
            Assert.That(battle.Process(CombatViewCommandData.PlaceUnit(battle.Sub(EntityKey.Unit_Sniper), BottomRowCell, Rotation4.R0)).Accepted,
                Is.True);
            var rejected = battle.Process(CombatViewCommandData.PlaceUnit(battle.Sub(EntityKey.Unit_Cowboy), MiddleCell, Rotation4.R0));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo("InvalidPlacement"));
        }

        // ConfiguredMovementBattle은 정해진 칸에 배치까지 마치므로, 진입까지만 같은 순서로 거친다.
        private sealed class Battle : IDisposable
        {
            private readonly FrameworkRuntime _runtime;
            private readonly IInGameCoreSystemGroupService _core;
            internal IBufferQuery Buffers { get; }
            internal BattleInitialization Definition { get; }

            internal Battle(CellCoord[] playerDeploymentCells)
            {
                var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config,
                    playerDeploymentCells: playerDeploymentCells);
                var builder = new SystemBuilderHelper();
                BufferSystemFactory.Create().ExportServices(builder);
                InGameCoreSystemFactory.Create().ExportServices(builder);
                _runtime = new FrameworkRuntime(builder.BuildSystemReader());
                try
                {
                    _runtime.InitData(database);
                    _runtime.PostBindService();
                    _core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
                    Buffers = _runtime.GetService(BufferSystemKey.Buffer);
                    Assert.That(_core.Characters.InitializeRoster(1).Succeeded, Is.True);
                    CoreIntegrationTestData.InstallDeck(Buffers);
                    Definition = CoreIntegrationTestData.DatabaseSource(Buffers).Read(database, 1, new StageKey(1101));
                    Buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                        new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                        new StageProgressData(2, 1101, 1, 0, Array.Empty<int>()) });
                    Assert.That(_core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
                    Assert.That(_core.BattleEntry.EnterBattle(1, new StageNodeId(2)).Accepted, Is.True);
                }
                catch { _runtime.Shutdown(); throw; }
            }

            internal EntityId Sub(EntityKey key) =>
                Buffers.CharacterReadSink.Characters.Single(c => c.CharacterKey == key).ToFacts().EntityId;

            internal CombatResult Process(CombatViewCommandData command)
            {
                int number = Buffers.CombatViewWriteSink.Submit(command);
                Assert.That(_core.CombatCommands.ProcessNextCommand(), Is.True);
                Assert.That(Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
                Assert.That(result.CommandNumber, Is.EqualTo(number));
                return result;
            }

            public void Dispose() => _runtime.Shutdown();
        }
    }
}
