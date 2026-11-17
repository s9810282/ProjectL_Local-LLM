using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using UnityEditor;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ImpactBoltAuthoringTests
    {
        [Test]
        public void DefaultAuthoring_ProducesSeparateImmutableDistanceOneValues()
        {
            var authoring = new ImpactBoltTraitAuthoring();
            var first = (ImpactBoltTraitData)authoring.ToData();
            var second = (ImpactBoltTraitData)authoring.ToData();
            Assert.That(first.DistanceCells, Is.EqualTo(1));
            Assert.That(second, Is.Not.SameAs(first));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void AuthoredDistance_ReachesCoreBattleAndOwnedDescription(int distance)
        {
            using var authoring = new AuthoredProfile(distance);
            var data = authoring.ReadUnit();
            Assert.That(((ImpactBoltTraitSpec)TraitSpecAdapter.Read(data.Traits.Single())).DistanceCells, Is.EqualTo(distance));
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: authoring.Database(data));
            Assert.That(Status(battle).DistanceCells, Is.EqualTo(distance));
            Assert.That(Description(battle.OwnedStats).DistanceCells, Is.EqualTo(distance));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void InvalidAuthoredDistance_IsRejectedAtDataAndAdapter(int distance)
        {
            using var authoring = new AuthoredProfile(distance);
            var data = authoring.ReadUnit();
            Assert.Throws<InvalidOperationException>(() => data.Validate());
            Assert.Throws<InvalidOperationException>(() => TraitSpecAdapter.Read(data.Traits.Single()));
        }

        [Test]
        public void EditingOwnedInput_DoesNotMutateProducedDataOrInstalledRuntime()
        {
            using var authoring = new AuthoredProfile(1);
            var firstData = authoring.ReadUnit();
            var firstMethod = authoring.ReadMethod();
            using var first = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: authoring.Database(firstData));
            authoring.SetDistance(2);
            AuthoredProfile.Set(authoring.Method, "splashPercent", 70);
            Assert.That(firstMethod.SplashPercent, Is.EqualTo(40));
            Assert.That(((ImpactBoltTraitSpec)TraitSpecAdapter.Read(firstData.Traits.Single())).DistanceCells, Is.EqualTo(1));
            Assert.That(Status(first).DistanceCells, Is.EqualTo(1));
            Assert.That(Description(first.OwnedStats).DistanceCells, Is.EqualTo(1));
            using var second = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: authoring.Database());
            Assert.That(Status(second).DistanceCells, Is.EqualTo(2));
            Assert.That(Description(second.OwnedStats).DistanceCells, Is.EqualTo(2));
            first.Submit(first.Attack(first.Actor, new CellCoord(3, 5)));
            second.Submit(second.Attack(second.Actor, new CellCoord(3, 5)));
            Assert.That(first.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(960));
            Assert.That(first.Character(EntityKey.Unit_Demon).Anchor, Is.EqualTo(new CellCoord(1, 5)));
            Assert.That(second.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(930));
            Assert.That(second.Character(EntityKey.Unit_Demon).Anchor, Is.EqualTo(new CellCoord(0, 5)));
        }

        [Test]
        public void ConfiguredImpactBoltProfile_ConvertsPlayerUnitAndTargeting()
        {
            using var authoring = new AuthoredProfile(2);
            AuthoredProfile.Set(authoring.Unit, "key", EntityKey.Unit_Ballista);
            var unit = authoring.ReadUnit();
            var method = authoring.ReadMethod();
            unit.Validate();
            method.Validate();
            Assert.That(unit.Key, Is.EqualTo(EntityKey.Unit_Ballista));
            var impact = unit.Traits.Single(t => t.TraitKey == TraitKey.ImpactBolt);
            Assert.That(((ImpactBoltTraitSpec)TraitSpecAdapter.Read(impact)).DistanceCells, Is.EqualTo(2));
            Assert.That((unit.FootprintWidth, unit.FootprintHeight), Is.EqualTo((1, 1)));
            Assert.That((unit.Role, unit.OwnerEligibility), Is.EqualTo((UnitRole.Regular, UnitOwnerEligibility.PlayerOnly)));
            var profile = BattleDefinitionAdapter.ReadAttackProfiles(authoring.Database(unit), new[] { UnitSpecAdapter.ReadUnit(unit) }).Single();
            Assert.That((profile.Method.Shape, profile.Method.ChoiceMode, profile.Method.Splash, profile.Method.SplashPercent),
                Is.EqualTo((TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Cross, 40)));
        }

        [TestCase(1, 40, 1, 960)]
        [TestCase(2, 40, 0, 960)]
        [TestCase(2, 70, 0, 930)]
        public void EditedDistanceAndSplash_ChangeQueryPreviewAndCommittedMovement(int distance, int percent, int finalX, int health)
        {
            using var authoring = new AuthoredProfile(distance);
            AuthoredProfile.Set(authoring.Method, "splashPercent", percent);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: authoring.Database());
            var target = battle.Character(EntityKey.Unit_Demon);
            var center = new CellCoord(3, 5);
            AssertAim(battle, center, true);
            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(center)));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            var hit = preview.Attack.Value.Targets.ToArray().Single(t => t.TargetId == target.EntityId);
            Assert.That((hit.AppliedSplashPercent, hit.TargetHealthAfter), Is.EqualTo((percent, health)));
            Assert.That(preview.Attack.Value.ProjectedTraitStatuses.ToArray().OfType<ImpactBoltTraitStatusSnapshot>().Single().DistanceCells,
                Is.EqualTo(distance));
            battle.Submit(battle.Attack(battle.Actor, center));
            Assert.That(battle.Character(EntityKey.Unit_Demon).Anchor, Is.EqualTo(new CellCoord(finalX, 5)));
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(health));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Structures.Single().Health.CurrentHealth, Is.EqualTo(900));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [TestCase(4, false)]
        [TestCase(6, true)]
        public void EditedRange_ChangesActualAcceptance(int range, bool accepted)
        {
            using var authoring = new AuthoredProfile(1);
            AuthoredProfile.Set(authoring.Unit, "attackRange", range);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: authoring.Database());
            AssertAimAndSubmit(battle, new CellCoord(6, 1), accepted);
        }

        [TestCase(TargetingShape.RadialSquare, true)]
        [TestCase(TargetingShape.CardinalLine, false)]
        public void EditedShape_ChangesActualAcceptance(TargetingShape shape, bool accepted)
        {
            using var authoring = new AuthoredProfile(1);
            AuthoredProfile.Set(authoring.Method, "shape", shape);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: authoring.Database());
            AssertAimAndSubmit(battle, new CellCoord(2, 3), accepted);
        }

        [TestCase(TargetingChoiceAngle.Unrestricted, true)]
        [TestCase(TargetingChoiceAngle.Front, false)]
        public void EditedAngle_ChangesActualAcceptance(TargetingChoiceAngle angle, bool accepted)
        {
            using var authoring = new AuthoredProfile(1);
            AuthoredProfile.Set(authoring.Method, "choiceAngle", angle);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: authoring.Database());
            AssertAimAndSubmit(battle, new CellCoord(3, 1), accepted);
        }

        [Test]
        public void NoneSplash_LeavesNearbyEnemyUntouched_AndNonNoneZeroIsRejected()
        {
            using var authoring = new AuthoredProfile(2);
            AuthoredProfile.Set(authoring.Method, "splash", TargetingSplash.None);
            AuthoredProfile.Set(authoring.Method, "splashPercent", 0);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, database: authoring.Database());
            AssertAimAndSubmit(battle, new CellCoord(3, 5), true);
            var enemy = battle.Character(EntityKey.Unit_Demon);
            Assert.That(enemy.Anchor, Is.EqualTo(new CellCoord(2, 5)));
            Assert.That(enemy.Health.CurrentHealth, Is.EqualTo(1000));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Structures.Single().Health.CurrentHealth, Is.EqualTo(900));
            AuthoredProfile.Set(authoring.Method, "splash", TargetingSplash.Cross);
            Assert.Throws<InvalidOperationException>(() => authoring.ReadMethod().Validate());
            Assert.Throws<InvalidOperationException>(() => authoring.Database());
        }

        [Test]
        public void FixtureDatabase_UsesOneValidatedCollectionForEveryQuery()
        {
            using var authoring = new AuthoredProfile(2);
            var db = authoring.Database();
            var unit = authoring.ReadUnit();
            var method = authoring.ReadMethod();
            db.SetUnit(unit);
            db.SetTargetingMethod(method);
            Assert.That(db.GetUnit(unit.Key), Is.SameAs(unit));
            Assert.That(db.TryGetUnit(unit.Key, out var foundUnit), Is.True);
            Assert.That(foundUnit, Is.SameAs(unit));
            Assert.That(db.GetAllUnits().Single(u => u.Key == unit.Key), Is.SameAs(unit));
            Assert.That(db.GetTargetingMethod(method.MethodKey), Is.SameAs(method));
            Assert.That(db.TryGetTargetingMethod(method.MethodKey, out var foundMethod), Is.True);
            Assert.That(foundMethod, Is.SameAs(method));
            Assert.That(db.GetAllTargetingMethods().Single(m => m.MethodKey == method.MethodKey), Is.SameAs(method));
            Assert.That(db.GetTargetingMethod(1).Splash, Is.EqualTo(TargetingSplash.None));
            Assert.Throws<ArgumentNullException>(() => db.SetUnit(null));
            Assert.Throws<ArgumentNullException>(() => db.SetTargetingMethod(null));
            Assert.Throws<KeyNotFoundException>(() => db.GetTargetingMethod(999));
            Assert.That(db.TryGetTargetingMethod(999, out _), Is.False);
            authoring.SetDistance(0);
            Assert.Throws<InvalidOperationException>(() => db.SetUnit(authoring.ReadUnit()));
            Assert.That(db.GetUnit(unit.Key), Is.SameAs(unit));
        }

        [Test]
        public void OwnedDescription_UsesPerInstanceResolvedDistanceAndEnforcesPresence()
        {
            using var authoring = new AuthoredProfile(1);
            var basis = UnitSpecAdapter.ReadUnit(authoring.ReadUnit());
            var definition = new TraitGrowthDefinition(new ImpactBoltTraitSpec(1), new ImpactBoltTraitSpec(2), "n", "d", "e", "ed");
            var catalog = new TraitGrowthCatalog(TraitGrowthCatalog.Disabled.Categories.Select(c => c.Category == basis.CategoryKey
                ? new CategoryTraitProgressionSpec(c.Category, new[] { new TraitGrowthStepSpec(1, 2, TraitGrowthKind.Enhance) }, new[] { TraitKey.ImpactBolt }) : c).ToArray(), new[] { definition });
            var owned = new[] {
                new OwnedCharacterRecord(new CharacterInstanceId(91), basis.Key, CharacterOwnershipRole.Sub),
                new OwnedCharacterRecord(new CharacterInstanceId(92), basis.Key, CharacterOwnershipRole.Sub, 2,
                    growth: new OwnedTraitGrowth(new[] { new TraitGrowthSelection(1, TraitKey.ImpactBolt) })) };
            var stats = OwnedCharacterStatsCalculator.Calculate(BattleDefinitionAdapter.ReadAttackProfiles(authoring.Database(), new[] { basis }),
                UnitSpecAdapter.ReadCategories(ConfiguredMovementBattle.Config),
                owned, null, owned.ToDictionary(o => o.Id, _ => 0), default, catalog);
            Assert.That(stats.Select(s => s.ImpactBoltDescription.DistanceCells), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(((ImpactBoltTraitSpec)basis.Traits.Span[0]).DistanceCells, Is.EqualTo(1));
            // 설정 묶음도 개체별 최종값이며, 전투 준비와 같은 resolver 결과와 값이 같다.
            Assert.That(stats.Select(s => s.TraitSettings.TryGet<ImpactBoltTraitSpec>(out var bolt) ? bolt.DistanceCells : -1),
                Is.EqualTo(new[] { 1, 2 }));
            for (int i = 0; i < owned.Length; i++)
                Assert.That(stats[i].TraitSettings.SameValues(new TraitSettingsSet(
                    OwnedUnitSpecResolver.Resolve(basis, owned[i], catalog).Traits.ToArray().Select(t => (t, TraitOrigin.Unit)))), Is.True);
            var first = stats[0];
            Assert.Throws<ArgumentException>(() => new OwnedCharacterStatsSnapshot(first.OwnedId, first.CharacterKey, first.Role,
                first.Level, first.Experience, first.SoulCount, first.Stats, first.AttackPattern, first.TraitSettings));
            var s = first.Stats;
            var noTraits = new CharacterBaselineStatsSnapshot(s.CurrentHealth,s.MaxHealth,s.AttackPower,s.PhysicalDefense,                s.MoveMethod,s.MoveRange,s.AttackRange,s.Priority,s.CategoryKey,Array.Empty<TraitKey>());
            Assert.Throws<ArgumentException>(() => new OwnedCharacterStatsSnapshot(first.OwnedId, first.CharacterKey, first.Role,
                first.Level, first.Experience, first.SoulCount, noTraits, first.AttackPattern, TraitSettingsSet.Empty, impactBoltDescription: first.ImpactBoltDescription));
        }

        [Test]
        public void EditingDedicatedTargeting_PreservesSharedMethodAndConsumers()
        {
            using var authoring = new AuthoredProfile(1);
            var db = authoring.Database();
            var shared = db.GetTargetingMethod(1);
            var consumers = db.GetAllUnits().Where(unit => unit.TargetingMethodKey == 1).ToArray();
            Assert.That(consumers, Is.Not.Empty);
            AuthoredProfile.Set(authoring.Method, "splashPercent", 70);
            db.SetTargetingMethod(authoring.ReadMethod());
            Assert.That(db.GetTargetingMethod(authoring.ReadMethod().MethodKey).SplashPercent, Is.EqualTo(70));
            Assert.That(db.GetTargetingMethod(1), Is.SameAs(shared));
            Assert.That((shared.Shape, shared.ChoiceMode, shared.ChoiceAngle, shared.Splash, shared.SplashPercent),
                Is.EqualTo((TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingChoiceAngle.Unrestricted, TargetingSplash.None, 0)));
            foreach (var unit in consumers)
            {
                Assert.That(db.GetUnit(unit.Key), Is.SameAs(unit));
                Assert.That(unit.TargetingMethodKey, Is.EqualTo(1));
            }
            var profiles = BattleDefinitionAdapter.ReadAttackProfiles(db, consumers.Select(UnitSpecAdapter.ReadUnit).ToArray());
            Assert.That(profiles.All(profile => profile.Method.Splash == TargetingSplash.None), Is.True);
        }

        private static void AssertAim(ConfiguredMovementBattle battle, CellCoord cell, bool accepted)
        {
            var b = battle.Buffers;
            var before = battle.CaptureGameplay();
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(battle.Actor.EntityId));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var range), Is.True);
            Assert.That(range.Range.Value.Succeeded, Is.True);
            Assert.That(range.Range.Value.AttackAims.ToArray().Contains(AttackAim.ForCell(cell)), Is.EqualTo(accepted));
            Assert.That(range.Range.Value.RangeCells.ToArray().Contains(cell), Is.EqualTo(accepted));
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(cell)));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Attack.Value.CanAttack, Is.EqualTo(accepted));
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
        }

        private static void AssertAimAndSubmit(ConfiguredMovementBattle battle, CellCoord cell, bool accepted)
        {
            AssertAim(battle, cell, accepted);
            battle.Buffers.CombatViewWriteSink.Submit(battle.Attack(battle.Actor, cell));
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.EqualTo(accepted));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(accepted ? UnitActionUse.Attacked : UnitActionUse.Available));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(accepted ? UnitActionUse.Attacked : UnitActionUse.Available));
        }

        internal static ImpactBoltTraitStatusSnapshot Status(ConfiguredMovementBattle battle) =>
            battle.Actor.Stats.TraitStatuses.ToArray().OfType<ImpactBoltTraitStatusSnapshot>().Single();

        private static ImpactBoltTraitStatusSnapshot Description(OwnedCharacterStatsSnapshot stats)
        {
            var description = stats.ImpactBoltDescription;
            Assert.That(description, Is.Not.Null);
            return description;
        }

        internal sealed class AuthoredProfile : IDisposable
        {
            internal UnitDataSO Unit { get; }
            internal TargetingMethodDataSO Method { get; }
            private readonly UnitTraitAuthoring _trait;
            private readonly CoreAuthoringFixture _fixture = new();

            internal AuthoredProfile(int distance)
            {
                var type = TypeCache.GetTypesDerivedFrom<UnitTraitAuthoring>().SingleOrDefault(t => t.Name == "ImpactBoltTraitAuthoring");
                Assert.That(type, Is.Not.Null, "Impact Bolt authoring is missing.");
                _trait = (UnitTraitAuthoring)Activator.CreateInstance(type);
                Method = _fixture.CreateTargetingMethod(7);
                Unit = _fixture.CreateUnit(EntityKey.Unit_Cowboy, CategoryKey.Cannon, Method, _trait);
                Set(Unit, "maxHealth", 15);
                Set(Unit, "physicalDefense", 5);
                Set(Unit, "attackRange", 4);
                Set(Unit, "attackPower", 100);
                Set(Unit, "priority", 30);
                Set(Method, "shape", TargetingShape.RadialSquare);
                Set(Method, "choiceAngle", TargetingChoiceAngle.Unrestricted);
                Set(Method, "splash", TargetingSplash.Cross);
                Set(Method, "splashPercent", 40);
                SetDistance(distance);
            }

            internal void SetDistance(int distance) => Set(_trait, "distanceCells", distance);
            internal UnitData ReadUnit() => (UnitData)Unit.GetData();
            internal TargetingMethodData ReadMethod() => (TargetingMethodData)Method.GetData();

            internal CoreIntegrationTestData Database(UnitData unit = null)
            {
                // 본부가 먼저 죽어 종료되지 않도록 입력 체력만 높인다. 준비 이후 상태를 덮어쓰지 않는다.
                var data = new CoreIntegrationTestData(ConfiguredMovementBattle.Config, headquartersHealth: 1000);
                data.SetUnit(unit ?? ReadUnit());
                data.SetTargetingMethod(ReadMethod());
                var enemy = data.GetUnit(EntityKey.Unit_Demon);
                data.SetUnit(new UnitData(enemy.Key, enemy.Role, enemy.OwnerEligibility, enemy.FootprintWidth, enemy.FootprintHeight,
                    1000, 0, enemy.AttackRange, enemy.Category, enemy.AttackPower, enemy.Priority, enemy.Traits.ToArray(),
                    enemy.TargetingMethodKey, enemy.Occlusion, enemy.Behavior, enemy.KillReward));
                return data;
            }

            internal static void Set(object target, string field, object value) =>
                CoreAuthoringFixture.Set(target, field, value);

            public void Dispose()
            {
                _fixture.Dispose();
            }
        }
    }
}
