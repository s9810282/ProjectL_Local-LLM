using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Placement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyRedeploymentResolverTests
    {
        [Test]
        public void ConfiguredMovement_EnemyRedeploymentPreservesCapturedStats()
        {
            var source = CombatCommonCommandTests.Command(CombatCommandKind.EnterCombat);
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 7), Owner.Enemy);
            enemy = enemy.With(stats: enemy.Stats.With(moveRange: 5));
            var waiting = CombatPlacementCalculator.Location(enemy, CharacterRuntimeArea.EnemyWaiting, null, null, null);
            var headquarters = new StructureCombatFacts(new EntityId(20),
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StructureSpec(
                    EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
                new HealthStateSnapshot(100, 100), new CellCoord(0, 7), Rotation4.R0, new[] { new CellCoord(0, 7) });
            var state = source.State.With(structures: new[] { headquarters },
                turn: new CombatTurnFacts(StagePhase.Deployment,
                    enemyOrigins: new[] { new CombatEnemyOrigin(enemy.EntityId, enemy.Anchor.Value, Rotation4.R90) }));
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state,
                new[] { source.Characters[1], waiting }, source.Profiles, source.Main, source.Owned, source.SoulCounts,
                source.Souls, source.Artifacts, battle: new CombatBattleFacts(8, 37, 1, 1, Array.Empty<CharacterInstanceId>(), 0));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var redeployed = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == enemy.EntityId);
            Assert.That(redeployed.Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(redeployed.Anchor, Is.EqualTo(enemy.Anchor));
            Assert.That(redeployed.Stats.SameValues(enemy.Stats), Is.True);
            Assert.That(redeployed.Stats.MoveRange, Is.EqualTo(5));
            Assert.That(waiting.Area, Is.EqualTo(CharacterRuntimeArea.EnemyWaiting));
        }

        [Test]
        public void PreEntryRedeploymentAndRejectedEntryLeavePreparedStormAndRandomnessInert()
        {
            var source = CombatCommonCommandTests.Command(CombatCommandKind.EnterCombat);
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 7), Owner.Enemy);
            var waiting = CombatPlacementCalculator.Location(enemy, CharacterRuntimeArea.EnemyWaiting, null, null, null);
            var headquarters = new StructureCombatFacts(new EntityId(20),
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StructureSpec(
                    EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
                new HealthStateSnapshot(100, 100), new CellCoord(0, 7), Rotation4.R0, new[] { new CellCoord(0, 7) });
            var definitions = new StageGimmickSpec[] { new TornadoGimmickSpec(new[] {
                new TornadoMemberSpec(31, new[] { Owner.Player, Owner.Enemy }, new CellCoord(1, 6)) }) };
            var state = StageGimmickComposition.Initialize(source.State.With(structures: new[] { headquarters },
                turn: new CombatTurnFacts(StagePhase.Deployment,
                    enemyOrigins: new[] { new CombatEnemyOrigin(enemy.EntityId, enemy.Anchor.Value, Rotation4.R90) })), definitions);
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state,
                new[] { source.Characters[1], waiting }, source.Profiles, source.Main, source.Owned, source.SoulCounts,
                source.Souls, source.Artifacts, battle: new CombatBattleFacts(8, 37, 1, 1, Array.Empty<CharacterInstanceId>(), 0,
                    gimmicks: definitions));
            var accepted = CombatCommandCalculator.Calculate(input, TornadoEnvironmentTests.Rules);
            Assert.That(accepted.Accepted, Is.True, accepted.Reason);
            Assert.That(CombatCommandComposition.Project(accepted).Characters.Single(c => c.EntityId == enemy.EntityId).Anchor, Is.EqualTo(enemy.Anchor));
            Assert.That(accepted.Events.OfType<CombatCharacterEvent>().Where(e => e.Kind == CombatEventKind.Relocated), Is.Empty);
            Assert.That(accepted.FinalState.Gimmicks.SameValues(state.Gimmicks), Is.True);
            Assert.That(accepted.FinalState.EnvironmentProgress.SameValues(state.EnvironmentProgress), Is.True);
            var invalid = input.WithWorld(state.With(structures: Array.Empty<StructureCombatFacts>()), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var rejected = CombatCommandCalculator.Calculate(invalid, TornadoEnvironmentTests.Rules);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Events, Is.Empty);
            Assert.That(rejected.FinalState.Gimmicks.SameValues(state.Gimmicks), Is.True);
            Assert.That(rejected.FinalState.EnvironmentProgress.SameValues(state.EnvironmentProgress), Is.True);
        }
        [Test]
        public void LethalObjectDuringRedeploymentEndsWithoutCombatEntered()
        {
            var source = CombatCommonCommandTests.Command(CombatCommandKind.EnterCombat);
            var main = source.Characters.Single(c => c.OwnershipRole == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterOwnershipRole.Main);
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 7), Owner.Enemy);
            var waiting = CombatPlacementCalculator.Location(enemy, CharacterRuntimeArea.EnemyWaiting, null, null, null);
            var mine = new MineSourceFact(901, enemy.Anchor.Value, 1, 1000, true, new[] { Owner.Player, Owner.Enemy });
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, source.State.Structures,
                source.State.ThornFields, objective: BattleObjective.DesignatedBoss(enemy.EntityId),
                turn: new CombatTurnFacts(StagePhase.Deployment,
                    enemyOrigins: new[] { new CombatEnemyOrigin(enemy.EntityId, enemy.Anchor.Value, Rotation4.R0) }),
                environment: new MovementEnvironmentFacts(new[] { mine }), environmentProgress: new EnvironmentProgressFacts(2, 902, 1));
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state,
                new[] { main, waiting }, source.Profiles, source.Main, source.Owned, source.SoulCounts,
                source.Souls, source.Artifacts, battle: new CombatBattleFacts(8, 37, 1, 1, Array.Empty<CharacterInstanceId>(), 0),
                environmentBindings: new EnvironmentSemanticBindings(Array.Empty<long>(), new long[] { 901 }, Array.Empty<EntityId>()));
            var result = CombatCommandCalculator.Calculate(input, TornadoEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Outcome.IsStageEnded, Is.True);
            Assert.That(result.Events.OfType<CombatTurnEvent>().Where(e => e.Kind == CombatEventKind.CombatEntered), Is.Empty);
            Assert.That(result.FinalState.Turn.StagePhase, Is.EqualTo(StagePhase.Deployment));
        }
        [Test]
        public void PaddedSingleCell_NegativeAnchorCanHaveInsideEnemyDeploymentCell()
        {
            // 도형 계산만 보존한다. 패딩된 유닛의 출현·재배치 지원을 활성화하지 않는다.
            var footprint = new TileFootprint(2, 1, new[] { new CellCoord(1, 0) });
            var anchor = new CellCoord(-1, 4);
            var occupied = footprint.GetOccupiedCell(0, anchor, Rotation4.R0);
            var board = new CombatCommandState(3,5,CombatFlowPhase.PlayerInput,                Array.Empty<StructureCombatFacts>(),Array.Empty<ThornFieldCellSnapshot>());
            Assert.That(footprint.CellCount, Is.EqualTo(1));
            Assert.That(occupied, Is.EqualTo(new CellCoord(0, 4)));
            Assert.That(board.IsInside(anchor), Is.False);
            Assert.That(board.IsInside(occupied), Is.True);
            Assert.That(new DeploymentZonePolicy(PlayerDeploymentZone.Rows(1)).CanDeploy(Owner.Enemy, new[] { occupied }), Is.True);
            Assert.That(board.Structures, Is.Empty);
        }
    }
}
