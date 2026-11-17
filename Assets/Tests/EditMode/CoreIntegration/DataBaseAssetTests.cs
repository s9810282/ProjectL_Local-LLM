using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.Factory.DBFactory;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class DataBaseAssetTests
    {
        [Test]
        public void RecoveryProduction_RegistersTableAndPreservesEachStagesOptionOrderAndEffects()
        {
            const string prefix = "Assets/ProjectL/DataBaseSO/InGameGroup/";
            var database = ProductionDatabase();
            var variants = new[]
            {
                (Chapter: 1, Stage: 1301, Options: new[] { 1, 2, 4 }),
                (Chapter: 1, Stage: 1302, Options: new[] { 1, 3, 4 }),
                (Chapter: 1, Stage: 1303, Options: new[] { 2, 3, 4 }),
                (Chapter: 2, Stage: 2203, Options: new[] { 1, 2, 4 })
            };
            foreach (var variant in variants)
            {
                var stage = RecoveryDefinitionAdapter.Read(database, variant.Chapter, new StageKey(variant.Stage));
                var options = stage.Options.ToArray();
                Assert.That(options.Select(option => option.Id.Value), Is.EqualTo(variant.Options), $"Stage {variant.Stage}");
                Assert.That(options.Select(option => option.EffectKind),
                    Is.EqualTo(new[] { RecoveryEffectKind.Heal, RecoveryEffectKind.Heal, RecoveryEffectKind.MaxHealthGrowth }),
                    $"Stage {variant.Stage}");
            }

            using var group = new SerializedObject(AssetDatabase.LoadMainAssetAtPath(prefix + "InGameGroup.asset"));
            var tables = group.FindProperty("databaseTables");
            Assert.That(Enumerable.Range(0, tables.arraySize).Select(i => tables.GetArrayElementAtIndex(i).objectReferenceValue),
                Has.Member(AssetDatabase.LoadMainAssetAtPath(prefix + "Recovery/RecoveryOptionDataTable.asset")));
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

        private static readonly TraitKey[] RankedTraitKeys =
        {
            TraitKey.Vanguard, TraitKey.CleanSweep, TraitKey.SoloAction, TraitKey.CallOfBlood,
            TraitKey.MoraleBoost, TraitKey.PositionSwap, TraitKey.Cohesion, TraitKey.Taunt,
            TraitKey.Entrenchment, TraitKey.GroundCompaction, TraitKey.ReconSquad, TraitKey.StationaryFire,
            TraitKey.ArmorBreaker, TraitKey.FinishingShot, TraitKey.TrackingShot, TraitKey.Hunter,
            TraitKey.FocusedBombardment, TraitKey.Sacrifice, TraitKey.DustScreen, TraitKey.DeadHand
        };

        private static IDataBaseQueryContext ProductionDatabase()
        {
            var assets = AssetDatabase.LoadAssetAtPath<DatabaseCatalogSO>("Assets/ProjectL/DataBaseSO/DatabaseCatalogSO.asset");
            return (IDataBaseQueryContext)DataBaseFactory.Create(assets);
        }

        internal static TraitGrowthCatalog ActiveGrowthCatalog() => ReadGrowthCatalog(ProductionDatabase().InGameQuery);

        private static TraitGrowthCatalog ReadGrowthCatalog(IInGameQuery data)
        {
            var tables = (IHasTraitGrowthTables)data;
            return TraitGrowthDefinitionAdapter.Build(tables.CategoryTraitProgressions, tables.TraitGrowthDefinitions,
                data.CategoryConfigTableQuery.Config.DefaultTraits);
        }

        [Test]
        public void GrowthProduction_OnlyRegisteredRankedKeysCanTrain()
        {
            Assert.That(ActiveGrowthCatalog().Definitions.Where(d => d.IsTrainable).Select(d => d.Key),
                Is.EquivalentTo(RankedTraitKeys));
        }

        [Test]
        public void GrowthProduction_ResolvesAuthoredValuesAtEachRank()
        {
            var data = ProductionDatabase().InGameQuery;
            var tables = (IHasTraitGrowthTables)data;
            var catalog = ReadGrowthCatalog(data);
            foreach (var authored in tables.TraitGrowthDefinitions.GetAll().Where(definition => definition.IsTrainable))
            {
                var normal = TraitSpecAdapter.Read(authored.Normal);
                Assert.That(RankedCombatTestData.Spec(catalog, authored.Key, 1).SameValues(normal), Is.True,
                    $"{authored.Key}, rank 1");
                var purchasedRankValues = authored.Rank3 == null ? new[] { authored.Rank2 } : new[] { authored.Rank2, authored.Rank3 };
                Assert.That(catalog.GetTrait(authored.Key).MaxRank, Is.EqualTo(purchasedRankValues.Length + 1), authored.Key.ToString());
                for (int index = 0; index < purchasedRankValues.Length; index++)
                {
                    int rank = index + 2;
                    var expected = purchasedRankValues[index];
                    var effectiveSpec = RankedCombatTestData.Spec(catalog, authored.Key, rank);
                    Assert.That(ReadEffectiveRankFields(effectiveSpec),
                        Is.EqualTo((expected.Amount, expected.SecondAmount, expected.PreventsLethalSacrificeDamage, expected.ExemptsAlliedAttackRangePenalty)),
                        $"{authored.Key}, rank {rank}");
                }
            }
        }

        private static (int Amount, int? SecondAmount, bool PreventsLethalDamage, bool ExemptsAlliedRangePenalty)
            ReadEffectiveRankFields(TraitSpec spec) => spec switch
        {
            VanguardTraitSpec trait => (trait.AttackBonus, null, false, false),
            CleanSweepTraitSpec trait => (trait.AttackBonusPerAllyAttack, null, false, false),
            SoloActionTraitSpec trait => (trait.ShieldAmount, null, false, false),
            CallOfBloodTraitSpec trait => (trait.EnemyMaxHealthHealPercent, null, false, false),
            MoraleBoostTraitSpec trait => (trait.DefeatedEnemyBaseAttackPercent, null, false, false),
            PositionSwapTraitSpec trait => (trait.ShieldAmount, null, false, false),
            CohesionTraitSpec trait => (trait.DefensePerAdjacentAlly, trait.ShieldAmountPerAlly, false, false),
            TauntTraitSpec trait => (trait.DamageReductionPercent, null, false, false),
            EntrenchmentTraitSpec trait => (trait.DamageReductionPercent, null, false, false),
            GroundCompactionTraitSpec trait => (trait.MovementRangeBonus, null, false, false),
            ReconSquadTraitSpec trait => (trait.IncreasePercent, null, false, false),
            StationaryFireTraitSpec trait => (trait.ExtraDamagePercent, null, false, false),
            ArmorBreakerTraitSpec trait => (trait.DefenseReductionPercentPerHit, null, false, false),
            FinishingShotTraitSpec trait => (trait.AttackThresholdPercent, null, false, false),
            TrackingShotTraitSpec trait => (trait.TargetHealthThresholdPercent, trait.ExtraDamagePercent, false, false),
            HunterTraitSpec trait => (trait.AttackPerTargetKill, null, false, false),
            FocusedBombardmentTraitSpec trait => (trait.ExtraDamagePercent, null, false, false),
            SacrificeTraitSpec trait => (trait.ShieldOnActivation, null, trait.PreventsLethalSacrificeDamage, false),
            DustScreenTraitSpec trait => (0, null, false, trait.ExemptsAlliedAttackRangePenalty),
            DeadHandTraitSpec trait => (trait.DamagePercent, null, false, false),
            _ => throw new ArgumentException($"Unexpected ranked spec: {spec.GetType().Name}.", nameof(spec))
        };

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
            Assert.That(OwnedUnitSpecResolver.ResolveBase(raw, catalog).Traits.ToArray()
                .Count(t => t.TraitKey == TraitKey.SteadfastWill), Is.EqualTo(1));
        }

        [Test]
        public void ShotGunKeepsItsNativeSteadfastWillWhenHeavyDefaultChanges()
        {
            var data = ProductionInGame();
            var raw = UnitSpecAdapter.ReadUnit(data.UnitTableQuery.GetUnit(EntityKey.Unit_ShotGun));
            var tables = (IHasTraitGrowthTables)data;
            var catalog = TraitGrowthDefinitionAdapter.Build(tables.CategoryTraitProgressions, tables.TraitGrowthDefinitions,
                new Dictionary<CategoryKey, TraitKey> { [CategoryKey.Heavy] = TraitKey.SoloAction });
            var effective = OwnedUnitSpecResolver.ResolveBase(raw, catalog).Traits.ToArray();

            Assert.That(effective.Select(trait => trait.TraitKey), Has.Member(TraitKey.SoloAction));
            Assert.That(effective.Count(trait => trait.TraitKey == TraitKey.SteadfastWill), Is.EqualTo(1),
                "Replacing the Heavy default must preserve Shotgun's native Steadfast Will.");
            Assert.That(raw.Traits.ToArray().Count(trait => trait.TraitKey == TraitKey.SteadfastWill), Is.EqualTo(1));
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
            Assert.That(catalog.Definitions.Count, Is.EqualTo(26));
            Assert.That(catalog.GetTrait(TraitKey.PositionSwap).Normal, Is.TypeOf<PositionSwapTraitSpec>());
            Assert.That(catalog.GetTrait(TraitKey.ReconSquad).Normal, Is.TypeOf<ReconSquadTraitSpec>());
        }

        [Test]
        public void GrowthProduction_UnitCatalogPreservesAuthoredNativeTraits()
        {
            var units = ProductionDatabase().InGameQuery.UnitTableQuery;
            var akimbo = UnitSpecAdapter.ReadUnit(units.GetUnit(EntityKey.Unit_Akimbo));
            Assert.That(akimbo.Traits.ToArray().Select(t => t.TraitKey), Is.EqualTo(new[] { TraitKey.KillChainAttack }));

            var cactus = UnitSpecAdapter.ReadUnit(units.GetUnit(EntityKey.Unit_CactusCannon));
            Assert.That(cactus.Traits.ToArray().Select(t => t.TraitKey), Is.EqualTo(new[] { TraitKey.ThornBomb }));
            var bomb = (ThornBombTraitSpec)cactus.Traits.Span[0];
            Assert.That(bomb.DamagePercent, Is.EqualTo(50));
            Assert.That((cactus.AttackPower, cactus.AttackRange, cactus.CategoryKey), Is.EqualTo((15, 3, CategoryKey.Cannon)));
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
