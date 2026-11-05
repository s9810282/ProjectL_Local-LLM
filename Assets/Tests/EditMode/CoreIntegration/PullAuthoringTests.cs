using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using UnityEditor;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PullAuthoringTests
    {
        [TestCase(null, 1)]
        [TestCase(0, 0)]
        [TestCase(2, 2)]
        public void ConfiguredExtension_RoundTripsAuthoringAndAdapter(int? authored, int expected)
        {
            var type = TypeCache.GetTypesDerivedFrom<UnitTraitAuthoring>().SingleOrDefault(t => t.Name == "PullTraitAuthoring");
            Assert.That(type, Is.Not.Null, "Pull authoring is missing.");
            var authoring = (UnitTraitAuthoring)Activator.CreateInstance(type);
            if (authored.HasValue)
                type.GetField("extraRangeCells", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(authoring, authored.Value);
            var data = authoring.ToData();
            data.Validate();
            var spec = TraitSpecAdapter.Read(data);
            Assert.That((int)data.TraitKey, Is.EqualTo(200));
            Assert.That(spec.GetType().GetProperty("ExtraRangeCells").GetValue(spec), Is.EqualTo(expected));
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new PullBattleSource(spec, range: 2));
            var status = battle.Actor.Stats.TraitStatuses.ToArray().OfType<PullTraitStatusSnapshot>().Single();
            Assert.That(status.ExtraRangeCells, Is.EqualTo(expected));
            Assert.That(status.IsAvailable, Is.True);
            battle.Submit(battle.Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value));
        }

        [TestCase(1, false)]
        [TestCase(2, true)]
        public void EditedExtension_ChangesActualQueryPreviewAndExecution(int extra, bool accepted)
        {
            var authoring = new PullTraitAuthoring();
            typeof(PullTraitAuthoring).GetField("extraRangeCells", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(authoring, extra);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new PullBattleSource(TraitSpecAdapter.Read(authoring.ToData()), target: new CellCoord(0, 4)));
            var target = battle.Character(EntityKey.Unit_Demon);
            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(target.Anchor.Value)));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            Assert.That(preview.Attack.Value.CanAttack, Is.EqualTo(accepted));
            battle.Buffers.CombatViewWriteSink.Submit(battle.Attack(battle.Actor, target.Anchor.Value));
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.EqualTo(accepted));
            Assert.That(battle.Character(EntityKey.Unit_Demon).Anchor, Is.EqualTo(new CellCoord(0, accepted ? 2 : 4)));
            Assert.That(battle.Character(EntityKey.Unit_Demon).Health.CurrentHealth, Is.EqualTo(accepted ? 95 : 100));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(accepted ? UnitActionUse.Attacked : UnitActionUse.Available));
        }

        [Test]
        public void NegativeAuthoredExtension_IsRejected()
        {
            var data = new PullTraitData(-1);
            Assert.Throws<InvalidOperationException>(() => data.Validate());
            Assert.Throws<InvalidOperationException>(() => TraitSpecAdapter.Read(data));
        }

        [TestCase(0)]
        [TestCase(2)]
        [TestCase(7)]
        public void OwnedAndBattleProjections_PreserveConfiguredExtension(int extra)
        {
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            var source = database.GetUnit(EntityKey.Unit_Cowboy);
            const int pullMethodKey = 2;
            database.SetTargetingMethod(new ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item.TargetingMethodData(
                pullMethodKey, TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            database.SetUnit(new UnitData(source.Key, source.Role, source.OwnerEligibility, source.FootprintWidth,
                source.FootprintHeight, source.MaxHealth, source.PhysicalDefense, source.AttackRange, source.Category,
                source.AttackPower, source.Priority, new UnitTraitData[] { new PullTraitData(extra) },
                pullMethodKey, source.Occlusion, source.Behavior, source.KillReward));
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new PullBattleSource(new PullTraitSpec(extra)), database: database);
            var owned = battle.OwnedStats.PullDescription;
            var combat = battle.Actor.Stats.TraitStatuses.ToArray().OfType<PullTraitStatusSnapshot>().Single();
            Assert.That(owned.ExtraRangeCells, Is.EqualTo(extra));
            Assert.That(combat.SameValues(owned), Is.True);
        }

        [Test]
        public void ConfiguredPullUnit_UsesRegisteredTargeting()
        {
            using var fixture = new CoreAuthoringFixture();
            var method = fixture.CreateTargetingMethod(2);
            CoreAuthoringFixture.Set(method, "shape", TargetingShape.CardinalLine);
            var authoring = new PullTraitAuthoring();
            CoreAuthoringFixture.Set(authoring, "extraRangeCells", 3);
            var unit = fixture.CreateUnit(EntityKey.Unit_Cowboy, CategoryKey.Rider, method, authoring);
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            database.SetTargetingMethod((TargetingMethodData)method.GetData());
            database.SetUnit((UnitData)unit.GetData());
            var data = database.InGameQuery.UnitTableQuery.GetUnit(EntityKey.Unit_Cowboy);
            data.Validate();
            var spec = UnitSpecAdapter.ReadUnit(data);
            Assert.That((spec.FootprintWidth, spec.FootprintHeight), Is.EqualTo((1, 1)));
            var pull = data.Traits.OfType<PullTraitData>().Single();
            Assert.That(pull.ExtraRangeCells, Is.EqualTo(3));
            Assert.DoesNotThrow(() => TraitSpecAdapter.Read(pull));
            var profile = BattleDefinitionAdapter.ReadAttackProfiles(database, new[] { spec }).Single();
            Assert.That(profile.Definition.Key, Is.EqualTo(EntityKey.Unit_Cowboy));
            Assert.That(profile.Method.Shape, Is.EqualTo(TargetingShape.CardinalLine));
        }

        [Test]
        public void SharedTargetingReference_ExportsKeysWithoutMutatingPriorData()
        {
            using var fixture = new CoreAuthoringFixture();
            var method = fixture.CreateTargetingMethod(17);
            var units = new[]
            {
                fixture.CreateUnit(EntityKey.Unit_Cowboy, CategoryKey.Rider, method),
                fixture.CreateUnit(EntityKey.Unit_Sniper, CategoryKey.Cannon, method)
            };
            var before = units.Select(unit => (UnitData)unit.GetData()).ToArray();
            CoreAuthoringFixture.Set(method, "methodKey", 23);
            var after = units.Select(unit => (UnitData)unit.GetData()).ToArray();
            Assert.That(before.Select(unit => unit.TargetingMethodKey), Is.EqualTo(new[] { 17, 17 }));
            Assert.That(after.Select(unit => unit.TargetingMethodKey), Is.EqualTo(new[] { 23, 23 }));
            var database = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            database.SetTargetingMethod((TargetingMethodData)method.GetData());
            foreach (var unit in after) database.SetUnit(unit);
            var profiles = BattleDefinitionAdapter.ReadAttackProfiles(database, after.Select(UnitSpecAdapter.ReadUnit).ToArray());
            Assert.That(profiles.Count, Is.EqualTo(2));
            Assert.That(profiles.All(profile => profile.Method.Shape == TargetingShape.RadialSquare &&
                profile.Method.Splash == TargetingSplash.None), Is.True);
        }
    }
}
