using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 경험치·레벨업 설계 §3.2 규칙 3(X2-1): 가해자 기록이 없는 처치는 플레이어 명령의 아군 행동자가 받고,
    /// 자동 진행에서는 아무도 받지 않는다. 배치 지뢰의 처치 경로와 처치가 없는 턴 경계·철도 미정의 대조군을 확인한다.
    /// </summary>
    public sealed class ExperienceAttributionPathTests
    {
        private static readonly LevelProgressionSpec Spec = new(150, 100, 10, 3, 1, 1);

        [Test]
        public void PlacementMine_KillsEnemy_PlacedUnitEarns()
        {
            // 첫 배치 국면: 손패의 서브를 지뢰 칸에 놓고, 3×3 안의 적(체력 10)이 지뢰 피해 10으로 죽는다.
            var i = CombatCommonCommandTests.Command(CombatCommandKind.PlaceUnit);
            var hand = CombatPlacementCalculator.Location(i.Characters[0], CharacterRuntimeArea.Hand, 0, null, null);
            var cell = new CellCoord(2, 0);
            var enemy = CombatCommandTestData.With(CombatCommandTestData.Character(30, EntityKey.Unit_Demon,
                new CellCoord(3, 0), Owner.Enemy), hp: 10);
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                i.State.Structures,i.State.ThornFields,new CombatTurnFacts(StagePhase.Deployment),                new MovementEnvironmentFacts(new[] { MineEnvironmentTests.Mine(cell: cell) }));
            var input = new CombatCommandInput(1, i.Kind, i.ActorId, default, state, new[] { hand, i.Characters[1], enemy },
                i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, targetCell: cell,
                battle: new CombatBattleFacts(10, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0, progression: Spec),
                environmentBindings: ObjectArrivalOrderTests.Bindings(11),
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);

            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, enemy.EntityId).Health.IsDead, Is.True);
            Assert.That(Final(result, input.ActorId).Experience.Earned, Is.EqualTo(10));
            Assert.That(result.Events.OfType<CombatExperienceEvent>().Single().RecipientId, Is.EqualTo(input.ActorId));
        }

        [TestCase(1)]
        [TestCase(5)]
        public void UnexhaustedGroundAtPlayerEnd_DoesNotCreateKillsOrExperience(int entryThreshold)
        {
            // 실제 진입 없이 턴만 끝나면 한도 직전의 지반도 누적·붕괴·처치·경험치를 만들지 않는다.
            var seed = CombatCommandTestData.Input(empty: true);
            var enemy = CombatCommandTestData.Character(20, EntityKey.Unit_Demon, new CellCoord(4, 4), Owner.Enemy);
            seed = CombatCommandTestData.With(seed, characters: seed.Characters.Append(enemy).ToArray());
            var input = EndTurn(seed, new MovementTileFact[]
            {
                new StaticTerrainFact(72, enemy.Anchor.Value, StaticTerrainKind.WeakGround, true,
                    new[] { Owner.Player, Owner.Enemy }, entryThreshold, entryThreshold - 1)
            });

            var result = CombatCommandCalculator.Calculate(input, TornadoEnvironmentTests.Rules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, enemy.EntityId).Health.IsDead, Is.False);
            Assert.That(Final(result, input.ActorId).Experience.Earned, Is.Zero);
            Assert.That(result.FinalState.Environment.SameValues(input.State.Environment), Is.True);
            Assert.That(result.Events.OfType<CombatFallEvent>(), Is.Empty);
            Assert.That(result.Events.OfType<CombatExperienceEvent>(), Is.Empty);
        }

        [Test]
        public void WithoutRailwayDefinition_DoesNotCreateKillsOrExperience()
        {
            var input = CombatCommandTestData.Input();
            Assert.That(CombatGimmickCalculator.TryAdvance(input), Is.Null);
            Assert.That(input.Characters.All(c => !c.Health.IsDead), Is.True);
            Assert.That(input.Characters.Sum(c => c.Experience.Earned), Is.Zero);
        }

        /// <summary>약한 지반 시험(WeakGroundTurnTests.Turn)과 같은 방식의 턴 종료 입력에 경험치 설정을 켠다.</summary>
        private static CombatCommandInput EndTurn(CombatCommandInput seed, MovementTileFact[] sources)
        {
            var turnInput = TornadoEnvironmentTests.Input(completed: 0, sources: sources,
                characters: seed.Characters.ToArray());
            var s = turnInput.State;
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.IsNight,                seed.State.Structures,seed.State.ThornFields,s.Turn,s.Environment,s.EnvironmentProgress);
            var b = turnInput.Battle;
            var battle = new CombatBattleFacts(b.ReserveCapacity, b.TimelineSeed, b.PlayerDeploymentDepth,
                b.EnemyDeploymentDepth, b.DrawOrder, b.DrawCursor, b.RailCells, b.GimmickDefinitions, b.Categories, Spec);
            return new CombatCommandInput(1, CombatCommandKind.EndPlayerTurn, seed.ActorId, default, state, seed.Characters,
                seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts, battle: battle,
                environmentBindings: new EnvironmentSemanticBindings(turnInput.EnvironmentBindings.TerrainSourceIds,
                    turnInput.EnvironmentBindings.ObjectSourceIds, seed.EnvironmentBindings.ObstacleEntityIds),
                obstacleContents: seed.ObstacleContents,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
        }

        private static CharacterCombatFacts Final(CombatCommandTransition result, EntityId id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == id);
    }
}
