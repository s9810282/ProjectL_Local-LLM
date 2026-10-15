using System.Linq;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public class CombatCommonCommandTests
    {
        internal static CombatCommandInput Move(params CellCoord[] cells)
        {
            var i = CombatCommandTestData.Input(empty: true);
            return new CombatCommandInput(1, CombatCommandKind.Move, i.ActorId, default,
                i.State, i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls,
                i.Artifacts, path: new MovementPath(cells));
        }

        [Test]
        public void Move_ChargesEnteredRoute_AndPreviewDoesNotMutate()
        {
            var i = Move(new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3));
            var preview = CombatMovementCalculator.Preview(i, System.Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(i, System.Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanMove, Is.True);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = result.Characters.Characters.Single().Final;
            Assert.That(final.Anchor, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(3));
            Assert.That(final.PendingMoveActionCost, Is.EqualTo(2));
            Assert.That(final.MovementCheckpointAnchor, Is.EqualTo(new CellCoord(1, 3)));
            Assert.That(final.Stats.RemainingMoveRange, Is.EqualTo(2));
            Assert.That(preview.PathMovement.ActionCostSpent, Is.EqualTo(2));
            Assert.That(i.Characters[0].Anchor, Is.EqualTo(new CellCoord(1, 3)));
            Assert.That(i.Characters[0].Stats.CurrentActionCost, Is.EqualTo(5));
        }

        [TestCase(1, 3, "SameAnchor")]
        [TestCase(7, 7, "MoveRangeExceeded")]
        [TestCase(-1, 3, "OutOfBounds")]
        public void Move_InvalidDestination_PreservesAllGameplay(int x, int y, string reason)
        {
            var i = x == 1 ? Move(new CellCoord(1, 3)) : x == -1 ?
                Move(new CellCoord(1, 3), new CellCoord(0, 3), new CellCoord(-1, 3)) :
                Move(new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3), new CellCoord(4, 3),
                    new CellCoord(5, 3), new CellCoord(6, 3), new CellCoord(7, 3), new CellCoord(7, 4),
                    new CellCoord(7, 5), new CellCoord(7, 6), new CellCoord(7, 7));
            var result = CombatCommandCalculator.Calculate(i, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.SameValues(i.State), Is.True);
            Assert.That(result.Field.Final.SameValues(i.Souls), Is.True);
        }

        [Test]
        public void Move_OccupiedDestination_IsRejected()
        {
            var i = CombatCommandTestData.Input();
            var input = new CombatCommandInput(1, CombatCommandKind.Move, i.ActorId, default, i.State,
                i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                path: new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3), new CellCoord(4, 3) }));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("DestinationOccupied"));
        }

        [TestCase(CombatCommandKind.Move)]
        [TestCase(CombatCommandKind.Attack)]
        public void PlayerCommand_CannotActForEnemyEvenWithWrongPublishedPhase(CombatCommandKind kind)
        {
            var i = CombatCommandTestData.Input(empty: true);
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Enemy);
            var input = new CombatCommandInput(1, kind, actor.EntityId, AttackAim.ForCell(new CellCoord(2, 3)), i.State,
                new[] { actor, i.Characters[1] }, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                path: kind == CombatCommandKind.Move ? new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3) }) : null);
            Assert.That(CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()).Accepted, Is.False);
        }

        [Test]
        public void Move_BackTowardCheckpoint_AccumulatesCostWithoutRefund()
        {
            var i = Move(new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3));
            var first = CombatCommandCalculator.Calculate(i, System.Array.Empty<IMovementTileRule>());
            var actor = first.Characters.Characters.Single().Final;
            var next = MovementTestData.Route(MovementTestData.After(first), new CellCoord(3, 3), new CellCoord(2, 3));
            var r = CombatCommandCalculator.Calculate(next, System.Array.Empty<IMovementTileRule>());
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(r.PathMovement.ActionCostSpent, Is.EqualTo(1));
            Assert.That(r.Characters.Characters.Single().Final.Stats.CurrentActionCost, Is.EqualTo(2));
            Assert.That(r.Characters.Characters.Single().Final.PendingMoveActionCost, Is.EqualTo(3));
        }

        internal static CombatCommandInput Command(CombatCommandKind kind, CombatCommandInput source = null,
            CombatCommandState state = null, CellCoord cell = default, int number = 1)
        {
            var i = source ?? CombatCommandTestData.Input(empty: true);
            state ??= new CombatCommandState(i.State.BoardWidth, i.State.BoardHeight, i.State.Phase,
                i.State.ActiveActorId, i.State.IsNight, i.State.Structures, i.State.ThornFields, i.State.Timeline,
                new CombatTurnFacts(StagePhase.Combat, participants: new[] { new CombatTurnParticipant(i.ActorId, 100, 10) }));
            return new CombatCommandInput(number, kind, i.ActorId, default, state ?? i.State,
                i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                targetCell: cell, battle: new CombatBattleFacts(10, 20, 37, 0, 0,
                    Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.CharacterInstanceId>(),
                    0, 11, 10, 2));
        }

        [Test]
        public void EndTurn_SchedulesNextTurn_RestoresAp_AndAdvancesNight()
        {
            var i = Command(CombatCommandKind.EndUnitTurn);
            var r = CombatCommandCalculator.Calculate(i, System.Array.Empty<IMovementTileRule>());
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(r.FinalState.ActiveActorId.IsEmpty, Is.True);
            Assert.That(r.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Automatic));
            Assert.That(r.FinalState.Turn.CurrentNightTime, Is.EqualTo(10));
            Assert.That(r.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(1));
            Assert.That(r.FinalState.Turn.Participants.Single().NextActionTime, Is.EqualTo(1));
            Assert.That(r.FinalState.Turn.Participants.Single().OpeningTurnPending, Is.False);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void EnterCombat_InitializesLivingParticipants_AndLocksMainPlacement(int preparationIndex)
        {
            var i = CombatCommandTestData.Input(empty: true);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                new[] { new StructureCombatFacts(new EntityId(11),
                    new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StructureSpec(
                        EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
                    new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.HealthStateSnapshot(100, 100),
                    new CellCoord(7, 6), Rotation4.R0, new[] { new CellCoord(7, 6) }) },
                i.State.ThornFields, Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.CombatTimelineEntrySnapshot>(),
                new CombatTurnFacts(StagePhase.Deployment, preparationIndex,
                    gimmicks: new[] { new CombatGimmickFacts(new StageGimmickId(1), 9, 3) }));
            var input = Command(CombatCommandKind.EnterCombat, i, state);
            input = Recommand(input, battle: new CombatBattleFacts(10, 20, 37, 0, 0,
                Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2,
                gimmicks: new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.RailwayGimmickSpec(1, 2) }));
            var r = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(r.FinalState.Turn.StagePhase, Is.EqualTo(StagePhase.Combat));
            Assert.That(r.FinalState.IsNight, Is.True);
            Assert.That(r.FinalState.Turn.MainPlacementLocked, Is.True);
            Assert.That(r.FinalState.Turn.Participants.Count, Is.EqualTo(2));
            Assert.That(r.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Automatic));
            Assert.That(r.FinalState.Turn.PreparationIndex, Is.EqualTo(preparationIndex));
            Assert.That(r.FinalState.Turn.Gimmicks.Single().NextActivationTurn, Is.EqualTo(1));
            Assert.That(r.FinalState.Turn.Gimmicks.Single().ActivationCount, Is.Zero);
            Assert.That(state.Turn.Gimmicks.Single().NextActivationTurn, Is.EqualTo(9));
            Assert.That(state.Turn.Gimmicks.Single().ActivationCount, Is.EqualTo(3));
        }

        [Test]
        public void TimelineForecast_DefaultTen_AndConfiguredFour_WhenNightAllowsThem()
        {
            var i = Command(CombatCommandKind.EndUnitTurn);
            var input = new CombatCommandInput(1, i.Kind, i.ActorId, default, i.State, i.Characters,
                i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                battle: new CombatBattleFacts(10, 1000, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2));
            Assert.That(CombatTurnCalculator.Forecast(input).Length, Is.EqualTo(10));
            Assert.That(CombatTurnCalculator.Forecast(input, 4).Length, Is.EqualTo(4));
            Assert.That(input.State.Turn.CompletedUnitTurnCount, Is.Zero);
        }

        [Test]
        public void TimelineForecast_RespectsCount_WithoutChangingFacts()
        {
            var i = Command(CombatCommandKind.EndUnitTurn);
            var a = CombatTurnCalculator.Forecast(i, 3);
            var b = CombatTurnCalculator.Forecast(i);
            Assert.That(a.Length, Is.EqualTo(3));
            Assert.That(b.Length, Is.EqualTo(3));
            Assert.That(b[1].EndsNightAfterTurn, Is.True);
            Assert.That(b[2].Kind, Is.EqualTo(ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.CombatTimelineEntryKind.NightEnd));
            Assert.That(CombatTurnCalculator.Forecast(i, 0), Is.Empty);
            Assert.That(i.State.Turn.CompletedUnitTurnCount, Is.Zero);
            Assert.That(a[0].UnitId, Is.EqualTo(i.ActorId));
        }

        [Test]
        public void TimelineForecast_IncludesRailwayAfterActiveTurn_BeforeNightEnd()
        {
            var i = Command(CombatCommandKind.EndUnitTurn);
            var t = i.State.Turn;
            var turn = new CombatTurnFacts(t.StagePhase, participants: t.Participants,
                gimmicks: new[] { new CombatGimmickFacts(new StageGimmickId(1), 1) });
            var state = new CombatCommandState(8, 8, i.State.Phase, i.ActorId, true,
                i.State.Structures, i.State.ThornFields, i.State.Timeline, turn);
            var battle = new CombatBattleFacts(10, 20, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2,
                gimmicks: new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.RailwayGimmickSpec(1, 2) });
            var query = new CombatCommandInput(1, i.Kind, i.ActorId, default, state, i.Characters,
                i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, battle: battle);
            var result = CombatTurnCalculator.Forecast(query);
            Assert.That(result.Select(e => e.Kind), Is.EqualTo(new[] {
                CombatTimelineEntryKind.UnitTurn, CombatTimelineEntryKind.StageGimmick,
                CombatTimelineEntryKind.UnitTurn, CombatTimelineEntryKind.NightEnd }));
            Assert.That(result[1].RemainingUnitTurns, Is.EqualTo(1));
            Assert.That(query.State.Turn.Gimmicks.Single().ActivationCount, Is.Zero);
        }

        [Test]
        public void LethalRoute_RejectsEndTurnWithoutAdvancingNight()
        {
            var i = Command(CombatCommandKind.EndUnitTurn);
            var actor = CombatCommandTestData.With(i.Characters[0], ap: 1);
            var state = new CombatCommandState(8, 8, i.State.Phase, i.ActorId, true, i.State.Structures,
                new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.ThornFieldCellSnapshot(new CellCoord(2, 3), 2, 100) },
                i.State.Timeline, new CombatTurnFacts(StagePhase.Combat, currentNightTime: 10, participants: i.State.Turn.Participants));
            var input = new CombatCommandInput(1, i.Kind, i.ActorId, default, state, new[] { actor, i.Characters[1] },
                i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, battle: i.Battle);
            var r = CombatCommandCalculator.Calculate(MovementTestData.Route(input, new CellCoord(1, 3), new CellCoord(2, 3)),
                new IMovementTileRule[] { new ThornMovementTileRule() });
            Assert.That(r.Accepted, Is.True, r.Reason);
            Assert.That(r.Characters.Characters.Single(c => c.Final.EntityId == actor.EntityId).Final.Health.IsDead, Is.True);
            Assert.That(r.Characters.Characters.Single(c => c.Final.EntityId == actor.EntityId).Final.Stats.CurrentActionCost, Is.Zero);
            Assert.That(r.FinalState.Turn.Participants.Any(p => p.EntityId == actor.EntityId), Is.False);
            Assert.That(r.FinalState.Turn.StagePhase, Is.EqualTo(StagePhase.Combat));
            Assert.That(r.FinalState.Turn.CurrentNightTime, Is.EqualTo(10));
            Assert.That(r.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Automatic));
            var rejected = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(MovementTestData.After(r),
                CombatCommandKind.EndUnitTurn), System.Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Characters, Is.Empty);
            Assert.That(rejected.FinalState.SameValues(r.FinalState), Is.True);
        }

        [Test]
        public void Placement_Return_IsFree_AndMainCannotBeReturned()
        {
            var i = CombatCommandTestData.Input(empty: true);
            var deployment = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                i.State.Structures, i.State.ThornFields, Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.CombatTimelineEntrySnapshot>(),
                new CombatTurnFacts(StagePhase.Deployment));
            var r = CombatCommandCalculator.Calculate(Command(CombatCommandKind.ReturnUnit, i, deployment), System.Array.Empty<IMovementTileRule>());
            Assert.That(r.Accepted, Is.True, r.Reason);
            var final = r.Characters.Characters.Single().Final;
            Assert.That(final.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(final.ReserveSlotIndex, Is.Zero);
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(final.OccupiedCells, Is.Empty);
        }

        [TestCase(1, 10, "FirstPreparation")]
        [TestCase(2, 1, "InsufficientGold")]
        public void Draw_QueryAndCommandHaveSameRejection(int preparation, int gold, string reason)
        {
            var i = Draw(preparation, gold);
            var query = CombatDrawCalculator.Availability(i);
            var result = CombatCommandCalculator.Calculate(i, System.Array.Empty<IMovementTileRule>());
            Assert.That(query.FailureReason.ToString(), Is.EqualTo(reason));
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(reason));
            Assert.That(result.Characters.Created, Is.Empty);
        }

        [Test]
        public void Draw_CreatesOwnedHandCharacter_AndProposesCursorIdGoldTogether()
        {
            var i = Draw(2, 10);
            var result = CombatCommandCalculator.Calculate(i, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var c = result.Characters.Created.Single();
            Assert.That(c.OwnedId.Value.Value, Is.EqualTo(99));
            Assert.That(c.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(c.ReserveSlotIndex, Is.Zero);
            Assert.That(c.EntityId.Value, Is.EqualTo(11));
            Assert.That(result.Draw.FinalGold, Is.EqualTo(8));
            Assert.That(result.Draw.FinalCursor, Is.EqualTo(1));
            Assert.That(result.Draw.FinalNextEntityId, Is.EqualTo(12));
            Assert.That(i.Battle.Gold, Is.EqualTo(10));
            Assert.That(i.Battle.DrawCursor, Is.Zero);
        }

        [Test]
        public void AutomaticEnemy_MovesOnce_Attacks_AndEndsTurn_InOneProposal()
        {
            var i = Command(CombatCommandKind.Attack);
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Enemy, 20);
            var s = actor.Stats;
            actor = new CharacterCombatFacts(actor.EntityId, null, actor.CharacterKey, Owner.Enemy, null, actor.Area, actor.Health,
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.UnitStatsSnapshot(s.MoveMethod,
                    2, s.MoveCost, s.AppointRange, s.AttackPower, s.PhysicalDefense, s.MaxActionCost,
                    s.AttackCost, 2, s.CurrentActionCost, s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category),
                null, actor.Anchor, actor.Rotation, actor.OccupiedCells, actor.MovementCheckpointAnchor, 0, 0);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.Automatic, actor.EntityId, true,
                i.State.Structures, i.State.ThornFields, i.State.Timeline, i.State.Turn);
            var automatic = new CombatCommandInput(0, CombatCommandKind.Attack, actor.EntityId, default,
                state, new[] { actor, i.Characters[1] }, i.Profiles, i.Main, i.Owned, i.SoulCounts,
                i.Souls, i.Artifacts, battle: i.Battle, processingKind: CombatProcessingKind.Advance);
            var result = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi.EnemyAiTurnService.Calculate(automatic, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.Attack.HasValue, Is.True);
            // 빈 중심의 추가 피해 조준은 불가하므로 두 칸 이동 후 적대 주 대상에게 20 피해를 준다.
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId.Value == 10).Final.Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(result.Attack.Value.PrimaryTargetId, Is.EqualTo(new EntityId(10)));
            Assert.That(result.Attack.Value.PrimaryCell, Is.EqualTo(i.Characters[1].Anchor));
            Assert.That(result.Attack.Value.Hits.Span[0].Role, Is.EqualTo(AttackTargetRole.Primary));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(20));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(2));
            Assert.That(result.FinalState.ActiveActorId.IsEmpty, Is.True);
            Assert.That(result.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(1));
            Assert.That(result.Input.Number, Is.Zero);
            Assert.That(automatic.Characters[0].Anchor, Is.EqualTo(new CellCoord(1, 3)));
            var events = result.Events.Select(e => e.Kind).ToArray();
            Assert.That(Array.IndexOf(events, CombatEventKind.Moved), Is.LessThan(Array.IndexOf(events, CombatEventKind.Attacked)));
            Assert.That(Array.IndexOf(events, CombatEventKind.Attacked), Is.LessThan(Array.IndexOf(events, CombatEventKind.TurnEnded)));
        }

        [Test]
        public void Railway_EliminatesOnlyUnits_OnRails_AndAdvancesSchedule()
        {
            var i = Command(CombatCommandKind.EndUnitTurn);
            var turn = new CombatTurnFacts(StagePhase.Combat, mainPlacementLocked: true, completedUnitTurnCount: 1,
                participants: i.State.Turn.Participants, gimmicks: new[] { new CombatGimmickFacts(new StageGimmickId(1), 1) });
            var state = new CombatCommandState(8, 8, CombatFlowPhase.Automatic, EntityId.Empty, true,
                i.State.Structures, i.State.ThornFields, i.State.Timeline, turn);
            var b = new CombatBattleFacts(10, 20, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2,
                new[] { new CellCoord(1, 3) }, new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.RailwayGimmickSpec(1, 2) });
            var automatic = new CombatCommandInput(0, CombatCommandKind.EndUnitTurn, EntityId.Empty, default,
                state, i.Characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                battle: b, processingKind: CombatProcessingKind.Advance);
            var result = CombatGimmickCalculator.TryAdvance(automatic);
            Assert.That(result.Characters.Characters.Single().Final.Health.IsDead, Is.True);
            Assert.That(result.FinalState.Turn.Gimmicks.Single().NextActivationTurn, Is.EqualTo(3));
            Assert.That(result.FinalState.Turn.Gimmicks.Single().ActivationCount, Is.EqualTo(1));
            Assert.That(result.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(1));
        }

        [TestCase(1, 3)]
        [TestCase(2, 4)]
        public void Move_FromPendingSegment_ChargesEveryEnteredStep(int x, int y)
        {
            var first = CombatCommandCalculator.Calculate(Move(new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3)), System.Array.Empty<IMovementTileRule>());
            var actor = first.Characters.Characters.Single().Final;
            var input = MovementTestData.Route(MovementTestData.After(first), new CellCoord(3, 3),
                new CellCoord(2, 3), new CellCoord(x, y));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = result.Characters.Characters.Single().Final;
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(1));
            Assert.That(final.PendingMoveActionCost, Is.EqualTo(4));
            Assert.That(result.PathMovement.ActionCostSpent, Is.EqualTo(2));
            Assert.That(final.MovementCheckpointAnchor, Is.EqualTo(new CellCoord(1, 3)));
            Assert.That(actor.Anchor, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(3));
        }

        [TestCase(2, 2, 3, true, 3, 2)]
        [TestCase(2, 3, 3, true, 1, 4)]
        [TestCase(2, 4, 3, false, 5, 0)]
        [TestCase(3, 2, 3, true, 2, 3)]
        [TestCase(3, 3, 3, false, 5, 0)]
        public void Move_OwnerCost_UsesFullRouteQuoteBeforeSpending(int cost, int x, int y, bool accepted, int ap, int pending)
        {
            var cells = x == 2 ? new[] { new CellCoord(1, 3), new CellCoord(2, 3) } : x == 3 ?
                new[] { new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3) } :
                new[] { new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3), new CellCoord(4, 3) };
            var input = Move(cells);
            var actor = WithMovement(input.Characters[0], cost: cost);
            input = Recommand(input, characters: new[] { actor, input.Characters[1] });
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.EqualTo(accepted), result.Reason);
            if (accepted)
            {
                var final = result.Characters.Characters.Single().Final;
                Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(ap));
                Assert.That(final.PendingMoveActionCost, Is.EqualTo(pending));
            }
            else
            {
                Assert.That(result.Reason, Is.EqualTo("ActionCostInsufficient"));
                Assert.That(result.Characters.Characters, Is.Empty);
            }
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(actor.PendingMoveActionCost, Is.Zero);
        }

        [Test]
        public void MovementSettlement_AttackAndTurnCommitOnlyAcceptedSegment()
        {
            var moved = CombatCommandCalculator.Calculate(Move(new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3)), System.Array.Empty<IMovementTileRule>());
            var actor = moved.Characters.Characters.Single().Final;
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy);
            var input = Recommand(Command(CombatCommandKind.Attack), characters: new[] { actor, moved.Input.Characters[1], enemy },
                aim: AttackAim.ForCell(new CellCoord(4, 3)));
            var rejected = CombatCommandCalculator.Calculate(Recommand(input, aim: AttackAim.ForCell(new CellCoord(99, 0))), System.Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Characters, Is.Empty);
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = result.Characters.Characters.Single(c => c.Final.EntityId == actor.EntityId).Final;
            Assert.That(final.MovementCheckpointAnchor, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(final.PendingMoveActionCost, Is.Zero);
            Assert.That(final.CommittedMoveDistance, Is.EqualTo(2));
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(1));
            Assert.That(final.Stats.RemainingMoveRange, Is.EqualTo(2));
            Assert.That(actor.PendingMoveActionCost, Is.EqualTo(2));
        }

        [Test]
        public void Placement_RelocationAndReturn_PreserveIdentityHealthStatsAndFreeResources()
        {
            var input = Command(CombatCommandKind.PlaceUnit);
            var turn = new CombatTurnFacts(StagePhase.Deployment);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                input.State.Structures, input.State.ThornFields, input.State.Timeline, turn);
            input = Recommand(input, state: state, cell: new CellCoord(2, 0),
                battle: new CombatBattleFacts(10, 20, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2));
            Assert.That(CombatPlacementCalculator.Preview(input).CanPlace, Is.True);
            var placed = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            var actor = placed.Characters.Characters.Single().Final;
            Assert.That(actor.EntityId, Is.EqualTo(input.ActorId));
            Assert.That(actor.OwnedId, Is.EqualTo(input.Characters[0].OwnedId));
            Assert.That(actor.Health, Is.EqualTo(input.Characters[0].Health));
            Assert.That(actor.Stats, Is.EqualTo(input.Characters[0].Stats));
            var returned = CombatCommandCalculator.Calculate(Recommand(input, kind: CombatCommandKind.ReturnUnit,
                characters: new[] { actor, input.Characters[1] }), System.Array.Empty<IMovementTileRule>());
            var hand = returned.Characters.Characters.Single().Final;
            Assert.That(hand.ReserveSlotIndex, Is.Zero);
            Assert.That(hand.OccupiedCells, Is.Empty);
            Assert.That(hand.Health, Is.EqualTo(actor.Health));
            Assert.That(hand.Stats, Is.EqualTo(actor.Stats));
            Assert.That(input.Characters[0].Anchor, Is.EqualTo(new CellCoord(1, 3)));
        }

        [TestCase(-1, 0)]
        [TestCase(8, 0)]
        [TestCase(1, 1)]
        [TestCase(7, 7)]
        public void Placement_InvalidOrOccupiedCell_PreservesAllFacts(int x, int y)
        {
            var input = Command(CombatCommandKind.PlaceUnit);
            var blocker = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(1, 1), Owner.Enemy);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                input.State.Structures, input.State.ThornFields, input.State.Timeline, new CombatTurnFacts(StagePhase.Deployment));
            input = Recommand(input, state: state, characters: input.Characters.Concat(new[] { blocker }).ToArray(), cell: new CellCoord(x, y),
                battle: new CombatBattleFacts(10, 20, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2));
            Assert.That(CombatPlacementCalculator.Preview(input).CanPlace, Is.False);
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState, Is.SameAs(state));
        }

        [Test]
        public void Return_FullHandRejectsWithoutChanges_AndFreedSlotAllowsReturn()
        {
            var input = Command(CombatCommandKind.ReturnUnit);
            var actor = input.Characters[0];
            var hand = new CharacterCombatFacts(new EntityId(3), new CharacterInstanceId(3), EntityKey.Unit_Akimbo,
                Owner.Player, CharacterOwnershipRole.Sub, CharacterRuntimeArea.Hand, actor.Health, actor.Stats,
                0, null, null, Array.Empty<CellCoord>(), CellCoord.Zero, 0, 0);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                input.State.Structures, input.State.ThornFields, input.State.Timeline, new CombatTurnFacts(StagePhase.Deployment));
            input = Recommand(input, state: state, characters: new[] { actor, input.Characters[1], hand },
                battle: new CombatBattleFacts(1, 20, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2));
            var rejected = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Reason, Is.EqualTo("ReserveFull"));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Characters, Is.Empty);
            Assert.That(rejected.FinalState, Is.SameAs(state));
            Assert.That(actor.Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(hand.ReserveSlotIndex, Is.Zero);
            var returned = CombatCommandCalculator.Calculate(Recommand(input, characters: new[] { actor, input.Characters[1] }), System.Array.Empty<IMovementTileRule>());
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            Assert.That(returned.Characters.Characters.Single().Final.ReserveSlotIndex, Is.Zero);
        }

        [Test]
        public void Placement_MainCannotReturnOrMoveAfterCombatLock_AndCombatPlacementRejects()
        {
            var input = Command(CombatCommandKind.ReturnUnit);
            var main = input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                input.State.Structures, input.State.ThornFields, input.State.Timeline, new CombatTurnFacts(StagePhase.Deployment, mainPlacementLocked: true));
            var returned = CombatCommandCalculator.Calculate(Recommand(input, state: state, actorId: main.EntityId), System.Array.Empty<IMovementTileRule>());
            Assert.That(returned.Accepted, Is.False);
            var placed = CombatCommandCalculator.Calculate(Recommand(input, kind: CombatCommandKind.PlaceMainCharacter, state: state, cell: new CellCoord(1, 0)), System.Array.Empty<IMovementTileRule>());
            Assert.That(placed.Accepted, Is.False);
            Assert.That(placed.Characters.Characters, Is.Empty);
            Assert.That(CombatCommandCalculator.Calculate(Recommand(input, kind: CombatCommandKind.PlaceUnit, cell: new CellCoord(1, 0)), System.Array.Empty<IMovementTileRule>()).Accepted, Is.False);
        }

        [TestCase(1, 100, 1)]
        [TestCase(35, 30, 4)]
        [TestCase(200, 30, 4)]
        public void EndTurn_PriorityControlsInterval_DelayControlsNight_AndRestoresSpentAp(int delay, int priority, long interval)
        {
            var input = Command(CombatCommandKind.EndUnitTurn);
            var actor = WithMovement(input.Characters[0], ap: 1, delay: delay, priority: priority);
            var turn = new CombatTurnFacts(StagePhase.Combat, participants: new[] { new CombatTurnParticipant(actor.EntityId, priority, delay) });
            var state = new CombatCommandState(8, 8, input.State.Phase, actor.EntityId, true,
                input.State.Structures, input.State.ThornFields, input.State.Timeline, turn);
            var battle = new CombatBattleFacts(10, 1000, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2);
            input = Recommand(input, state: state, battle: battle, characters: new[] { actor, input.Characters[1] });
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Turn.Participants.Single().NextActionTime, Is.EqualTo(interval));
            Assert.That(result.FinalState.Turn.CurrentNightTime, Is.EqualTo(delay));
            Assert.That(result.Characters.Characters.Single().Final.Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(1));
            Assert.That(turn.CurrentNightTime, Is.Zero);
        }

        [Test]
        public void EndTurn_TimeOverflow_ThrowsWithoutChangingCharacterOrTurnFacts()
        {
            var input = Command(CombatCommandKind.EndUnitTurn);
            var turn = new CombatTurnFacts(StagePhase.Combat, currentTime: long.MaxValue, participants: input.State.Turn.Participants);
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, true,
                input.State.Structures, input.State.ThornFields, input.State.Timeline, turn);
            input = Recommand(input, state: state);
            Assert.Throws<OverflowException>(() => CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()));
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(turn.CurrentTime, Is.EqualTo(long.MaxValue));
            Assert.That(turn.CompletedUnitTurnCount, Is.Zero);
        }

        [Test]
        public void Timeline_OpeningPriority_AgreesBetweenForecastAndAdvance()
        {
            var input = Command(CombatCommandKind.EndUnitTurn);
            var participants = new[] { new CombatTurnParticipant(new EntityId(1), 30, 35), new CombatTurnParticipant(new EntityId(10), 100, 10) };
            var turn = new CombatTurnFacts(StagePhase.Combat, participants: participants);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.Automatic, EntityId.Empty, true,
                input.State.Structures, input.State.ThornFields, input.State.Timeline, turn);
            var automatic = Recommand(input, state: state, automatic: true);
            var first = CombatCommandCalculator.Advance(automatic, System.Array.Empty<IMovementTileRule>());
            Assert.That(first.FinalState.ActiveActorId.Value, Is.EqualTo(10));
            var forecast = CombatTurnCalculator.Forecast(automatic, 2);
            Assert.That(forecast.Select(e => e.UnitId.Value), Is.EqualTo(new[] { new EntityId(10), new EntityId(1) }));
            Assert.That(CombatTurnCalculator.Forecast(automatic, 2).Select(e => e.UnitId), Is.EqualTo(forecast.Select(e => e.UnitId)));
            Assert.That(turn.CurrentTime, Is.Zero);
            Assert.That(turn.Participants.All(p => p.OpeningTurnPending), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => CombatTurnCalculator.Forecast(automatic, -1));
        }

        [Test]
        public void NightRecallFailure_DoesNotEndTurnOrRestoreApInInput()
        {
            var input = Command(CombatCommandKind.EndUnitTurn);
            var hand = CombatPlacementCalculator.Location(input.Characters[0], CharacterRuntimeArea.Hand, 0, null, null);
            hand = new CharacterCombatFacts(new EntityId(3), new CharacterInstanceId(3), EntityKey.Unit_Akimbo,
                Owner.Player, CharacterOwnershipRole.Sub, hand.Area, hand.Health, hand.Stats, 0, null, null,
                Array.Empty<CellCoord>(), CellCoord.Zero, 0, 0);
            var actor = CombatCommandTestData.With(input.Characters[0], ap: 1);
            var battle = new CombatBattleFacts(1, 10, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0, 11, 0, 2);
            input = Recommand(input, battle: battle, characters: new[] { actor, input.Characters[1], hand });
            Assert.Throws<InvalidOperationException>(() => CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()));
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(1));
            Assert.That(actor.Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(input.State.Turn.PreparationIndex, Is.EqualTo(1));
            Assert.That(input.State.Turn.CompletedUnitTurnCount, Is.Zero);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EnemyRedeployment_ReservesOriginsBeforeFallback_AndRejectsFullZone(bool blocked)
        {
            var input = Command(CombatCommandKind.EnterCombat);
            var main = CombatCommandTestData.With(input.Characters[1], position: new CellCoord(1, 7));
            var a = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(1, 7), Owner.Enemy);
            var b = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(2, 7), Owner.Enemy);
            var waiting = new[] { a, b }.Select(c => CombatPlacementCalculator.Location(c, CharacterRuntimeArea.EnemyWaiting, null, null, null)).ToArray();
            var hq = Headquarters(new CellCoord(0, 7));
            var turn = new CombatTurnFacts(StagePhase.Deployment, 2, true,
                enemyOrigins: new[] { new CombatEnemyOrigin(a.EntityId, a.Anchor.Value, Rotation4.R90), new CombatEnemyOrigin(b.EntityId, b.Anchor.Value, Rotation4.R0) });
            var state = new CombatCommandState(blocked ? 3 : 4, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                new[] { hq }, input.State.ThornFields, input.State.Timeline, turn,
                new MovementEnvironmentFacts(new MovementTileFact[] {
                    new DestinationTileModifierFact(7, new CellCoord(3, 7), true, new[] { Owner.Enemy }, 3, 5) }));
            input = Recommand(input, state: state, characters: new[] { main }.Concat(waiting).ToArray(),
                battle: new CombatBattleFacts(8, 100, 37, 1, 1, Array.Empty<CharacterInstanceId>(), 0, 11, 0, 2));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.EqualTo(!blocked), result.Reason);
            if (blocked)
            {
                Assert.That(result.Reason, Is.EqualTo("EnemyRedeploymentBlocked"));
                Assert.That(result.Characters.Characters, Is.Empty);
                Assert.That(result.FinalState, Is.SameAs(state));
            }
            else
            {
                Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == a.EntityId).Final.Anchor, Is.EqualTo(new CellCoord(3, 7)));
                var redeployed = result.Characters.Characters.Single(c => c.Final.EntityId == a.EntityId).Final;
                Assert.That(redeployed.PositionModifier.SourceIds, Is.EqualTo(new long[] { 7 }));
                Assert.That(redeployed.Health.MaxHealth, Is.EqualTo(105));
                Assert.That(redeployed.Stats.PositionAttackBonus, Is.EqualTo(3));
                Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == b.EntityId).Final.Anchor, Is.EqualTo(new CellCoord(2, 7)));
                Assert.That(result.FinalState.Turn.EnemyOrigins[0].Anchor, Is.EqualTo(new CellCoord(1, 7)));
                Assert.That(result.FinalState.Turn.Participants.Count, Is.EqualTo(3));
            }
            Assert.That(waiting.All(c => c.Area == CharacterRuntimeArea.EnemyWaiting), Is.True);
        }

        [TestCase("first", "FirstPreparation")]
        [TestCase("combat", "WrongPhase")]
        [TestCase("ended", "StageEnded")]
        [TestCase("empty", "DeckEmpty")]
        [TestCase("limit", "LivingSubLimitReached")]
        public void Draw_GameplayRejectionPrecedesAffordability_WithoutProposingAnyWrite(string condition, string reason)
        {
            var input = Draw(condition == "first" ? 1 : 2, 0);
            var phase = condition == "ended" ? CombatFlowPhase.Finished : input.State.Phase;
            var turn = new CombatTurnFacts(condition == "combat" ? StagePhase.Combat : StagePhase.Deployment,
                condition == "first" ? 1 : 2);
            var state = new CombatCommandState(8, 8, phase, EntityId.Empty, false, input.State.Structures,
                input.State.ThornFields, input.State.Timeline, turn);
            var old = input.Battle;
            var battle = new CombatBattleFacts(condition == "limit" ? 1 : 10, 20, 37, 0, 0, old.DrawOrder,
                condition == "empty" ? 1 : 0, 11, 0, 2);
            input = Recommand(input, state: state, battle: battle);
            Assert.That(CombatDrawCalculator.Availability(input).FailureReason.ToString(), Is.EqualTo(reason));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Created, Is.Empty);
            Assert.That(result.Draw, Is.Null);
            Assert.That(battle.Gold, Is.Zero);
            Assert.That(result.FinalState, Is.SameAs(state));
        }

        [Test]
        public void Draw_AllocatorOverflow_ThrowsBeforeCursorGoldOrCharacterChange()
        {
            var input = Draw(2, 10);
            var battle = new CombatBattleFacts(10, 20, 37, 0, 0, input.Battle.DrawOrder, 0, (long)int.MaxValue + 1, 10, 2);
            input = Recommand(input, battle: battle);
            Assert.Throws<OverflowException>(() => CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()));
            Assert.That(battle.DrawCursor, Is.Zero);
            Assert.That(battle.Gold, Is.EqualTo(10));
            Assert.That(battle.NextEntityId, Is.EqualTo((long)int.MaxValue + 1));
            Assert.That(input.Characters.Count, Is.EqualTo(2));
        }

        [TestCase(3, 3, 5, 1)]
        [TestCase(3, 4, 3, 0)]
        [TestCase(4, 3, 3, 0)]
        public void MainMovement_AttackCommitsEnteredDistanceAndChargesBaseCostOnce(int x, int y, int ap, int remaining)
        {
            var input = Command(CombatCommandKind.Move);
            var main = CombatCommandTestData.With(input.Characters[1], position: new CellCoord(2, 3));
            main = new CharacterCombatFacts(main.EntityId, main.OwnedId, main.CharacterKey, main.Owner,
                main.OwnershipRole, main.Area, main.Health, main.Stats, null, main.Anchor, main.Rotation,
                main.OccupiedCells, main.Anchor.Value, 0, 0);
            main = WithMovement(main, cost: 2, ap: 10, maximumAp: 10, moveRange: 2, attackCost: 3);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, main.EntityId, true,
                input.State.Structures, input.State.ThornFields, input.State.Timeline,
                new CombatTurnFacts(StagePhase.Combat, participants: new[] { new CombatTurnParticipant(main.EntityId, 100, 10) }));
            input = Recommand(input, state: state, characters: new[] { main }, actorId: main.EntityId, cell: new CellCoord(x, y));
            var cells = new System.Collections.Generic.List<CellCoord> { new CellCoord(2, 3) };
            for (int column = 3; column <= x; column++) cells.Add(new CellCoord(column, 3));
            if (y == 4) cells.Add(new CellCoord(x, 4));
            var move = CombatCommandCalculator.Calculate(MovementTestData.Route(input, cells.ToArray()), System.Array.Empty<IMovementTileRule>());
            Assert.That(move.Accepted, Is.True, move.Reason);
            var actor = move.Characters.Characters.Single().Final;
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(x, y + 1), Owner.Enemy);
            var attacked = CombatCommandCalculator.Calculate(Recommand(input, kind: CombatCommandKind.Attack,
                characters: new[] { actor, target }, aim: AttackAim.ForCell(target.Anchor.Value)), System.Array.Empty<IMovementTileRule>());
            Assert.That(attacked.Accepted, Is.True, attacked.Reason);
            var final = attacked.Characters.Characters.Single(c => c.Final.EntityId == main.EntityId).Final;
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(ap));
            Assert.That(final.Stats.RemainingMoveRange, Is.EqualTo(remaining));
            Assert.That(final.MovementCheckpointAnchor, Is.EqualTo(new CellCoord(x, y)));
            Assert.That(final.PendingMoveActionCost, Is.Zero);
            Assert.That(final.CommittedMoveDistance, Is.EqualTo(2 - remaining));
            Assert.That(main.Stats.CurrentActionCost, Is.EqualTo(10));
        }

        [Test]
        public void Main_AttackFirstPreservesMovementBudget_AndFollowingMoveChargesOnlyDistance()
        {
            var input = Command(CombatCommandKind.Attack);
            var main = CombatCommandTestData.With(input.Characters[1], position: new CellCoord(2, 3));
            main = new CharacterCombatFacts(main.EntityId, main.OwnedId, main.CharacterKey, main.Owner,
                main.OwnershipRole, main.Area, main.Health, main.Stats, null, main.Anchor, main.Rotation,
                main.OccupiedCells, main.Anchor.Value, 0, 0);
            main = WithMovement(main, cost: 2, ap: 10, maximumAp: 10, moveRange: 2, attackCost: 3);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, main.EntityId, true,
                input.State.Structures, input.State.ThornFields, input.State.Timeline,
                new CombatTurnFacts(StagePhase.Combat, participants: new[] { new CombatTurnParticipant(main.EntityId, 100, 10) }));
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 4), Owner.Enemy);
            input = Recommand(input, state: state, characters: new[] { main, target }, actorId: main.EntityId,
                aim: AttackAim.ForCell(target.Anchor.Value));
            var attack = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(attack.Accepted, Is.True, attack.Reason);
            var actor = attack.Characters.Characters.Single(c => c.Final.EntityId == main.EntityId).Final;
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(7));
            Assert.That(actor.Stats.RemainingMoveRange, Is.EqualTo(2));
            Assert.That(actor.CommittedMoveDistance, Is.Zero);
            Assert.That(actor.MovementCheckpointAnchor, Is.EqualTo(new CellCoord(2, 3)));
            var move = CombatCommandCalculator.Calculate(MovementTestData.Route(MovementTestData.After(attack),
                new CellCoord(2, 3), new CellCoord(3, 3)), System.Array.Empty<IMovementTileRule>());
            Assert.That(move.Accepted, Is.True, move.Reason);
            var final = move.Characters.Characters.Single().Final;
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(final.PendingMoveActionCost, Is.EqualTo(2));
            Assert.That(final.Stats.RemainingMoveRange, Is.EqualTo(1));
            Assert.That(main.Stats.CurrentActionCost, Is.EqualTo(10));
        }

        [TestCase(CombatCommandKind.Move)]
        [TestCase(CombatCommandKind.Attack)]
        public void InsufficientAp_RejectsBeforeMovementCheckpointOrAttackSettlement(CombatCommandKind kind)
        {
            var input = Command(kind);
            var actor = CombatCommandTestData.With(input.Characters[0], ap: 0);
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy);
            input = Recommand(input, characters: new[] { actor, input.Characters[1], target }, cell: new CellCoord(2, 3),
                aim: AttackAim.ForCell(new CellCoord(4, 3)));
            if (kind == CombatCommandKind.Move) input = MovementTestData.Route(input, new CellCoord(1, 3), new CellCoord(2, 3));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            if (kind == CombatCommandKind.Attack)
                Assert.That(result.Reason, Is.EqualTo(AttackFailureReason.ActionCostInsufficient.ToString()));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(actor.Anchor, Is.EqualTo(new CellCoord(1, 3)));
            Assert.That(actor.PendingMoveActionCost, Is.Zero);
            Assert.That(actor.Stats.CurrentActionCost, Is.Zero);
        }

        [Test]
        public void Timeline_EqualPrioritySeededOrder_IsIndependentOfParticipantInputOrder()
        {
            var input = Command(CombatCommandKind.EndUnitTurn);
            var values = new[] { new CombatTurnParticipant(new EntityId(1), 100, 10), new CombatTurnParticipant(new EntityId(10), 100, 10) };
            EntityId[] ids = null;
            foreach (var ordered in new[] { values, values.Reverse().ToArray() })
            {
                var turn = new CombatTurnFacts(StagePhase.Combat, participants: ordered);
                var state = new CombatCommandState(8, 8, CombatFlowPhase.Automatic, EntityId.Empty, true,
                    input.State.Structures, input.State.ThornFields, input.State.Timeline, turn);
                var query = Recommand(input, state: state, automatic: true);
                var forecast = CombatTurnCalculator.Forecast(query, 2).Select(e => e.UnitId.Value).ToArray();
                if (ids == null) ids = forecast;
                else Assert.That(forecast, Is.EqualTo(ids));
                Assert.That(CombatCommandCalculator.Advance(query, System.Array.Empty<IMovementTileRule>()).FinalState.ActiveActorId, Is.EqualTo(ids[0]));
                Assert.That(ordered.All(p => p.NextActionTime == 0 && p.CompletedTurnCount == 0), Is.True);
            }
        }

        [Test]
        public void EnterCombat_MainAloneIsEnough_AndFailureDoesNotLockOrResetCheckpoints()
        {
            var input = Command(CombatCommandKind.EnterCombat);
            var main = input.Characters[1];
            var turn = new CombatTurnFacts(StagePhase.Deployment);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                new[] { Headquarters(new CellCoord(6, 6)) }, input.State.ThornFields, input.State.Timeline, turn);
            input = Recommand(input, state: state, characters: new[] { main });
            var entered = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(entered.Accepted, Is.True, entered.Reason);
            Assert.That(entered.FinalState.Turn.Participants.Single().EntityId, Is.EqualTo(main.EntityId));
            Assert.That(entered.FinalState.Turn.MainPlacementLocked, Is.True);
            var checkpoint = entered.Characters.Characters.Single().Final;
            Assert.That(checkpoint.MovementCheckpointAnchor, Is.EqualTo(main.Anchor.Value));
            Assert.That(checkpoint.PendingMoveActionCost, Is.Zero);
            var invalidState = new CombatCommandState(8, 8, state.Phase, EntityId.Empty, false,
                Array.Empty<StructureCombatFacts>(), state.ThornFields, state.Timeline, turn);
            var rejected = CombatCommandCalculator.Calculate(Recommand(input, state: invalidState), System.Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo("InvalidEnemyHeadquarters"));
            Assert.That(rejected.Characters.Characters, Is.Empty);
            Assert.That(turn.MainPlacementLocked, Is.False);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Railway_MainDeathEndsBattle_RegularDeathLeavesMainAndStructuresIntact(bool killMain)
        {
            var input = Command(CombatCommandKind.EndUnitTurn);
            var main = input.Characters[1];
            var regular = input.Characters[0];
            var turn = new CombatTurnFacts(StagePhase.Combat, completedUnitTurnCount: 1,
                participants: new[] { new CombatTurnParticipant(main.EntityId, 100, 10), new CombatTurnParticipant(regular.EntityId, 100, 10) },
                gimmicks: new[] { new CombatGimmickFacts(new StageGimmickId(1), 1) });
            var hq = Headquarters(new CellCoord(6, 6));
            var state = new CombatCommandState(8, 8, CombatFlowPhase.Automatic, EntityId.Empty, true,
                new[] { hq }, input.State.ThornFields, input.State.Timeline, turn);
            var battle = new CombatBattleFacts(8, 100, 37, 1, 1, Array.Empty<CharacterInstanceId>(), 0, 21, 10, 2,
                new[] { killMain ? main.Anchor.Value : regular.Anchor.Value },
                new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks.RailwayGimmickSpec(1, 2) });
            var result = CombatGimmickCalculator.TryAdvance(Recommand(input, state: state, battle: battle, automatic: true));
            Assert.That(result.Outcome.Outcome, Is.EqualTo(killMain ? StageOutcome.Defeat : StageOutcome.InProgress));
            var victim = result.Characters.Characters.Single(c => c.Final.EntityId == (killMain ? main.EntityId : regular.EntityId)).Final;
            Assert.That(victim.Health.CurrentHealth, Is.Zero);
            Assert.That(victim.OccupiedCells, Is.Empty);
            Assert.That(result.FinalState.Turn.Participants.Any(p => p.EntityId == victim.EntityId), Is.False);
            Assert.That(result.FinalState.Structures.Single().Health, Is.EqualTo(hq.Health));
            var survivor = killMain ? regular : main;
            var finalSurvivor = result.Characters.Characters.FirstOrDefault(c => c.Final.EntityId == survivor.EntityId)?.Final ?? survivor;
            Assert.That(finalSurvivor.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(finalSurvivor.Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(finalSurvivor.Anchor, Is.EqualTo(survivor.Anchor));
        }

        [Test]
        public void Outcome_MainDeathPrecedesHeadquartersDeath_AndRejectsInvalidMainBinding()
        {
            var input = Command(CombatCommandKind.EndUnitTurn);
            var main = CombatCommandTestData.With(input.Characters[1], hp: 0);
            var hq = Headquarters(new CellCoord(6, 6));
            var deadHq = new StructureCombatFacts(hq.EntityId, hq.Definition,
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.HealthStateSnapshot(0, 100),
                hq.Anchor, hq.Rotation, hq.OccupiedCells);
            var result = BattleOutcomeCalculator.Resolve(new[] { input.Characters[0], main }, new[] { deadHq });
            Assert.That(result.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(result.Reason, Is.EqualTo(StageOutcomeReason.PlayerMainCharacterDied));
            Assert.Throws<InvalidOperationException>(() => BattleOutcomeCalculator.Resolve(new[] { input.Characters[0] }, new[] { hq }));
            Assert.Throws<InvalidOperationException>(() => BattleOutcomeCalculator.Resolve(new[] { main, main }, new[] { hq }));
            Assert.That(input.Characters[0].Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void Draw_LivingLimitCountsOnlyLivingPlayerSubsOnFieldOrInHand()
        {
            var source = Draw(2, 10);
            var enemy = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(6, 6), Owner.Enemy);
            var dead = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(5, 5), Owner.Player);
            dead = new CharacterCombatFacts(dead.EntityId, dead.OwnedId, dead.CharacterKey, dead.Owner, dead.OwnershipRole,
                CharacterRuntimeArea.Excluded, new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.HealthStateSnapshot(0, 100),
                dead.Stats, null, null, null, Array.Empty<CellCoord>(), dead.MovementCheckpointAnchor, 0, 0);
            var state = new CombatCommandState(8, 8, source.State.Phase, EntityId.Empty, false,
                new[] { Headquarters(new CellCoord(7, 6)) }, source.State.ThornFields, source.State.Timeline, source.State.Turn);
            var owned = source.Owned.Concat(new[] { new OwnedCharacterRecord(dead.OwnedId.Value, dead.CharacterKey, CharacterOwnershipRole.Sub) }).ToArray();
            var counts = source.SoulCounts.ToDictionary(p => p.Key, p => p.Value);
            counts.Add(dead.OwnedId.Value, 0);
            var input = new CombatCommandInput(1, source.Kind, EntityId.Empty, default, state,
                source.Characters.Concat(new[] { enemy, dead }).ToArray(), source.Profiles, source.Main, owned, counts,
                source.Souls, source.Artifacts, battle: new CombatBattleFacts(2, 20, 37, 0, 0, source.Battle.DrawOrder, 0, 21, 10, 2));
            Assert.That(CombatDrawCalculator.Availability(input).CanDraw, Is.True);
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Characters.Created.Single().OwnedId.Value.Value, Is.EqualTo(99));
            Assert.That(result.Characters.Created.Single().ReserveSlotIndex, Is.Zero);
            Assert.That(dead.Health.IsDead, Is.True);
            Assert.That(source.Battle.DrawCursor, Is.Zero);
        }

        [Test]
        public void Draw_ZeroCostIsFree_AndExhaustedOrderRejectsWithoutCharacterOrCursorChanges()
        {
            var source = Draw(2, 0);
            var battle = new CombatBattleFacts(10, 20, 37, 0, 0, source.Battle.DrawOrder, 0, 11, 0, 0);
            var input = Recommand(source, battle: battle);
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Draw.FinalGold, Is.Zero);
            Assert.That(result.Draw.FinalCursor, Is.EqualTo(1));
            var after = Recommand(input, battle: new CombatBattleFacts(10, 20, 37, 0, 0, battle.DrawOrder,
                1, 12, 0, 0), characters: input.Characters.Concat(result.Characters.Created).ToArray());
            Assert.That(CombatDrawCalculator.Availability(after).RemainingCount, Is.Zero);
            var rejected = CombatCommandCalculator.Calculate(after, System.Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Reason, Is.EqualTo("DeckEmpty"));
            Assert.That(rejected.Characters.Created, Is.Empty);
            Assert.That(rejected.Draw, Is.Null);
            Assert.That(battle.DrawCursor, Is.Zero);
        }

        [TestCase(100, false)]
        [TestCase(1, true)]
        public void AutomaticEnemy_InitialAttackDoesNotMove_AndTerminalAttackSkipsTurnEnd(int mainHealth, bool terminal)
        {
            var input = AutomaticInput(mainHealth: mainHealth);
            var result = CombatCommandCalculator.Advance(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.Movement.HasValue, Is.False);
            Assert.That(result.Attack.Value.ActionCostSpent, Is.EqualTo(2));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(terminal ? 1 : 20));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.TurnEnded), Is.EqualTo(terminal ? 0 : 1));
            Assert.That(result.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(terminal ? 0 : 1));
            Assert.That(result.Outcome.Outcome, Is.EqualTo(terminal ? StageOutcome.Defeat : StageOutcome.InProgress));
            Assert.That(input.Characters[1].Health.CurrentHealth, Is.EqualTo(mainHealth));
        }

        [Test]
        public void AutomaticEnemy_LethalRouteSkipsAttackAndTurnEnd()
        {
            var input = AutomaticInput();
            var actor = input.Characters[0];
            var state = new CombatCommandState(8, 8, CombatFlowPhase.Automatic, actor.EntityId, true,
                input.State.Structures, new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.ThornFieldCellSnapshot(new CellCoord(1, 2), 5, 100) },
                input.State.Timeline, input.State.Turn);
            input = Recommand(input, characters: new[] { actor, input.Characters[1] }, state: state, automatic: true);
            var result = CombatCommandCalculator.Calculate(MovementTestData.Route(input,
                new CellCoord(1, 3), new CellCoord(1, 2)), new IMovementTileRule[] { new ThornMovementTileRule() });
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.Movement.HasValue, Is.False);
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.TurnEnded), Is.Zero);
            Assert.That(result.FinalState.Turn.CompletedUnitTurnCount, Is.Zero);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == actor.EntityId).Final.Health.IsDead, Is.True);
            Assert.That(result.FinalState.Turn.Participants, Is.Empty);
            Assert.That(input.Characters[1].Health.CurrentHealth, Is.EqualTo(100));
            var rejected = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(MovementTestData.After(result),
                CombatCommandKind.Attack, input.Aim), System.Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
        }

        [TestCase(1, 1)]
        [TestCase(5, 0)]
        public void AutomaticEnemy_InsufficientApOrZeroDamageEndsWithoutIllegalAction(int ap, int attackPower)
        {
            var input = AutomaticInput(ap: ap, attackPower: attackPower);
            var result = CombatCommandCalculator.Advance(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.Movement.HasValue, Is.False);
            Assert.That(result.FinalState.Turn.CompletedUnitTurnCount, Is.EqualTo(1));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.TurnEnded), Is.EqualTo(1));
            Assert.That(result.Characters.Characters.Single().Final.Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(ap));
        }

        private static CombatCommandInput AutomaticInput(int mainHealth = 100, int ap = 5, int attackPower = 20)
        {
            var input = Command(CombatCommandKind.Attack);
            var actor = CombatCommandTestData.With(CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo,
                new CellCoord(1, 3), Owner.Enemy, attackPower), ap: ap);
            var main = CombatCommandTestData.With(input.Characters[1], hp: mainHealth, position: new CellCoord(2, 3));
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, attackPower),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var state = new CombatCommandState(8, 8, CombatFlowPhase.Automatic, actor.EntityId, true,
                input.State.Structures, input.State.ThornFields, input.State.Timeline, input.State.Turn);
            return new CombatCommandInput(0, CombatCommandKind.Attack, actor.EntityId, default, state,
                new[] { actor, main }, profiles, input.Main, input.Owned, input.SoulCounts, input.Souls,
                input.Artifacts, battle: input.Battle, processingKind: CombatProcessingKind.Advance);
        }

        private static CombatCommandInput Recommand(CombatCommandInput input, CombatCommandKind? kind = null,
            CharacterCombatFacts[] characters = null, CombatCommandState state = null, CombatBattleFacts battle = null,
            CellCoord? cell = null, AttackAim? aim = null, EntityId? actorId = null, bool automatic = false) =>
            new(automatic ? 0 : input.Number, kind ?? input.Kind, actorId ?? input.ActorId, aim ?? input.Aim,
                state ?? input.State, characters ?? input.Characters.ToArray(), input.Profiles, input.Main, input.Owned,
                input.SoulCounts, input.Souls, input.Artifacts, cell ?? input.TargetCell, input.Rotation, battle ?? input.Battle,
                automatic ? CombatProcessingKind.Advance : CombatProcessingKind.Command,
                input.Path, input.MovementContext, input.BattleInstanceId, input.ArtifactItems);

        private static CharacterCombatFacts WithMovement(CharacterCombatFacts c, int? cost = null, int? ap = null, int? priority = null, int? delay = null,
            int? maximumAp = null, int? moveRange = null, int? attackCost = null)
        {
            var s = c.Stats;
            var stats = new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.UnitStatsSnapshot(
                s.MoveMethod, moveRange ?? s.MoveRange, cost ?? s.MoveCost, s.AppointRange, s.AttackPower,
                s.PhysicalDefense, maximumAp ?? s.MaxActionCost, attackCost ?? s.AttackCost, moveRange ?? s.RemainingMoveRange, ap ?? s.CurrentActionCost,
                priority ?? s.Priority, delay ?? s.Delay, s.TraitStatuses.ToArray(), s.Category,
                s.ArtifactAttackBonus, s.EffectiveAttackPower, s.SoulAttackBonus, s.SoulMaxHealthBonus, s.PositionAttackBonus);
            return new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole, c.Area,
                c.Health, stats, c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.MovementCheckpointAnchor,
                c.CommittedMoveDistance, c.PendingMoveActionCost, c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus,
                c.PendingMoveDistance, c.AttackRefundEligibleMoveCost, c.PositionModifier);
        }

        private static StructureCombatFacts Headquarters(CellCoord cell) => new(new EntityId(20),
            new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StructureSpec(
                EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
            new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.HealthStateSnapshot(100, 100),
            cell, Rotation4.R0, new[] { cell });

        private static CombatCommandInput Draw(int preparation, int gold)
        {
            var i = CombatCommandTestData.Input(empty: true);
            var owned = i.Owned.Concat(new[] { new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.OwnedCharacterRecord(
                new CharacterInstanceId(99), EntityKey.Unit_Akimbo,
                ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterOwnershipRole.Sub) }).ToArray();
            var counts = i.SoulCounts.ToDictionary(p => p.Key, p => p.Value);
            counts.Add(new CharacterInstanceId(99), 0);
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, EntityId.Empty, false,
                i.State.Structures, i.State.ThornFields,
                Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.CombatTimelineEntrySnapshot>(),
                new CombatTurnFacts(StagePhase.Deployment, preparation));
            return new CombatCommandInput(1, CombatCommandKind.DrawCharacter, EntityId.Empty, default,
                state, i.Characters, i.Profiles, i.Main, owned, counts, i.Souls, i.Artifacts,
                battle: new CombatBattleFacts(10, 20, 37, 0, 0, new[] { new CharacterInstanceId(99) }, 0, 11, gold, 2));
        }
    }
}
