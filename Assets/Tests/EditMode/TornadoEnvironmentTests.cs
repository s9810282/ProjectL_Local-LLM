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
    public sealed class TornadoEnvironmentTests
    {
        internal static readonly IMovementTileRule[] Rules = {
            new ThornMovementTileRule(), new FlagMovementTileRule(), new SheriffBadgeMovementTileRule(), new MineMovementTileRule() };
        internal static TornadoSourceFact Source(long id = 31, CellCoord? cell = null, long due = 3, int repeatInterval = 3) =>
            new(id, cell ?? new CellCoord(1, 3), new[] { Owner.Player, Owner.Enemy }, due, repeatIntervalUnitTurns: repeatInterval);

        // 기존 공유 회귀의 한 칸 배치는 명시적인 한 칸 테스트 프로필로 옮긴다. 운영 입력의 기본 영역은 3×3이다.
        internal static CombatCommandInput WithStorms(CombatCommandInput input)
        {
            var old = input.State.Environment.Tiles.OfType<TornadoSourceFact>().ToArray();
            if (old.Length == 0) return input;
            var enabled = old.Where(t => t.Active).ToArray();
            var definitions = input.Battle?.GimmickDefinitions.ToList() ?? new System.Collections.Generic.List<StageGimmickSpec>();
            var entries = input.State.Gimmicks.Entries.ToList();
            if (enabled.Length > 0)
            {
                definitions.Add(new TornadoGimmickSpec(enabled.Select(t => new TornadoMemberSpec(t.Id, t.KnownTo, t.Cell, 1,
                    sideLength: 1, firstActivationAfterControllerTurns: t.NextDueCompletedTurn, repeatIntervalControllerTurns: t.RepeatIntervalUnitTurns)).ToArray()));
                entries.Add(new TornadoGimmickState(new StageGimmickId(definitions.Count), enabled.Select(t => {
                    int dx = t.Cell.X == input.State.BoardWidth - 1 ? -1 : 1;
                    int dy = t.Cell.Y == input.State.BoardHeight - 1 ? -1 : 1;
                    var direction = input.State.BoardWidth == 1 || input.State.BoardHeight == 1 ? CellCoord.Zero : new CellCoord(dx, dy);
                    return new TornadoMemberState(t.Id, t.Cell, dx, TornadoGimmickPhase.Warning, t.NextDueCompletedTurn, direction);
                }).ToArray()));
            }
            var environment = new MovementEnvironmentFacts(input.State.Environment.Tiles.Where(t => t is not TornadoSourceFact).ToArray());
            var bindings = new EnvironmentSemanticBindings(input.EnvironmentBindings.TerrainSourceIds.Except(old.Select(t => t.Id)).ToArray(),
                input.EnvironmentBindings.ObjectSourceIds, input.EnvironmentBindings.ObstacleEntityIds);
            var state = input.State.With(environment: environment, gimmicks: new StageGimmickStateSet(entries));
            var b = input.Battle;
            var battle = new CombatBattleFacts(b?.ReserveCapacity ?? 8, b?.TimelineSeed ?? 1, b?.PlayerZone ?? PlayerDeploymentZone.Rows(1),
                b?.DrawOrder ?? Array.Empty<CharacterInstanceId>(), b?.DrawCursor ?? 0, b?.RailCells, definitions,
                b?.Categories, b?.Progression ?? default, b?.ClearRewards);
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, state, input.Characters, input.Profiles,
                input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, input.TargetCell, input.Rotation, battle,
                input.ProcessingKind, input.Path, input.MovementContext, input.BattleInstanceId, input.ArtifactItems, bindings,
                input.ObstacleContents, input.ItemKey, input.ItemInventory, input.ItemDefinitions, input.GoldBalance, input.FameBalance,
                input.Membership, input.ExpectedBattleInstanceId, input.ExpectedControllerTurnSequence, input.ExpectedMoveCommandNumber);
        }

        internal static CombatCommandInput Input(long completed = 2, MovementTileFact[] sources = null,
            CharacterCombatFacts[] characters = null, int width = 8, int height = 8,
            CombatControllerPhase phase = CombatControllerPhase.Player, long sequence = 1, long nightStart = 0)
        {
            var seed = CombatCommandTestData.Input(empty: true);
            sources ??= new MovementTileFact[] { Source() };
            var environment = new MovementEnvironmentFacts(sources);
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: phase, controllerTurnSequence: sequence,
                completedPlayerTurnCount: completed, nightStartPlayerTurnCount: nightStart, completedControllerTurnCount: sequence - 1);
            bool player = phase == CombatControllerPhase.Player;
            var state = new CombatCommandState(width, height, player ? CombatFlowPhase.PlayerInput : CombatFlowPhase.Automatic,
                true, seed.State.Structures, seed.State.ThornFields, turn, environment);
            return WithStorms(new CombatCommandInput(player ? 1 : 0, player ? CombatCommandKind.EndPlayerTurn : CombatCommandKind.CompleteEnemyTurn,
                EntityId.Empty, default, state, characters ?? seed.Characters, seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                battle: new CombatBattleFacts(4, 1, width, height, Array.Empty<CharacterInstanceId>(), 0),
                processingKind: player ? CombatProcessingKind.Command : CombatProcessingKind.Advance,
                environmentBindings: new EnvironmentSemanticBindings(sources.Where(s => s is StaticTerrainFact || s is TornadoSourceFact || s is DestinationTileModifierFact)
                    .Select(s => s.Id).ToArray(), sources.OfType<ObjectSourceFact>().Select(s => s.Id).ToArray(), Array.Empty<EntityId>()),
                expectedControllerTurnSequence: sequence));
        }

        [Test]
        public void FatalEnemyEndKeepsItsIncrementBeforeTornadoAndStopsAtActualCenter()
        {
            var sources = TornadoGimmickResolutionTests.WallsExcept(8, 8, CellCoord.Zero, new CellCoord(0, 7), new CellCoord(3, 4)).ToList();
            sources.Add(new MineSourceFact(900, CellCoord.Zero, 1, 1000, true, new[] { Owner.Player, Owner.Enemy }));
            var input = TornadoGimmickResolutionTests.Input(count: 1, center: new CellCoord(2, 2), warned: true,
                victim: new CellCoord(0, 7), sources: sources.ToArray());
            var characters = input.Characters.Select(c => c.EntityId.Value == 10 ? CombatCommandTestData.With(c, position: new CellCoord(3, 4)) : c).ToArray();
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: 2,
                completedControllerTurnCount: 1);
            input = input.WithWorld(input.State.With(turn: turn, phase: CombatFlowPhase.Automatic), characters, input.Main,
                input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var result = CombatTurnCalculator.EndEnemyPhase(input, Rules);
            Assert.That(result.Outcome.IsStageEnded, Is.True);
            Assert.That(result.FinalState.Turn.CompletedControllerTurnCount, Is.EqualTo(2));
            Assert.That(result.Events[0].Kind, Is.EqualTo(CombatEventKind.EnemyPhaseEnded));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatTornadoCenterEvent>().Select(e => e.Center), Is.EqualTo(new[] { new CellCoord(2, 2), new CellCoord(3, 3) }));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ValidateGimmickChanges(result));
        }
    }
}
