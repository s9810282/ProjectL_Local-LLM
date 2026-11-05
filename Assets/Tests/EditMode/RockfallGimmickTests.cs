using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class RockfallGimmickTests
    {
        [Test]
        public void DefaultCycleWarnsAt2And6AndImpactsAt4And8()
        {
            var input = Input();
            for (int count = 1; count <= 8; count++)
            {
                input = At(input, count);
                uint rng = input.State.EnvironmentProgress.RandomState;
                var result = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Player, count));
                if (count % 2 != 0) { Assert.That(result, Is.Null); continue; }
                Assert.That(result, Is.Not.Null);
                var facts = (RockfallGimmickState)result.FinalState.Gimmicks.Get(new StageGimmickId(1));
                Assert.That(facts.Phase, Is.EqualTo(count % 4 == 2 ? RockfallGimmickPhase.Warning : RockfallGimmickPhase.Cooldown));
                Assert.That(facts.NextBoundaryCount, Is.EqualTo(count + 2));
                if (count % 4 == 0) Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(rng));
                else Assert.That(facts.WarnedCells.Count, Is.InRange(3, 5));
                Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result.Events));
                input = CombatCommandComposition.Project(result);
            }
        }

        [Test]
        public void PreviewDoesNotConsumeSelectionOrRng()
        {
            var input = At(Input(), 2);
            var state = input.State;
            CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var a = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Player, 2));
            var b = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Player, 2));
            Assert.That(input.State, Is.SameAs(state));
            Assert.That(a.FinalState.SameValues(b.FinalState), Is.True);
        }

        [Test]
        public void OccupiedWalkableCellsRemainCandidates()
        {
            var input = At(Input(new RockfallGimmickSpec(minimumRockCount: 64, maximumRockCount: 64)), 2);
            var result = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Player, 2));
            var warning = (RockfallGimmickState)result.FinalState.Gimmicks.Get(new StageGimmickId(1));
            Assert.That(warning.WarnedCells.Count, Is.EqualTo(64));
            Assert.That(warning.WarnedCells, Does.Contain(input.Characters[0].Anchor.Value));
        }

        [Test]
        public void ShortageUsesAvailableDistinctCells()
        {
            var spec = new RockfallGimmickSpec(minimumRockCount: 100, maximumRockCount: 100,
                footprintOffsets: new[] { CellCoord.Zero, new CellCoord(1, 0) });
            var result = CombatGimmickCalculator.TryAdvance(At(Input(spec), 2), new StageGimmickBoundary(Owner.Player, 2));
            var warning = (RockfallGimmickState)result.FinalState.Gimmicks.Get(new StageGimmickId(1));
            Assert.That(warning.WarnedCells.Count, Is.EqualTo(64));
            Assert.That(warning.WarnedCells.Distinct().Count(), Is.EqualTo(64));
        }

        [Test]
        public void ZeroCandidatesStillCompletesCycle()
        {
            var spec = new RockfallGimmickSpec(footprintOffsets: new[] { CellCoord.Zero, new CellCoord(8, 0) });
            var input = At(Input(spec), 2);
            var warned = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Player, 2));
            Assert.That(((RockfallGimmickState)warned.FinalState.Gimmicks.Get(new StageGimmickId(1))).WarnedCells, Is.Empty);
            var impact = CombatGimmickCalculator.TryAdvance(At(CombatCommandComposition.Project(warned), 4), new StageGimmickBoundary(Owner.Player, 4));
            Assert.That(((RockfallGimmickState)impact.FinalState.Gimmicks.Get(new StageGimmickId(1))).Phase, Is.EqualTo(RockfallGimmickPhase.Cooldown));
            Assert.That(impact.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(warned.FinalState.EnvironmentProgress.RandomState));
        }

        [Test]
        public void OversizedValidMaskCompletesAnEmptyWarningWithoutCoordinateOverflow()
        {
            var spec = new RockfallGimmickSpec(footprintOffsets: new[] { CellCoord.Zero, new CellCoord(int.MaxValue, 0) });
            Assert.DoesNotThrow(() => CombatGimmickCalculator.TryAdvance(At(Input(spec), 2), new StageGimmickBoundary(Owner.Player, 2)));
        }

        [Test]
        public void WallsAndCliffsAreExcludedButSandstormAndOccupiedCellsRemain()
        {
            var input = At(Input(new RockfallGimmickSpec(minimumRockCount: 64, maximumRockCount: 64)), 2);
            var terrain = new MovementEnvironmentFacts(new MovementTileFact[] {
                new StaticTerrainFact(1, CellCoord.Zero, StaticTerrainKind.Wall, true, Array.Empty<Owner>()),
                new StaticTerrainFact(2, new CellCoord(0, 1), StaticTerrainKind.Cliff, true, Array.Empty<Owner>()),
                new StaticTerrainFact(3, new CellCoord(0, 2), StaticTerrainKind.Sandstorm, true, Array.Empty<Owner>()) });
            input = input.WithWorld(input.State.With(environment: terrain,
                environmentProgress: new EnvironmentProgressFacts(1, 4, input.State.EnvironmentProgress.RandomState)), input.Characters, input.Main, input.SoulCounts,
                input.Souls, new EnvironmentSemanticBindings(new long[] { 1, 2, 3 }, Array.Empty<long>(), Array.Empty<EntityId>()));
            var warned = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Player, 2));
            var cells = warned.Events.OfType<CombatRockfallEvent>().Single().Final.WarnedCells;
            Assert.That(cells.Count, Is.EqualTo(62));
            Assert.That(cells.Contains(CellCoord.Zero), Is.False);
            Assert.That(cells.Contains(new CellCoord(0, 1)), Is.False);
            Assert.That(cells, Does.Contain(new CellCoord(0, 2)));
        }

        [Test]
        public void ImpactKeepsWarnedCellsAfterTerrainChangesAndDeduplicatesOverlappingMasks()
        {
            var spec = new RockfallGimmickSpec(minimumRockCount: 64, maximumRockCount: 64,
                footprintOffsets: new[] { CellCoord.Zero, new CellCoord(1, 0) });
            var warned = CombatGimmickCalculator.TryAdvance(At(Input(spec), 2), new StageGimmickBoundary(Owner.Player, 2));
            var input = At(CombatCommandComposition.Project(warned), 4);
            var wall = new StaticTerrainFact(1, input.Characters[0].Anchor.Value, StaticTerrainKind.Wall, true, Array.Empty<Owner>());
            input = input.WithWorld(input.State.With(environment: new MovementEnvironmentFacts(new[] { wall }),
                environmentProgress: new EnvironmentProgressFacts(1, 2, input.State.EnvironmentProgress.RandomState)), input.Characters,
                input.Main, input.SoulCounts, input.Souls, new EnvironmentSemanticBindings(new long[] { 1 }, Array.Empty<long>(), Array.Empty<EntityId>()));
            var result = CombatGimmickCalculator.TryAdvance(input, new StageGimmickBoundary(Owner.Enemy, 4));
            Assert.That(result.Events.OfType<CombatRockfallEvent>().Single().Expected.SameValues(warned.Events.OfType<CombatRockfallEvent>().Single().Final), Is.True);
            Assert.That(result.Events.OfType<CombatRockfallEvent>().Single().RecipientIds.Count, Is.EqualTo(input.Characters.Count));
            Assert.That(CombatCommandComposition.Project(result).Characters.Select(c => c.Health.CurrentHealth), Is.All.EqualTo(80));
        }

        internal static CombatCommandInput Input(RockfallGimmickSpec spec = null)
        {
            var source = CombatCommandTestData.Input();
            var state = source.State.With(gimmicks: new StageGimmickStateSet(new StageGimmickState[] {
                new RockfallGimmickState(new StageGimmickId(1), RockfallGimmickPhase.Cooldown, 2, Array.Empty<CellCoord>()) }));
            var battle = new CombatBattleFacts(8, 42, 2, 2, Array.Empty<CharacterInstanceId>(), 0,
                Array.Empty<CellCoord>(), new StageGimmickSpec[] { spec ?? new RockfallGimmickSpec() });
            return new CombatCommandInput(1, source.Kind, source.ActorId, source.Aim, state, source.Characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts, battle: battle,
                battleInstanceId: 1, expectedBattleInstanceId: 1, expectedControllerTurnSequence: 1);
        }

        internal static CombatCommandInput At(CombatCommandInput input, long count) => CombatCommandTestData.With(input,
            state: input.State.With(turn: new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Player,
                controllerTurnSequence: 1, completedControllerTurnCount: count)));
    }
}
