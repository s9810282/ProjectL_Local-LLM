using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 지정 보스가 공격 직접 피해가 아닌 경로(넉백 낙사·환경 폭발)로 죽어도 같은 목표로 승리하고,
    /// 대기 포효 등 이후 반응이 나오지 않는지 확인한다(계획 T6.1). 운영 보스는 강제 이동 면역이므로
    /// 넉백 사망 검증에는 면역 없는 지정 목표 fixture를 쓴다(운영 보스 면역을 해제하지 않는다).
    /// </summary>
    public sealed class BossDeathPathTests
    {
        [Test]
        public void KnockbackIntoCliff_WinsByDesignatedBoss_WithoutRoar()
        {
            // 카운터 4: 이 공격이 5번째 피격이지만 보스가 떨어져 죽으므로 포효하지 않는다.
            var input = WithPlayerKnockback(BossRoarTests.Input(4), 3);
            input = Designated(CliffFallTests.WithEnvironment(input, CliffFallTests.Ground(StaticTerrainKind.Cliff, cell: new CellCoord(2, 3))));

            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            AssertBossVictoryWithoutReaction(result);
            Assert.That(result.Events.OfType<CombatFallEvent>().Single().EntityId, Is.EqualTo(BossRoarTests.Boss));
        }

        [Test]
        public void MineBlastFromPlayerMove_WinsByDesignatedBoss_WithoutRoar()
        {
            var seed = Designated(CliffFallTests.WithEnvironment(BossRoarTests.Input(4, bossHp: 1),
                MineEnvironmentTests.Mine(id: 60, damage: 3, cell: new CellCoord(4, 4))));
            var move = new CombatCommandInput(1, CombatCommandKind.Move, BossRoarTests.Player, default, seed.State, seed.Characters,
                seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                path: new MovementPath(new[] { new CellCoord(4, 3), new CellCoord(4, 4) }), battleInstanceId: seed.BattleInstanceId,
                environmentBindings: seed.EnvironmentBindings, obstacleContents: seed.ObstacleContents,
                expectedBattleInstanceId: seed.BattleInstanceId, expectedControllerTurnSequence: (seed.State).Turn.ControllerTurnSequence);

            var result = CombatCommandCalculator.Calculate(move, MineEnvironmentTests.Rules);

            AssertBossVictoryWithoutReaction(result);
        }

        private static void AssertBossVictoryWithoutReaction(CombatCommandTransition result)
        {
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(BossRoarTests.Final(result, BossRoarTests.Boss).Health.IsDead, Is.True);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(result.Outcome.Reason, Is.EqualTo(StageOutcomeReason.DesignatedBossDefeated));
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.Roared || e.Kind == CombatEventKind.Summoned), Is.False);
            Assert.That(result.Events.OfType<CombatOutcomeEvent>().Count(), Is.EqualTo(1));
        }

        private static CombatCommandInput Designated(CombatCommandInput input)
        {
            var s = input.State;
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.IsNight,s.Structures,s.ThornFields,s.Turn,s.Environment,s.EnvironmentProgress,s.WallBlocking,BattleObjective.DesignatedBoss(BossRoarTests.Boss),                s.Allocation);
            return CombatCommandTestData.With(input, state: state);
        }

        private static CombatCommandInput WithPlayerKnockback(CombatCommandInput input, int distance)
        {
            var characters = input.Characters.Select(c => c.EntityId == BossRoarTests.Player
                ? CombatCommandTestData.With(c, traits: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(distance) }) : c).ToArray();
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, new KnockbackTraitSpec(distance)), p.Method)
                : p).ToArray();
            return CombatCommandTestData.With(input, characters, profiles: profiles);
        }
    }
}
