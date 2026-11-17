using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class TornadoGimmickEvidenceTests
    {
        [Test]
        public void ScheduledGroupAndEntryHaveReadableValidatedEvidence()
        {
            var raw = TornadoGimmickResolutionTests.Advance(TornadoGimmickResolutionTests.Input(center: new CellCoord(2, 2), warned: true), 2);
            var result = WithBoundary(raw);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
            var input = TornadoGimmickResolutionTests.Input();
            var projection = new MovementProjection(input);
            TornadoCalculator.ResolveGroupedEntry(projection, input.ActorId, input.Path.Origin, TornadoEnvironmentTests.Rules);
            result = projection.Complete();
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Single(c => c.Kind == CombatEventKind.Relocated).TornadoContact.StepIndex, Is.EqualTo(-1));
        }

        [Test]
        public void WarningAndSeveralSourceAdvancesUseOneValidatedGroupChange()
        {
            var warning = WithBoundary(TornadoGimmickResolutionTests.Advance(TornadoGimmickResolutionTests.Input(count: 1, multiple: true), 1));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(warning));
            var travel = WithBoundary(TornadoGimmickResolutionTests.Advance(TornadoGimmickResolutionTests.Input(
                center: new CellCoord(2, 2), warned: true, multiple: true), 2));
            Assert.That(travel.Events.OfType<CombatTornadoGimmickEvent>().Count(), Is.EqualTo(1));
            Assert.That(travel.Events.OfType<CombatTornadoGimmickEvent>().Single().Advances.Select(a => a.Expected.SourceId), Is.EqualTo(new long[] { 31, 32 }));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(travel.Events));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(travel));
        }

        [TestCase("missing-center")]
        [TestCase("reordered-centers")]
        [TestCase("missing-contact-origin")]
        [TestCase("duplicate-throw")]
        [TestCase("wrong-source")]
        [TestCase("stale-group")]
        [TestCase("wrong-deadline")]
        [TestCase("wrong-horizontal")]
        public void ForgedOrderedEvidenceFailsClosed(string corruption)
        {
            var raw = TornadoGimmickResolutionTests.Advance(TornadoGimmickResolutionTests.Input(center: new CellCoord(2, 2), warned: true), 2);
            var result = WithBoundary(raw);
            var events = result.Events.ToList();
            int center = events.FindIndex(e => e is CombatTornadoCenterEvent);
            int relocation = events.FindIndex(e => e is CombatCharacterEvent c && c.Kind == CombatEventKind.Relocated);
            var move = (CombatCharacterEvent)events[relocation];
            var group = events.OfType<CombatTornadoGimmickEvent>().Single();
            if (corruption == "missing-center") events.RemoveAt(center);
            if (corruption == "reordered-centers")
            {
                int next = events.FindIndex(center + 1, e => e is CombatTornadoCenterEvent);
                (events[center], events[next]) = (events[next], events[center]);
            }
            if (corruption == "missing-contact-origin") events[relocation] = new CombatCharacterEvent(move.Kind, move.Expected, move.Final,
                move.DamageOrigins, move.TornadoSourceId, move.DamageCause, move.MovementCause);
            if (corruption == "duplicate-throw") events.Insert(relocation + 1, move);
            if (corruption == "wrong-source") events[relocation] = new CombatCharacterEvent(move.Kind, move.Expected, move.Final,
                move.DamageOrigins, 999, move.DamageCause, move.MovementCause,
                new TornadoContactOrigin(group.Expected.Id, 999, move.TornadoContact.Center, move.TornadoContact.StepIndex));
            if (corruption == "stale-group")
            {
                var old = group.Expected.Members[0];
                var stale = new TornadoGimmickState(group.Expected.Id, new[] { new TornadoMemberState(old.SourceId,
                    new CellCoord(1, 1), old.HorizontalSign, old.Phase, old.NextBoundaryCount, old.LockedDirection) });
                events[events.IndexOf(group)] = new CombatTornadoGimmickEvent(stale, group.Final, group.Boundary, group.Advances);
            }
            if (corruption == "wrong-deadline" || corruption == "wrong-horizontal")
            {
                var old = group.Final.Members[0];
                var wrong = new TornadoMemberState(old.SourceId, old.Center, corruption == "wrong-horizontal" ? -old.HorizontalSign : old.HorizontalSign,
                    old.Phase, corruption == "wrong-deadline" ? 99 : old.NextBoundaryCount, old.LockedDirection);
                var after = new TornadoGimmickState(group.Final.Id, new[] { wrong });
                events[events.IndexOf(group)] = new CombatTornadoGimmickEvent(group.Expected, after, group.Boundary,
                    new[] { new TornadoMemberAdvance(group.Expected.Members[0], wrong, 3, group.Advances[0].Path) });
                result = new CombatCommandTransition(result.Input, true, "", result.Characters, result.Field,
                    result.FinalState.With(gimmicks: result.FinalState.Gimmicks.Replace(after)), result.Outcome, events: events);
            }
            else result = CombatCommandComposition.WithEvents(result, events);
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
        }

        [Test]
        public void FatalIntermediateLandingPublishesOnlyReachedPrefixAndNoLaterSource()
        {
            var result = Fatal();
            Assert.That(result.Outcome.IsStageEnded, Is.True);
            var group = result.Events.OfType<CombatTornadoGimmickEvent>().Single();
            Assert.That(group.Advances.Count, Is.EqualTo(1));
            Assert.That(group.Advances[0].Path, Is.EqualTo(new[] { new CellCoord(2, 2), new CellCoord(3, 3) }));
            Assert.That(group.Final.Members[0].NextBoundaryCount, Is.EqualTo(2));
            Assert.That(group.Final.Members[1].SameValues(group.Expected.Members[1]), Is.True);
            Assert.That(result.Events.OfType<CombatTornadoCenterEvent>().Select(c => c.Center), Is.EqualTo(group.Advances[0].Path));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.StageEnded), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Relocated).TornadoContact.StepIndex, Is.EqualTo(1));
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(result.Input.State.EnvironmentProgress.RandomState));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadEventShapes(result.Events));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
        }

        [Test]
        public void CenterAfterFatalLandingCannotBeInvented()
        {
            var result = Fatal();
            var events = result.Events.ToList();
            int end = events.FindIndex(e => e.Kind == CombatEventKind.StageEnded);
            events.Insert(end, new CombatTornadoCenterEvent(new TornadoContactOrigin(TornadoGimmickResolutionTests.Id, 31, new CellCoord(4, 4), 2)));
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ValidateGimmickChanges(CombatCommandComposition.WithEvents(result, events)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FatalEndpointCannotReserveNextCycleOrInventALaterWarning(bool laterWarning)
        {
            var result = Fatal(endpoint: true, secondWaiting: true);
            var group = result.Events.OfType<CombatTornadoGimmickEvent>().Single();
            var old = group.Advances.Single();
            Assert.That(old.Path.Count, Is.EqualTo(4));
            var normal = new TornadoMemberState(old.Final.SourceId, old.Final.Center, old.Final.HorizontalSign,
                TornadoGimmickPhase.Cooldown, 3, null);
            var advances = new[] { new TornadoMemberAdvance(old.Expected, normal, 3, old.Path) }.ToList();
            var members = group.Final.Members.Select(m => m.SourceId == normal.SourceId ? normal : m).ToArray();
            if (laterWarning)
            {
                var next = group.Expected.Members[1];
                var warning = new TornadoMemberState(next.SourceId, next.Center, next.HorizontalSign,
                    TornadoGimmickPhase.Warning, 3, new CellCoord(1, 1));
                advances.Add(new TornadoMemberAdvance(next, warning, 3, new[] { next.Center }));
                members[1] = warning;
            }
            var after = new TornadoGimmickState(group.Final.Id, members);
            var events = result.Events.Select(e => ReferenceEquals(e, group)
                ? new CombatTornadoGimmickEvent(group.Expected, after, group.Boundary, advances) : e).ToArray();
            var forged = new CombatCommandTransition(result.Input, true, "", result.Characters, result.Field,
                result.FinalState.With(gimmicks: result.FinalState.Gimmicks.Replace(after)), result.Outcome, events: events);
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ValidateGimmickChanges(forged));
        }

        private static CombatCommandTransition Fatal(bool endpoint = false, bool secondWaiting = false)
        {
            var victim = endpoint ? new CellCoord(6, 6) : new CellCoord(3, 4);
            var cells = TornadoGimmickResolutionTests.WallsExcept(8, 8, new CellCoord(0, 7), victim, CellCoord.Zero).ToList();
            cells.Add(new MineSourceFact(900, CellCoord.Zero, 1, 1000, true, TornadoGimmickResolutionTests.Both));
            var input = TornadoGimmickResolutionTests.Input(center: new CellCoord(2, 2), warned: true, victim: new CellCoord(0, 7),
                multiple: true, sources: cells.ToArray());
            if (secondWaiting)
            {
                var group = (TornadoGimmickState)input.State.Gimmicks.Get(TornadoGimmickResolutionTests.Id);
                var next = group.Members[1];
                var changed = new TornadoGimmickState(group.Id, new[] { group.Members[0],
                    new TornadoMemberState(next.SourceId, next.Center, 1, TornadoGimmickPhase.Cooldown, 2, null) });
                input = input.WithWorld(input.State.With(gimmicks: input.State.Gimmicks.Replace(changed)), input.Characters,
                    input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            }
            var characters = input.Characters.Select(c => c.EntityId == new EntityId(10) ? CombatCommandTestData.With(c, position: victim) : c).ToArray();
            input = input.WithWorld(input.State, characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            return WithBoundary(TornadoGimmickResolutionTests.Advance(input, 2));
        }

        internal static CombatCommandTransition WithBoundary(CombatCommandTransition raw)
        {
            long count = raw.Input.State.Turn.CompletedControllerTurnCount;
            var old = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Player,
                controllerTurnSequence: 1, completedControllerTurnCount: count - 1);
            var original = raw.Input.WithWorld(raw.Input.State.With(turn: old), raw.Input.Characters, raw.Input.Main,
                raw.Input.SoulCounts, raw.Input.Souls, raw.Input.EnvironmentBindings);
            var events = new CombatEvent[] { new CombatTurnEvent(CombatEventKind.PlayerTurnEnded, EntityId.Empty, old, raw.FinalState.Turn) }.Concat(raw.Events).ToArray();
            return new CombatCommandTransition(original, true, "", raw.Characters, raw.Field, raw.FinalState, raw.Outcome,
                events: events, environmentBindings: raw.EnvironmentBindings, healthExecutions: raw.HealthExecutions);
        }
    }
}
