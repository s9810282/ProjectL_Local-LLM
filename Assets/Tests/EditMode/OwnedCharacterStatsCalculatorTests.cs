using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Categories;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class OwnedCharacterStatsCalculatorTests
    {
        private static readonly CharacterInstanceId MainId = new(1);
        private static readonly CharacterInstanceId SubId = new(2);
        private static readonly CharacterInstanceId SecondSubId = new(3);

        [TestCase(1)]
        [TestCase(3)]
        public void ConfiguredMovement_OwnedStatsAndPreparedFactsUseSuppliedCategoryAtEveryLevel(int level)
        {
            var categories = Categories().Select(c => c.Key == CategoryKey.Rider
                ? new CategorySpec(c.Key,c.RuleSpecs.ToArray(),5,new CategoryLevelGrowth(2, 1)) : c).ToArray();
            var owned = new[] { MainRecord(), Sub(SubId, level) };
            var souls = Souls((MainId, 0), (SubId, 0));
            var query = OwnedCharacterStatsCalculator.Calculate(Profiles(), categories, owned, MainState(), souls, default);
            var original = Initialization();
            var initialization = new BattleInitialization(original.Stage, original.MainAnchor, original.MainRotation,
                original.Units, original.Structures, categories);
            var prepared = BattlePreparationCalculator.Prepare(initialization, owned, MainState(), souls,
                new InitialDrawPlan(new[] { SubId }), Array.Empty<CharacterCombatFacts>());
            var stats = query.Single(c => c.OwnedId == SubId).Stats;
            var battle = prepared.Characters.Single(c => c.OwnedId == SubId);
            Assert.That(stats.MoveRange, Is.EqualTo(5));
            Assert.That(battle.Stats.MoveRange, Is.EqualTo(5));
            Assert.That(stats.AttackPower, Is.EqualTo(10 + level - 1));
            Assert.That(stats.MaxHealth, Is.EqualTo(20 + 2 * (level - 1)));
            Assert.That(query[0].Stats.MoveRange, Is.EqualTo(3));
        }


        [Test]
        public void OwnedThornDescription_NoTraitHasNoPayload()
        {
            var result = Calculate(new[] { MainRecord(), Sub(SubId) }, MainState(),
                Souls((MainId, 0), (SubId, 0)));

            Assert.That(result.All(item => item.ThornFieldDescription == null), Is.True);
        }

        [Test]
        public void OwnedThornDescription_UsesConfiguredValuesInsteadOfConstants()
        {
            var unit = CombatCommandTestData.Spec(EntityKey.Unit_CactusCannon, 10, CategoryKey.Cannon,
                new ThornFieldTraitSpec(17, 9, 40, UnitTurnTickPhase.Started));
            var owned = new OwnedCharacterRecord(SubId, unit.Key, CharacterOwnershipRole.Sub);

            var result = OwnedCharacterStatsCalculator.Calculate(UnitSpecTestFactory.Profiles(unit), Categories(),
                new[] { owned }, null, Souls((SubId, 0)), default);

            var description = result.Single().ThornFieldDescription;
            Assert.That(description, Is.Not.Null);
            Assert.That((description.ThornDamage, description.LifetimeUnitTurns,
                description.DefenseReductionPercent, description.TickPhase),
                Is.EqualTo((17, 9, 40, UnitTurnTickPhase.Started)));
            Assert.That(result.Single().Stats.TraitKeys.ToArray(), Is.EqualTo(new[] { TraitKey.ThornField }));
            Assert.That(result.Single().TraitSettings.TryGet<ThornFieldTraitSpec>(out var thorn), Is.True);
            Assert.That((thorn.ThornDamage, thorn.LifetimeUnitTurns, thorn.DefenseReductionPercent, thorn.TickPhase),
                Is.EqualTo((17, 9, 40, UnitTurnTickPhase.Started)));
        }

        [Test]
        public void AttackPattern_UsesProfileMethodAndBaselineRangeFromAttackerCell()
        {
            var unit = new UnitSpec(EntityKey.Unit_Cowboy, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 20,
                MoveMethod.LiftAndPlace, 2, CategoryKey.Rider, 10, 1, 7, Array.Empty<TraitSpec>(), UnitOwnerEligibility.PlayerOnly);
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.Cross, 50);

            var row = OwnedCharacterStatsCalculator.Calculate(new[] { new CombatAttackProfile(unit, method) }, Categories(),
                new[] { Sub(SubId) }, null, Souls((SubId, 0)), new LevelProgressionSpec(150, 100, 10, 3, 1, 1)).Single();

            Assert.That(row.Stats.AttackRange, Is.EqualTo(2));
            // 넘긴 경험치 설정으로 다음 레벨 진행값을 싣는다(서브 레벨 1 = 요구량 100).
            Assert.That((row.Experience.IsEnabled, row.Experience.Current, row.Experience.Required, row.Experience.Remaining),
                Is.EqualTo((true, 0, 100, 100)));
            Assert.That(row.AttackPattern.Method, Is.SameAs(method));
            Assert.That(row.AttackPattern.RangeCells, Is.EqualTo(new[]
            {
                new CellCoord(0, -2), new CellCoord(0, -1), new CellCoord(-2, 0), new CellCoord(-1, 0),
                new CellCoord(1, 0), new CellCoord(2, 0), new CellCoord(0, 1), new CellCoord(0, 2),
            }));
            Assert.That(row.AttackPattern.SplashOffsets, Is.EquivalentTo(new[]
            {
                new CellCoord(0, 1), new CellCoord(0, -1), new CellCoord(1, 0), new CellCoord(-1, 0),
            }));
            Assert.Throws<ArgumentException>(() => new AttackPatternSnapshot(method, row.AttackPattern.RangeCells,
                Array.Empty<CellCoord>()));
        }

        [Test]
        public void OwnedTraitSettings_FollowTraitKeysAndConstructorChecksThem()
        {
            var item = Calculate(new[] { MainRecord(), Sub(SubId) }, MainState(), Souls((MainId, 0), (SubId, 0)))
                .Single(c => c.OwnedId == SubId);
            Assert.That(item.TraitSettings.Traits.Select(t => t.TraitKey), Is.EqualTo(item.Stats.TraitKeys.ToArray()));
            Assert.That(item.TraitSettings.TryGet<FocusTraitSpec>(out _), Is.True);

            var error = Assert.Throws<ArgumentException>(() => new OwnedCharacterStatsSnapshot(item.OwnedId, item.CharacterKey,
                item.Role, item.Level, item.Experience, item.SoulCount, item.Stats, item.AttackPattern, TraitSettingsSet.Empty, null));
            Assert.That(error.ParamName, Is.EqualTo("traitSettings"));
            Assert.Throws<ArgumentNullException>(() => new OwnedCharacterStatsSnapshot(item.OwnedId, item.CharacterKey,
                item.Role, item.Level, item.Experience, item.SoulCount, item.Stats, item.AttackPattern, null, null));
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void OwnedThornDescription_ConstructorRejectsTraitPayloadMismatch(bool hasTrait, bool hasPayload)
        {
            var stats = new CharacterBaselineStatsSnapshot(10,10,5,0,MoveMethod.LiftAndPlace,                3,4,1,CategoryKey.Cannon,                hasTrait ? new[] { TraitKey.ThornField } : Array.Empty<TraitKey>());
            var description = hasPayload ? new ThornFieldTraitStatusSnapshot(17, 9, 40, UnitTurnTickPhase.Started) : null;

            var error = Assert.Throws<ArgumentException>(() => new OwnedCharacterStatsSnapshot(
                SubId, EntityKey.Unit_CactusCannon, CharacterOwnershipRole.Sub, 1, new ExperienceProgressSnapshot(false, 0, 0), 0, stats, Pattern(), hasTrait ? new TraitSettingsSet(new (TraitSpec, TraitOrigin)[] { (new ThornFieldTraitSpec(17, 9, 40, UnitTurnTickPhase.Started), TraitOrigin.Unit) }) : TraitSettingsSet.Empty,
                description));

            Assert.That(error.ParamName, Is.EqualTo("thornFieldDescription"));
        }

        [Test]
        public void DesignExamples_MainAndSubIncludeSoulsAndPersistentGrowth()
        {
            var result = Calculate(new[] { MainRecord(), Sub(SubId) }, MainState(),
                Souls((MainId, 2), (SubId, 3)));

            var main = result[0];
            Assert.That((main.OwnedId, main.CharacterKey, main.Role, main.Level, main.SoulCount),
                Is.EqualTo((MainId, EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main, 1, 2)));
            // 메인은 정의 공격력 5가 아니라 지속 공격력 15에 영혼 2를 더한다.
            Assert.That((main.Stats.AttackPower, main.Stats.CurrentHealth, main.Stats.MaxHealth),
                Is.EqualTo((17, 18, 32)));
            Assert.That(main.Stats.MoveRange, Is.EqualTo(3));

            var sub = result[1];
            Assert.That((sub.OwnedId, sub.CharacterKey, sub.Role, sub.SoulCount),
                Is.EqualTo((SubId, EntityKey.Unit_Cowboy, CharacterOwnershipRole.Sub, 3)));
            Assert.That((sub.Stats.AttackPower, sub.Stats.CurrentHealth, sub.Stats.MaxHealth),
                Is.EqualTo((13, 23, 23)));
            Assert.That((sub.Stats.MoveMethod, sub.Stats.MoveRange, sub.Stats.AttackRange),
                Is.EqualTo((MoveMethod.LiftAndPlace, 4, 4)));
            Assert.That((sub.Stats.PhysicalDefense, sub.Stats.Priority), Is.EqualTo((1, 7)));
            Assert.That(sub.Stats.CategoryKey, Is.EqualTo(CategoryKey.Rider));
            Assert.That(sub.Stats.TraitKeys.ToArray(), Is.EqualTo(new[] { TraitKey.Focus }));
        }

        [Test]
        public void SameUnitKeyCards_AreSeparatedByOwnedId_AndLevelDoesNotChangeStats()
        {
            var result = Calculate(new[] { MainRecord(), Sub(SubId, level: 1), Sub(SecondSubId, level: 3) },
                MainState(), Souls((MainId, 0), (SubId, 0), (SecondSubId, 2)));

            Assert.That(result.Select(item => (item.OwnedId, item.Level, item.SoulCount)), Is.EqualTo(new[]
                { (MainId, 1, 0), (SubId, 1, 0), (SecondSubId, 3, 2) }));
            Assert.That((result[1].Stats.AttackPower, result[1].Stats.MaxHealth), Is.EqualTo((10, 20)));
            Assert.That((result[2].Stats.AttackPower, result[2].Stats.MaxHealth), Is.EqualTo((12, 22)));
        }

        [Test]
        public void OwnedOrder_IsPreserved()
        {
            var result = Calculate(new[] { Sub(SecondSubId), MainRecord(), Sub(SubId) }, MainState(),
                Souls((MainId, 0), (SubId, 0), (SecondSubId, 0)));

            Assert.That(result.Select(item => item.OwnedId), Is.EqualTo(new[] { SecondSubId, MainId, SubId }));
        }

        [Test]
        public void EmptyRoster_ReturnsEmptyWithoutDefinitions()
        {
            var result = OwnedCharacterStatsCalculator.Calculate(Array.Empty<CombatAttackProfile>(), Array.Empty<CategorySpec>(),
                Array.Empty<OwnedCharacterRecord>(), null, new Dictionary<CharacterInstanceId, int>(), default);

            Assert.That(result.Count, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => OwnedCharacterStatsCalculator.Calculate(
                Array.Empty<CombatAttackProfile>(), Array.Empty<CategorySpec>(), Array.Empty<OwnedCharacterRecord>(),
                MainState(), new Dictionary<CharacterInstanceId, int>(), default));
        }

        [Test]
        public void NullArguments_AreRejected()
        {
            var owned = new[] { MainRecord() };
            var souls = Souls((MainId, 0));

            Assert.Throws<ArgumentNullException>(() => OwnedCharacterStatsCalculator.Calculate(
                null, Categories(), owned, MainState(), souls, default));
            Assert.Throws<ArgumentNullException>(() => OwnedCharacterStatsCalculator.Calculate(
                Profiles(), null, owned, MainState(), souls, default));
            Assert.Throws<ArgumentNullException>(() => OwnedCharacterStatsCalculator.Calculate(
                Profiles(), Categories(), null, MainState(), souls, default));
            Assert.Throws<ArgumentNullException>(() => OwnedCharacterStatsCalculator.Calculate(
                Profiles(), Categories(), owned, MainState(), null, default));
        }

        [TestCase("MissingDefinition")]
        [TestCase("SubWithMainDefinition")]
        [TestCase("EnemyOnlyDefinition")]
        [TestCase("MissingSoul")]
        [TestCase("NegativeSoul")]
        [TestCase("MainWithoutState")]
        [TestCase("StateWithoutMain")]
        [TestCase("TwoMains")]
        [TestCase("DuplicateOwnedId")]
        [TestCase("NullRecord")]
        public void InvalidRoster_IsRejectedWithoutPartialResult(string condition)
        {
            OwnedCharacterRecord[] owned = { MainRecord(), Sub(SubId) };
            MainCharacterPersistentState main = MainState();
            var souls = Souls((MainId, 0), (SubId, 0), (SecondSubId, 0));
            switch (condition)
            {
                case "MissingDefinition":
                    owned[1] = new OwnedCharacterRecord(SubId, EntityKey.Unit_Sniper, CharacterOwnershipRole.Sub);
                    break;
                case "SubWithMainDefinition":
                    owned[1] = new OwnedCharacterRecord(SubId, EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub);
                    break;
                case "EnemyOnlyDefinition":
                    owned[1] = new OwnedCharacterRecord(SubId, EntityKey.Unit_Demon, CharacterOwnershipRole.Sub);
                    break;
                case "MissingSoul":
                    souls.Remove(SubId);
                    break;
                case "NegativeSoul":
                    souls[SubId] = -1;
                    break;
                case "MainWithoutState":
                    main = null;
                    break;
                case "StateWithoutMain":
                    owned = new[] { Sub(SubId) };
                    break;
                case "TwoMains":
                    owned = new[] { MainRecord(), new OwnedCharacterRecord(SecondSubId, EntityKey.Unit_Akimbo,
                        CharacterOwnershipRole.Main) };
                    break;
                case "DuplicateOwnedId":
                    owned[1] = new OwnedCharacterRecord(MainId, EntityKey.Unit_Cowboy, CharacterOwnershipRole.Sub);
                    break;
                case "NullRecord":
                    owned[1] = null;
                    break;
            }

            Assert.Throws<InvalidOperationException>(() =>
                OwnedCharacterStatsCalculator.Calculate(Profiles(), Categories(), owned, main, souls, default));
        }

        [Test]
        public void Overflow_IsRejected()
        {
            var units = new[] { MainSpec(), SubSpec(maxHealth: int.MaxValue) };

            Assert.Throws<OverflowException>(() => OwnedCharacterStatsCalculator.Calculate(UnitSpecTestFactory.Profiles(units), Categories(),
                new[] { MainRecord(), Sub(SubId) }, MainState(), Souls((MainId, 0), (SubId, 1)), default));
        }

        [Test]
        public void Results_CannotBeChangedThroughReturnedValuesOrInputs()
        {
            var result = Calculate(new[] { MainRecord() }, MainState(), Souls((MainId, 0)));

            var list = (IList<OwnedCharacterStatsSnapshot>)result;
            Assert.Throws<NotSupportedException>(() => list[0] = null);
            var keys = new[] { TraitKey.Focus };
            var stats = new CharacterBaselineStatsSnapshot(1,1,0,0,MoveMethod.LiftAndPlace,0,0,0,                CategoryKey.Main,keys);
            keys[0] = TraitKey.Knockback;
            Assert.That(stats.TraitKeys.ToArray(), Is.EqualTo(new[] { TraitKey.Focus }));
        }

        [Test]
        public void QueryResult_EnforcesStatusAndItemAgreement()
        {
            var item = Calculate(new[] { MainRecord() }, MainState(), Souls((MainId, 0)))[0];

            var ready = new CharacterStatsQueryResult(CharacterStatsQueryStatus.Ready, RunOutcome.Defeat, new[] { item });
            Assert.That((ready.Status, ready.RunOutcome, ready.Items.Count),
                Is.EqualTo((CharacterStatsQueryStatus.Ready, RunOutcome.Defeat, 1)));
            Assert.Throws<ArgumentException>(() => new CharacterStatsQueryResult(CharacterStatsQueryStatus.Ready,
                RunOutcome.InProgress, Array.Empty<OwnedCharacterStatsSnapshot>()));
            Assert.Throws<ArgumentException>(() => new CharacterStatsQueryResult(CharacterStatsQueryStatus.BattleActive,
                RunOutcome.InProgress, new[] { item }));
            Assert.Throws<ArgumentException>(() => new CharacterStatsQueryResult(CharacterStatsQueryStatus.Ready,
                RunOutcome.InProgress, new[] { item, item }));
            Assert.That(new CharacterStatsQueryResult(CharacterStatsQueryStatus.EmptyRoster, RunOutcome.InProgress,
                Array.Empty<OwnedCharacterStatsSnapshot>()).Items.Count, Is.Zero);
        }

        [Test]
        public void QueryMatchesBattleCreationBeforeEnvironment()
        {
            OwnedCharacterRecord[] owned = { MainRecord(), Sub(SubId), Sub(SecondSubId, level: 2) };
            MainCharacterPersistentState main = MainState();
            var souls = Souls((MainId, 2), (SubId, 3), (SecondSubId, 1));
            var query = OwnedCharacterStatsCalculator.Calculate(Profiles(), Categories(), owned, main, souls, default);
            var prepared = BattlePreparationCalculator.Prepare(Initialization(), owned, main, souls,
                new InitialDrawPlan(new[] { SubId, SecondSubId }), Array.Empty<CharacterCombatFacts>());

            foreach (OwnedCharacterStatsSnapshot item in query)
            {
                CharacterCombatFacts battle = prepared.Characters.Single(x => x.OwnedId == item.OwnedId);
                Assert.That(battle.Stats.PositionAttackBonus, Is.Zero, "비교 전제: 위치 보정이 없는 칸");
                Assert.That((item.Stats.CurrentHealth, item.Stats.MaxHealth, item.Stats.AttackPower),
                    Is.EqualTo((battle.Health.CurrentHealth, battle.Health.MaxHealth, battle.Stats.EffectiveAttackPower)));
                Assert.That((item.Stats.PhysicalDefense, item.Stats.MoveRange, item.Stats.AttackRange),
                    Is.EqualTo((battle.Stats.PhysicalDefense, battle.Stats.MoveRange, battle.Stats.AttackRange)));
                Assert.That((item.Stats.Priority, item.Stats.MoveMethod, item.Stats.CategoryKey),
                    Is.EqualTo((battle.Stats.Priority, battle.Stats.MoveMethod, battle.Stats.Category.Key)));
                Assert.That(item.Stats.TraitKeys.ToArray(),
                    Is.EqualTo(battle.Stats.TraitStatuses.ToArray().Select(status => status.Key).ToArray()));
            }
        }

        [TestCase(TraitKey.Focus, false, "")]
        [TestCase(TraitKey.ThornField, false, "")]
        [TestCase(TraitKey.Roar, true, "")]
        [TestCase(TraitKey.Regeneration, true, "")]
        [TestCase(TraitKey.Intimidation, true, "")]
        [TestCase(TraitKey.KillChainAttack, true, "")]
        [TestCase(TraitKey.Knockback, true, "")]
        [TestCase(TraitKey.Pull, true, "")]
        [TestCase(TraitKey.ImpactBolt, true, "")]
        public void OwnedTraitAvailability_MatchesBattleTraitStatus(TraitKey key, bool available, string partial)
        {
            TraitSpec trait = key switch
            {
                TraitKey.Focus => new FocusTraitSpec(2, 3),
                TraitKey.ThornField => new ThornFieldTraitSpec(5, 5),
                TraitKey.Roar => new RoarTraitSpec(2),
                TraitKey.Regeneration => new RegenerationTraitSpec(3),
                TraitKey.Intimidation => new IntimidationTraitSpec(20),
                TraitKey.KillChainAttack => new KillChainAttackTraitSpec(),
                TraitKey.Knockback => new KnockbackTraitSpec(1),
                TraitKey.Pull => new PullTraitSpec(1),
                TraitKey.ImpactBolt => new ImpactBoltTraitSpec(1),
                _ => throw new ArgumentOutOfRangeException(nameof(key))
            };
            var sub = new UnitSpec(EntityKey.Unit_Cowboy, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 20,
                MoveMethod.LiftAndPlace, 4, CategoryKey.Rider, 10, 1, 7, new[] { trait }, UnitOwnerEligibility.PlayerOnly);
            var units = new[] { MainSpec(), sub, EnemySpec() };
            OwnedCharacterRecord[] owned = { MainRecord(), Sub(SubId) };
            var souls = Souls((MainId, 0), (SubId, 0));
            var query = OwnedCharacterStatsCalculator.Calculate(UnitSpecTestFactory.Profiles(units), Categories(), owned, MainState(), souls, default)
                .Single(c => c.OwnedId == SubId);
            var original = Initialization();
            var prepared = BattlePreparationCalculator.Prepare(new BattleInitialization(original.Stage, original.MainAnchor,
                    original.MainRotation, units, original.Structures, original.Categories), owned, MainState(), souls,
                new InitialDrawPlan(new[] { SubId }), Array.Empty<CharacterCombatFacts>());
            var status = prepared.Characters.Single(c => c.OwnedId == SubId).Stats.TraitStatuses.ToArray().Single();

            var entry = query.TraitAvailability.Single();
            Assert.That(entry.Key, Is.EqualTo(key));
            Assert.That((entry.IsAvailable, entry.UnavailableReason, entry.PartialUnavailableReason),
                Is.EqualTo((status.IsAvailable, status.UnavailableReason, status.PartialUnavailableReason)));
            Assert.That((entry.IsAvailable, entry.PartialUnavailableReason), Is.EqualTo((available, partial)));
        }

        [Test]
        public void OwnedTraitAvailability_FollowsTraitKeyOrderAndConstructorChecksIt()
        {
            var item = Calculate(new[] { MainRecord(), Sub(SubId) }, MainState(), Souls((MainId, 0), (SubId, 0)))
                .Single(c => c.OwnedId == SubId);
            Assert.That(item.TraitAvailability.Select(a => a.Key), Is.EqualTo(item.Stats.TraitKeys.ToArray()));
            Assert.That(new OwnedCharacterStatsSnapshot(item.OwnedId, item.CharacterKey, item.Role, item.Level, item.Experience, item.SoulCount,
                item.Stats, item.AttackPattern, item.TraitSettings, null).TraitAvailability, Is.Empty);
            Assert.Throws<ArgumentException>(() => new OwnedCharacterStatsSnapshot(item.OwnedId, item.CharacterKey, item.Role,
                item.Level, item.Experience, item.SoulCount, item.Stats, item.AttackPattern, item.TraitSettings, null,
                traitAvailability: new[] { new TraitAvailabilitySnapshot(TraitKey.Knockback, "", "") }));
            Assert.Throws<ArgumentException>(() => new OwnedCharacterStatsSnapshot(item.OwnedId, item.CharacterKey, item.Role,
                item.Level, item.Experience, item.SoulCount, item.Stats, item.AttackPattern, item.TraitSettings, null, traitAvailability: new[] {
                    item.TraitAvailability[0], item.TraitAvailability[0] }));
        }

        private static IReadOnlyList<OwnedCharacterStatsSnapshot> Calculate(OwnedCharacterRecord[] owned,
            MainCharacterPersistentState main, Dictionary<CharacterInstanceId, int> souls) =>
            OwnedCharacterStatsCalculator.Calculate(Profiles(), Categories(), owned, main, souls, default);

        private static MainCharacterPersistentState MainState() => new(18, 30, 15);

        private static OwnedCharacterRecord MainRecord() =>
            new(MainId, EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main);

        private static OwnedCharacterRecord Sub(CharacterInstanceId id, int level = 1) =>
            new(id, EntityKey.Unit_Cowboy, CharacterOwnershipRole.Sub, level);

        private static Dictionary<CharacterInstanceId, int> Souls(params (CharacterInstanceId Id, int Count)[] souls) =>
            souls.ToDictionary(x => x.Id, x => x.Count);

        private static CategorySpec[] Categories() => UnitSpecTestFactory.CreateDefaultCategoryCatalog();

        private static UnitSpec[] Units() => new[] { MainSpec(), SubSpec(), EnemySpec() };

        private static CombatAttackProfile[] Profiles() => UnitSpecTestFactory.Profiles(Units());

        private static AttackPatternSnapshot Pattern() =>
            new(UnitSpecTestFactory.DefaultMethod(), Array.Empty<CellCoord>(), Array.Empty<CellCoord>());

        // 메인 정의의 1x1 크기를 유지하고 사거리는 fixture에서 명시한다.
        private static UnitSpec MainSpec() =>
            new(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, 1, 1, new[] { CellCoord.Zero }, 10,
                MoveMethod.LiftAndPlace, 1, CategoryKey.Main, 5, 0, 5,
                Array.Empty<TraitSpec>(), UnitOwnerEligibility.PlayerOnly);

        private static UnitSpec SubSpec(int maxHealth = 20) =>
            new(EntityKey.Unit_Cowboy, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, maxHealth,
                MoveMethod.LiftAndPlace, 4, CategoryKey.Rider, 10, 1, 7,
                new TraitSpec[] { new FocusTraitSpec(2, 3) }, UnitOwnerEligibility.PlayerOnly);

        private static UnitSpec EnemySpec() =>
            new(EntityKey.Unit_Demon, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 10,
                MoveMethod.LiftAndPlace, 2, CategoryKey.Main, 5, 0, 1,
                Array.Empty<TraitSpec>(), UnitOwnerEligibility.EnemyOnly);

        private static BattleInitialization Initialization()
        {
            var stage = new StageConfig(8, 6, 8, 2, 2, 42, Array.Empty<InitialEnemyPlacementSpec>(),
                Array.Empty<CellCoord>(), Array.Empty<StageGimmickSpec>(),
                Array.Empty<InitialEnemyStructurePlacementSpec>());
            return new BattleInitialization(stage, CellCoord.Zero, Rotation4.R0, Units(),
                Array.Empty<StructureSpec>(), Categories());
        }
    }
}
