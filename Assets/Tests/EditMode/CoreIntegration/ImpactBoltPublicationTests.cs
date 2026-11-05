using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ImpactBoltPublicationTests
    {
        private static readonly CellCoord Center = new CellCoord(3, 3);
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [TestCase(1, CategoryKey.Main, false)]
        [TestCase(2, CategoryKey.Main, false)]
        [TestCase(1, CategoryKey.Heavy, false)]
        [TestCase(2, CategoryKey.Heavy, false)]
        [TestCase(1, CategoryKey.Heavy, true)]
        [TestCase(2, CategoryKey.Heavy, true)]
        public void RepeatedPreview_PublishesConfiguredDamageAndMovementExactlyOnce(
            int distance, CategoryKey targetCategory, bool immune)
        {
            using var battle = Create(distance, targetCategory, immune);
            var b = battle.Buffers;
            var targets = Targets(battle);
            var before = battle.CaptureGameplay();
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            int handled = b.CombatStateReadSink.HandledCommandNumber;
            int queued = b.CombatViewReadSink.Commands.Count;
            Assert.That(targets.Select(t => t.Anchor.Value), Is.EqualTo(new[]
            {
                new CellCoord(2, 3), Center, new CellCoord(4, 3)
            }));
            Assert.That(battle.Actor.Anchor, Is.EqualTo(new CellCoord(0, 1)));
            Assert.That(battle.Actor.Stats.Category.Key, Is.EqualTo(CategoryKey.Rider));
            Assert.That(battle.Actor.Stats.AttackPower, Is.EqualTo(5));
            Assert.That(targets.All(t => t.Stats.Category.Key == targetCategory), Is.True);
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Available));

            AttackPreview servicePreview = default;
            for (int i = 0; i < 3; i++)
            {
                b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(
                    battle.Actor.EntityId, AttackAim.ForCell(Center)));
                Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
                Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
                servicePreview = preview.Attack.Value;
                AssertPreview(servicePreview, targets, distance, targetCategory);
                Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
                Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
                Assert.That(b.CombatViewReadSink.Commands.Count, Is.EqualTo(queued));
            }

            // 미리보기 대상의 점유 칸은 최초 자리이고, 밀린 뒤 자리는 넉백 요약(Knockbacks)이 제공한다. 아래에서 실제 게시 결과와 대조한다.
            // 이 호출이 명령을 한 번 제출하므로 아래에서는 새 명령을 제출하지 않는다.
            var input = battle.Publication().Attack(battle.Actor, Center);
            Assert.That(b.CombatViewReadSink.Commands.Count, Is.EqualTo(queued + 1));
            var beforeProjection = new CoreIntegrationSnapshot(b);
            var calculatedPreview = CombatCommandCalculator.Preview(input, NoRules);
            var projection = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(projection.Accepted, Is.True, projection.Reason);
            Assert.That(projection.Outcome.IsStageEnded, Is.False);
            AssertPreview(calculatedPreview, targets, distance, targetCategory);
            beforeProjection.AssertUnchanged(b);
            var projected = projection.Characters.Characters.Select(change => change.Final).ToArray();
            AssertFinalTargets(projected, targets, distance, targetCategory, immune);
            AssertMovement(projection.Events, targets, distance, immune);

            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.CommandNumber, Is.EqualTo(input.Number));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(input.Number));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatViewReadSink.Commands.Count, Is.EqualTo(queued + 1));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(b.CombatStateReadSink.Completion, Is.Null);
            var attack = result.Attack.Value;
            Assert.That(attack.ActionUseSettlement.ActorId, Is.EqualTo(battle.Actor.EntityId));
            Assert.That(attack.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(attack.PiercingShot, Is.Null);
            Assert.That(attack.PrimaryCell, Is.EqualTo(servicePreview.PrimaryCell));
            Assert.That(attack.PrimaryTargetId, Is.EqualTo(servicePreview.PrimaryTargetId));
            Assert.That(attack.DesignatedCells.ToArray(), Is.EqualTo(servicePreview.DesignatedCells.ToArray()));
            Assert.That(attack.AdditionalCells.ToArray(), Is.EqualTo(servicePreview.AdditionalCells.ToArray()));
            Assert.That(attack.AffectedCells.ToArray(), Is.EqualTo(servicePreview.AffectedCells.ToArray()));
            Assert.That(attack.Hits.Length, Is.EqualTo(3));
            foreach (var target in servicePreview.Targets.ToArray())
            {
                var hit = attack.Hits.ToArray().Single(h => h.TargetId == target.TargetId);
                Assert.That(hit.TargetHealthBefore, Is.EqualTo(target.TargetHealthBefore));
                Assert.That(hit.TargetHealthAfter, Is.EqualTo(target.TargetHealthAfter));
                Assert.That(hit.Role, Is.EqualTo(target.Role));
                Assert.That(hit.AppliedSplashPercent, Is.EqualTo(target.AppliedSplashPercent));
            }
            var published = b.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            AssertFinalTargets(published, targets, distance, targetCategory, immune);
            // Buffer를 거친 미리보기의 넉백 요약이 실제 게시된 결과의 요약·최종 위치와 같다(설계 2026-10-27 §4.2).
            // 중심 대상은 밀 대상이 아니라 기록이 없고, 이웃 둘만 기록된다.
            Assert.That(servicePreview.Knockbacks.Select(k => k.TargetId),
                Is.EqualTo(attack.Knockbacks.Select(k => k.TargetId)));
            Assert.That(servicePreview.Knockbacks.Select(k => k.TargetId),
                Is.EquivalentTo(targets.Where(t => t.Anchor != Center).Select(t => t.EntityId)));
            foreach (var record in servicePreview.Knockbacks)
            {
                var committed = attack.Knockbacks.Single(k => k.TargetId == record.TargetId);
                var original = targets.Single(t => t.EntityId == record.TargetId);
                Assert.That(record.Stop, Is.EqualTo(immune ? AttackKnockbackStop.Immune : AttackKnockbackStop.Completed));
                Assert.That(record.Stop, Is.EqualTo(committed.Stop));
                Assert.That(record.Path, Is.EqualTo(committed.Path));
                Assert.That(record.FinalAnchor, Is.EqualTo(ExpectedAnchor(original, distance, immune)));
                Assert.That(published.Single(c => c.EntityId == record.TargetId).Anchor, Is.EqualTo(record.FinalAnchor));
            }
            foreach (var expected in projected)
            {
                Assert.That(published.Single(c => c.EntityId == expected.EntityId).SameValues(expected), Is.True);
            }
            AssertMovement(result.Events, targets, distance, immune);
            var expectedMoves = projection.Events.OfType<CombatCharacterEvent>()
                .Where(e => e.Kind == CombatEventKind.Moved).ToArray();
            var actualMoves = result.Events.OfType<CombatCharacterEvent>()
                .Where(e => e.Kind == CombatEventKind.Moved).ToArray();
            Assert.That(actualMoves.Length, Is.EqualTo(expectedMoves.Length));
            for (int i = 0; i < expectedMoves.Length; i++)
            {
                Assert.That(actualMoves[i].Expected.SameValues(expectedMoves[i].Expected), Is.True);
                Assert.That(actualMoves[i].Final.SameValues(expectedMoves[i].Final), Is.True);
            }

            var once = new CoreIntegrationSnapshot(b);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            once.AssertUnchanged(b);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void StaleVictimPosition_RejectsAllStoresThenRestoresAppliesAndReapplies(int distance)
        {
            using var battle = Create(distance);
            var b = battle.Buffers;
            var targets = Targets(battle);
            var victim = targets.Single(t => t.Anchor == new CellCoord(2, 3));
            var input = battle.Publication().Attack(battle.Actor, Center);
            var beforeCalculation = new CoreIntegrationSnapshot(b);
            var calculated = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(calculated.Accepted, Is.True, calculated.Reason);
            Assert.That(calculated.Outcome.IsStageEnded, Is.False);
            beforeCalculation.AssertUnchanged(b);
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            var altered = victim.WithPlacement(victim.Area, null, new CellCoord(1, 4), victim.Rotation,
                new[] { new CellCoord(1, 4) });
            Change(battle, victim, altered);
            var stale = new CoreIntegrationSnapshot(b);
            Assert.Throws<InvalidOperationException>(() => b.BattleWriteSink.Apply(calculated));
            stale.AssertUnchanged(b);

            Change(battle, altered, victim);
            Assert.That(b.CharacterReadSink.Characters.Single(c => c.EntityId == victim.EntityId)
                .ToFacts().SameValues(victim), Is.True);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
            b.BattleWriteSink.Apply(calculated);
            AssertFinalTargets(b.CharacterReadSink.Characters.Select(c => c.ToFacts()),
                targets, distance, CategoryKey.Main, false);
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(input.Number));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.CommandNumber, Is.EqualTo(input.Number));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(result.Attack.Value.PiercingShot, Is.Null);
            AssertMovement(result.Events, targets, distance, false);
            var once = new CoreIntegrationSnapshot(b);
            Assert.DoesNotThrow(() => b.BattleWriteSink.Apply(calculated));
            once.AssertUnchanged(b);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            once.AssertUnchanged(b);
        }

        private static ConfiguredMovementBattle Create(int distance,
            CategoryKey targetCategory = CategoryKey.Main, bool immune = false)
        {
            return new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new ImpactBoltBattleSource(new ImpactBoltTraitSpec(distance),
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single,
                        TargetingSplash.Radial, 70))
                {
                    TargetCategory = targetCategory,
                    TargetImmune = immune
                });
        }

        private static CharacterCombatFacts[] Targets(ConfiguredMovementBattle battle)
        {
            return battle.Buffers.CharacterReadSink.Characters
                .Where(c => c.CharacterKey == EntityKey.Unit_Demon)
                .Select(c => c.ToFacts()).OrderBy(c => c.Anchor.Value.X).ToArray();
        }

        private static void AssertPreview(AttackPreview preview, CharacterCombatFacts[] targets,
            int distance, CategoryKey targetCategory)
        {
            Assert.That(preview.CanAttack, Is.True, preview.FailureReason.ToString());
            Assert.That(preview.PrimaryCell, Is.EqualTo(Center));
            Assert.That(preview.PrimaryTargetId, Is.EqualTo(targets.Single(t => t.Anchor == Center).EntityId));
            Assert.That(preview.PiercingShot, Is.Null);
            Assert.That(preview.ActionUseSettlement.FinalActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(preview.ProjectedTraitStatuses.ToArray().OfType<ImpactBoltTraitStatusSnapshot>()
                .Single().DistanceCells, Is.EqualTo(distance));
            Assert.That(preview.Targets.Length, Is.EqualTo(targets.Length));
            foreach (var original in targets)
            {
                var target = preview.Targets.ToArray().Single(t => t.TargetId == original.EntityId);
                bool center = original.Anchor == Center;
                Assert.That(target.IsDirectHit, Is.True);
                Assert.That(target.Role, Is.EqualTo(center ? AttackTargetRole.Primary : AttackTargetRole.Additional));
                Assert.That(target.AppliedSplashPercent, Is.EqualTo(center ? 100 : 70));
                Assert.That(target.TargetHealthBefore, Is.EqualTo(100));
                Assert.That(target.TargetHealthAfter, Is.EqualTo(ExpectedHealth(center, targetCategory)));
                Assert.That(target.ExpectedDamage, Is.EqualTo(100 - ExpectedHealth(center, targetCategory)));
                Assert.That(target.TargetOccupiedCells.ToArray(), Is.EqualTo(original.OccupiedCells));
            }
        }

        private static void AssertFinalTargets(IEnumerable<CharacterCombatFacts> characters,
            CharacterCombatFacts[] targets, int distance, CategoryKey targetCategory, bool immune)
        {
            var current = characters.ToArray();
            foreach (var original in targets)
            {
                var final = current.Single(c => c.EntityId == original.EntityId);
                var expected = ExpectedAnchor(original, distance, immune);
                Assert.That(final.Anchor, Is.EqualTo(expected));
                Assert.That(final.OccupiedCells, Is.EqualTo(new[] { expected }));
                Assert.That(final.Health.CurrentHealth, Is.EqualTo(ExpectedHealth(original.Anchor == Center, targetCategory)));
            }
        }

        private static void AssertMovement(IEnumerable<CombatEvent> events, CharacterCombatFacts[] targets,
            int distance, bool immune)
        {
            var moves = events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Moved).ToArray();
            Assert.That(moves.Length, Is.EqualTo(immune ? 0 : 2 * distance));
            foreach (var original in targets)
            {
                var targetMoves = moves.Where(e => e.Final.EntityId == original.EntityId).ToArray();
                int count = immune || original.Anchor == Center ? 0 : distance;
                Assert.That(targetMoves.Length, Is.EqualTo(count));
                for (int step = 0; step < count; step++)
                {
                    Assert.That(targetMoves[step].Expected.Anchor, Is.EqualTo(ExpectedAnchor(original, step, false)));
                    var expected = ExpectedAnchor(original, step + 1, false);
                    Assert.That(targetMoves[step].Final.Anchor, Is.EqualTo(expected));
                    Assert.That(targetMoves[step].Final.OccupiedCells, Is.EqualTo(new[] { expected }));
                }
            }
        }

        private static CellCoord ExpectedAnchor(CharacterCombatFacts original, int distance, bool immune)
        {
            if (immune || original.Anchor == Center)
            {
                return original.Anchor.Value;
            }
            return original.Anchor.Value.X == 2 ? new CellCoord(2 - distance, 3) : new CellCoord(4 + distance, 3);
        }

        private static int ExpectedHealth(bool center, CategoryKey category)
        {
            return category == CategoryKey.Heavy ? (center ? 96 : 98) : (center ? 95 : 97);
        }

        private static void Change(ConfiguredMovementBattle battle, CharacterCombatFacts before,
            CharacterCombatFacts after)
        {
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(
                new[] { new CharacterStateChange(before, after) }, Array.Empty<SoulCountChange>()));
        }
    }
}
