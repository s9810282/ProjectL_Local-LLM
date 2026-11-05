using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class HaboobGimmickTests
    {
        [Test]
        public void DefaultFootprintHasRows2_4_4_2()
        {
            var state = Prepare(4, 4);
            var area = (HaboobGimmickState)state.Gimmicks.Get(new StageGimmickId(1));
            Assert.That(area.Cells.GroupBy(c => c.Y).Select(g => g.Count()), Is.EqualTo(new[] { 2, 4, 4, 2 }));
            Assert.That(area.Phase, Is.EqualTo(HaboobGimmickPhase.Active));
            Assert.That(area.NextBoundaryCount, Is.EqualTo(1));
        }

        [Test]
        public void FullMaskMustFit()
        {
            Assert.That(Assert.Throws<InvalidOperationException>(() => Prepare(3, 4)).Message, Does.Contain("영역"));
            Assert.That(Assert.Throws<InvalidOperationException>(() => Prepare(8, 8, new HaboobGimmickSpec(footprintOffsets:
                new[] { CellCoord.Zero, new CellCoord(int.MaxValue, 0) }))).Message, Does.Contain("영역"));
        }

        [Test]
        public void FixedInitialAnchorPlacesMaskWithoutConsumingRandom()
        {
            var spec = new HaboobGimmickSpec(initialAnchor: new CellCoord(2, 3));
            var before = Base(8, 8);
            var state = StageGimmickComposition.Initialize(before, new StageGimmickSpec[] { spec });
            var area = (HaboobGimmickState)state.Gimmicks.Entries.Single();
            Assert.That(area.Cells, Is.EqualTo(spec.FootprintOffsets.Select(c => new CellCoord(c.X + 2, c.Y + 3))));
            Assert.That(area.Cells.Contains(new CellCoord(2, 3)), Is.False, "기본 모양은 기준점 (0,0)이 빈칸이다.");
            Assert.That(state.EnvironmentProgress.RandomState, Is.EqualTo(before.EnvironmentProgress.RandomState));
        }

        [Test]
        public void FixedInitialAnchorMustKeepWholeMaskInside()
        {
            Assert.DoesNotThrow(() => Prepare(8, 8, new HaboobGimmickSpec(initialAnchor: new CellCoord(4, 4))));
            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                Prepare(8, 8, new HaboobGimmickSpec(initialAnchor: new CellCoord(5, 0)))).Message, Does.Contain("시작 위치"));
            Assert.That(Assert.Throws<InvalidOperationException>(() =>
                Prepare(8, 8, new HaboobGimmickSpec(initialAnchor: new CellCoord(0, 5)))).Message, Does.Contain("시작 위치"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HaboobGimmickSpec(initialAnchor: new CellCoord(-1, 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HaboobGimmickSpec(initialAnchor: new CellCoord(0, -1)));
        }

        [Test]
        public void RelocationAfterFixedStartIsRandomAndLeavesStart()
        {
            var spec = new HaboobGimmickSpec(initialAnchor: CellCoord.Zero);
            var input = Input(spec);
            var warning = CombatGimmickCalculator.TryAdvance(At(input, 1), new StageGimmickBoundary(Owner.Player, 1));
            var moved = CombatGimmickCalculator.TryAdvance(At(CombatCommandComposition.Project(warning), 2), new StageGimmickBoundary(Owner.Enemy, 2));
            var cells = ((HaboobGimmickState)moved.FinalState.Gimmicks.Entries.Single()).Cells;
            Assert.That((cells.Min(c => c.X), cells.Min(c => c.Y)), Is.Not.EqualTo((0, 0)));
            Assert.That(moved.FinalState.EnvironmentProgress.RandomState, Is.Not.EqualTo(input.State.EnvironmentProgress.RandomState));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OnlyCandidateKeepsArea(bool immediatelyWarning)
        {
            var spec = new HaboobGimmickSpec(2, immediatelyWarning ? 2 : 1);
            var state = Prepare(4, 4, spec);
            var initial = (HaboobGimmickState)state.Gimmicks.Entries.Single();
            var input = Input(spec, state);
            if (!immediatelyWarning)
                input = CombatCommandComposition.Project(CombatGimmickCalculator.TryAdvance(At(input, 1), new StageGimmickBoundary(Owner.Player, 1)));
            var result = CombatGimmickCalculator.TryAdvance(At(input, 2), new StageGimmickBoundary(Owner.Enemy, 2));
            var after = (HaboobGimmickState)result.FinalState.Gimmicks.Entries.Single();
            Assert.That(after.Cells, Is.EqualTo(initial.Cells));
            Assert.That(after.Phase, Is.EqualTo(immediatelyWarning ? HaboobGimmickPhase.RelocationWarning : HaboobGimmickPhase.Active));
            Assert.That(after.NextBoundaryCount, Is.EqualTo(immediatelyWarning ? 4 : 3));
            Assert.That(result.Events.Count, Is.EqualTo(1));
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(state.EnvironmentProgress.RandomState));
        }

        [Test]
        public void WarningDoesNotRevealOrSampleDestination()
        {
            var input = Input();
            var initial = (HaboobGimmickState)input.State.Gimmicks.Entries.Single();
            var warning = CombatGimmickCalculator.TryAdvance(At(input, 1), new StageGimmickBoundary(Owner.Player, 1));
            var warned = (HaboobGimmickState)warning.FinalState.Gimmicks.Entries.Single();
            Assert.That(warned.Cells, Is.EqualTo(initial.Cells));
            Assert.That(warned.Phase, Is.EqualTo(HaboobGimmickPhase.RelocationWarning));
            Assert.That(warned.NextBoundaryCount, Is.EqualTo(2));
            Assert.That(warning.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
            var moved = CombatGimmickCalculator.TryAdvance(At(CombatCommandComposition.Project(warning), 2), new StageGimmickBoundary(Owner.Enemy, 2));
            Assert.That(((HaboobGimmickState)moved.FinalState.Gimmicks.Entries.Single()).Cells.SequenceEqual(initial.Cells), Is.False);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(warning.Events));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(moved.Events));
        }

        [Test]
        public void BlockedCellsDoNotExcludeHaboob()
        {
            var state = Prepare(4, 4);
            var terrain = new MovementEnvironmentFacts(Enumerable.Range(0, 16).Select(i => (MovementTileFact)
                new StaticTerrainFact(i + 1, new CellCoord(i % 4, i / 4), i % 2 == 0 ? StaticTerrainKind.Wall : StaticTerrainKind.Cliff,
                    true, Array.Empty<Owner>())).ToArray());
            var blocked = state.With(environment: terrain, environmentProgress: new EnvironmentProgressFacts(1, 17, 42));
            var cell = new CellCoord(1, 0);
            var structure = new StructureCombatFacts(new EntityId(99), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 10, false, 0), new HealthStateSnapshot(10, 10), cell, Rotation4.R0, new[] { cell });
            blocked = blocked.With(structures: new[] { structure });
            var initialized = StageGimmickComposition.Initialize(blocked, new StageGimmickSpec[] { new HaboobGimmickSpec() });
            Assert.That(((HaboobGimmickState)initialized.Gimmicks.Entries.Single()).Cells.Count, Is.EqualTo(12));
        }

        internal static CombatCommandState Prepare(int width = 8, int height = 8, HaboobGimmickSpec spec = null) =>
            StageGimmickComposition.Initialize(Base(width, height), new StageGimmickSpec[] { spec ?? new HaboobGimmickSpec() });

        private static CombatCommandState Base(int width, int height) =>
            new CombatCommandState(width, height, CombatFlowPhase.PlayerInput, true,
                Array.Empty<StructureCombatFacts>(), Array.Empty<ThornFieldCellSnapshot>(),
                new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Player, controllerTurnSequence: 1));

        internal static CombatCommandInput Input(HaboobGimmickSpec spec = null, CombatCommandState state = null)
        {
            spec ??= new HaboobGimmickSpec();
            var source = CombatCommandTestData.Input();
            return new CombatCommandInput(1, source.Kind, source.ActorId, source.Aim, state ?? Prepare(spec: spec), source.Characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                battle: new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0,
                    Array.Empty<CellCoord>(), new StageGimmickSpec[] { spec }), battleInstanceId: 1,
                expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1);
        }

        internal static CombatCommandInput At(CombatCommandInput input, long count) => RockfallGimmickTests.At(input, count);
    }
}
