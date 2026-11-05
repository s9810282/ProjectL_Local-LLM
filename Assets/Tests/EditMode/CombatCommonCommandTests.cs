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
    public sealed class CombatCommonCommandTests
    {
        [Test]
        public void RemovedManualDrawCommand_IsRejectedBeforeCalculation()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Command((CombatCommandKind)8));
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

        [TestCase(CombatCommandKind.Move)]
        [TestCase(CombatCommandKind.Attack)]
        public void PlayerCommand_CannotActForEnemyEvenWithWrongPublishedPhase(CombatCommandKind kind)
        {
            var i = CombatCommandTestData.Input(empty: true);
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Enemy);
            var input = new CombatCommandInput(1, kind, actor.EntityId, AttackAim.ForCell(new CellCoord(2, 3)), i.State,
                new[] { actor, i.Characters[1] }, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts,
                path: kind == CombatCommandKind.Move ? new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3) }) : null,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (i.State).Turn.ControllerTurnSequence);
            Assert.That(CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()).Accepted, Is.False);
        }

        [Test]
        public void Placement_Return_IsFree_AndMainCannotBeReturned()
        {
            var i = CombatCommandTestData.Input(empty: true);
            var deployment = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                i.State.Structures,i.State.ThornFields,                new CombatTurnFacts(StagePhase.Deployment));
            var r = CombatCommandCalculator.Calculate(Command(CombatCommandKind.ReturnUnit, i, deployment), System.Array.Empty<IMovementTileRule>());
            Assert.That(r.Accepted, Is.True, r.Reason);
            var final = r.Characters.Characters.Single().Final;
            Assert.That(final.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(final.ReserveSlotIndex, Is.Zero);
            Assert.That(final.ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            Assert.That(final.OccupiedCells, Is.Empty);
        }

        [Test]
        public void Placement_RelocationAndReturn_PreserveIdentityHealthStatsAndFreeResources()
        {
            var input = Command(CombatCommandKind.PlaceUnit);
            var turn = new CombatTurnFacts(StagePhase.Deployment);
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                input.State.Structures,input.State.ThornFields,turn);
            input = Recommand(input, state: state, cell: new CellCoord(2, 0),
                battle: new CombatBattleFacts(10, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0));
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
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                input.State.Structures,input.State.ThornFields,new CombatTurnFacts(StagePhase.Deployment));
            input = Recommand(input, state: state, characters: input.Characters.Concat(new[] { blocker }).ToArray(), cell: new CellCoord(x, y),
                battle: new CombatBattleFacts(10, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0));
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
                0, null, null, Array.Empty<CellCoord>(), UnitActionUse.Available);
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                input.State.Structures,input.State.ThornFields,new CombatTurnFacts(StagePhase.Deployment));
            input = Recommand(input, state: state, characters: new[] { actor, input.Characters[1], hand },
                battle: new CombatBattleFacts(1, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0));
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
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                input.State.Structures,input.State.ThornFields,new CombatTurnFacts(StagePhase.Deployment, mainPlacementLocked: true));
            var returned = CombatCommandCalculator.Calculate(Recommand(input, state: state, actorId: main.EntityId), System.Array.Empty<IMovementTileRule>());
            Assert.That(returned.Accepted, Is.False);
            var placed = CombatCommandCalculator.Calculate(Recommand(input, kind: CombatCommandKind.PlaceMainCharacter, state: state, cell: new CellCoord(1, 0)), System.Array.Empty<IMovementTileRule>());
            Assert.That(placed.Accepted, Is.False);
            Assert.That(placed.Characters.Characters, Is.Empty);
            Assert.That(CombatCommandCalculator.Calculate(Recommand(input, kind: CombatCommandKind.PlaceUnit, cell: new CellCoord(1, 0)), System.Array.Empty<IMovementTileRule>()).Accepted, Is.False);
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
            var state = new CombatCommandState(blocked ? 3 : 4,8,CombatFlowPhase.PlayerInput,false,                new[] { hq },input.State.ThornFields,turn,                new MovementEnvironmentFacts(new MovementTileFact[] {
                    new DestinationTileModifierFact(7, new CellCoord(3, 7), true, new[] { Owner.Enemy }, 3, 5) }));
            input = Recommand(input, state: state, characters: new[] { main }.Concat(waiting).ToArray(),
                battle: new CombatBattleFacts(8, 37, 7, 1, Array.Empty<CharacterInstanceId>(), 0));
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
                Assert.That(CombatCommandComposition.Project(result).Characters.Count(c => c.Area == CharacterRuntimeArea.Field), Is.EqualTo(3));
            }
            Assert.That(waiting.All(c => c.Area == CharacterRuntimeArea.EnemyWaiting), Is.True);
        }

        [Test]
        public void Outcome_MainDeathPrecedesHeadquartersDeath_AndRejectsInvalidMainBinding()
        {
            var input = Command(CombatCommandKind.EndPlayerTurn);
            var main = CombatCommandTestData.With(input.Characters[1], hp: 0);
            var hq = Headquarters(new CellCoord(6, 6));
            var deadHq = new StructureCombatFacts(hq.EntityId, hq.Definition,
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.HealthStateSnapshot(0, 100),
                hq.Anchor, hq.Rotation, hq.OccupiedCells);
            var result = BattleOutcomeCalculator.Resolve(new[] { input.Characters[0], main }, new[] { deadHq }, BattleObjective.EnemyHeadquarters);
            Assert.That(result.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(result.Reason, Is.EqualTo(StageOutcomeReason.PlayerMainCharacterDied));
            Assert.Throws<InvalidOperationException>(() => BattleOutcomeCalculator.Resolve(new[] { input.Characters[0] }, new[] { hq }, BattleObjective.EnemyHeadquarters));
            Assert.Throws<InvalidOperationException>(() => BattleOutcomeCalculator.Resolve(new[] { main, main }, new[] { hq }, BattleObjective.EnemyHeadquarters));
            Assert.That(input.Characters[0].Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void AutomaticEnemy_AttacksBadgeHolder_ConsumesProtection_ThenDamagesNextTurn()
        {
            var input = AutomaticInput();
            var actor = input.Characters[0];
            input = CombatCommandTestData.With(input, new[] { actor, HeldEnvironmentEffects.AcquireBadge(input.Characters[1], 41) });

            var first = CombatCommandCalculator.Advance(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(first.Attack.HasValue, Is.True);
            var shielded = first.Characters.Characters.Single(c => c.Final.EntityId == input.Characters[1].EntityId).Final;
            Assert.That(shielded.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(shielded.HeldEnvironment.BadgeAvailable, Is.False);

            var second = CombatCommandCalculator.Advance(CombatCommandTestData.With(input, new[] { actor, shielded }),
                System.Array.Empty<IMovementTileRule>());
            Assert.That(second.Attack.HasValue, Is.True);
            Assert.That(second.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(20));
        }

        [Test]
        public void Move_MatchesPreviewAndLeavesInputUnchanged()
        {
            var input = Move(new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3));
            var preview = CombatMovementCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.CanMove, Is.True);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Anchor, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(2));
            Assert.That(input.Characters[0].Anchor, Is.EqualTo(new CellCoord(1, 3)));
            Assert.That(input.Characters[0].ActionUse, Is.EqualTo(UnitActionUse.Available));
        }

        [Test]
        public void Move_OccupiedDestination_IsRejected()
        {
            var source = CombatCommandTestData.Input();
            var input = MovementTestData.Route(source, new CellCoord(1, 3), new CellCoord(2, 3),
                new CellCoord(3, 3), new CellCoord(4, 3));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("DestinationOccupied"));
            Assert.That(result.FinalState.SameValues(input.State), Is.True);
        }

        [TestCase(1)]
        [TestCase(2)]
        public void EnterCombat_LocksMainAndDefersUnitsUntilPlayerStart(int preparationIndex)
        {
            var source = Command(CombatCommandKind.EnterCombat);
            var before = source.State.With(isNight: false,
                turn: new CombatTurnFacts(StagePhase.Deployment, preparationIndex),
                structures: new[] { Headquarters(new CellCoord(6, 6)) });
            var entered = CombatCommandCalculator.Calculate(Recommand(source, state: before), Array.Empty<IMovementTileRule>());
            Assert.That(entered.Accepted, Is.True, entered.Reason);
            Assert.That(entered.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingPlayer));
            Assert.That(entered.FinalState.Turn.MainPlacementLocked, Is.True);
            Assert.That(entered.FinalState.Turn.PreparationIndex, Is.EqualTo(preparationIndex));
            Assert.That(CombatCommandComposition.Project(entered).Characters.All(c => c.ActionUse == UnitActionUse.Deferred), Is.True);
            var started = CombatTurnCalculator.BeginControllerTurn(CombatCommandComposition.Project(entered));
            Assert.That(started.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Player));
            Assert.That(CombatCommandComposition.Project(started).Characters.All(c => c.ActionUse == UnitActionUse.Available), Is.True);
            Assert.That(before.Turn.MainPlacementLocked, Is.False);
        }

        [Test]
        public void EnterCombat_MainAloneIsEnough_AndInvalidObjectiveLeavesInputUnlocked()
        {
            var source = Command(CombatCommandKind.EnterCombat);
            var main = source.Characters[1];
            var before = source.State.With(isNight: false, turn: new CombatTurnFacts(StagePhase.Deployment),
                structures: new[] { Headquarters(new CellCoord(6, 6)) });
            var input = Recommand(source, state: before, characters: new[] { main });
            var entered = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(entered.Accepted, Is.True, entered.Reason);
            Assert.That(entered.Characters.Characters.Single().Final.ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            var rejected = CombatCommandCalculator.Calculate(Recommand(input,
                state: before.With(structures: Array.Empty<StructureCombatFacts>())), Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo("InvalidEnemyHeadquarters"));
            Assert.That(rejected.Characters.Characters, Is.Empty);
            Assert.That(before.Turn.MainPlacementLocked, Is.False);
            Assert.That(main.ActionUse, Is.EqualTo(UnitActionUse.Available));
        }

        [Test]
        public void ManualEnd_ChangesOnlyControllerProgress()
        {
            var input = Command(CombatCommandKind.EndPlayerTurn);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingEnemy));
            Assert.That(result.FinalState.Turn.CompletedPlayerTurnCount, Is.EqualTo(1));
            Assert.That(result.FinalState.Turn.ControllerTurnSequence, Is.EqualTo(input.State.Turn.ControllerTurnSequence));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(CombatCommandCalculator.Calculate(CombatCommandComposition.Project(result),
                Array.Empty<IMovementTileRule>()).Accepted, Is.False);
        }

        [Test]
        public void CompletedPlayerTurnOverflow_LeavesInputUnchanged()
        {
            var input = Command(CombatCommandKind.EndPlayerTurn);
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Player,
                controllerTurnSequence: 1, completedPlayerTurnCount: long.MaxValue, nightStartPlayerTurnCount: long.MaxValue);
            input = Recommand(input, state: input.State.With(turn: turn));
            Assert.Throws<OverflowException>(() => CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()));
            Assert.That(input.State.Turn.CompletedPlayerTurnCount, Is.EqualTo(long.MaxValue));
            Assert.That(input.Characters.All(c => c.ActionUse == UnitActionUse.Available), Is.True);
        }

        [TestCase(CombatCommandKind.Move)]
        [TestCase(CombatCommandKind.Attack)]
        public void ActionsRequireNoSharedFunding(CombatCommandKind kind)
        {
            var source = CombatCommandTestData.Input();
            var input = Recommand(Command(kind, source), state: source.State.With(),
                aim: source.Aim);
            if (kind == CombatCommandKind.Move) input = MovementTestData.Route(input, new CellCoord(1, 3), new CellCoord(2, 3));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.ActionUseSettlement.FinalActionUse, Is.EqualTo(kind == CombatCommandKind.Move ? UnitActionUse.Moved : UnitActionUse.Attacked));
            Assert.That(result.Characters.Characters, Is.Not.Empty);
            Assert.That(result.Events, Is.Not.Empty);
            Assert.That(result.FinalState.TraitTurn.PlayerAttackActionCount,
                Is.EqualTo(input.State.TraitTurn.PlayerAttackActionCount + (kind == CombatCommandKind.Attack ? 1 : 0)));
            Assert.That(result.FinalState.TraitTurn.EnemyAttackActionCount, Is.EqualTo(input.State.TraitTurn.EnemyAttackActionCount));
            Assert.That(result.FinalState.With(traitTurn: input.State.TraitTurn, movementUndo: input.State.MovementUndo).SameValues(input.State), Is.True);
            Assert.That(input.State.TraitTurn.PlayerAttackActionCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Main_ActionOrder_PerControllerTurn(bool attackFirst)
        {
            var source = Command(CombatCommandKind.Attack);
            var main = CombatCommandTestData.With(source.Characters[1], position: new CellCoord(2, 3));
            var target = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 4), Owner.Enemy);
            var input = Recommand(source, characters: new[] { main, target }, actorId: main.EntityId,
                aim: AttackAim.ForCell(target.Anchor.Value));
            if (!attackFirst) input = MovementTestData.Route(input, new CellCoord(2, 3), new CellCoord(3, 3));
            var first = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(first.Accepted, Is.True, first.Reason);
            var after = CombatCommandComposition.Project(first);
            var secondInput = attackFirst
                ? MovementTestData.Route(after, new CellCoord(2, 3), new CellCoord(3, 3))
                : after.WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(target.Anchor.Value), default, null, null);
            var second = CombatCommandCalculator.Calculate(secondInput, Array.Empty<IMovementTileRule>());
            // 메인도 이동 → 공격은 되고, 공격 → 이동은 안 된다(설계 2026-10-27 이동 후 공격).
            if (attackFirst)
            {
                Assert.That(second.Accepted, Is.False);
                Assert.That(second.Reason, Is.EqualTo("ActionAlreadyUsed"));
                Assert.That(second.Characters.Characters, Is.Empty);
            }
            else
            {
                Assert.That(second.Accepted, Is.True, second.Reason);
            }
            Assert.That(main.ActionUse, Is.EqualTo(UnitActionUse.Available));
        }

        [TestCase(100, false)]
        [TestCase(1, true)]
        public void EnemyAttack_TerminalAttackDoesNotPublishDay(int mainHealth, bool terminal)
        {
            var input = AutomaticInput(mainHealth: mainHealth);
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.PathMovement, Is.Null);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(terminal ? StageOutcome.Defeat : StageOutcome.InProgress));
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.NightEnded ||
                e.Kind == CombatEventKind.ControllerTurnStarted), Is.False);
            Assert.That(input.Characters[1].Health.CurrentHealth, Is.EqualTo(mainHealth));
        }

        [Test]
        public void EnemyWithoutCurrentTarget_MovesWithoutAttacking()
        {
            var input = AutomaticInput();
            var main = CombatCommandTestData.With(input.Characters[1], position: new CellCoord(7, 7));
            input = CombatCommandTestData.With(input, characters: new[] { input.Characters[0], main });
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement, Is.Not.Null);
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
            Assert.That(main.Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void ZeroDamageEnemyAttack_IsStillAnAction()
        {
            var input = AutomaticInput(attackPower: 0);
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.PathMovement, Is.Null);
            Assert.That(MovementTestData.Actor(result).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
        }

        [Test]
        public void NightRecallCapacityFailure_LeavesFactsUnchanged()
        {
            var source = Command(CombatCommandKind.EndPlayerTurn);
            var actor = source.Characters[0].With(actionUse: UnitActionUse.Moved);
            var hand = new CharacterCombatFacts(new EntityId(3), new CharacterInstanceId(3), EntityKey.Unit_Akimbo,
                Owner.Player, CharacterOwnershipRole.Sub, CharacterRuntimeArea.Hand, actor.Health, actor.Stats,
                0, null, null, Array.Empty<CellCoord>());
            var input = EnemyControllerPhaseTests.Input(source, CombatControllerPhase.Enemy, completed: 8,
                characters: new[] { actor, source.Characters[1], hand });
            input = Recommand(input, automatic: true, battle: new CombatBattleFacts(1, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0));
            Assert.Throws<InvalidOperationException>(() => CombatTurnCalculator.EndEnemyPhase(input, Array.Empty<IMovementTileRule>()));
            Assert.That(actor.ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(actor.Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(input.State.Turn.PreparationIndex, Is.EqualTo(1));
        }

        internal static CombatCommandInput Move(params CellCoord[] cells) =>
            MovementTestData.Route(CombatCommandTestData.Input(empty: true), cells);

        internal static CombatCommandInput Command(CombatCommandKind kind, CombatCommandInput source = null,
            CombatCommandState state = null, CellCoord cell = default, int number = 1)
        {
            var input = source ?? CombatCommandTestData.Input(empty: true);
            state ??= input.State;
            return new CombatCommandInput(number, kind, kind == CombatCommandKind.EndPlayerTurn ? EntityId.Empty : input.ActorId,
                input.Aim, state, input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts,
                input.Souls, input.Artifacts, targetCell: cell,
                battle: input.Battle ?? new CombatBattleFacts(10, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0),
                battleInstanceId: input.BattleInstanceId, expectedBattleInstanceId: input.BattleInstanceId,
                expectedControllerTurnSequence: state.Turn.ControllerTurnSequence);
        }

        private static CombatCommandInput AutomaticInput(int mainHealth = 100, int attackPower = 20)
        {
            var source = Command(CombatCommandKind.Attack);
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Enemy, attackPower);
            var main = CombatCommandTestData.With(source.Characters[1], hp: mainHealth, position: new CellCoord(2, 3));
            var profiles = source.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, attackPower),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                controllerTurnSequence: 2, completedPlayerTurnCount: 1);
            // 적 진영 턴 중간이므로 적 진영 시작을 다시 실행하지 않는다.
            var state = source.State.With(phase: CombatFlowPhase.Automatic,turn: turn);
            return new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, actor.EntityId, default,
                state, new[] { actor, main },
                profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                battle: source.Battle, processingKind: CombatProcessingKind.Advance, battleInstanceId: source.BattleInstanceId,
                expectedBattleInstanceId: source.BattleInstanceId, expectedControllerTurnSequence: state.Turn.ControllerTurnSequence);
        }

        private static CombatCommandInput Recommand(CombatCommandInput input, CombatCommandKind? kind = null,
            CharacterCombatFacts[] characters = null, CombatCommandState state = null, CombatBattleFacts battle = null,
            CellCoord? cell = null, AttackAim? aim = null, EntityId? actorId = null, bool automatic = false) =>
            new(automatic ? 0 : input.Number, kind ?? input.Kind, actorId ?? input.ActorId, aim ?? input.Aim,
                state ?? input.State, characters ?? input.Characters.ToArray(), input.Profiles, input.Main, input.Owned,
                input.SoulCounts, input.Souls, input.Artifacts, cell ?? input.TargetCell, input.Rotation, battle ?? input.Battle,
                automatic ? CombatProcessingKind.Advance : input.ProcessingKind, input.Path, input.MovementContext,
                input.BattleInstanceId, input.ArtifactItems, input.EnvironmentBindings, input.ObstacleContents, input.ItemKey,
                input.ItemInventory, input.ItemDefinitions, input.GoldBalance, input.FameBalance, input.Membership,
                input.ExpectedBattleInstanceId, input.ExpectedControllerTurnSequence);

        private static StructureCombatFacts Headquarters(CellCoord cell) => new(new EntityId(20),
            new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
            new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.HealthStateSnapshot(100, 100),
            cell, Rotation4.R0, new[] { cell });
    }
}
