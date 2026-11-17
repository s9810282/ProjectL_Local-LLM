using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Tests.CoreIntegration
{
    /// <summary>클릭 최단 경로 이동의 게시 계약(설계 C4·C5, 계획 K6·K7): 계산 출처 리비전과 기존 Move 합류.</summary>
    public sealed class MoveRangePublicationTests
    {
        [Test]
        public void MoveRange_PublishesRoutesStampedWithTheCalculationRevision_AndARouteRunsAsOneMove()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ConfiguredMovementBattle.EncounterSource());
            var actor = battle.Actor;
            long revision = battle.Buffers.CombatStateReadSink.QueryRevision;

            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.MoveRange(actor.EntityId));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
            var range = preview.MoveRange.Value;

            Assert.That(range.Succeeded, Is.True, range.FailureReason.ToString());
            Assert.That(range.SourceRevision, Is.EqualTo(revision));
            Assert.That(battle.Buffers.CombatStateReadSink.QueryRevision, Is.EqualTo(revision), "범위 조회는 판을 바꾸지 않는다.");
            var destinations = range.Destinations.ToArray();
            Assert.That(destinations, Is.Not.Empty);
            foreach (var destination in destinations)
            {
                Assert.That(range.TryGetRoute(destination.TargetAnchor, out var route), Is.True);
                Assert.That(route.Origin, Is.EqualTo(actor.Anchor.Value));
                Assert.That(route.StepCount, Is.EqualTo(destination.StepDistance));
            }

            var occupied = battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts())
                .Where(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead)
                .SelectMany(c => c.OccupiedCells).ToHashSet();
            var target = destinations.Where(d => !occupied.Contains(d.TargetAnchor))
                .OrderByDescending(d => d.StepDistance).First().TargetAnchor;
            Assert.That(range.TryGetRoute(target, out var chosen), Is.True);

            battle.Submit(battle.PreviewMove(actor, chosen.Cells.ToArray()));

            Assert.That(battle.Actor.Anchor, Is.EqualTo(target));
            Assert.That(battle.Buffers.CombatStateReadSink.QueryRevision, Is.GreaterThan(revision));
        }
    }
}
