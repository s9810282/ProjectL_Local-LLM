using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class MainSpecializationBattleTests
    {
        [TestCase(0, 20)]
        [TestCase(1, 22)]
        public void StationaryNative_UsesOwnedRankInPreviewAndActualHit(int purchases, int damage)
        {
            var key = TraitKey.StationaryFire;
            var config = ConfiguredMovementBattle.Config;
            var database = new CoreIntegrationTestData(config);
            var tables = new TraitGrowthTestRuntime.GrowthTables(CategoryKey.Rider, false);
            tables.ReplaceCategory(new CategoryTraitProgressionData(CategoryKey.Rider,
                Array.Empty<TraitGrowthStepSpec>(), Array.Empty<TraitKey>()));
            tables.ReplaceDefinition(TraitKey.Vanguard, new TraitGrowthDefinitionData(new StationaryFireTraitData(100),
                null, "", "", "", "", new TraitRankValues(key, 120), new TraitRankValues(key, 150)));
            database.CategoryTraitProgressions = tables;
            database.TraitGrowthDefinitions = tables;
            database.SetUnit(new UnitData(EntityKey.Unit_Cowboy, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                1, 1, 10, 0, 3, CategoryKey.Rider, 10, 1, new UnitTraitData[] { new StationaryFireTraitData(50) },
                targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            database.SetUnit(new UnitData(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                1, 1, 100, 0, 2, CategoryKey.Rider, 1, 1, Array.Empty<UnitTraitData>(), targetingMethodKey: 1,
                killReward: UnitKillRewardSpec.Excluded));
            var target = new CellCoord(0, 3);
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular, new CombatStageDetail(42, "field", 1, 0, 1,
                new[] { new EntityPlacement(EntityKey.Unit_Demon, target, Rotation4.R180) }, Array.Empty<EntityKey>(),
                Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters, CellCoord.Zero, Rotation4.R0,
                new CellCoord(7, 5), Rotation4.R180, null)));
            using var battle = new ConfiguredMovementBattle(config, database: database, configureOwned: buffers =>
            {
                if (purchases == 1) TraitGrowthStatSettlementTests.SeedPurchase(buffers, key);
            });
            Assert.That(battle.OwnedStats.TraitRanks[key], Is.EqualTo(purchases + 1));
            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(target)));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Attack.Value.Targets.ToArray().Single().ExpectedDamage, Is.EqualTo(damage));
            battle.Submit(battle.Attack(battle.Actor, target));
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(100 - damage));
        }

        [Test]
        public void NativeAkimboWithoutGrowth_UsesCatalogAmountsAndPreservesUncontrolledDuration()
        {
            using var game = new TraitGrowthTestRuntime(main: true, initialLevel: 1);
            game.Tables.ReplaceCategory(new CategoryTraitProgressionData(CategoryKey.Main,
                Array.Empty<TraitGrowthStepSpec>(), Array.Empty<TraitKey>()));
            game.Tables.ReplaceDefinition(TraitKey.Vanguard, new TraitGrowthDefinitionData(new MoraleBoostTraitData(10, 1),
                null, "", "", "", "", new TraitRankValues(TraitKey.MoraleBoost, 20), new TraitRankValues(TraitKey.MoraleBoost, 30)));
            game.Tables.ReplaceDefinition(TraitKey.TrackingShot, new TraitGrowthDefinitionData(new CleanSweepTraitData(1, 1),
                null, "", "", "", "", new TraitRankValues(TraitKey.CleanSweep, 4), new TraitRankValues(TraitKey.CleanSweep, 7)));
            game.Database.SetUnit(new UnitData(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                1, 1, 10, 0, 2, CategoryKey.Main, 5, 1,
                new UnitTraitData[] { new MoraleBoostTraitData(20, 2), new CleanSweepTraitData(3, 3) },
                targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            var row = game.Core.CharacterStats.ReadOwned().Items.Single(r => r.OwnedId == game.TargetId);
            Assert.That(row.TraitRanks.Values, Is.EqualTo(new[] { 1, 1 }));
            var definition = CoreIntegrationTestData.DatabaseSource(game.Buffers).Read(game.Database, 1, new StageKey(1101));
            var profile = OwnedUnitSpecResolver.BuildProfiles(definition.AttackProfiles, new[] { game.Current },
                definition.TraitGrowth).Single(p => p.OwnedId == game.TargetId);
            var native = profile.Definition.Traits.ToArray();
            var morale = native.OfType<MoraleBoostTraitSpec>().Single();
            var sweep = native.OfType<CleanSweepTraitSpec>().Single();
            Assert.That((morale.DefeatedEnemyBaseAttackPercent, morale.DurationControllerTurnEnds), Is.EqualTo((10, 2)));
            Assert.That((sweep.AttackBonusPerAllyAttack, sweep.DurationControllerTurnEnds), Is.EqualTo((1, 1)));
            game.EnterBattle();
            Assert.That(game.Buffers.CharacterReadSink.Characters.Single(c => c.OwnedId == game.TargetId)
                .Stats.TraitStatuses.ToArray().Select(t => t.Key), Is.EquivalentTo(row.TraitRanks.Keys));
        }
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void CategoryDefaultBattle_SpecializationUsesCapturedProductionBenefit(CategoryKey selected)
        {
            using var game = new TraitGrowthTestRuntime(main: true, initialLevel: 1);
            var production = DataBaseAssetTests.ProductionInGame();
            var tables = (IHasTraitGrowthTables)production;
            game.Database.Config = production.CategoryConfigTableQuery.Config;
            game.Database.TraitGrowthDefinitions = tables.TraitGrowthDefinitions;
            game.Database.CategoryTraitProgressions = tables.CategoryTraitProgressions;
            var start = new CellCoord(2, 1);
            var wall = new CellCoord(3, 1);
            var landing = new CellCoord(4, 1);
            game.Database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1,
                    new[] { new EntityPlacement(EntityKey.Unit_Demon, new CellCoord(2, 3), Rotation4.R180) },
                    Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                    start, Rotation4.R0, new CellCoord(7, 5), Rotation4.R180, null, wallCells: new[] { wall })));
            game.Database.SetUnit(new UnitData(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                1, 1, 20, 0, 3, CategoryKey.Heavy, 2, 1, new UnitTraitData[] { new KnockbackTraitData(1) },
                targetingMethodKey: 1, behavior: new UnitBehaviorSpec(false, true, false), killReward: UnitKillRewardSpec.Excluded));
            Assert.That(game.Service.ApplyMainSpecialization(MainSpecializationTestSupport.Prepare(game, selected)).Succeeded, Is.True);
            game.EnterBattle();
            var buffers = game.Buffers;
            CharacterCombatFacts Actor(EntityKey key) => buffers.CharacterReadSink.Characters.Single(c => c.CharacterKey == key).ToFacts();
            void Submit(CombatViewCommandData command)
            {
                buffers.CombatViewWriteSink.Submit(command);
                Assert.That(game.Core.CombatCommands.ProcessNextCommand(), Is.True);
                Assert.That(buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
                Assert.That(result.Accepted, Is.True, result.Reason);
            }
            Assert.That(Actor(EntityKey.Unit_Cowboy).Stats.TraitStatuses.ToArray().Count(t => t.Key == TraitKey.Leap), Is.EqualTo(1));
            Assert.That(Actor(EntityKey.Unit_Demon).Stats.TraitStatuses.ToArray().Count(t => t.Key == TraitKey.SteadfastWill), Is.EqualTo(1));
            Assert.That(Actor(EntityKey.Unit_Akimbo).Stats.CombatCategoryKey, Is.EqualTo(selected));
            game.Database.Config = ConfiguredMovementBattle.Config;
            Submit(CombatViewCommandData.PlaceMainCharacter(start, Rotation4.R0));
            Submit(CombatViewCommandData.EnterCombat());
            Assert.That(game.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var main = Actor(EntityKey.Unit_Akimbo);
            var path = new MovementPath(new[] { start, wall, landing });
            buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Move(main.EntityId, path));
            Assert.That(game.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Move.Value.CanMove, Is.EqualTo(selected == CategoryKey.Rider));
            if (selected == CategoryKey.Rider)
            {
                Submit(CombatViewCommandData.Move(main.EntityId, path, preview.MovementContext));
                Assert.That(Actor(EntityKey.Unit_Akimbo).Anchor, Is.EqualTo(landing));
                return;
            }

            Submit(CombatViewCommandData.EndPlayerTurn(buffers.CombatStateReadSink.BattleInstanceId,
                buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
            for (int i = 0; i < 12 && Actor(EntityKey.Unit_Akimbo).Health.CurrentHealth == main.Health.CurrentHealth; i++)
                Assert.That(game.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var hit = Actor(EntityKey.Unit_Akimbo);
            Assert.That(hit.Health.CurrentHealth, Is.LessThan(main.Health.CurrentHealth));
            Assert.That(hit.Anchor, Is.EqualTo(selected == CategoryKey.Heavy ? start : new CellCoord(2, 0)));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void CategoryDefaultBattle_ReplacementAndClearReachAttackPreviewAndPublication(bool assigned)
        {
            var config = new CategoryConfigData(3, 4, 3, 2, 0, defaultTraits: assigned
                ? new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Rider] = TraitKey.Knockback } : null);
            var database = new CoreIntegrationTestData(config);
            var tables = new TraitGrowthTestRuntime.GrowthTables(CategoryKey.Rider, false);
            tables.ReplaceCategory(new CategoryTraitProgressionData(CategoryKey.Rider,
                Array.Empty<TraitGrowthStepSpec>(), Array.Empty<TraitKey>()));
            database.CategoryTraitProgressions = tables;
            database.TraitGrowthDefinitions = tables;
            var target = new CellCoord(0, 3);
            database.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1, new[] { new EntityPlacement(EntityKey.Unit_Demon, target, Rotation4.R180) },
                    Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                    CellCoord.Zero, Rotation4.R0, new CellCoord(7, 5), Rotation4.R180, null)));
            database.SetUnit(new UnitData(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                1, 1, 50, 0, 2, CategoryKey.Rider, 1, 1, Array.Empty<UnitTraitData>(), targetingMethodKey: 1,
                killReward: UnitKillRewardSpec.Excluded));
            using var battle = new ConfiguredMovementBattle(config, database: database,
                configureOwned: buffers => Seed(buffers, CategoryKey.Rider));
            foreach (var actor in new[] { battle.Main, battle.Actor, battle.Character(EntityKey.Unit_Demon) })
            {
                var grants = actor.Stats.TraitStatuses.ToArray().OfType<KnockbackTraitStatusSnapshot>().ToArray();
                Assert.That(grants.Select(t => t.DistanceCells), Is.EqualTo(assigned ? new[] { 1 } : Array.Empty<int>()));
            }
            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(target)));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            battle.Submit(battle.Attack(battle.Actor, target));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            var distances = assigned ? new[] { 1 } : Array.Empty<int>();
            Assert.That(preview.Attack.Value.Knockbacks.Select(k => k.RequestedDistance), Is.EqualTo(distances));
            Assert.That(result.Attack.Value.Knockbacks.Select(k => k.RequestedDistance), Is.EqualTo(distances));
            Assert.That(battle.Character(EntityKey.Unit_Demon).Anchor, Is.EqualTo(assigned ? new CellCoord(0, 4) : target));
        }

        [TestCase(CategoryKey.Rider, 6, 7)]
        [TestCase(CategoryKey.Heavy, 1, 10)]
        [TestCase(CategoryKey.Cannon, 7, 11)]
        public void OwnedReadBattleMovementAndAttackUseSelectedClassOnce(CategoryKey category, int distance, int outgoing)
        {
            using var battle = Create(category);
            var main = battle.Main;
            var row = battle.MainOwnedStats;
            Assert.That((row.Stats.CategoryKey, row.Stats.CombatCategoryKey, row.Stats.SpecializationCategory),
                Is.EqualTo((CategoryKey.Main, category, (CategoryKey?)category)));
            Assert.That((row.Stats.MoveRange, main.Stats.MoveRange), Is.EqualTo((distance, distance)));
            Assert.That(main.Stats.IdentityCategoryKey, Is.EqualTo(CategoryKey.Main));
            Assert.That(main.OwnershipRole, Is.EqualTo(CharacterOwnershipRole.Main));
            Assert.That(main.ActionUse, Is.EqualTo(UnitActionUse.Available));
            battle.Move(main, new CellCoord(1, 0));
            main = battle.Main;
            Assert.That(main.ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(main.StageRecord.DistanceMoved, Is.EqualTo(1));
            var enemy = battle.Character(EntityKey.Unit_Demon);
            int before = enemy.Health.CurrentHealth;
            battle.Submit(battle.Attack(main, enemy.Anchor.Value));
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(before - outgoing));
            Assert.That(battle.Main.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(battle.Main.Stats.SpecializationCategory, Is.EqualTo(category));
            battle.Buffers.CombatViewWriteSink.Submit(battle.Attack(battle.Main, enemy.Anchor.Value));
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var rejected), Is.True);
            Assert.That(rejected.Accepted, Is.False);
            battle.Submit(battle.EndPlayerTurn());
        }

        [Test]
        public void ZeroConfiguredDistanceStillDisablesVoluntaryMovement()
        {
            using var battle = Create(CategoryKey.Cannon, cannonDistance: 0);
            Assert.That(battle.Main.Stats.MoveRange, Is.Zero);
            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Move(battle.Main.EntityId,
                new MovementPath(new[] { CellCoord.Zero, new CellCoord(1, 0) })));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.MovementContext, Is.Null);
            Assert.That(preview.Move.Value.FailureReason, Is.EqualTo(MovementFailureReason.VoluntaryMoveDisabled));
            Assert.That(battle.Main.ActionUse, Is.EqualTo(UnitActionUse.Available));
            battle.Submit(battle.EndPlayerTurn());
        }

        [Test]
        public void BattlePublicationCannotLoseOwnedSpecialization()
        {
            using var battle = Create(CategoryKey.Rider);
            var main = battle.Main;
            var s = main.Stats;
            var lost = main.With(stats: new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.AttackRange, s.AttackPower,
                s.PhysicalDefense, s.Priority, s.TraitStatuses.ToArray(), s.Category));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(
                new[] { new CharacterStateChange(main, lost) }, Array.Empty<SoulCountChange>())));
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void FreshBattleCannotLoseOwnedSpecialization()
        {
            using var battle = Create(CategoryKey.Rider);
            var fresh = battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            var membership = battle.Buffers.CombatStateReadSink.Current.Membership;
            battle.Buffers.BattleWriteSink.EndBattle();
            var store = (ICharacterBufferStore)battle.Buffers.CharacterReadSink;
            Assert.DoesNotThrow(() => store.PrepareBattle(fresh, membership));
            var main = fresh.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var s = main.Stats;
            var lost = main.With(stats: new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.AttackRange, s.AttackPower,
                s.PhysicalDefense, s.Priority, s.TraitStatuses.ToArray(), s.Category));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => store.PrepareBattle(fresh.Select(c => c.EntityId == main.EntityId ? lost : c).ToArray(), membership));
            before.AssertUnchanged(battle.Buffers);
        }

        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void AttackFirstDoesNotReopenMovementAndNewRunClearsClass(CategoryKey category)
        {
            using var battle = Create(category);
            var id = battle.Main.OwnedId.Value;
            battle.Submit(battle.Attack(battle.Main, battle.Character(EntityKey.Unit_Demon).Anchor.Value));
            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Move(battle.Main.EntityId,
                new MovementPath(new[] { CellCoord.Zero, new CellCoord(1, 0) })));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Move.Value.CanMove, Is.False);
            Assert.That(battle.Main.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            battle.Core.BattleCompletion.AbandonBattle();
            Assert.That(battle.Buffers.CombatStateReadSink.Completion, Is.Not.Null);
            battle.Core.BattleCompletion.EndBattle();
            Assert.That(battle.Buffers.CharacterReadSink.Owned.Single(o => o.Id == id).SpecializationCategory, Is.EqualTo(category));
            Assert.That(battle.Core.RunStart.StartNewRun(1).Succeeded, Is.True);
            var fresh = battle.Buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main);
            Assert.That(fresh.Id, Is.Not.EqualTo(id));
            Assert.That(fresh.SpecializationCategory, Is.Null);
            Assert.That(battle.Core.CharacterStats.ReadOwned().Items.Single(o => o.OwnedId == fresh.Id).Stats.CombatCategoryKey, Is.EqualTo(CategoryKey.Main));
        }

        private static ConfiguredMovementBattle Create(CategoryKey category, bool enemy = true, int cannonDistance = 7)
        {
            var config = new CategoryConfigData(3, 6, 1, cannonDistance, 0);
            var data = new CoreIntegrationTestData(config);
            data.SetUnit(new UnitData(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                1, 1, 100, 0, 4, CategoryKey.Main, 10, 1, Array.Empty<UnitTraitData>(), targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            data.SetUnit(new UnitData(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                1, 1, 100, 0, 4, CategoryKey.Heavy, 10, 1, Array.Empty<UnitTraitData>(), targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
            return new ConfiguredMovementBattle(config, new ConfiguredMovementBattle.EncounterSource(enemy ? new CellCoord(1, 3) : (CellCoord?)null),
                database: data, configureOwned: buffers => Seed(buffers, category));
        }

        internal static void Seed(IBufferQuery buffers, CategoryKey category)
        {
            // M2는 이미 전직한 보유 입력을 검증한다. 실제 획득·문맥 조정은 M3 이후의 책임이다.
            var main = buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main);
            var store = (ICharacterBufferStore)buffers.CharacterReadSink;
            store.InstallMainSpecialization(store.PrepareMainSpecialization(MainSpecializationRules.Plan(main, category).Change));
        }
    }
}
