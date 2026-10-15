using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class CombatCommandCalculationTests
    {
        [Test]
        public void ThreeTargets_ChargeAttackApOnce_AndScaleEachAdditionalFromCommonDamage()
        {
            CombatCommandInput input = CombatCommandTestData.Input();
            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.Length, Is.EqualTo(3));
            Assert.That(result.Attack.Value.ActionCostSpent, Is.EqualTo(2));
            Assert.That(result.Characters.Characters.Single(x => x.Final.EntityId == input.ActorId)
                .Final.Stats.CurrentActionCost, Is.EqualTo(3));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(x => x.DamageDealt), Is.EqualTo(new[] { 20, 14, 14 }));
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(5));
        }

        [Test]
        public void InvalidAim_RejectsWithoutMovementHealthOrApChanges()
        {
            CombatCommandInput input = CombatCommandTestData.Input(aim: AttackAim.ForCell(new CellCoord(99, 99)), pending: true);
            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState, Is.SameAs(input.State));
            Assert.That(result.Field.Final, Is.SameAs(input.Souls));
        }

        [TestCase(CategoryKey.Base, false)]
        [TestCase(CategoryKey.Rider, false)]
        [TestCase(CategoryKey.Heavy, false)]
        [TestCase(CategoryKey.Cannon, true)]
        public void EmptySingle_OnlyCannonIsAcceptedAndPaysNormalCost(CategoryKey category, bool accepted)
        {
            var input = CombatCommandTestData.Input(empty: true, pending: true, category: category);
            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.EqualTo(accepted), result.Reason);
            if (!accepted)
            {
                Assert.That(result.Reason, Is.EqualTo(AttackFailureReason.InvalidPrimaryTarget.ToString()));
                Assert.That(result.Characters.Characters, Is.Empty);
                Assert.That(result.Events, Is.Empty);
                Assert.That(result.FinalState, Is.SameAs(input.State));
                Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(5));
                Assert.That(input.Characters[0].PendingMoveActionCost, Is.EqualTo(1));
                return;
            }
            Assert.That(result.Attack.Value.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
            Assert.That(result.Attack.Value.PrimaryCell, Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(result.Attack.Value.ActionCostSpent, Is.EqualTo(2));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Stats.CurrentActionCost, Is.EqualTo(3));
        }

        [Test]
        public void EmptyDirection_RejectsWithoutAttackCost()
        {
            var method = new TargetingMethodSpec(TargetingShape.Fan, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            CombatCommandTransition result = CombatCommandCalculator.Calculate(CombatCommandTestData.Input(
                method: method, aim: AttackAim.ForDirection(AttackDirection.Up), empty: true), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        [TestCase(CategoryKey.Base)]
        [TestCase(CategoryKey.Rider)]
        [TestCase(CategoryKey.Heavy)]
        public void NonCannonSplash_ExcludesCasterAndFriendlySecondary(CategoryKey category)
        {
            var center = new CellCoord(2, 3);
            var input = CombatCommandTestData.Input(aim: AttackAim.ForCell(center), friendly: true, category: category);
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(5, EntityKey.Unit_Demon, center, Owner.Enemy) }).ToArray());
            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(5) }));
            var final = CombatCommandComposition.Project(result);
            Assert.That(final.Characters.Single(c => c.EntityId == input.ActorId).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(final.Characters.Single(c => c.EntityId == new EntityId(2)).Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void CannonSplash_HitsFriendlySecondaryAndExcludesCaster()
        {
            var input = CombatCommandTestData.Input(category: CategoryKey.Cannon);
            var allyId = new EntityId(5);
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(5, EntityKey.Unit_Demon, new CellCoord(5, 4), Owner.Player) }).ToArray());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var hit = result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == allyId);
            Assert.That(hit.Role, Is.EqualTo(AttackTargetRole.Additional));
            Assert.That(hit.DamageDealt, Is.EqualTo(14));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Has.No.Member(input.ActorId));
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == input.ActorId).Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void BoardEdge_ClipsAdditionalCellsWithoutRejectingLegalSingle()
        {
            var center = new CellCoord(0, 0);
            var input = CombatCommandTestData.Input(aim: AttackAim.ForCell(center), empty: true);
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, center, Owner.Enemy) }).ToArray());
            CombatCommandTransition result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.AdditionalCells.Length, Is.EqualTo(3));
            Assert.That(result.Attack.Value.AdditionalCells.ToArray().All(c => c.X >= 0 && c.Y >= 0), Is.True);
        }

        [TestCase(false, 50, 30)]
        [TestCase(true, 20, 0)]
        public void StructureFootprint_IsHitOnce_AndOccupiesEveryCellUntilDeath(bool lethal, int health, int remaining)
        {
            var input = CombatCommandTestData.Input(aim: AttackAim.ForCell(new CellCoord(2, 3)), empty: true);
            var cells = new[] { new CellCoord(2, 3), new CellCoord(2, 4) };
            var structure = new StructureCombatFacts(new EntityId(20),
                new StructureSpec(EntityKey.Structure_PowderBarrel, 1, 2, new[] { CellCoord.Zero, CellCoord.Up }, health, false, 0),
                new HealthStateSnapshot(health, health), cells[0], Rotation4.R0, cells, Owner.Enemy);
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, false,
                new[] { structure }, input.State.ThornFields, input.State.Timeline);
            // 본부 파괴의 전투 종료와 폭발 피해를 제외하고 점유 해제만 검사한다.
            input = ObstacleReactionTests.Copy(CombatCommandTestData.With(input, state: state),
                profiles: new[] { new ObstacleContentProfile(structure.EntityId, ObstacleContentKind.PowderBarrel, 0) });
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.Value.Hits.Length, Is.EqualTo(1));
            Assert.That(result.Attack.Value.Hits.Span[0].Role, Is.EqualTo(AttackTargetRole.Primary));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(20));
            Assert.That(result.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(remaining));
            var projected = CombatCommandComposition.Project(result);
            foreach (var cell in cells)
            {
                var route = cell.Y == 3 ? new[] { new CellCoord(1, 3), new CellCoord(2, 3) } :
                    new[] { new CellCoord(1, 3), new CellCoord(1, 4), new CellCoord(2, 4) };
                var move = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(projected, CombatCommandKind.Move,
                    path: new MovementPath(route)), System.Array.Empty<IMovementTileRule>());
                Assert.That(move.Accepted, Is.EqualTo(lethal), move.Reason);
                if (!lethal) Assert.That(move.Reason, Is.EqualTo("DestinationOccupied"));
            }
            Assert.That(structure.Health.CurrentHealth, Is.EqualTo(health));
        }

        [Test]
        public void RepeatedAttacks_ChargeEachAcceptedIntent_AndDeathReleasesCellForMovement()
        {
            var input = CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0), AttackAim.ForCell(new CellCoord(2, 3)), empty: true);
            var target = CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Demon,
                new CellCoord(2, 3), Owner.Enemy), hp: 30);
            input = CombatCommandTestData.With(input, input.Characters.Concat(new[] { target }).ToArray());
            var first = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(first.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(20));
            var second = CombatCommandCalculator.Calculate(CombatCommandComposition.Project(first), System.Array.Empty<IMovementTileRule>());
            Assert.That(second.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(10));
            Assert.That(second.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Stats.CurrentActionCost, Is.EqualTo(1));
            Assert.That(second.Characters.Characters.Single(c => c.Final.EntityId == target.EntityId).Final.OccupiedCells, Is.Empty);
            var move = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(CombatCommandComposition.Project(second),
                CombatCommandKind.Move, path: new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3) })), System.Array.Empty<IMovementTileRule>());
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(move.Characters.Characters.Single().Final.Stats.CurrentActionCost, Is.Zero);
            Assert.That(move.Characters.Characters.Single().Final.Rotation, Is.EqualTo(Rotation4.R0));
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(30));
        }

        [TestCase(TargetingChoiceMode.Direction, CategoryKey.Base, false)]
        [TestCase(TargetingChoiceMode.All, CategoryKey.Base, false)]
        [TestCase(TargetingChoiceMode.Direction, CategoryKey.Rider, false)]
        [TestCase(TargetingChoiceMode.All, CategoryKey.Rider, false)]
        [TestCase(TargetingChoiceMode.Direction, CategoryKey.Heavy, false)]
        [TestCase(TargetingChoiceMode.All, CategoryKey.Heavy, false)]
        [TestCase(TargetingChoiceMode.Direction, CategoryKey.Cannon, true)]
        [TestCase(TargetingChoiceMode.All, CategoryKey.Cannon, true)]
        public void FriendlyOnlyGroup_OnlyCannonHitsUnitsAndStructures(TargetingChoiceMode choice, CategoryKey category, bool accepted)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, choice, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, choice == TargetingChoiceMode.All
                ? AttackAim.ForAll() : AttackAim.ForDirection(AttackDirection.Right), empty: true, category: category);
            var friend = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Player);
            var structure = new StructureCombatFacts(new EntityId(20),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 50, false, 0),
                new HealthStateSnapshot(50, 50), new CellCoord(3, 3), Rotation4.R0, new[] { new CellCoord(3, 3) }, Owner.Player);
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, false,
                new[] { structure }, input.State.ThornFields, input.State.Timeline);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input,
                input.Characters.Concat(new[] { friend }).ToArray(), state), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.EqualTo(accepted), result.Reason);
            if (!accepted)
            {
                Assert.That(result.Reason, Is.EqualTo((choice == TargetingChoiceMode.All
                    ? AttackFailureReason.UnresolvedEmptyAll : AttackFailureReason.NoEligibleTargets).ToString()));
                Assert.That(result.Characters.Characters, Is.Empty);
                Assert.That(result.Events, Is.Empty);
                Assert.That(result.FinalState, Is.SameAs(state));
                Assert.That(structure.Health.CurrentHealth, Is.EqualTo(50));
                return;
            }
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId.Value), Is.EqualTo(new[] { 2, 20 }));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(new[] { 20, 20 }));
            Assert.That(result.Attack.Value.Hits.ToArray().All(h => h.Role == AttackTargetRole.Designated), Is.True);
            Assert.That(result.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(30));
        }

        [TestCase(CategoryKey.Base, false)]
        [TestCase(CategoryKey.Rider, false)]
        [TestCase(CategoryKey.Heavy, false)]
        [TestCase(CategoryKey.Cannon, true)]
        public void DeadTargetCell_OnlyCannonCanAimAtEmptyCell_AndCasterCenterIsRejected(CategoryKey category, bool accepted)
        {
            var input = CombatCommandTestData.Input(category: category);
            var dead = CombatCommandTestData.With(input.Characters[2], hp: 0);
            dead = new CharacterCombatFacts(dead.EntityId, null, dead.CharacterKey, dead.Owner, null,
                CharacterRuntimeArea.Excluded, dead.Health, dead.Stats, null, null, null,
                Array.Empty<CellCoord>(), dead.MovementCheckpointAnchor, 0, 0);
            input = CombatCommandTestData.With(input, new[] { input.Characters[0], input.Characters[1], dead });
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.EqualTo(accepted), result.Reason);
            if (accepted)
            {
                Assert.That(result.Attack.Value.Hits.ToArray(), Is.Empty);
                Assert.That(result.Attack.Value.ActionCostSpent, Is.EqualTo(2));
            }
            else
            {
                Assert.That(result.Reason, Is.EqualTo(AttackFailureReason.InvalidPrimaryTarget.ToString()));
                Assert.That(result.Characters.Characters, Is.Empty);
                Assert.That(result.Events, Is.Empty);
                Assert.That(result.FinalState, Is.SameAs(input.State));
            }
            var self = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(input, CombatCommandKind.Attack,
                AttackAim.ForCell(input.Characters[0].Anchor.Value)), System.Array.Empty<IMovementTileRule>());
            Assert.That(self.Accepted, Is.False);
            Assert.That(self.Characters.Characters, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Attack_EliminatesOnlyActualCasualtiesFromCanonicalTurnParticipants(bool lethal)
        {
            var input = CombatCommandTestData.Input();
            var chars = input.Characters.ToArray();
            if (lethal)
            {
                chars[2] = CombatCommandTestData.With(chars[2], hp: 1);
                chars[3] = CombatCommandTestData.With(chars[3], hp: 1);
            }
            var turn = new CombatTurnFacts(StagePhase.Combat, participants: chars.Select(c =>
                new CombatTurnParticipant(c.EntityId, c.Stats.Priority, c.Stats.Delay)).ToArray());
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, false,
                input.State.Structures, input.State.ThornFields, input.State.Timeline, turn);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, state), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.FinalState.Turn.Participants.Select(p => p.EntityId.Value),
                Is.EquivalentTo(lethal ? new[] { 1, 4, 10 } : new[] { 1, 2, 3, 4, 10 }));
            Assert.That(result.Events.Count(e => e.Kind == CombatEventKind.Died), Is.EqualTo(lethal ? 2 : 0));
            Assert.That(turn.Participants.Count, Is.EqualTo(5));
        }
    }
}
