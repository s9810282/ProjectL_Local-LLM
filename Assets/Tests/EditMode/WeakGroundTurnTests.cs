using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class WeakGroundTurnTests
    {
        [TestCase(CombatControllerPhase.Player, 0)]
        [TestCase(CombatControllerPhase.Enemy, 1)]
        [TestCase(CombatControllerPhase.Enemy, 8)]
        [TestCase(CombatControllerPhase.Player, 0, 1)]
        [TestCase(CombatControllerPhase.Enemy, 1, 5)]
        public void ControllerCompletion_DoesNotCollapseOrCountGround(CombatControllerPhase phase, long completed, int entryThreshold = 2)
        {
            var source = CombatCommandTestData.Input(empty: true);
            var main = source.Characters.Single(c => c.OwnershipRole ==
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterOwnershipRole.Main);
            var ground = new StaticTerrainFact(71, main.Anchor.Value, StaticTerrainKind.WeakGround,
                true, new[] { Owner.Player }, entryThreshold, entryThreshold - 1);
            // 명령 끝에 예정 붕괴가 남지 않는 계약이므로, 마지막 진입 전의 유효한 상태에서 턴 경계만 검증한다.
            Assert.That(ground.PendingCollapse, Is.False);
            source = StaticTerrainIntegrationTests.Terrain(source, ground);
            var input = EnemyControllerPhaseTests.Input(source, phase, completed, characters: new[] { main });
            var result = phase == CombatControllerPhase.Player
                ? CombatTurnCalculator.EndPlayerTurn(input, Array.Empty<IMovementTileRule>())
                : CombatTurnCalculator.EndEnemyPhase(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Environment.SameValues(input.State.Environment), Is.True);
            Assert.That(result.Events.OfType<CombatFallEvent>(), Is.Empty);
            Assert.That(result.Events.OfType<CombatMovementSourceEvent>(), Is.Empty);
            Assert.That(CombatCommandComposition.Project(result).Characters.Single().Health, Is.EqualTo(main.Health));
            Assert.That(result.FinalState.Turn.StagePhase, Is.EqualTo(completed == 8 ? StagePhase.Deployment : StagePhase.Combat));
        }

        // 마지막 진입 뒤 붕괴하면 같은 원본이 낭떠러지가 되고 그 위의 점유자가 떨어진다(약한 지반 설계 WG1, 2026-10-27 View 작업에서 사용자 허락으로 기대값 변경).
        [Test]
        public void EntryThenCollapse_TurnsGroundIntoCliffAndDropsOccupant()
        {
            var source = CombatCommandTestData.Input();
            var actor = source.Characters[0];
            var ground = new StaticTerrainFact(71, actor.Anchor.Value, StaticTerrainKind.WeakGround,
                true, new[] { Owner.Player }, 1);
            var input = StaticTerrainIntegrationTests.Terrain(source, ground);
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId),
                new System.Collections.Generic.List<CombatEvent>());
            WeakGroundCalculator.RegisterEntry(projection, actor.EntityId, ground.Cell);
            WeakGroundCalculator.CollapsePending(projection);
            var final = projection.Environment.Tiles.OfType<StaticTerrainFact>().Single(t => t.Id == ground.Id);
            Assert.That(final.Kind, Is.EqualTo(StaticTerrainKind.Cliff));
            Assert.That(final.Cell, Is.EqualTo(ground.Cell));
            Assert.That(projection.Events.OfType<CombatMovementSourceEvent>().Count(), Is.EqualTo(2));
            var fall = projection.Events.OfType<CombatFallEvent>().Single();
            Assert.That(fall.EntityId, Is.EqualTo(actor.EntityId));
            Assert.That(fall.Cell, Is.EqualTo(ground.Cell));
            Assert.That(projection.Character(actor.EntityId).Health.IsDead, Is.True);
        }
    }
}
