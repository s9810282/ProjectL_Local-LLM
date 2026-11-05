using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Enum;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Terrain.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TornadoGimmickAuthoringTests
    {
        [Test]
        public void NonDefaultSoUsesOneTuningSourceForFixedRandomAndPreparedGeometry()
        {
            using var fixture = new CoreAuthoringFixture();
            var so = fixture.Create<TerrainDefinitionDataSO>();
            CoreAuthoringFixture.Set(so, "kind", TerrainContentKind.Tornado);
            CoreAuthoringFixture.Set(so, "firstActivationAfterUnitTurns", 5L);
            CoreAuthoringFixture.Set(so, "repeatIntervalUnitTurns", 4);
            CoreAuthoringFixture.Set(so, "tornadoSideLength", 5);
            CoreAuthoringFixture.Set(so, "tornadoDiagonalDistance", 2);
            CoreAuthoringFixture.Set(so, "tornadoWarningLeadControllerTurns", 2);
            var data = (TerrainDefinitionData)so.GetData();
            var fixedMember = data.CreateTornadoMember(31, new[] { Owner.Enemy }, new CellCoord(2, 2));
            var randomMember = data.CreateTornadoMember(32, new[] { Owner.Player });
            CoreAuthoringFixture.Set(so, "tornadoSideLength", 3);
            uint random = 71;
            var group = new TornadoGimmickSpec(new[] { randomMember, fixedMember });
            var calculator = typeof(BattlePreparationCalculator).Assembly.GetType(
                typeof(BattlePreparationCalculator).Namespace + ".StageGimmicks.TornadoGimmickPreparationCalculator");
            var prepareArgs = new object[] { group, new StageGimmickId(1), 10, 10, random, 0L, "SO profile" };
            var state = (TornadoGimmickState)calculator.GetMethod("Prepare", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, prepareArgs);
            Assert.That(state.NextBoundaryCount, Is.EqualTo(3));
            Assert.That(TornadoGeometry.Footprint(state.Members[0].Center, fixedMember.SideLength).Count, Is.EqualTo(25));
            var warnArgs = new object[] { fixedMember, state.Members[0], 10, 10, prepareArgs[4] };
            var warning = (TornadoMemberState)calculator.GetMethod("Warn", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, warnArgs);
            var travel = ((TornadoMemberState State, IReadOnlyList<CellCoord> Path))calculator.GetMethod("CompleteTravel", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { fixedMember, warning, 10, 10, warnArgs[4] });
            Assert.That(travel.Path, Is.EqualTo(new[] { new CellCoord(2, 2), new CellCoord(3, 3), new CellCoord(4, 4) }));
            Assert.That(travel.State.NextBoundaryCount, Is.EqualTo(7));
            Assert.That(fixedMember.KnownTo, Is.EqualTo(new[] { Owner.Enemy }));
            Assert.That(randomMember.FirstActivationAfterControllerTurns, Is.EqualTo(5));
            Assert.That(randomMember.RepeatIntervalControllerTurns, Is.EqualTo(4));
        }

        [Test]
        public void ExistingStageAuthoringProducesOneGroupWithoutSwitchingLegacyRuntimeInput()
        {
            using var fixture = new CoreAuthoringFixture();
            var terrain = fixture.Create<TerrainDefinitionDataSO>();
            CoreAuthoringFixture.Set(terrain, "kind", TerrainContentKind.Tornado);
            var stage = fixture.Create<CombatStageData>();
            var environment = new CombatStageEnvironmentInput();
            var element = typeof(CombatStageEnvironmentInput).GetNestedType("TerrainPlacementInput", BindingFlags.NonPublic);
            var placements = Array.CreateInstance(element, 2);
            for (int i = 0; i < 2; i++)
            {
                var placement = Activator.CreateInstance(element);
                CoreAuthoringFixture.Set(placement, "definition", terrain);
                CoreAuthoringFixture.Set(placement, "sourceId", 32L - i);
                CoreAuthoringFixture.Set(placement, "cell", new CellInput(2, 2));
                CoreAuthoringFixture.Set(placement, "knownTo", new[] { Owner.Enemy });
                CoreAuthoringFixture.Set(placement, "randomizeInitialPosition", i == 0);
                CoreAuthoringFixture.Set(placement, "tornadoInitialHorizontalSign", -1);
                placements.SetValue(placement, i);
            }
            CoreAuthoringFixture.Set(environment, "terrainPlacements", placements);
            CoreAuthoringFixture.Set(stage, "environment", environment);
            var detail = (CombatStageDetail)stage.GetData();
            var property = typeof(CombatStageDetail).GetProperty("Tornado");
            Assert.That(property, Is.Not.Null, "작성 입력에서 준비용 그룹 명세를 보존해야 합니다.");
            var spec = (TornadoGimmickSpec)property.GetValue(detail);
            Assert.That(spec.Members.Select(m => m.SourceId), Is.EqualTo(new long[] { 31, 32 }));
            Assert.That(spec.Members[0].InitialHorizontalSign, Is.EqualTo(-1));
            Assert.That(spec.Members[1].InitialCenter, Is.Null);
            Assert.That(detail.TornadoPlacements.Single().Id, Is.EqualTo(31));
            Assert.That(detail.InitialRandomTornadoPlacements.Single().SourceId, Is.EqualTo(32));
            var emptyStage = fixture.Create<CombatStageData>();
            Assert.That(property.GetValue((CombatStageDetail)emptyStage.GetData()), Is.Null);
        }

        [TestCase("tornadoSideLength", 2)]
        [TestCase("tornadoDiagonalDistance", 0)]
        [TestCase("tornadoWarningLeadControllerTurns", 0)]
        [TestCase("tornadoWarningLeadControllerTurns", 4)]
        public void InvalidSoProfileFailsAtInputConversion(string field, int value)
        {
            using var fixture = new CoreAuthoringFixture();
            var so = fixture.Create<TerrainDefinitionDataSO>();
            CoreAuthoringFixture.Set(so, "kind", TerrainContentKind.Tornado);
            CoreAuthoringFixture.Set(so, field, value);
            Assert.Throws<InvalidOperationException>(() => so.GetData());
        }
    }
}
