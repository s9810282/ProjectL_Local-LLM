using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.InGame;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.Factory.DBFactory.Group;
using ProjectL.Systems.ServiceKeys.DBFactory;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using UnityEditor;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class DataBaseAssetTests
    {
        [Test]
        public void RunGoldFacilities_ActiveRestReferencesAndOrdinaryShopMatchPurchases()
        {
            const string prefix = "Assets/ProjectL/DataBaseSO/InGameGroup/";
            var data = ProductionInGame();
            var rest = (EventStageDetail)data.StageTableQuery.GetStages(1).First(s => s.StageData is EventStageDetail).StageData;
            Assert.That(rest.RecoveryOptionKeys, Is.EqualTo(new[] { 2, 4 }));
            var heal = data.RecoveryOptionTableQuery.GetRecoveryOption(2);
            Assert.That((heal.Cost, heal.EffectKind, heal.HealMode, heal.HealValue),
                Is.EqualTo((5, RecoveryEffectKind.Heal, RecoveryHealMode.PercentOfMax, 30)));
            var growth = data.RecoveryOptionTableQuery.GetRecoveryOption(4);
            Assert.That((growth.Cost, growth.EffectKind, growth.MaxHealthIncrease), Is.EqualTo((10, RecoveryEffectKind.MaxHealthGrowth, 10)));
            Assert.That(data.RecoveryOptionTableQuery.TryGetRecoveryOption(1, out _), Is.True);
            Assert.That(data.RecoveryOptionTableQuery.TryGetRecoveryOption(3, out _), Is.True);
            var group = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(prefix + "InGameGroup.asset"));
            var tables = group.FindProperty("databaseTables");
            Assert.That(Enumerable.Range(0, tables.arraySize).Select(i => tables.GetArrayElementAtIndex(i).objectReferenceValue),
                Has.Member(AssetDatabase.LoadMainAssetAtPath(prefix + "Recovery/RecoveryOptionDataTable.asset")));
            var source = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(prefix + "Stage/Event/Event_Recovery.asset"));
            var options = source.FindProperty("recoveryOptions");
            Assert.That(options.arraySize, Is.EqualTo(2));
            Assert.That(options.GetArrayElementAtIndex(0).objectReferenceValue,
                Is.EqualTo(AssetDatabase.LoadMainAssetAtPath(prefix + "Recovery/Item/RecoveryOption_2.asset")));
            Assert.That(options.GetArrayElementAtIndex(1).objectReferenceValue,
                Is.EqualTo(AssetDatabase.LoadMainAssetAtPath(prefix + "Recovery/Item/RecoveryOption_4.asset")));
            var office = (ShopStageDetail)data.StageTableQuery.GetStages(1).First(s => s.StageData is ShopStageDetail).StageData;
            Assert.That(office.ItemKeys, Is.Empty);
            Assert.That(office.UnitKeys, Is.EqualTo(new[] { EntityKey.Unit_ShotGun }));
            Assert.That(office.UnitRecruitCostGold, Is.EqualTo(600));
        }

        [Test]
        public void RecoverySO_ExportsIndependentGrowthPriceAndMagnitudeWithoutChangingOldData()
        {
            using var fixture = new CoreAuthoringFixture();
            var asset = fixture.Create<RecoveryOptionDataSO>();
            CoreAuthoringFixture.Set(asset, "optionId", 4);
            CoreAuthoringFixture.Set(asset, "effectKind", RecoveryEffectKind.MaxHealthGrowth);
            CoreAuthoringFixture.Set(asset, "cost", 13);
            CoreAuthoringFixture.Set(asset, "maxHealthIncrease", 12);
            var first = (RecoveryOptionData)asset.GetData();
            Assert.DoesNotThrow(first.Validate);
            CoreAuthoringFixture.Set(asset, "cost", 17);
            CoreAuthoringFixture.Set(asset, "maxHealthIncrease", 15);
            var next = (RecoveryOptionData)asset.GetData();
            Assert.That((first.Cost, first.MaxHealthIncrease), Is.EqualTo((13, 12)));
            Assert.That((next.Cost, next.MaxHealthIncrease), Is.EqualTo((17, 15)));
            Assert.That(next, Is.Not.SameAs(first));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void RecoverySO_RejectsNonPositiveGrowth(int amount)
        {
            using var fixture = new CoreAuthoringFixture();
            var asset = fixture.Create<RecoveryOptionDataSO>();
            CoreAuthoringFixture.Set(asset, "effectKind", RecoveryEffectKind.MaxHealthGrowth);
            CoreAuthoringFixture.Set(asset, "maxHealthIncrease", amount);
            Assert.Throws<InvalidOperationException>(() => ((RecoveryOptionData)asset.GetData()).Validate());
        }

        private static readonly (TraitKey Key, int[] First, int[] Second)[] ProductionRanks =
        {
            (TraitKey.Vanguard, new[] { 1, 4, 9 }, null),
            (TraitKey.CleanSweep, new[] { 1, 4, 7 }, null),
            (TraitKey.SoloAction, new[] { 10, 19, 30 }, null),
            (TraitKey.CallOfBlood, new[] { 10, 13, 17 }, null),
            (TraitKey.MoraleBoost, new[] { 10, 20, 30 }, null),
            (TraitKey.PositionSwap, new[] { 0, 10, 18 }, null),
            (TraitKey.Cohesion, new[] { 1, 4, 7 }, new[] { 1, 6, 9 }),
            (TraitKey.Taunt, new[] { 0, 10, 15 }, null),
            (TraitKey.Entrenchment, new[] { 50, 56, 62 }, null),
            (TraitKey.GroundCompaction, new[] { 1, 2, 3 }, null),
            (TraitKey.ReconSquad, new[] { 10, 15, 20 }, null),
            (TraitKey.StationaryFire, new[] { 100, 120, 150 }, null),
            (TraitKey.ArmorBreaker, new[] { 10, 20, 30 }, null),
            (TraitKey.FinishingShot, new[] { 15, 20, 30 }, null),
            (TraitKey.TrackingShot, new[] { 50, 55, 60 }, new[] { 20, 25, 33 }),
            (TraitKey.Hunter, new[] { 3, 5, 8 }, null),
            (TraitKey.FocusedBombardment, new[] { 70, 80, 100 }, null),
        };

        private static System.Collections.Generic.IEnumerable<TestCaseData> ProductionRankCases() =>
            ProductionRanks.SelectMany(row => Enumerable.Range(1, 3).Select(rank => new TestCaseData(
                row.Key, rank, row.First[rank - 1], row.Second?[rank - 1])));

        internal static TraitGrowthCatalog ActiveGrowthCatalog()
        {
            var assets = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            var data = ((IDataBaseQueryContext)DataBaseFactory.Create(assets)).InGameQuery;
            var tables = (IHasTraitGrowthTables)data;
            return TraitGrowthDefinitionAdapter.Build(tables.CategoryTraitProgressions, tables.TraitGrowthDefinitions,
                data.CategoryConfigTableQuery.Config.DefaultTraits);
        }

        [Test]
        public void GrowthProduction_OnlyTheSeventeenRankedKeysCanTrain()
        {
            Assert.That(ActiveGrowthCatalog().Definitions.Where(d => d.IsTrainable).Select(d => d.Key),
                Is.EquivalentTo(ProductionRanks.Select(row => row.Key)
                    .Concat(new[] { TraitKey.Sacrifice, TraitKey.DustScreen })));
        }

        [TestCaseSource(nameof(ProductionRankCases))]
        public void GrowthProduction_EffectiveSpecMatchesRankTuning(TraitKey key, int rank, int first, int? second)
        {
            var spec = RankedCombatTestData.Spec(key, rank);
            (int, int?) amounts = spec switch
            {
                VanguardTraitSpec t => (t.AttackBonus, null),
                CleanSweepTraitSpec t => (t.AttackBonusPerAllyAttack, null),
                SoloActionTraitSpec t => (t.ShieldAmount, null),
                CallOfBloodTraitSpec t => (t.EnemyMaxHealthHealPercent, null),
                MoraleBoostTraitSpec t => (t.DefeatedEnemyBaseAttackPercent, null),
                PositionSwapTraitSpec t => (t.ShieldAmount, null),
                CohesionTraitSpec t => (t.DefensePerAdjacentAlly, t.ShieldAmountPerAlly),
                TauntTraitSpec t => (t.DamageReductionPercent, null),
                EntrenchmentTraitSpec t => (t.DamageReductionPercent, null),
                GroundCompactionTraitSpec t => (t.MovementRangeBonus, null),
                ReconSquadTraitSpec t => (t.IncreasePercent, null),
                StationaryFireTraitSpec t => (t.ExtraDamagePercent, null),
                ArmorBreakerTraitSpec t => (t.DefenseReductionPercentPerHit, null),
                FinishingShotTraitSpec t => (t.AttackThresholdPercent, null),
                TrackingShotTraitSpec t => (t.TargetHealthThresholdPercent, t.ExtraDamagePercent),
                HunterTraitSpec t => (t.AttackPerTargetKill, null),
                FocusedBombardmentTraitSpec t => (t.ExtraDamagePercent, null),
                _ => throw new ArgumentException($"Unexpected ranked spec: {spec.GetType().Name}.")
            };
            Assert.That(spec.TraitKey, Is.EqualTo(key));
            Assert.That(amounts, Is.EqualTo((first, second)));
        }

        internal static IInGameQuery ProductionInGame()
        {
            var group = AssetDatabase.LoadAssetAtPath<InGameGroup>("Assets/ProjectL/DataBaseSO/InGameGroup/InGameGroup.asset");
            Assert.That(group, Is.Not.Null);
            var builder = new SystemBuilderHelper();
            InGameDataBaseFactory.Create(group).ExportServices(builder);
            return builder.BuildSystemReader().ServiceStoreReader.GetService(DataBaseGroupKey.InGame);
        }

        [Test]
        public void CategoryDefaultBattle_ProductionAssignmentsAndGrowthPools()
        {
            var data = ProductionInGame();
            var defaults = data.CategoryConfigTableQuery.Config.DefaultTraits;
            Assert.That(defaults.Keys, Is.EquivalentTo(new[] { CategoryKey.Rider, CategoryKey.Heavy }));
            Assert.That(defaults[CategoryKey.Rider], Is.EqualTo(TraitKey.Leap));
            Assert.That(defaults[CategoryKey.Heavy], Is.EqualTo(TraitKey.SteadfastWill));
            var tables = (IHasTraitGrowthTables)data;
            var catalog = TraitGrowthDefinitionAdapter.Build(tables.CategoryTraitProgressions, tables.TraitGrowthDefinitions, defaults);
            Assert.That(catalog.GetCategory(CategoryKey.Rider).Pool, Has.Member(TraitKey.Leap).And.No.Member(TraitKey.SteadfastWill));
            Assert.That(catalog.GetCategory(CategoryKey.Heavy).Pool, Has.Member(TraitKey.SteadfastWill).And.No.Member(TraitKey.Leap));
            var raw = UnitSpecAdapter.ReadUnit(data.UnitTableQuery.GetUnit(EntityKey.Unit_ShotGun));
            Assert.That(raw.Traits.ToArray().Count(t => t.TraitKey == TraitKey.SteadfastWill), Is.EqualTo(1));
            Assert.That(OwnedUnitSpecResolver.ResolveBase(raw, catalog).Traits.ToArray()
                .Count(t => t.TraitKey == TraitKey.SteadfastWill), Is.EqualTo(1));
        }

        [Test]
        public void GrowthProduction_UsesCommonAndExclusivePoolsWithTwoAcquisitions()
        {
            var data = ProductionInGame();
            var tables = (IHasTraitGrowthTables)data;
            var catalog = TraitGrowthDefinitionAdapter.Build(tables.CategoryTraitProgressions, tables.TraitGrowthDefinitions,
                data.CategoryConfigTableQuery.Config.DefaultTraits);
            var common = new[] { TraitKey.Vanguard, TraitKey.CleanSweep, TraitKey.SoloAction, TraitKey.CallOfBlood,
                TraitKey.MoraleBoost, TraitKey.Disarm, TraitKey.AnkleBreaker };
            var exclusive = new[]
            {
                (CategoryKey.Main, new[] { TraitKey.Knockback }),
                (CategoryKey.Heavy, new[] { TraitKey.PositionSwap, TraitKey.Cohesion, TraitKey.Sacrifice, TraitKey.Taunt,
                    TraitKey.Entrenchment, TraitKey.SteadfastWill, TraitKey.Knockback }),
                (CategoryKey.Rider, new[] { TraitKey.GroundCompaction, TraitKey.DustScreen, TraitKey.Leap, TraitKey.Acceleration, TraitKey.ReconSquad }),
                (CategoryKey.Cannon, new[] { TraitKey.StationaryFire, TraitKey.ArmorBreaker, TraitKey.FinishingShot,
                    TraitKey.TrackingShot, TraitKey.Hunter, TraitKey.FocusedBombardment })
            };
            foreach (var (category, keys) in exclusive)
            {
                var row = catalog.GetCategory(category);
                Assert.That(row.Pool, Is.EquivalentTo(common.Concat(keys)), category.ToString());
                Assert.That(row.Steps.Select(s => (s.StepId, s.UnlockLevel, s.Kind)), Is.EqualTo(new[]
                {
                    (10, 2, TraitGrowthKind.Acquire), (30, 3, TraitGrowthKind.Acquire)
                }));
            }
            Assert.That(catalog.Definitions.Count, Is.EqualTo(25));
            Assert.That(catalog.GetTrait(TraitKey.PositionSwap).Normal, Is.TypeOf<PositionSwapTraitSpec>());
            Assert.That(catalog.GetTrait(TraitKey.ReconSquad).Normal, Is.TypeOf<ReconSquadTraitSpec>());
        }

        [Test]
        public void GrowthProduction_AkimboKeepsPrePresentationTestNativeTrait()
        {
            var unit = UnitSpecAdapter.ReadUnit(ProductionInGame().UnitTableQuery.GetUnit(EntityKey.Unit_Akimbo));
            Assert.That(unit.Traits.ToArray().Select(t => t.TraitKey), Is.EqualTo(new[] { TraitKey.KillChainAttack }));
        }

        [TestCase(null)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        [TestCase(CategoryKey.Cannon)]
        public void GrowthProduction_LevelJumpExcludesOwnershipAndKeepsUnchosenCandidates(CategoryKey? specialization)
        {
            var assets = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            var database = (IDataBaseQueryContext)DataBaseFactory.Create(assets);
            var data = database.InGameQuery;
            var tables = (IHasTraitGrowthTables)data;
            var catalog = TraitGrowthDefinitionAdapter.Build(tables.CategoryTraitProgressions, tables.TraitGrowthDefinitions,
                data.CategoryConfigTableQuery.Config.DefaultTraits);
            var units = data.UnitTableQuery.GetAllUnits().Select(UnitSpecAdapter.ReadUnit)
                .Where(u => u.OwnerEligibility != UnitOwnerEligibility.EnemyOnly &&
                    (u.Role == UnitRole.MainCharacter || u.Role == UnitRole.Regular && u.CategoryKey != CategoryKey.Boss)).ToArray();
            var profiles = BattleDefinitionAdapter.ReadAttackProfiles(database, units);
            TraitGrowthCatalogValidator.Validate(catalog, profiles);
            foreach (var profile in profiles.Where(p => !specialization.HasValue || p.Definition.Role == UnitRole.MainCharacter))
            {
                var basis = profile.Definition;
                var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), basis.Key,
                    basis.Role == UnitRole.MainCharacter ? CharacterOwnershipRole.Main : CharacterOwnershipRole.Sub,
                    3, specializationCategory: specialization);
                var row = EffectiveTraitProgression.Resolve(basis, owned, catalog);
                Assert.That(row.Pool.Distinct().Count(), Is.EqualTo(row.Pool.Count));
                if (specialization.HasValue)
                    Assert.That(row.Pool, Is.EquivalentTo(catalog.GetCategory(CategoryKey.Main).Pool
                        .Concat(catalog.GetCategory(specialization.Value).Pool).Distinct()));
                var native = OwnedUnitSpecResolver.Resolve(basis, owned, catalog).Traits.ToArray().Select(t => t.TraitKey).ToArray();
                var candidates = TraitGrowthRules.Candidates(owned, profile, catalog);
                Assert.That(candidates, Is.Not.Empty);
                Assert.That(candidates.Intersect(native), Is.Empty);
                var sample = TraitGrowthOfferSampler.Sample(candidates, TraitGrowthKind.Acquire, 42);
                var selected = sample[0];
                var offer = new TraitGrowthOffer(new RunId(Guid.NewGuid()), 1, owned.Id, 10, TraitGrowthKind.Acquire,
                    sample.Select(k => new TraitGrowthChoice(k, TraitGrowthKind.Acquire, "trait", "description")).ToArray());
                var request = new TraitGrowthChoiceRequest(offer.RunId, offer.OfferId, owned.Id, 10, selected);
                var result = TraitGrowthRules.PlanChoice(owned, request, offer, profile, catalog);
                Assert.That(result.Succeeded, Is.True);
                var remaining = TraitGrowthRules.Candidates(result.Change.Final, profile, catalog);
                var next = TraitGrowthRules.NextStep(result.Change.Final, row);
                Assert.That(next, Is.Not.Null);
                Assert.That(next.StepId, Is.EqualTo(30));
                Assert.That(remaining, Has.No.Member(selected));
                Assert.That(remaining, Is.SupersetOf(sample.Skip(1)));
                var secondSample = Enumerable.Range(0, 200)
                    .Select(seed => TraitGrowthOfferSampler.Sample(remaining, TraitGrowthKind.Acquire, seed))
                    .First(keys => sample.Count == 1 || keys.Contains(sample[1]));
                var secondOffer = new TraitGrowthOffer(offer.RunId, 2, owned.Id, 30, TraitGrowthKind.Acquire,
                    secondSample.Select(k => new TraitGrowthChoice(k, TraitGrowthKind.Acquire, "trait", "description")).ToArray());
                var secondRequest = new TraitGrowthChoiceRequest(offer.RunId, 2, owned.Id, 30, secondSample[0]);
                var secondResult = TraitGrowthRules.PlanChoice(result.Change.Final, secondRequest, secondOffer, profile, catalog);
                Assert.That(secondResult.Succeeded, Is.True);
                Assert.That(TraitGrowthRules.NextStep(secondResult.Change.Final, row), Is.Null);
                Assert.That(TraitGrowthRules.Candidates(secondResult.Change.Final, profile, catalog), Is.Empty);
            }
        }

        [TestCase(TraitKey.Taunt)]
        [TestCase(TraitKey.Acceleration)]
        public void GrowthProduction_CannonKeepsPreviouslyLegalTraitWithoutReopeningIt(TraitKey previousTrait)
        {
            var assets = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            var database = (IDataBaseQueryContext)DataBaseFactory.Create(assets);
            var data = database.InGameQuery;
            var tables = (IHasTraitGrowthTables)data;
            var catalog = TraitGrowthDefinitionAdapter.Build(tables.CategoryTraitProgressions, tables.TraitGrowthDefinitions,
                data.CategoryConfigTableQuery.Config.DefaultTraits);
            var unit = data.UnitTableQuery.GetAllUnits().Select(UnitSpecAdapter.ReadUnit).First(u =>
                u.CategoryKey == CategoryKey.Cannon && u.Role == UnitRole.Regular && u.OwnerEligibility != UnitOwnerEligibility.EnemyOnly &&
                !u.Traits.ToArray().Any(t => t.TraitKey == previousTrait));
            var profile = BattleDefinitionAdapter.ReadAttackProfiles(database, new[] { unit }).Single();
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), unit.Key, CharacterOwnershipRole.Sub, 3,
                growth: new OwnedTraitGrowth(new[] { new TraitGrowthSelection(10, previousTrait) }));
            TraitGrowthRules.ValidateHistory(owned, profile, catalog);
            Assert.That(OwnedUnitSpecResolver.Resolve(unit, owned, catalog).Traits.ToArray().Select(t => t.TraitKey), Has.Member(previousTrait));
            Assert.That(TraitGrowthRules.NextStep(owned, catalog.GetCategory(CategoryKey.Cannon)).StepId, Is.EqualTo(30));
            var candidates = TraitGrowthRules.Candidates(owned, profile, catalog);
            Assert.That(candidates, Is.Not.Empty);
            Assert.That(candidates, Has.No.Member(previousTrait));
            var sample = TraitGrowthOfferSampler.Sample(candidates, TraitGrowthKind.Acquire, 42);
            var offer = new TraitGrowthOffer(new RunId(Guid.NewGuid()), 1, owned.Id, 30, TraitGrowthKind.Acquire,
                sample.Select(k => new TraitGrowthChoice(k, TraitGrowthKind.Acquire, "trait", "description")).ToArray());
            var request = new TraitGrowthChoiceRequest(offer.RunId, 1, owned.Id, 30, sample[0]);
            var result = TraitGrowthRules.PlanChoice(owned, request, offer, profile, catalog);
            Assert.That(result.Succeeded, Is.True);
            Assert.That(OwnedUnitSpecResolver.Resolve(unit, result.Change.Final, catalog).Traits.ToArray().Select(t => t.TraitKey), Has.Member(previousTrait));
            var fresh = new OwnedCharacterRecord(owned.Id, unit.Key, CharacterOwnershipRole.Sub, 3);
            Assert.That(TraitGrowthRules.Candidates(fresh, profile, catalog), Has.No.Member(previousTrait));
            var invalidOffer = new TraitGrowthOffer(offer.RunId, 2, fresh.Id, 10, TraitGrowthKind.Acquire,
                new[] { new TraitGrowthChoice(previousTrait, TraitGrowthKind.Acquire, "trait", "description") });
            Assert.That(TraitGrowthRules.PlanChoice(fresh, new TraitGrowthChoiceRequest(offer.RunId, 2, fresh.Id, 10, previousTrait),
                invalidOffer, profile, catalog).FailureReason, Is.EqualTo(TraitGrowthFailureReason.NotCandidate));
        }

        [TestCase(CategoryKey.Rider, TraitKey.Leap)]
        [TestCase(CategoryKey.Heavy, TraitKey.SteadfastWill)]
        public void CategoryDefaultBattle_ProductionMainAndStagePrepareSpecializedTraits(CategoryKey selected, TraitKey expected)
        {
            var assets = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            var database = (IDataBaseQueryContext)DataBaseFactory.Create(assets);
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create().ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());
            try
            {
                runtime.InitData(database);
                runtime.PostBindService();
                var core = runtime.GetService(InGameSystemGroupServiceKey.Core);
                var buffers = runtime.GetService(BufferSystemKey.Buffer);
                Assert.That(core.Characters.InitializeRoster(1).Succeeded, Is.True);
                CoreIntegrationTestData.InstallDeck(buffers);
                var roster = (CharacterRosterService)core.Characters;
                var main = buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main);
                Assert.That(main.CharacterKey, Is.EqualTo(database.InGameQuery.PlayerLoadoutTableQuery.GetPlayerLoadout(1).MainCharacterKey));
                var choice = roster.PrepareMainSpecialization(buffers.RunProgressReadSink.RunId, buffers.CharacterReadSink.RosterRevision,
                    main.Id, selected);
                Assert.That(roster.ApplyMainSpecialization(choice).Succeeded, Is.True);
                var source = CoreIntegrationTestData.DatabaseSource(buffers).Read(database, 1, new StageKey(1101));
                Assert.That(buffers.RunDeckReadSink.TryGetCurrent(out var deck), Is.True);
                var owned = deck.OrderedIds.Select(id => buffers.CharacterReadSink.Owned.Single(o => o.Id == id)).ToArray();
                Assert.That(buffers.CharacterReadSink.TryGetMainState(main.Id, out var persistent), Is.True);
                var prepared = BattlePreparationCalculator.Prepare(source, owned, persistent,
                    owned.ToDictionary(o => o.Id, o => buffers.CharacterReadSink.GetSoulCount(o.Id)),
                    core.Characters.PlanInitialDraw(source.Stage.TimelineSeed), Array.Empty<CharacterCombatFacts>(), deck);
                var initial = prepared.Characters.Single(c => c.OwnedId == main.Id);
                Assert.That(initial.Stats.CombatCategoryKey, Is.EqualTo(selected));
                Assert.That(initial.Stats.TraitStatuses.ToArray().Count(t => t.Key == expected), Is.EqualTo(1));
                var enemies = prepared.Characters.Where(c => c.Owner == Owner.Enemy && c.Stats.CombatCategoryKey == CategoryKey.Rider).ToArray();
                Assert.That(enemies, Is.Not.Empty);
                Assert.That(enemies.All(c => c.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Leap)), Is.True);
            }
            finally
            {
                runtime.Shutdown();
            }
        }

        [TestCase(10, 1)]
        [TestCase(17, 3)]
        public void ConfiguredRegenerationUnit_ExportsAmountAndInterval(int amount, int interval)
        {
            using var fixture = new CoreAuthoringFixture();
            var authoring = new RegenerationTraitAuthoring();
            CoreAuthoringFixture.Set(authoring, "healAmount", amount);
            CoreAuthoringFixture.Set(authoring, "healEveryOwnSideTurnEnds", interval);
            var asset = fixture.CreateUnit(EntityKey.Unit_UndeadHeavy, CategoryKey.Heavy, authoring);
            CoreAuthoringFixture.Set(asset, "ownerEligibility", UnitOwnerEligibility.EnemyOnly);
            var data = (UnitData)asset.GetData();
            var regeneration = data.Traits.OfType<RegenerationTraitData>().Single();
            Assert.That((regeneration.HealAmount, regeneration.HealEveryOwnSideTurnEnds), Is.EqualTo((amount, interval)));
            var spec = (RegenerationTraitSpec)TraitSpecAdapter.Read(regeneration);
            Assert.That((spec.HealAmount, spec.HealEveryOwnSideTurnEnds), Is.EqualTo((amount, interval)));
            Assert.DoesNotThrow(() => UnitSpecAdapter.ReadUnit(data));
        }

        [TestCase(5, 1, 0, 1)]
        [TestCase(7, 0, 2, 0)]
        public void ConfiguredRoarUnit_ExportsThresholdAndCounts(int threshold, int perHit, int perOwnEnd, int perOpposingEnd)
        {
            using var fixture = new CoreAuthoringFixture();
            var authoring = new RoarTraitAuthoring();
            CoreAuthoringFixture.Set(authoring, "hitThreshold", threshold);
            CoreAuthoringFixture.Set(authoring, "countPerHit", perHit);
            CoreAuthoringFixture.Set(authoring, "countPerOwnSideTurnEnd", perOwnEnd);
            CoreAuthoringFixture.Set(authoring, "countPerOpposingSideTurnEnd", perOpposingEnd);
            var asset = fixture.CreateUnit(EntityKey.Unit_UndeadHeavy, CategoryKey.Heavy, authoring);
            CoreAuthoringFixture.Set(asset, "ownerEligibility", UnitOwnerEligibility.EnemyOnly);
            var roar = ((UnitData)asset.GetData()).Traits.OfType<RoarTraitData>().Single();
            Assert.That((roar.HitThreshold, roar.CountPerHit, roar.CountPerOwnSideTurnEnd, roar.CountPerOpposingSideTurnEnd),
                Is.EqualTo((threshold, perHit, perOwnEnd, perOpposingEnd)));
            var spec = (RoarTraitSpec)TraitSpecAdapter.Read(roar);
            Assert.That((spec.HitThreshold, spec.CountPerHit, spec.CountPerOwnSideTurnEnd, spec.CountPerOpposingSideTurnEnd),
                Is.EqualTo((threshold, perHit, perOwnEnd, perOpposingEnd)));
        }

        [Test]
        public void RoarData_WithNoPositiveCount_IsRejected()
        {
            Assert.Throws<InvalidOperationException>(() => new RoarTraitData(5, 0, 0, 0).Validate());
            Assert.Throws<InvalidOperationException>(() => new RoarTraitData(5, -1, 1, 0).Validate());
            Assert.Throws<InvalidOperationException>(() => new RoarTraitData(0, 1, 0, 0).Validate());
            Assert.DoesNotThrow(() => new RoarTraitData(5, 0, 0, 1).Validate());
        }

        [TestCase(30, 1)]
        [TestCase(25, 3)]
        public void ConfiguredExposureUnit_ExportsPercentageAndDuration(int percent, int duration)
        {
            using var fixture = new CoreAuthoringFixture();
            var authoring = new HuntersMarkTraitAuthoring();
            CoreAuthoringFixture.Set(authoring, "bonusDamagePercent", percent);
            CoreAuthoringFixture.Set(authoring, "durationAffectedSideTurnEnds", duration);
            var asset = fixture.CreateUnit(EntityKey.Unit_ShotGun, CategoryKey.Heavy, authoring);
            var data = (UnitData)asset.GetData();
            var grant = data.Traits.OfType<HuntersMarkTraitData>().Single();
            Assert.That((grant.BonusDamagePercent, grant.DurationAffectedSideTurnEnds), Is.EqualTo((percent, duration)));
            grant.Validate();
            var spec = (HuntersMarkTraitSpec)TraitSpecAdapter.Read(grant);
            Assert.That((spec.BonusDamagePercent, spec.DurationAffectedSideTurnEnds), Is.EqualTo((percent, duration)));
            Assert.DoesNotThrow(() => UnitSpecAdapter.ReadUnit(data));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ConfiguredGrowthDefinitions_KeepPayloadKeysAndIndependentClocks(bool regeneration)
        {
            using var fixture = new CoreAuthoringFixture();
            var asset = fixture.Create<TraitGrowthDefinitionDataSO>();
            CoreAuthoringFixture.Set(asset, "group", TraitGrowthGroup.Common);
            UnitTraitAuthoring normal = regeneration ? new RegenerationTraitAuthoring() : new HuntersMarkTraitAuthoring();
            UnitTraitAuthoring enhanced = regeneration ? new RegenerationTraitAuthoring() : new HuntersMarkTraitAuthoring();
            string field = regeneration ? "healEveryOwnSideTurnEnds" : "durationAffectedSideTurnEnds";
            string property = regeneration ? "HealEveryOwnSideTurnEnds" : "DurationAffectedSideTurnEnds";
            CoreAuthoringFixture.Set(normal, field, 2);
            CoreAuthoringFixture.Set(enhanced, field, 3);
            CoreAuthoringFixture.Set(asset, "normal", normal);
            CoreAuthoringFixture.Set(asset, "enhanced", enhanced);
            var data = (TraitGrowthDefinitionData)asset.GetData();
            Assert.That(data.Key, Is.EqualTo(regeneration ? TraitKey.Regeneration : TraitKey.HuntersMark));
            foreach (var entry in new[] { (Trait: data.Normal, Clock: 2), (Trait: data.Enhanced, Clock: 3) })
            {
                entry.Trait.Validate();
                Assert.That(entry.Trait.TraitKey, Is.EqualTo(data.Key));
                var spec = TraitSpecAdapter.Read(entry.Trait);
                Assert.That(entry.Trait.GetType().GetProperty(property).GetValue(entry.Trait), Is.EqualTo(entry.Clock));
                Assert.That(spec.GetType().GetProperty(property).GetValue(spec), Is.EqualTo(entry.Clock));
            }
        }

        [TestCase("key")]
        [TestCase("normalClock")]
        [TestCase("enhancedClock")]
        public void ConfiguredGrowthDefinitions_RejectMismatchedKeysAndInvalidClocks(string invalid)
        {
            using var fixture = new CoreAuthoringFixture();
            var asset = fixture.Create<TraitGrowthDefinitionDataSO>();
            CoreAuthoringFixture.Set(asset, "group", TraitGrowthGroup.Common);
            var normal = new HuntersMarkTraitAuthoring();
            UnitTraitAuthoring enhanced = invalid == "key" ? new RegenerationTraitAuthoring() : new HuntersMarkTraitAuthoring();
            if (invalid == "normalClock") CoreAuthoringFixture.Set(normal, "durationAffectedSideTurnEnds", 0);
            if (invalid == "enhancedClock") CoreAuthoringFixture.Set(enhanced, "durationAffectedSideTurnEnds", 0);
            CoreAuthoringFixture.Set(asset, "normal", normal);
            CoreAuthoringFixture.Set(asset, "enhanced", enhanced);
            Assert.Throws<InvalidOperationException>(() => asset.GetData());
        }
    }
}
