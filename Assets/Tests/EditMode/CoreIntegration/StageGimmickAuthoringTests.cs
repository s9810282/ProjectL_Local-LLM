using System;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class StageGimmickAuthoringTests
    {
        [Test]
        public void NonDefaultHaboobSettingsDriveRealPreparationAndRelocation()
        {
            using var fixture = new CoreAuthoringFixture();
            var so = fixture.Create<CombatStageData>();
            var input = new HaboobGimmickAuthoring();
            CoreAuthoringFixture.Set(input, "enabled", true);
            CoreAuthoringFixture.Set(input, "relocationIntervalControllerTurns", 4);
            CoreAuthoringFixture.Set(input, "warningLeadControllerTurns", 2);
            var mask = new[] { new CellInput(0, 0), new CellInput(1, 0) };
            CoreAuthoringFixture.Set(input, "footprintOffsets", mask);
            CoreAuthoringFixture.Set(so, "haboob", input);
            var authored = ((CombatStageDetail)so.GetData()).Haboob;
            var db = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            db.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1, Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(),
                    Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters, new CellCoord(1, 0), Rotation4.R0,
                    new CellCoord(3, 5), Rotation4.R180, null, haboob: authored)));
            using var baseline = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config);
            var definition = CoreIntegrationTestData.DatabaseSource(baseline.Buffers).Read(db, 1, new StageKey(1101));
            Assert.That(((HaboobGimmickSpec)definition.Stage.StageGimmicks.Span[0]).RelocationIntervalControllerTurns, Is.EqualTo(4));
            CoreAuthoringFixture.Set(input, "relocationIntervalControllerTurns", 99);
            mask[0] = new CellInput(99, 99);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new FixedSource(definition), database: db);
            var initial = (HaboobGimmickState)battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries.Single();
            Assert.That(initial.Cells.Count, Is.EqualTo(2));
            Assert.That(initial.NextBoundaryCount, Is.EqualTo(2));
            uint random = battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.RandomState;
            RockfallPublicationTests.EndPlayer(battle);
            battle.AdvanceToPlayerOrDay();
            var warning = (HaboobGimmickState)battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries.Single();
            Assert.That(warning.Phase, Is.EqualTo(HaboobGimmickPhase.RelocationWarning));
            Assert.That(warning.Cells, Is.EqualTo(initial.Cells));
            Assert.That(warning.NextBoundaryCount, Is.EqualTo(4));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.RandomState, Is.EqualTo(random));
            RockfallPublicationTests.EndPlayer(battle);
            battle.AdvanceToPlayerOrDay();
            var moved = (HaboobGimmickState)battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries.Single();
            Assert.That(moved.Phase, Is.EqualTo(HaboobGimmickPhase.Active));
            Assert.That(moved.NextBoundaryCount, Is.EqualTo(6));
            Assert.That(moved.Cells.SequenceEqual(initial.Cells), Is.False);
        }

        [Test]
        public void HaboobFixedInitialAnchorReachesPreparedArea()
        {
            using var fixture = new CoreAuthoringFixture();
            var so = fixture.Create<CombatStageData>();
            var input = new HaboobGimmickAuthoring();
            CoreAuthoringFixture.Set(input, "enabled", true);
            CoreAuthoringFixture.Set(input, "footprintOffsets", new[] { new CellInput(0, 0), new CellInput(1, 0) });
            CoreAuthoringFixture.Set(input, "initialAnchor", new CellInput(1, 2));
            Assert.That(input.ToData().InitialAnchor, Is.Null, "켜지 않으면 시작 위치는 무작위다.");
            CoreAuthoringFixture.Set(input, "fixedInitialAnchor", true);
            CoreAuthoringFixture.Set(so, "haboob", input);
            var authored = ((CombatStageDetail)so.GetData()).Haboob;
            Assert.That(authored.InitialAnchor, Is.EqualTo(new CellCoord(1, 2)));
            var db = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            db.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1, Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(),
                    Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters, new CellCoord(1, 0), Rotation4.R0,
                    new CellCoord(3, 5), Rotation4.R180, null, haboob: authored)));
            using var baseline = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config);
            var definition = CoreIntegrationTestData.DatabaseSource(baseline.Buffers).Read(db, 1, new StageKey(1101));
            Assert.That(((HaboobGimmickSpec)definition.Stage.StageGimmicks.Span[0]).InitialAnchor, Is.EqualTo(new CellCoord(1, 2)));
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new FixedSource(definition), database: db);
            var initial = (HaboobGimmickState)battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries.Single();
            Assert.That(initial.Cells, Is.EqualTo(new[] { new CellCoord(1, 2), new CellCoord(2, 2) }));
        }

        [TestCase(0)]
        [TestCase(3)]
        public void InvalidHaboobLeadAndMaskAreRejected(int lead)
        {
            var input = new HaboobGimmickAuthoring();
            CoreAuthoringFixture.Set(input, "enabled", true);
            CoreAuthoringFixture.Set(input, "warningLeadControllerTurns", lead);
            Assert.Throws<ArgumentOutOfRangeException>(() => input.ToData());
            CoreAuthoringFixture.Set(input, "warningLeadControllerTurns", 1);
            CoreAuthoringFixture.Set(input, "footprintOffsets", new[] { new CellInput(1, 1) });
            Assert.Throws<ArgumentException>(() => input.ToData());
        }

        [Test]
        public void HaboobAuthoringIsDisabledByDefaultAndRejectsInvalidLead()
        {
            var type = typeof(CombatStageData).Assembly.GetType(typeof(CombatStageData).Namespace + ".HaboobGimmickAuthoring");
            Assert.That(type, Is.Not.Null, "Typed Haboob SO authoring is required.");
            var input = Activator.CreateInstance(type);
            var convert = type.GetMethod("ToData");
            Assert.That(convert.Invoke(input, null), Is.Null);
            CoreAuthoringFixture.Set(input, "enabled", true);
            CoreAuthoringFixture.Set(input, "warningLeadControllerTurns", 3);
            Assert.That(Assert.Throws<TargetInvocationException>(() => convert.Invoke(input, null)).InnerException,
                Is.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void RockfallAuthoringChangesReachPreparedSpec()
        {
            using var fixture = new CoreAuthoringFixture();
            var so = fixture.Create<CombatStageData>();
            var input = new RockfallGimmickAuthoring();
            CoreAuthoringFixture.Set(input, "enabled", true);
            CoreAuthoringFixture.Set(input, "cooldownControllerTurns", 3);
            CoreAuthoringFixture.Set(input, "warningControllerTurns", 1);
            CoreAuthoringFixture.Set(input, "minimumRockCount", 1);
            CoreAuthoringFixture.Set(input, "maximumRockCount", 2);
            CoreAuthoringFixture.Set(input, "maximumHealthLossPercent", 35);
            CoreAuthoringFixture.Set(input, "bindAffectedSideTurns", 2);
            var mask = Enumerable.Range(0, 6).SelectMany(y => Enumerable.Range(0, 8).Where(x => x != 3 || y != 5)
                .Select(x => new CellInput(x, y))).ToArray();
            CoreAuthoringFixture.Set(input, "footprintOffsets", mask);
            CoreAuthoringFixture.Set(so, "rockfall", input);
            var authored = ((CombatStageDetail)so.GetData()).Rockfall;
            var db = new CoreIntegrationTestData(ConfiguredMovementBattle.Config);
            db.SetStage(new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1, Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(),
                    Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters, new CellCoord(1, 0), Rotation4.R0,
                    new CellCoord(3, 5), Rotation4.R180, null, rockfall: authored)));
            using var baseline = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config);
            var definition = CoreIntegrationTestData.DatabaseSource(baseline.Buffers).Read(db, 1, new StageKey(1101));
            var spec = (RockfallGimmickSpec)definition.Stage.StageGimmicks.Span[0];
            Assert.That(spec.CooldownControllerTurns, Is.EqualTo(3));
            Assert.That(spec.MaximumHealthLossPercent, Is.EqualTo(35));
            CoreAuthoringFixture.Set(input, "maximumHealthLossPercent", 99);
            mask[0] = new CellInput(99, 99);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new FixedSource(definition), database: db);
            var health = battle.Actor.Health;
            RockfallPublicationTests.EndPlayer(battle);
            battle.AdvanceToPlayerOrDay();
            Assert.That(((RockfallGimmickState)battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries.Single()).Phase,
                Is.EqualTo(RockfallGimmickPhase.Cooldown));
            RockfallPublicationTests.EndPlayer(battle);
            Assert.That(((RockfallGimmickState)battle.Buffers.CombatStateReadSink.Current.Gimmicks.Entries.Single()).Phase,
                Is.EqualTo(RockfallGimmickPhase.Warning));
            battle.AdvanceToPlayerOrDay();
            Assert.That(battle.Actor.Health.CurrentHealth, Is.EqualTo(health.CurrentHealth - (long)health.MaxHealth * 35 / 100));
            Assert.That(battle.Actor.StatusEffects.Entries.OfType<ActionRestrictionStatus>().Single().RemainingAffectedSideTurnEnds, Is.EqualTo(2));
        }

        [TestCase("minimumRockCount", 0)]
        [TestCase("maximumRockCount", 2)]
        [TestCase("bindAffectedSideTurns", 0)]
        [TestCase("bindAffectedSideTurns", int.MaxValue)]
        public void InvalidEnabledAuthoringFailsBeforePreparation(string field, int value)
        {
            var input = new RockfallGimmickAuthoring();
            CoreAuthoringFixture.Set(input, "enabled", true);
            CoreAuthoringFixture.Set(input, field, value);
            Assert.Throws<ArgumentOutOfRangeException>(() => input.ToData());
        }

        [Test]
        public void EmptyOrDuplicateMasksAreRejected()
        {
            var input = new RockfallGimmickAuthoring();
            CoreAuthoringFixture.Set(input, "enabled", true);
            CoreAuthoringFixture.Set(input, "footprintOffsets", Array.Empty<CellInput>());
            Assert.Throws<ArgumentException>(() => input.ToData());
            CoreAuthoringFixture.Set(input, "footprintOffsets", new[] { new CellInput(0, 0), new CellInput(0, 0) });
            Assert.Throws<ArgumentException>(() => input.ToData());
        }

        private sealed class FixedSource : IBattleInitializationSource
        {
            private readonly BattleInitialization _definition;
            internal FixedSource(BattleInitialization definition) { _definition = definition; }
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey) => _definition;
        }

        [Test]
        public void DisabledRockfallIsAbsentAndEnabledInputIsFreshAndValidated()
        {
            var type = typeof(CombatStageData).Assembly.GetType(
                typeof(CombatStageData).Namespace + ".RockfallGimmickAuthoring");
            Assert.That(type, Is.Not.Null, "Typed Rockfall SO authoring is required.");
            var input = Activator.CreateInstance(type);
            var convert = type.GetMethod("ToData");
            Assert.That(convert.Invoke(input, null), Is.Null);
            CoreAuthoringFixture.Set(input, "enabled", true);
            CoreAuthoringFixture.Set(input, "cooldownControllerTurns", 3);
            var first = convert.Invoke(input, null);
            CoreAuthoringFixture.Set(input, "cooldownControllerTurns", 7);
            Assert.That(first.GetType().GetProperty("CooldownControllerTurns").GetValue(first), Is.EqualTo(3));
            Assert.That(convert.Invoke(input, null), Is.Not.SameAs(first));
            CoreAuthoringFixture.Set(input, "warningControllerTurns", 0);
            Assert.That(Assert.Throws<TargetInvocationException>(() => convert.Invoke(input, null)).InnerException,
                Is.InstanceOf<ArgumentOutOfRangeException>());
        }
    }
}
