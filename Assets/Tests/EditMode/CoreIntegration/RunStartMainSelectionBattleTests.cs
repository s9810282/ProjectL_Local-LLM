using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.PlayerSkill.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using TestDatabase = ProjectL.Tests.CoreIntegration.RunStartMainSelectionTestRuntime.TestDatabase;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class RunStartMainSelectionBattleTests
    {
        private static readonly CellCoord MainCell = new(1, 1);

        [TestCase(EntityKey.Unit_Akimbo, PlayerSkillKey.PowderBlossom, TraitKey.KillChainAttack, 30, 10, 5)]
        [TestCase(EntityKey.Unit_Sniper, PlayerSkillKey.MagicBullet, TraitKey.PiercingFalloff, 10, 5, 2)]
        public void SelectedMain_BattleInitializationUsesOwnDefinitionAndSkill(EntityKey selected,
            PlayerSkillKey expectedSkill, TraitKey expectedTrait, int health, int attack, int defense)
        {
            using var game = CreateBattleRuntime();
            var started = game.Core.RunStart.StartNewRun(1, selected);
            Assert.That(started.Succeeded, Is.True, started.FailureReason.ToString());
            var main = started.Acquired.Single(c => c.Role == CharacterOwnershipRole.Main);

            EnterFirstBattle(game);

            var skill = AssertBattleMain(game, main.Id, selected, expectedSkill, expectedTrait, health, attack, defense);
            DeployMain(game);
            var points = expectedSkill == PlayerSkillKey.MagicBullet
                ? new[] { new CellCoord(3, 1), new CellCoord(3, 2) }
                : Array.Empty<CellCoord>();
            var buffers = game.Buffers;
            buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.ForPlayerSkillForecast(skill.ActorId, points));
            Assert.That(game.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.SkillForecast.CanUse, Is.True, preview.SkillForecast.FailureReason.ToString());

            var used = Submit(game, CombatViewCommandData.ForPlayerSkill(skill.ActorId, points, preview.SkillForecast.Source));

            Assert.That(used.PlayerSkill.SkillKey, Is.EqualTo(expectedSkill));
            Assert.That(used.PlayerSkill.UseSettlement.ActorId, Is.EqualTo(skill.ActorId));
            Assert.That(used.PlayerSkill.Events.OfType<CombatPlayerSkillAttackEvent>().SelectMany(e => e.Attack.Hits), Is.Not.Empty);
            Assert.That(buffers.CombatStateReadSink.Current.CommandState.PlayerSkill.RemainingUses, Is.EqualTo(1));
        }

        [Test]
        public void FreshRun_WithAnotherMain_DropsPreviousMainProgression() =>
            AssertFreshRun(EntityKey.Unit_Sniper, PlayerSkillKey.MagicBullet, TraitKey.PiercingFalloff, 10, 5, 2);

        [Test]
        public void FreshRun_WithSameMain_StartsFreshRun() =>
            AssertFreshRun(EntityKey.Unit_Akimbo, PlayerSkillKey.PowderBlossom, TraitKey.KillChainAttack, 30, 10, 5);

        private static void AssertFreshRun(EntityKey nextMain, PlayerSkillKey expectedSkill, TraitKey expectedTrait,
            int health, int attack, int defense)
        {
            using var game = CreateBattleRuntime();
            var loadout = game.Database.Loadout;
            game.Database.Loadout = new PlayerLoadoutData(loadout.LoadoutKey, loadout.MainCharacterKey,
                loadout.Entries.ToArray(), loadout.StartingHandSize, loadout.StartingRunCurrency, new[] { 3, 0, 1 });
            var original = game.Core.RunStart.StartNewRun(1, EntityKey.Unit_Akimbo);
            Assert.That(original.Succeeded, Is.True, original.FailureReason.ToString());
            var buffers = game.Buffers;
            var roster = game.Core.Characters;
            var before = roster.ReadPlayerSkillUpgradeAvailability();
            Assert.That(before.CanApply, Is.True, before.FailureReason.ToString());
            var applied = roster.TryApplyPlayerSkillUpgrade(before.RunId, before.RosterRevision,
                before.MainId, PlayerSkillUpgradeKey.Bloom);
            Assert.That(applied.Succeeded, Is.True, applied.FailureReason.ToString());
            Assert.That(buffers.CharacterReadSink.TryGetMainState(before.MainId, out var originalState), Is.True);
            buffers.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                Array.Empty<SoulCountChange>(), main: new MainCharacterPersistentChange(before.MainId, originalState,
                    originalState.With(currentHealth: 12, persistentMaxHealth: 39, persistentAttack: 17, persistentDefense: 8))));
            buffers.CurrencyWriteSink.SetAmount(62);
            buffers.ItemInventoryWriteSink.PublishGrant(new ItemInventoryChange(buffers.ItemInventoryReadSink.Current,
                new ItemInventorySnapshot(new Dictionary<int, int> { [1] = 2 })));

            EnterFirstBattle(game);
            var originalBattle = buffers.CombatStateReadSink.Current;
            Assert.That(originalBattle.CommandState.PlayerSkill.UpgradeKey, Is.EqualTo(PlayerSkillUpgradeKey.Bloom));
            Assert.That(game.Core.BattleCompletion.AbandonBattle(), Is.EqualTo(BattleAbandonResult.Abandoned));
            game.Core.BattleCompletion.EndBattle();
            Assert.That(buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.Defeat));

            var started = game.Core.RunStart.StartNewRun(1, nextMain);

            Assert.That(started.Succeeded, Is.True, started.FailureReason.ToString());
            var fresh = roster.ReadPlayerSkillUpgradeAvailability();
            var main = started.Acquired.Single(c => c.Role == CharacterOwnershipRole.Main);
            Assert.That(fresh.RunId.IsEmpty, Is.False);
            Assert.That(fresh.RunId, Is.Not.EqualTo(before.RunId));
            Assert.That(fresh.MainId, Is.EqualTo(main.Id));
            Assert.That(fresh.MainId, Is.Not.EqualTo(before.MainId));
            Assert.That(fresh.Definition.Key, Is.EqualTo(expectedSkill));
            Assert.That(fresh.CurrentBranch, Is.EqualTo(PlayerSkillUpgradeKey.None));
            Assert.That(buffers.RunProgressReadSink.Outcome, Is.EqualTo(RunOutcome.InProgress));
            Assert.That(buffers.RunProgressReadSink.Completion, Is.Null);
            Assert.That(buffers.CombatStateReadSink.Completion, Is.Null);
            Assert.That(buffers.CurrentProgressReadSink.HasMap, Is.False);
            Assert.That(buffers.StageProgressReadSink.GetStages(1), Is.Empty);
            Assert.That(buffers.ItemInventoryReadSink.Current.Counts, Is.Empty);
            Assert.That(buffers.CurrencyReadSink.Amount, Is.EqualTo(137));
            Assert.That(buffers.RunProgressReadSink.StartingHandSize, Is.EqualTo(4));
            Assert.That(buffers.CharacterReadSink.Owned, Is.EqualTo(started.Acquired));
            Assert.That(started.Acquired.Select(c => c.Id).Intersect(original.Acquired.Select(c => c.Id)), Is.Empty);
            Assert.That(started.Acquired.Select(c => c.CharacterKey), Is.EqualTo(new[]
            {
                nextMain, EntityKey.Unit_ShotGun, EntityKey.Unit_Cowboy, EntityKey.Unit_DynamiteHammer
            }));
            Assert.That(buffers.RunDeckReadSink.TryGetCurrent(out var deck), Is.True);
            Assert.That(deck.RunId, Is.EqualTo(fresh.RunId));
            Assert.That(deck.OrderedIds, Is.EqualTo(new[] { main.Id, started.Acquired[3].Id, started.Acquired[1].Id }));

            EnterFirstBattle(game);

            AssertBattleMain(game, main.Id, nextMain, expectedSkill, expectedTrait, health, attack, defense);
        }

        private static PlayerSkillBattleState AssertBattleMain(RunStartMainSelectionTestRuntime game,
            CharacterInstanceId mainId, EntityKey expectedMain, PlayerSkillKey expectedSkill, TraitKey expectedTrait,
            int health, int attack, int defense)
        {
            var buffers = game.Buffers;
            var battle = buffers.CombatStateReadSink.Current;
            Assert.That(battle.Membership.MainId, Is.EqualTo(mainId));
            var skill = battle.CommandState.PlayerSkill;
            Assert.That(skill, Is.Not.Null);
            Assert.That(skill.Definition.Key, Is.EqualTo(expectedSkill));
            Assert.That(skill.Definition.MainCharacterKey, Is.EqualTo(expectedMain));
            Assert.That(skill.MainInstanceId, Is.EqualTo(mainId));
            Assert.That(skill.UpgradeKey, Is.EqualTo(PlayerSkillUpgradeKey.None));
            Assert.That(skill.RemainingUses, Is.EqualTo(2));
            var actor = buffers.CharacterReadSink.Characters.Single(c => c.EntityId == skill.ActorId).ToFacts();
            Assert.That(actor.OwnedId, Is.EqualTo(mainId));
            Assert.That(actor.Owner, Is.EqualTo(Owner.Player));
            Assert.That(actor.OwnershipRole, Is.EqualTo(CharacterOwnershipRole.Main));
            Assert.That(actor.CharacterKey, Is.EqualTo(expectedMain));
            Assert.That((actor.Health.CurrentHealth, actor.Health.MaxHealth, actor.Stats.AttackPower, actor.Stats.PhysicalDefense),
                Is.EqualTo((health, health, attack, defense)));
            Assert.That(actor.Stats.TraitStatuses.ToArray().Select(t => t.Key), Is.EqualTo(new[] { expectedTrait }));
            return skill;
        }

        private static RunStartMainSelectionTestRuntime CreateBattleRuntime()
        {
            var data = new TestDatabase { PlayerActiveSkills = new SkillTable() };
            data.SetUnit(TestDatabase.Main(EntityKey.Unit_Akimbo, 30, 10, 5, 4,
                new UnitTraitData[] { new KillChainAttackTraitData() }));
            data.SetUnit(TestDatabase.Main(EntityKey.Unit_Sniper, 10, 5, 2, 4,
                new UnitTraitData[] { new PiercingFalloffTraitData(25, 4) }));
            data.SetTargetingMethod(new TargetingMethodData(1, TargetingShape.CardinalLine,
                TargetingChoiceMode.Single, TargetingSplash.None, 0));
            data.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1,
                    new[]
                    {
                        new EntityPlacement(EntityKey.Unit_Demon, new CellCoord(2, 1), Rotation4.R180),
                        new EntityPlacement(EntityKey.Unit_Cowboy, new CellCoord(3, 1), Rotation4.R180)
                    },
                    Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                    MainCell, Rotation4.R0, new CellCoord(7, 5), Rotation4.R180, null,
                    playerDeploymentCells: new[] { MainCell })));
            return new RunStartMainSelectionTestRuntime(data);
        }

        private static void EnterFirstBattle(RunStartMainSelectionTestRuntime game)
        {
            game.Buffers.StageProgressWriteSink.RegisterStages(1, new[]
            {
                new StageProgressData(1, 0, 0, 0, new[] { 2 }),
                new StageProgressData(2, 1101, 1, 0, Array.Empty<int>())
            });
            var chapter = game.Core.ChapterProgression.EnterChapter(1);
            Assert.That(chapter.Accepted, Is.True, chapter.FailureReason.ToString());
            var battle = game.Core.BattleEntry.EnterBattle(1, new StageNodeId(2));
            Assert.That(battle.Accepted, Is.True, battle.FailureReason.ToString());
        }

        private static void DeployMain(RunStartMainSelectionTestRuntime game)
        {
            Submit(game, CombatViewCommandData.PlaceMainCharacter(MainCell, Rotation4.R0));
            Submit(game, CombatViewCommandData.EnterCombat());
            Assert.That(game.Core.CombatAdvance.AdvanceCombat(), Is.True);
        }

        private static CombatResult Submit(RunStartMainSelectionTestRuntime game, CombatViewCommandData command)
        {
            int number = game.Buffers.CombatViewWriteSink.Submit(command);
            Assert.That(game.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(game.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.CommandNumber, Is.EqualTo(number));
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        private sealed class SkillTable : IPlayerActiveSkillTableQuery
        {
            private readonly PlayerActiveSkillData[] _rows =
            {
                new(new PlayerSkillDefinition(PlayerSkillKey.PowderBlossom, EntityKey.Unit_Akimbo, 2)),
                new(new PlayerSkillDefinition(PlayerSkillKey.MagicBullet, EntityKey.Unit_Sniper, 2))
            };

            public PlayerActiveSkillData GetPlayerSkill(PlayerSkillKey key) =>
                TryGetPlayerSkill(key, out var data) ? data : throw new KeyNotFoundException();

            public bool TryGetPlayerSkill(PlayerSkillKey key, out PlayerActiveSkillData data)
            {
                data = _rows.FirstOrDefault(row => row.Definition.Key == key);
                return data != null;
            }

            public bool TryGetPlayerSkillForMain(EntityKey main, out PlayerActiveSkillData data)
            {
                data = _rows.FirstOrDefault(row => row.Definition.MainCharacterKey == main);
                return data != null;
            }
        }
    }
}
