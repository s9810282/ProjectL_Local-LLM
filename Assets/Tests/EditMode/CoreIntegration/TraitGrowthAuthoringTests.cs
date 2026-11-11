using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.ArtifactBoard.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.Factory.DBFactory.Group;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys.DBFactory;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitGrowthAuthoringTests
    {
        private readonly List<Object> _objects = new();
        private readonly CoreAuthoringFixture _inputs = new();
        private string _assetFolder;

        [TearDown]
        public void Cleanup()
        {
            if (_assetFolder != null)
            {
                AssetDatabase.DeleteAsset(_assetFolder);
                _assetFolder = null;
            }
            foreach (var item in _objects)
                if (item != null && !AssetDatabase.Contains(item)) Object.DestroyImmediate(item);
            _objects.Clear();
            _inputs.Dispose();
        }

        [Test]
        public void LegacyEnhancedAloneDoesNotExposeTraining()
        {
            var definition = Definition(new KnockbackTraitAuthoring(), new KnockbackTraitAuthoring());
            var data = (TraitGrowthDefinitionData)definition.GetData();
            Assert.That(data.IsTrainable, Is.False);
        }

        [Test]
        public void RankedAuthoring_ValidatesBothHalvesAndPublishesIndependentFinalAmounts()
        {
            var normal = new VanguardTraitAuthoring();
            Set(normal, "attackBonus", 1);
            var so = Definition(normal);
            var two = new VanguardRankAuthoring();
            Set(two, "attackBonus", 4);
            Set(so, "rank2", two);
            var rank2Only = (TraitGrowthDefinitionData)so.GetData();
            Assert.That(rank2Only.Rank2.Amount, Is.EqualTo(4));
            Assert.That(rank2Only.Rank3, Is.Null);
            var three = new VanguardRankAuthoring();
            Set(so, "rank3", three);
            Assert.Throws<InvalidOperationException>(() => so.GetData());
            Set(three, "attackBonus", 9);
            var data = (TraitGrowthDefinitionData)so.GetData();
            Assert.That(data.IsTrainable, Is.True);
            Assert.That(((VanguardTraitData)data.Normal).AttackBonus, Is.EqualTo(1));
            Assert.That((data.Rank2.Amount, data.Rank3.Amount), Is.EqualTo((4, 9)));
            Set(normal, "attackBonus", 2);
            data = (TraitGrowthDefinitionData)so.GetData();
            Assert.That(((VanguardTraitData)data.Normal).AttackBonus, Is.EqualTo(2));
            Assert.That((data.Rank2.Amount, data.Rank3.Amount), Is.EqualTo((4, 9)));
            var wrong = new HunterRankAuthoring();
            Set(wrong, "attackPerTargetKill", 5);
            Set(so, "rank2", wrong);
            Assert.Throws<InvalidOperationException>(() => so.GetData());
            Set(so, "rank2", null);
            Assert.Throws<InvalidOperationException>(() => so.GetData());
        }

        [Test]
        public void RankFields_SurviveUnitySerializationWithoutOverwritingNormal()
        {
            var normal = new TauntTraitAuthoring();
            Set(normal, "radiusCells", 3);
            Set(normal, "damageReductionPercent", 2);
            var so = Definition(normal);
            var two = new TauntRankAuthoring();
            var three = new TauntRankAuthoring();
            Set(two, "damageReductionPercent", 10);
            Set(three, "damageReductionPercent", 15);
            Set(so, "rank2", two);
            Set(so, "rank3", three);
            string folder = "__TraitGrowthTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", folder);
            _assetFolder = "Assets/" + folder;
            string path = _assetFolder + "/Ranked.asset";
            AssetDatabase.CreateAsset(so, path);
            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(so);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var data = (TraitGrowthDefinitionData)AssetDatabase.LoadAssetAtPath<TraitGrowthDefinitionDataSO>(path).GetData();
            var spec = (TauntTraitSpec)TraitSpecAdapter.Read(data.Normal);
            Assert.That((spec.RadiusCells, spec.DamageReductionPercent, data.Rank2.Amount, data.Rank3.Amount),
                Is.EqualTo((3, 2, 10, 15)));
        }

        [Test]
        public void SOConversion_CopiesStepsAndConvertsReferencesToKeys()
        {
            var trait = Definition(new KnockbackTraitAuthoring(), new KnockbackTraitAuthoring());
            var row = Row(CategoryKey.Heavy, new[] { Step(10, 2), Step(20, 2, TraitGrowthKind.Enhance) }, trait);
            var first = (CategoryTraitProgressionData)row.GetData();
            var second = (CategoryTraitProgressionData)row.GetData();
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(first.Steps.Select(s => s.StepId), Is.EqualTo(new[] { 10, 20 }));
            Assert.That(first.Pool, Is.EqualTo(new[] { TraitKey.Knockback }));
            Set(row, "steps", new List<TraitGrowthStepAuthoring>());
            Set(row, "pool", new List<TraitGrowthDefinitionDataSO>());
            Assert.That(first.Steps.Count, Is.EqualTo(2));
            Assert.That(first.Pool.Count, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() => ((IList<TraitKey>)first.Pool)[0] = TraitKey.Pull);
            var normal = (TraitGrowthDefinitionData)trait.GetData();
            var again = (TraitGrowthDefinitionData)trait.GetData();
            Assert.That(again, Is.Not.SameAs(normal));
            Assert.That(again.Normal, Is.Not.SameAs(normal.Normal));
            Assert.That(again.Enhanced, Is.Not.SameAs(normal.Enhanced));
            var catalog = TraitGrowthDefinitionAdapter.Build(
                new CategoryTraitProgressionTable(Table<CategoryTraitProgressionDataTableSO>(Rows(row))),
                new TraitGrowthDefinitionTable(Table<TraitGrowthDefinitionDataTableSO>(trait)));
            var spec = catalog.GetTrait(TraitKey.Knockback);
            Assert.That(spec.Name, Is.EqualTo("normal"));
            Assert.That(spec.EnhancedName, Is.EqualTo("enhanced"));
            Assert.That(spec.Normal.TraitKey, Is.EqualTo(TraitKey.Knockback));
            Assert.That(spec.Enhanced.TraitKey, Is.EqualTo(TraitKey.Knockback));
            AssertPureData(first);
            AssertPureData(normal);
        }

        [Test]
        public void Registration_AbsentPairIsDisabledAndOwningGroupExposesPresentPair()
        {
            var absent = CreateDatabase(Group());
            var disabled = (IHasTraitGrowthTables)absent;
            Assert.That(disabled.CategoryTraitProgressions, Is.Null);
            Assert.That(disabled.TraitGrowthDefinitions, Is.Null);
            Assert.That(TraitGrowthDefinitionAdapter.Build(null, null), Is.SameAs(TraitGrowthCatalog.Disabled));
            var row = Row(CategoryKey.Heavy, new[] { Step(1, 2) }, SafeDefinitions());
            var present = CreateDatabase(Group(Table<CategoryTraitProgressionDataTableSO>(Rows(row)),
                Table<TraitGrowthDefinitionDataTableSO>(Pool(row))));
            var owning = (IHasTraitGrowthTables)present;
            Assert.That(owning.CategoryTraitProgressions.GetAll().Count, Is.EqualTo(4));
            Assert.That(owning.CategoryTraitProgressions.Get(CategoryKey.Main).Steps, Is.Empty);
            Assert.That(owning.TraitGrowthDefinitions.GetAll().Count, Is.EqualTo(5));
            Assert.That(owning.TraitGrowthDefinitions.TryGet(TraitKey.SoloAction, out var definition), Is.True);
            Assert.That(definition.Key, Is.EqualTo(TraitKey.SoloAction));
            Assert.That(owning.CategoryTraitProgressions.TryGet(CategoryKey.Boss, out _), Is.False);
            Assert.Throws<KeyNotFoundException>(() => owning.CategoryTraitProgressions.Get(CategoryKey.Boss));
            Assert.Throws<KeyNotFoundException>(() => owning.TraitGrowthDefinitions.Get(TraitKey.Pull));
            Assert.That(TraitGrowthDefinitionAdapter.Build(owning.CategoryTraitProgressions, owning.TraitGrowthDefinitions)
                .GetCategory(CategoryKey.Heavy).Steps[0].StepId, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Registration_RejectsOneMissingTable(bool missingDefinitions)
        {
            Assert.DoesNotThrow(() => CreateDatabase(Group()));
            DatabaseTable table = missingDefinitions
                ? Table<CategoryTraitProgressionDataTableSO>(Rows())
                : Table<TraitGrowthDefinitionDataTableSO>();
            var failure = Assert.Throws<InvalidOperationException>(() => InGameDataBaseFactory.Create(Group(table)));
            Assert.That(failure.Message, Does.Contain("함께 등록"));
        }

        [Test]
        public void Registration_RejectsDuplicateCategoryAndMissingRequiredCategory()
        {
            Assert.DoesNotThrow(() => CreateDatabase(Group()));
            var categories = Rows();
            var duplicate = Assert.Throws<InvalidOperationException>(() => InGameDataBaseFactory.Create(Group(
                Table<CategoryTraitProgressionDataTableSO>(categories.Concat(new[] { categories[0] }).ToArray()),
                Table<TraitGrowthDefinitionDataTableSO>())));
            Assert.That(duplicate.Message, Does.Contain("CategoryTraitProgression").And.Contain("중복"));
            var missing = Assert.Throws<InvalidOperationException>(() => InGameDataBaseFactory.Create(Group(
                Table<CategoryTraitProgressionDataTableSO>(categories.Take(3).ToArray()),
                Table<TraitGrowthDefinitionDataTableSO>())));
            Assert.That(missing.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(missing.InnerException.Message, Does.Contain("Main/Heavy/Rider/Cannon"));
        }

        [Test]
        public void Registration_RejectsDuplicateTraitAndUnregisteredPoolReference()
        {
            Assert.DoesNotThrow(() => CreateDatabase(Group()));
            var definitions = SafeDefinitions();
            var duplicate = Assert.Throws<InvalidOperationException>(() => InGameDataBaseFactory.Create(Group(
                Table<CategoryTraitProgressionDataTableSO>(Rows()),
                Table<TraitGrowthDefinitionDataTableSO>(definitions[0], definitions[0]))));
            Assert.That(duplicate.Message, Does.Contain("TraitGrowthDefinition").And.Contain("중복"));
            var row = Row(CategoryKey.Heavy, Array.Empty<TraitGrowthStepAuthoring>(), definitions[0]);
            var missing = Assert.Throws<InvalidOperationException>(() => InGameDataBaseFactory.Create(Group(
                Table<CategoryTraitProgressionDataTableSO>(Rows(row)), Table<TraitGrowthDefinitionDataTableSO>())));
            Assert.That(missing.InnerException, Is.TypeOf<ArgumentException>());
            Assert.That(missing.InnerException.Message, Does.Contain("후보에 대응하는 특성 정의"));
        }

        [Test]
        public void Registration_UsesSharedCandidateSufficiencyBeforeReturningDatabase()
        {
            var validDefinitions = SafeDefinitions();
            var validRow = Row(CategoryKey.Heavy, new[] { Step(1, 2) }, validDefinitions);
            Assert.DoesNotThrow(() => CreateDatabase(Group(Table<CategoryTraitProgressionDataTableSO>(Rows(validRow)),
                Table<TraitGrowthDefinitionDataTableSO>(validDefinitions))));
            var definitions = validDefinitions.Take(1).ToArray();
            var row = Row(CategoryKey.Heavy, new[] { Step(1, 2), Step(2, 3) }, definitions);
            var failure = Assert.Throws<InvalidOperationException>(() => InGameDataBaseFactory.Create(Group(
                Table<CategoryTraitProgressionDataTableSO>(Rows(row)), Table<TraitGrowthDefinitionDataTableSO>(definitions))));
            Assert.That(failure.Message, Does.Contain("Heavy").And.Contain("Unit_Shielder").And.Contain("Step=2")
                .And.Contain("Path=[SoloAction]").And.Contain("Candidates=0").And.Contain("Required=1"));
        }

        [Test]
        public void SOConversion_RejectsMissingOrWrongKeyPayloadAndNullPoolReference()
        {
            Assert.Throws<InvalidOperationException>(() => Definition(null).GetData());
            Assert.Throws<InvalidOperationException>(() => Definition(new KnockbackTraitAuthoring(), new HuntersMarkTraitAuthoring()).GetData());
            Assert.DoesNotThrow(() => Definition(new KnockbackTraitAuthoring()).GetData());
            Assert.Throws<InvalidOperationException>(() => Row(CategoryKey.Heavy, Array.Empty<TraitGrowthStepAuthoring>(),
                new TraitGrowthDefinitionDataSO[] { null }).GetData());
        }

        [Test]
        public void Serialization_RoundTripPreservesReferencesOrderAndBothEffectForms()
        {
            string name = "__TraitGrowthTests_" + Guid.NewGuid().ToString("N");
            AssetDatabase.CreateFolder("Assets", name);
            _assetFolder = "Assets/" + name;
            var trait = Definition(new KnockbackTraitAuthoring(), new KnockbackTraitAuthoring());
            Set(GetField(trait, "enhanced"), "distanceCells", 3);
            var category = Row(CategoryKey.Main, new[] { Step(20, 2), Step(10, 2, TraitGrowthKind.Enhance) }, trait);
            string definitionPath = _assetFolder + "/Trait.asset";
            string categoryPath = _assetFolder + "/Category.asset";
            AssetDatabase.CreateAsset(trait, definitionPath);
            AssetDatabase.CreateAsset(category, categoryPath);
            AssetDatabase.SaveAssets();
            Resources.UnloadAsset(category);
            Resources.UnloadAsset(trait);
            AssetDatabase.ImportAsset(categoryPath, ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<CategoryTraitProgressionDataSO>(categoryPath);
            var data = (CategoryTraitProgressionData)reloaded.GetData();
            var forms = (TraitGrowthDefinitionData)AssetDatabase.LoadAssetAtPath<TraitGrowthDefinitionDataSO>(definitionPath).GetData();
            Assert.That(data.Category, Is.EqualTo(CategoryKey.Main));
            Assert.That(data.Steps.Select(s => s.StepId), Is.EqualTo(new[] { 20, 10 }));
            Assert.That(data.Pool.Single(), Is.EqualTo(TraitKey.Knockback));
            Assert.That(((KnockbackTraitData)forms.Normal).DistanceCells, Is.EqualTo(1));
            Assert.That(((KnockbackTraitData)forms.Enhanced).DistanceCells, Is.EqualTo(3));
        }

        private T New<T>() where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            _objects.Add(value);
            return value;
        }

        [Test]
        public void QueryPayload_NestedDistanceArraysCannotMutateValidatedDefinitions()
        {
            var normal = new DistanceDamageScaleTraitAuthoring();
            var enhanced = new DistanceDamageScaleTraitAuthoring();
            Set(normal, "percentByDistance", new[] { 100, 100 });
            Set(enhanced, "percentByDistance", new[] { 150, 150 });
            var query = new TraitGrowthDefinitionTable(Table<TraitGrowthDefinitionDataTableSO>(Definition(normal, enhanced)));
            var data = query.Get(TraitKey.DistanceDamageScale);
            foreach (var form in new[] { data.Normal, data.Enhanced })
            {
                var percents = ((DistanceDamageScaleTraitData)form).PercentByDistance;
                Assert.Throws<NotSupportedException>(() => ((IList<int>)percents)[0] = -1);
            }
            Assert.That(((DistanceDamageScaleTraitData)query.GetAll().Single().Normal).PercentByDistance[0], Is.EqualTo(100));
            Assert.That(((DistanceDamageScaleTraitData)query.GetAll().Single().Enhanced).PercentByDistance[0], Is.EqualTo(150));
            Assert.DoesNotThrow(() => TraitSpecAdapter.Read(data.Normal));
            Assert.DoesNotThrow(() => TraitSpecAdapter.Read(data.Enhanced));
        }


        private TraitGrowthDefinitionDataSO Definition(UnitTraitAuthoring normal, UnitTraitAuthoring enhanced = null)
        {
            var result = New<TraitGrowthDefinitionDataSO>();
            Set(result, "group", TraitGrowthGroup.Common);
            Set(result, "normal", normal);
            Set(result, "enhanced", enhanced);
            Set(result, "displayName", "normal");
            Set(result, "description", "description");
            Set(result, "enhancedName", "enhanced");
            Set(result, "enhancedDescription", "enhanced description");
            return result;
        }

        [Test]
        public void CategoryDefaults_RequireRegisteredDefinitionsEvenWithoutGrowth()
        {
            var group = Group();
            var config = _objects.OfType<CategoryConfigDataSO>().Single();
            var property = new SerializedObject(config).FindProperty("riderDefaultTrait");
            Assert.That(property, Is.Not.Null, "Rider default must be editable in category authoring.");
            Set(config, "riderDefaultTrait", Definition(new LeapTraitAuthoring()));
            Assert.Throws<InvalidOperationException>(() => CreateDatabase(group));
            Set(config, "riderDefaultTrait", null);
            Assert.DoesNotThrow(() => CreateDatabase(group));
        }

        [Test]
        public void CategoryDefaults_FilterAuthoringChoicesBeforeRegistration()
        {
            var options = SafeDefinitions().Take(1).ToArray();
            var group = Group(Table<CategoryTraitProgressionDataTableSO>(Rows(Row(CategoryKey.Heavy,
                new[] { Step(10, 2) }, options))), Table<TraitGrowthDefinitionDataTableSO>(options));
            Assert.DoesNotThrow(() => CreateDatabase(group));
            var config = _objects.OfType<CategoryConfigDataSO>().Single();
            Set(config, "heavyDefaultTrait", options[0]);
            Assert.Throws<InvalidOperationException>(() => CreateDatabase(group));
        }

        [Test]
        public void CategoryDefaults_ReplaceAndClearReferencesWithoutChangingCapturedData()
        {
            var config = New<CategoryConfigDataSO>();
            var leap = Definition(new LeapTraitAuthoring());
            var knockback = new KnockbackTraitAuthoring();
            Set(knockback, "distanceCells", 3);
            var replacement = Definition(knockback);
            var rows = new CategoryTraitProgressionTable(Table<CategoryTraitProgressionDataTableSO>(Rows()));
            var definitions = new TraitGrowthDefinitionTable(Table<TraitGrowthDefinitionDataTableSO>(leap, replacement));
            TraitGrowthCatalog Capture() => TraitGrowthDefinitionAdapter.Build(rows, definitions,
                ((CategoryConfigData)config.GetData()).DefaultTraits);

            Set(config, "riderDefaultTrait", leap);
            Set(config, "heavyDefaultTrait", leap);
            var original = Capture();
            Assert.That(original.GetDefaultTrait(CategoryKey.Heavy).TraitKey, Is.EqualTo(TraitKey.Leap));
            Set(config, "riderDefaultTrait", replacement);
            Assert.That(((KnockbackTraitSpec)Capture().GetDefaultTrait(CategoryKey.Rider)).DistanceCells, Is.EqualTo(3));
            Set(config, "riderDefaultTrait", null);
            Assert.That(Capture().GetDefaultTrait(CategoryKey.Rider), Is.Null);
            Assert.That(original.GetDefaultTrait(CategoryKey.Rider).TraitKey, Is.EqualTo(TraitKey.Leap));
        }

        [TestCase(TraitKey.Leap)]
        [TestCase(TraitKey.ThornField)]
        [TestCase(TraitKey.MoveAttackDiscount)]
        public void CategoryDefaults_RejectMissingOrUnavailableDefinition(TraitKey unavailable)
        {
            var rows = new CategoryTraitProgressionTable(Table<CategoryTraitProgressionDataTableSO>(Rows()));
            var definitions = new TraitGrowthDefinitionTable(Table<TraitGrowthDefinitionDataTableSO>());
            Assert.Throws<InvalidOperationException>(() => TraitGrowthDefinitionAdapter.Build(rows, definitions,
                new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Rider] = unavailable }));
        }

        [TestCase(UnitOwnerEligibility.EnemyOnly)]
        [TestCase(UnitOwnerEligibility.PlayerOnly)]
        public void CategoryDefaults_ValidateTargetingWithoutGrowthSteps(UnitOwnerEligibility owner)
        {
            var pull = Definition(new PullTraitAuthoring());
            var group = Group(Table<CategoryTraitProgressionDataTableSO>(Rows()), Table<TraitGrowthDefinitionDataTableSO>(pull));
            var unit = (UnitDataSO)((List<DatabaseItem>)GetField(_objects.OfType<UnitDataTableSO>().Single(), "databaseItems")).Single();
            Set(unit, "ownerEligibility", owner);
            var config = _objects.OfType<CategoryConfigDataSO>().Single();
            Assert.DoesNotThrow(() => CreateDatabase(group));
            Set(config, "heavyDefaultTrait", pull);
            var failure = Assert.Throws<InvalidOperationException>(() => CreateDatabase(group));
            Assert.That(failure.ToString(), Does.Contain("끌어오기"));
        }

        [Test]
        public void CategoryDefaultBattle_CapturesNativeAndSpecializedDependencyVariantsOnce()
        {
            var defaults = new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Rider] = TraitKey.Leap };
            var db = new CoreIntegrationTestData(new CategoryConfigData(3, 4, 3, 2, 0, defaultTraits: defaults));
            NecromancyTraitAuthoring Summons(EntityKey key)
            {
                var target = _inputs.CreateUnit(key, CategoryKey.Rider);
                Set(target, "ownerEligibility", UnitOwnerEligibility.EnemyOnly);
                db.SetUnit((UnitData)target.GetData());
                var trait = new NecromancyTraitAuthoring();
                Set(trait, "summonedUnit", target);
                return trait;
            }
            var normal = Summons(EntityKey.Unit_Gravekeeper);
            var enhanced = Summons(EntityKey.Unit_UndeadHeavy);
            var summon = Definition(normal, enhanced);
            db.SetUnit(new UnitData(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                1, 1, 10, 0, 2, CategoryKey.Main, 1, 1, new UnitTraitData[] { normal.ToData() }, targetingMethodKey: 1,
                killReward: UnitKillRewardSpec.Excluded));
            db.CategoryTraitProgressions = new CategoryTraitProgressionTable(Table<CategoryTraitProgressionDataTableSO>(Rows(
                Row(CategoryKey.Main, new[] { Step(10, 2, TraitGrowthKind.Enhance) }, summon))));
            db.TraitGrowthDefinitions = new TraitGrowthDefinitionTable(Table<TraitGrowthDefinitionDataTableSO>(
                summon, Definition(new LeapTraitAuthoring())));
            // 보류 전에 기록된 강화 이력의 참조만 검증한다. 소환 명령은 실행하지 않는다.
            var owned = new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main, 2,
                growth: OwnedTraitGrowth.Empty.Append(new TraitGrowthSelection(10, TraitKey.Necromancy)), specializationCategory: CategoryKey.Rider);
            var init = BattleDefinitionAdapter.Read(db, 1, 1101, new[] { owned });
            Assert.That(init.Units.Select(u => u.Key), Is.EquivalentTo(new[] { EntityKey.Unit_Akimbo, EntityKey.Unit_Demon,
                EntityKey.Unit_Gravekeeper, EntityKey.Unit_UndeadHeavy }));
            var raw = init.Units.Single(u => u.Key == EntityKey.Unit_UndeadHeavy);
            Assert.That(raw.Traits.Length, Is.Zero);
            var profiles = OwnedUnitSpecResolver.BuildProfiles(BattleDefinitionAdapter.ReadAttackProfiles(db, init.Units), new[] { owned }, init.TraitGrowth);
            Assert.That(profiles.Single(p => p.Definition.Key == raw.Key).Definition.Traits.ToArray().Select(t => t.TraitKey),
                Is.EqualTo(new[] { TraitKey.Leap }));
            db.Config = new CategoryConfigData(3, 4, 3, 2, 0, defaultTraits:
                new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Rider] = TraitKey.Necromancy });
            Assert.That(Assert.Throws<InvalidOperationException>(() => BattleDefinitionAdapter.Read(db, 1, 1101, new[] { owned })).Message,
                Does.Contain("DeferredUnitTurnTiming"));
        }

        private TraitGrowthDefinitionDataSO[] SafeDefinitions()
        {
            var solo = new SoloActionTraitAuthoring();
            Set(solo, "shieldAmount", 1);
            Set(solo, "radiusCells", 1);
            Set(solo, "requiredOccupiedNeighborCount", 0);
            Set(solo, "resetEveryControllerTurnEnds", 1);
            var vanguard = new VanguardTraitAuthoring();
            Set(vanguard, "attackBonus", 2);
            Set(vanguard, "durationControllerTurnEnds", 1);
            var tracking = new TrackingShotTraitAuthoring();
            Set(tracking, "targetHealthThresholdPercent", 50);
            Set(tracking, "extraDamagePercent", 10);
            var ground = new GroundCompactionTraitAuthoring();
            Set(ground, "movementRangeBonus", 1);
            Set(ground, "opposingTurnEndsToExpire", 1);
            return new[] { Definition(solo), Definition(vanguard), Definition(tracking), Definition(ground),
                Definition(new HuntersMarkTraitAuthoring()) };
        }

        private static TraitGrowthStepAuthoring Step(int id, int level, TraitGrowthKind kind = TraitGrowthKind.Acquire)
        {
            var row = new TraitGrowthStepAuthoring();
            Set(row, "stepId", id);
            Set(row, "unlockLevel", level);
            Set(row, "kind", kind);
            return row;
        }

        private CategoryTraitProgressionDataSO Row(CategoryKey category, TraitGrowthStepAuthoring[] steps,
            params TraitGrowthDefinitionDataSO[] pool)
        {
            var row = New<CategoryTraitProgressionDataSO>();
            Set(row, "category", category);
            Set(row, "steps", steps.ToList());
            Set(row, "pool", pool.ToList());
            return row;
        }

        private CategoryTraitProgressionDataSO[] Rows(CategoryTraitProgressionDataSO enabled = null) =>
            new[] { CategoryKey.Main, CategoryKey.Heavy, CategoryKey.Rider, CategoryKey.Cannon }.Select(category =>
                enabled != null && ((CategoryKey)GetField(enabled, "category")) == category
                    ? enabled : Row(category, Array.Empty<TraitGrowthStepAuthoring>())).ToArray();

        private static TraitGrowthDefinitionDataSO[] Pool(CategoryTraitProgressionDataSO row) =>
            ((List<TraitGrowthDefinitionDataSO>)GetField(row, "pool")).ToArray();

        private T Table<T>(params DatabaseItem[] items) where T : DatabaseTable
        {
            var table = New<T>();
            Set(table, "databaseItems", items.ToList());
            return table;
        }

        private InGameGroup Group(params DatabaseTable[] additions)
        {
            var method = _inputs.CreateTargetingMethod();
            var unit = _inputs.CreateUnit(EntityKey.Unit_Shielder, CategoryKey.Heavy, method);
            var category = New<CategoryConfigDataSO>();
            using (var serialized = new SerializedObject(category))
            {
                string[] fields = { "mainMovement", "riderMovement", "heavyMovement", "cannonMovement", "bossMovement" };
                int[] distances = { 3, 4, 3, 2, 0 };
                for (int i = 0; i < fields.Length; i++)
                    serialized.FindProperty(fields[i]).FindPropertyRelative("movementAllowance").intValue = distances[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            var board = New<ArtifactBoardConfigDataSO>();
            Set(board, "initialWidth", 4);
            Set(board, "initialHeight", 4);
            // 등록 계약이 요구하는 테이블은 명시하고, 이 시험에 불필요한 항목은 빈 입력으로 둡니다.
            var tables = new DatabaseTable[]
            {
                Table<StageMapDataTableSO>(),
                Table<StageRootDataTableSO>(),
                Table<FieldDataTableSO>(),
                Table<TargetingMethodDataTableSO>(method),
                Table<UnitDataTableSO>(unit),
                Table<StructureDataTableSO>(),
                Table<EnemyLoadoutDataTableSO>(),
                Table<PlayerLoadoutDataTableSO>(),
                Table<UnitRosterDataTableSO>(),
                Table<ArtifactDefinitionDataTableSO>(),
                Table<CategoryConfigDataTableSO>(category),
                Table<ArtifactBoardConfigDataTableSO>(board),
                Table<StageEconomyDataTableSO>(),
                Table<RecoveryOptionDataTableSO>(),
                Table<ConsumableItemDataTableSO>()
            };
            var group = New<InGameGroup>();
            Set(group, "databaseTables", tables.Concat(additions).ToList());
            return group;
        }

        private IInGameQuery CreateDatabase(InGameGroup inGame)
        {
            var builder = new SystemBuilderHelper();
            InGameDataBaseFactory.Create(inGame).ExportServices(builder);
            return builder.BuildSystemReader().ServiceStoreReader.GetService(DataBaseGroupKey.InGame);
        }

        private static object GetField(object target, string name) => Field(target.GetType(), name).GetValue(target);
        private static void Set(object target, string name, object value) => Field(target.GetType(), name).SetValue(target, value);
        private static FieldInfo Field(Type type, string name)
        {
            for (; type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) return field;
            }
            throw new InvalidOperationException("Missing fixture field: " + name);
        }

        private static void AssertPureData(object value)
        {
            Assert.That(value, Is.Not.InstanceOf<Object>());
            foreach (var property in value.GetType().GetProperties())
            {
                var item = property.GetValue(value);
                Assert.That(item, Is.Not.InstanceOf<Object>());
                if (item is System.Collections.IEnumerable list && item is not string)
                    foreach (var entry in list) Assert.That(entry, Is.Not.InstanceOf<Object>());
            }
        }
    }
}
