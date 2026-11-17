using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.PlayerSkill.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PlayerSkillLifetimeTests
    {
        [Test]
        public void RosterRejectsStaleRequestsWrongMainAndEveryPreparedBattlePhase()
        {
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config) { PlayerActiveSkills = new SkillTable() };
            using var game = new PlayerSkillRuntimeFixture(database);
            var roster = game.Core.Characters;
            var before = roster.ReadPlayerSkillUpgradeAvailability();
            var branch = PlayerSkillUpgradeKey.Bloom;
            Assert.That(before.CanApply, Is.True);
            Assert.That(roster.TryApplyPlayerSkillUpgrade(new RunId(Guid.NewGuid()), before.RosterRevision, before.MainId, branch).FailureReason,
                Is.EqualTo(PlayerSkillUpgradeFailureReason.SourceChanged));
            Assert.That(roster.TryApplyPlayerSkillUpgrade(before.RunId, before.RosterRevision - 1, before.MainId, branch).FailureReason,
                Is.EqualTo(PlayerSkillUpgradeFailureReason.SourceChanged));
            Assert.That(roster.TryApplyPlayerSkillUpgrade(before.RunId, before.RosterRevision, new CharacterInstanceId(999), branch).FailureReason,
                Is.EqualTo(PlayerSkillUpgradeFailureReason.InvalidMain));
            Assert.That(roster.ReadPlayerSkillUpgradeAvailability().RosterRevision, Is.EqualTo(before.RosterRevision));

            game.Enter(2);
            game.AssertBlockedByBattle();
            game.DeployMain();
            game.AssertBlockedByBattle();
            game.Core.BattleCompletion.AbandonBattle();
            game.AssertBlockedByBattle();
        }

        [Test]
        public void BoundSkillRetainsUpgradeBetweenStagesAndResetsOnlyOnRealNewRun()
        {
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config, headquartersHealth: 6)
                { PlayerActiveSkills = new SkillTable() };
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1,
                    new[] { new EntityPlacement(EntityKey.Unit_Demon, new CellCoord(2, 5), Rotation4.R180) },
                    Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                    new CellCoord(1, 0), Rotation4.R0, new CellCoord(0, 1), Rotation4.R180, null,
                    playerDeploymentCells: new[] { CellCoord.Zero, new CellCoord(1, 0) })));
            using var game = new PlayerSkillRuntimeFixture(database);
            var before = game.Core.Characters.ReadPlayerSkillUpgradeAvailability();
            var applied = game.Core.Characters.TryApplyPlayerSkillUpgrade(before.RunId, before.RosterRevision,
                before.MainId, PlayerSkillUpgradeKey.Condemnation);
            Assert.That(applied.Succeeded, Is.True);
            Assert.That(applied.RosterRevision, Is.EqualTo(before.RosterRevision + 1));
            var after = game.Core.Characters.ReadPlayerSkillUpgradeAvailability();
            Assert.That(after.CurrentBranch, Is.EqualTo(PlayerSkillUpgradeKey.Condemnation));
            Assert.That(game.Core.Characters.TryApplyPlayerSkillUpgrade(after.RunId, after.RosterRevision,
                after.MainId, PlayerSkillUpgradeKey.Bloom).FailureReason, Is.EqualTo(PlayerSkillUpgradeFailureReason.AlreadyUpgraded));

            game.Enter(2);
            Assert.That(game.Skill.MainInstanceId, Is.EqualTo(before.MainId));
            Assert.That(game.Skill.UpgradeKey, Is.EqualTo(PlayerSkillUpgradeKey.Condemnation));
            Assert.That(game.Skill.RemainingUses, Is.EqualTo(2));
            game.DeployMain();
            var battleId = game.Buffers.CombatStateReadSink.BattleInstanceId;
            var forecast = game.Forecast();
            Assert.That(forecast.CanUse, Is.True);
            Assert.That(game.Skill.RemainingUses, Is.EqualTo(2));
            var result = game.Submit(CombatViewCommandData.ForPlayerSkill(game.Skill.ActorId, Array.Empty<CellCoord>(), forecast.Source));
            Assert.That(result.PlayerSkill, Is.Not.Null);
            Assert.That(game.Skill.RemainingUses, Is.EqualTo(1));
            Assert.That(game.Forecast().FailureReason, Is.EqualTo(PlayerSkillFailureReason.UsedThisTurn));
            Assert.Throws<InvalidOperationException>(() => game.Core.BattleEntry.EnterBattle(1, new StageNodeId(2)));
            Assert.That(game.Skill.RemainingUses, Is.EqualTo(1));
            Assert.That(game.Buffers.CombatStateReadSink.BattleInstanceId, Is.EqualTo(battleId));

            game.FinishCurrentBattleWithoutChangingRoster();
            game.Enter(3);
            Assert.That(game.Buffers.CombatStateReadSink.BattleInstanceId, Is.GreaterThan(battleId));
            Assert.That(game.Skill.RemainingUses, Is.EqualTo(2));
            Assert.That(game.Skill.UpgradeKey, Is.EqualTo(PlayerSkillUpgradeKey.Condemnation));

            game.Core.BattleCompletion.AbandonBattle();
            game.Core.BattleCompletion.EndBattle();
            Assert.That(game.Core.RunStart.StartNewRun(1).Succeeded, Is.True);
            var fresh = game.Core.Characters.ReadPlayerSkillUpgradeAvailability();
            Assert.That(fresh.RunId, Is.Not.EqualTo(before.RunId));
            Assert.That(fresh.MainId, Is.Not.EqualTo(before.MainId));
            Assert.That(fresh.CurrentBranch, Is.EqualTo(PlayerSkillUpgradeKey.None));
        }

        [Test]
        public void FollowingPlayerTurnReopensOpportunityWithoutRestoringUses()
        {
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config) { PlayerActiveSkills = new SkillTable() };
            using var game = new PlayerSkillRuntimeFixture(database);
            game.Enter(2);
            game.DeployMain();
            var source = game.Forecast().Source;
            game.Submit(CombatViewCommandData.ForPlayerSkill(game.Skill.ActorId, Array.Empty<CellCoord>(), source));
            Assert.That(game.Forecast().FailureReason, Is.EqualTo(PlayerSkillFailureReason.UsedThisTurn));
            game.Submit(CombatViewCommandData.EndPlayerTurn(source.BattleInstanceId, source.ControllerTurnSequence));
            for (int i = 0; i < 30 && game.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase != CombatControllerPhase.Player; i++)
                Assert.That(game.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var next = game.Forecast();
            Assert.That(next.CanUse, Is.True);
            Assert.That(next.Source.ControllerTurnSequence, Is.GreaterThan(source.ControllerTurnSequence));
            Assert.That(game.Skill.RemainingUses, Is.EqualTo(1));
        }

        [Test]
        public void MissingMainMappingIsNormalAndDoesNotBindASkill()
        {
            using var game = new PlayerSkillRuntimeFixture(new CoreIntegrationTestData(ConfiguredMovementBattle.Config));
            var availability = game.Core.Characters.ReadPlayerSkillUpgradeAvailability();
            Assert.That(availability.FailureReason, Is.EqualTo(PlayerSkillUpgradeFailureReason.NoSkillConfigured));
            game.Enter(2);
            Assert.That(game.Skill, Is.Null);
        }

        private sealed class SkillTable : IPlayerActiveSkillTableQuery
        {
            private readonly PlayerActiveSkillData _data = new(new PlayerSkillDefinition(PlayerSkillKey.PowderBlossom, EntityKey.Unit_Akimbo, 2));

            public PlayerActiveSkillData GetPlayerSkill(PlayerSkillKey key) => TryGetPlayerSkill(key, out var result)
                ? result : throw new System.Collections.Generic.KeyNotFoundException();

            public bool TryGetPlayerSkill(PlayerSkillKey key, out PlayerActiveSkillData data)
            {
                data = key == _data.Definition.Key ? _data : null;
                return data != null;
            }

            public bool TryGetPlayerSkillForMain(EntityKey key, out PlayerActiveSkillData data)
            {
                data = key == _data.Definition.MainCharacterKey ? _data : null;
                return data != null;
            }
        }
    }

    internal sealed class PlayerSkillRuntimeFixture : IDisposable
    {
        private readonly FrameworkRuntime _runtime;
        internal IInGameCoreSystemGroupService Core { get; }
        internal IBufferQuery Buffers { get; }
        internal PlayerSkillBattleState Skill => Buffers.CombatStateReadSink.Current.CommandState.PlayerSkill;

        internal PlayerSkillRuntimeFixture(IDataBaseQueryContext database, bool useAuthoredChapter = false)
        {
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create().ExportServices(builder);
            _runtime = new FrameworkRuntime(builder.BuildSystemReader());
            try
            {
                _runtime.InitData(database);
                _runtime.PostBindService();
                Core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
                Buffers = _runtime.GetService(BufferSystemKey.Buffer);
                Assert.That(Core.RunStart.StartNewRun(1).Succeeded, Is.True);
                if (!useAuthoredChapter)
                {
                    Buffers.StageProgressWriteSink.Clear();
                    Buffers.StageProgressWriteSink.RegisterStages(1, new[] {
                        new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                        new StageProgressData(2, 1101, 1, 0, new[] { 3 }),
                        new StageProgressData(3, 1101, 2, 0, Array.Empty<int>()) });
                }
                Assert.That(Core.ChapterProgression.EnterChapter(1).Accepted, Is.True);
            }
            catch
            {
                _runtime.Shutdown();
                throw;
            }
        }

        internal void Enter(int node) => Assert.That(Core.BattleEntry.EnterBattle(1, new StageNodeId(node)).Accepted, Is.True);

        internal CellCoord FindMainPlacement()
        {
            var state = Buffers.CombatStateReadSink.Current.CommandState;
            for (int y = 0; y < state.BoardHeight; y++)
            {
                for (int x = 0; x < state.BoardWidth; x++)
                {
                    var cell = new CellCoord(x, y);
                    Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.MainCharacterPlacement(cell, Rotation4.R0));
                    Assert.That(Core.CombatPreview.ProcessLatestPreview(), Is.True);
                    Assert.That(Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
                    if (preview.Placement.Value.CanPlace) return cell;
                }
            }
            throw new InvalidOperationException("운영 스테이지에서 메인을 배치할 수 있는 칸을 찾지 못했습니다.");
        }

        internal void DeployMain(CellCoord? anchor = null)
        {
            Submit(CombatViewCommandData.PlaceMainCharacter(anchor ?? CellCoord.Zero, Rotation4.R0));
            Submit(CombatViewCommandData.EnterCombat());
            Assert.That(Core.CombatAdvance.AdvanceCombat(), Is.True);
        }

        internal CombatResult Submit(CombatViewCommandData command)
        {
            int number = Buffers.CombatViewWriteSink.Submit(command);
            Assert.That(Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        internal PlayerSkillForecast Forecast(CellCoord[] points = null)
        {
            Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ForPlayerSkillForecast(Skill.ActorId, points ?? Array.Empty<CellCoord>()));
            Assert.That(Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(Buffers.CombatStateReadSink.TryGetLatestPreview(out var result), Is.True);
            return result.SkillForecast;
        }

        internal PlayerSkillSelectionResult Selection(CellCoord[] points)
        {
            Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ForPlayerSkillSelection(Skill.ActorId, points));
            Assert.That(Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(Buffers.CombatStateReadSink.TryGetLatestPreview(out var result), Is.True);
            return result.SkillSelection;
        }

        internal void FinishCurrentBattleWithoutChangingRoster()
        {
            var combat = Buffers.CombatStateReadSink;
            Submit(CombatViewCommandData.Attack(Skill.ActorId, AttackAim.ForCell(new CellCoord(0, 1)),
                combat.BattleInstanceId, combat.Current.CommandState.Turn.ControllerTurnSequence));
            Assert.That(combat.Completion, Is.Not.Null);
            Core.BattleCompletion.EndBattle();
        }

        internal void AssertBlockedByBattle()
        {
            var value = Core.Characters.ReadPlayerSkillUpgradeAvailability();
            Assert.That(value.FailureReason, Is.EqualTo(PlayerSkillUpgradeFailureReason.InBattle));
            var result = Core.Characters.TryApplyPlayerSkillUpgrade(value.RunId, value.RosterRevision, value.MainId, PlayerSkillUpgradeKey.Bloom);
            Assert.That(result.FailureReason, Is.EqualTo(PlayerSkillUpgradeFailureReason.InBattle));
            Assert.That(Buffers.CharacterReadSink.RosterRevision, Is.EqualTo(value.RosterRevision));
        }

        public void Dispose() => _runtime.Shutdown();
    }
}
