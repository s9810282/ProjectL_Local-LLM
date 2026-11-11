using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyAiBaselineTests
    {
        internal static IEnumerable<string> Fixtures => EnemyAiBenchmarkFixtures.Ids;

        [TestCaseSource(nameof(Fixtures))]
        public void FixtureManifest_RebuildsIdenticalVisibleFacts(string fixtureId)
        {
            var first = EnemyAiBenchmarkFixtures.Build(fixtureId, 1);
            var rebuilt = EnemyAiBenchmarkFixtures.Build(fixtureId, 1);
            Assert.That(first.Characters.Zip(rebuilt.Characters, (a, b) => a.SameValues(b)), Is.All.True);
            Assert.That(first.State.Environment.SameValues(rebuilt.State.Environment), Is.True);
            Assert.That(first.State.Gimmicks.SameValues(rebuilt.State.Gimmicks), Is.True);
            string manifest = EnemyAiBenchmarkFixtures.Describe(first, true);
            Assert.That(EnemyAiBenchmarkFixtures.Describe(rebuilt, true), Is.EqualTo(manifest));
            Assert.That(EnemyAiBenchmarkFixtures.Hash(EnemyAiBenchmarkFixtures.Describe(rebuilt, true)),
                Is.EqualTo(EnemyAiBenchmarkFixtures.Hash(manifest)));
            string visible = EnemyAiBenchmarkFixtures.Describe(first, false);
            foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
                Assert.That(EnemyAiBenchmarkFixtures.Describe(EnemyAiBenchmarkFixtures.Build(fixtureId, seed), false), Is.EqualTo(visible));
        }

        [TestCaseSource(nameof(Fixtures))]
        public void Fixtures_HaveDeclaredActorsAndValidBoard(string fixtureId)
        {
            var input = EnemyAiBenchmarkFixtures.Build(fixtureId, 1);
            var state = input.State;
            Assert.That(input.Characters.Count(c => c.Owner == Owner.Enemy), Is.EqualTo(EnemyAiBenchmarkFixtures.EnemyCount(fixtureId)));
            Assert.That(input.Characters.Count(c => c.Owner == Owner.Player), Is.EqualTo(4));
            Assert.That(input.Characters.Select(c => c.EntityId), Is.Unique);
            var cells = input.Characters.SelectMany(c => c.OccupiedCells).ToArray();
            Assert.That(cells, Is.Unique);
            Assert.That(cells.All(c => c.X >= 0 && c.Y >= 0 && c.X < state.BoardWidth && c.Y < state.BoardHeight), Is.True);
            Assert.That(state.Phase, Is.EqualTo(CombatFlowPhase.Automatic));
            Assert.That(state.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
            var transition = CombatCommandCalculator.Advance(input, EnemyAiBenchmarkFixtures.Rules);
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            if (fixtureId == "U8") Assert.That(transition.Events.Any(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.True);
            if (fixtureId == "W8") Assert.That(transition.PathMovement, Is.Not.Null);
        }

        [TestCaseSource(nameof(Fixtures))]
        public void Counters_DoNotAffectSelectedAction(string fixtureId)
        {
            var input = EnemyAiBenchmarkFixtures.Build(fixtureId, 7);
            string before = EnemyAiBenchmarkFixtures.Describe(input, true);
            var baseline = CombatCommandCalculator.Advance(input, EnemyAiBenchmarkFixtures.Rules);
            var counters = new EnemyDecisionDiagnostics(captureAlternatives: true);
            var measured = CombatCommandCalculator.Advance(input, EnemyAiBenchmarkFixtures.Rules, counters);
            Assert.That(Trace(measured), Is.EqualTo(Trace(baseline)));
            Assert.That(CombatCommandComposition.Project(measured).Characters
                .Zip(CombatCommandComposition.Project(baseline).Characters, (a, b) => a.SameValues(b)), Is.All.True);
            Assert.That(measured.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(baseline.FinalState.EnvironmentProgress.RandomState));
            Assert.That(EnemyAiBenchmarkFixtures.Describe(input, true), Is.EqualTo(before));
            Assert.That(counters.WorkUnits, Is.EqualTo(counters.ExpandedNodes + counters.PreviewCalls));
            Assert.That(counters.WorkUnits, Is.Positive);
        }

        [Test]
        public void ExplanationStorage_IsBoundedAtFourAlternatives()
        {
            var enabled = new EnemyDecisionDiagnostics(captureAlternatives: true);
            var disabled = new EnemyDecisionDiagnostics();
            for (int i = 1; i <= 12; i++)
            {
                enabled.RecordAlternative(new EntityId(i), null, CellCoord.Zero);
                disabled.RecordAlternative(new EntityId(i), null, CellCoord.Zero);
            }
            Assert.That(enabled.AlternativeCount, Is.EqualTo(4));
            Assert.That(enabled.GetAlternative(3).ActorId, Is.EqualTo(new EntityId(4)));
            Assert.Throws<ArgumentOutOfRangeException>(() => enabled.GetAlternative(4));
            Assert.That(disabled.AlternativeCount, Is.Zero);
        }

        internal static string Trace(CombatCommandTransition result, AttackAim? selectedAim = null)
        {
            var attack = result.Attack;
            var move = result.PathMovement;
            var use = result.ActionUseSettlement;
            var taunt = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .FirstOrDefault(t => t.TraitKey == TraitKey.Taunt && t.TraitActivationKind == TraitActivationKind.TauntSelected);
            return string.Join("\t", result.Accepted, use?.ActorId.Value ?? 0,
                attack.HasValue ? "Attack" : move != null ? "Move" : "Boundary",
                attack?.TargetCell is CellCoord cell ? EnemyAiBenchmarkFixtures.Cell(cell) : "",
                attack?.AttackDirection.ToString() ?? "",
                move == null ? "" : EnemyAiBenchmarkFixtures.Cells(move.RequestedPath.Cells),
                taunt?.SourceUnitId.Value ?? 0,
                use?.BeforeActionUse.ToString() ?? "", use?.FinalActionUse.ToString() ?? "",
                result.FinalState.Turn.ControllerPhase,
                string.Join(",", result.Events.Select(e => e.Kind)),
                selectedAim?.Kind.ToString() ?? "",
                selectedAim?.Cell is CellCoord aimCell ? EnemyAiBenchmarkFixtures.Cell(aimCell) : "",
                selectedAim?.Direction.ToString() ?? "");
        }
    }
}
