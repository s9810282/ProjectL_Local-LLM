using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 필드 유닛 공격 범위 조회(호버 공격 범위 설계 UR2)를 확인한다. 차례·국면과 무관하고, 칸 규칙은 행동 범위와 같으며,
    /// 지형은 플레이어 시점이다.
    /// </summary>
    public sealed class UnitAttackRangeQueryTests
    {
        private static readonly EntityId Active = new EntityId(1);
        private static readonly EntityId Main = new EntityId(10);
        private static readonly EntityId Enemy = new EntityId(3);

        [TestCase(0, UnitActionUse.Available)]
        [TestCase(3, UnitActionUse.Available)]
        [TestCase(3, UnitActionUse.Attacked)]
        public void UnitMoveCells_MatchOrdinaryDistanceRangeWithoutReopeningActions(int allowance, UnitActionUse actionUse)
        {
            var available = MovementTestData.WithMovement(MovementTestData.SafeRoute(
                new CellCoord(1, 1), new CellCoord(2, 1)), allowance);
            var expected = CombatMovementCalculator.Range(available).Destinations.ToArray()
                .Select(destination => destination.TargetAnchor).ToArray();
            var input = CombatCommandTestData.With(available, available.Characters.Select(character =>
                character.EntityId == available.ActorId ? character.With(actionUse: actionUse) : character).ToArray());

            var result = CombatCommandCalculator.GetUnitAttackRange(input);

            Assert.That(result.MoveCells.ToArray(), Is.EqualTo(expected));
            Assert.That(result.MoveCells.ToArray(), Is.EqualTo(result.MoveCells.ToArray()
                .OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray()));
            Assert.That(input.Characters.Single(character => character.EntityId == input.ActorId).ActionUse, Is.EqualTo(actionUse));
            Assert.That(input.State.SameValues(available.State), Is.True);
            if (allowance == 0) Assert.That(result.MoveCells.Length, Is.Zero);
            else Assert.That(result.MoveCells.Length, Is.GreaterThan(0));
        }

        [Test]
        public void ActiveActor_SameCellsAsAttackRange()
        {
            var input = CombatCommandTestData.Input();

            var range = CombatCommandCalculator.GetAttackRange(input);
            var unit = CombatCommandCalculator.GetUnitAttackRange(input);

            Assert.That(range.Succeeded, Is.True, range.FailureReason.ToString());
            Assert.That(unit.Succeeded, Is.True, unit.FailureReason.ToString());
            Assert.That(unit.UnitId, Is.EqualTo(Active));
            Assert.That(unit.RangeCells.ToArray(), Is.Not.Empty);
            Assert.That(unit.RangeCells.ToArray(), Is.EqualTo(range.RangeCells.ToArray()));
        }

        [Test]
        public void EnemyUnit_SameCellsAsItsOwnAttackRangeOnItsTurn()
        {
            var source = CombatCommandTestData.Input();
            var s = source.State;
            var enemyTurn = new CombatCommandState(s.BoardWidth,s.BoardHeight,CombatFlowPhase.Automatic,s.Structures,s.ThornFields,turn: new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: 1));
            var ownTurn = For(source, Enemy, enemyTurn, CombatProcessingKind.Advance);

            var range = CombatCommandCalculator.GetAttackRange(ownTurn);
            var unit = CombatCommandCalculator.GetUnitAttackRange(For(source, Enemy));

            Assert.That(range.Succeeded, Is.True, range.FailureReason.ToString());
            Assert.That(unit.Succeeded, Is.True, unit.FailureReason.ToString());
            Assert.That(unit.UnitId, Is.EqualTo(Enemy));
            Assert.That(unit.RangeCells.ToArray(), Is.Not.Empty);
            Assert.That(unit.RangeCells.ToArray(), Is.EqualTo(range.RangeCells.ToArray()));
        }

        [Test]
        public void AnotherAvailableAlly_CanQueryAndActDuringPlayerTurn()
        {
            var input = For(CombatCommandTestData.Input(), Main);

            Assert.That(CombatCommandCalculator.GetAttackRange(input).Succeeded, Is.True);
            var unit = CombatCommandCalculator.GetUnitAttackRange(input);
            Assert.That(unit.Succeeded, Is.True, unit.FailureReason.ToString());
            Assert.That(unit.UnitId, Is.EqualTo(Main));
            Assert.That(unit.RangeCells.ToArray(), Is.Not.Empty);
        }

        [Test]
        public void DeploymentPhase_SameCellsAsCombatPhase()
        {
            var source = CombatCommandTestData.Input();
            var s = source.State;
            var deployment = new CombatCommandState(s.BoardWidth,s.BoardHeight,CombatFlowPhase.PlayerInput,s.Structures,s.ThornFields,new CombatTurnFacts(StagePhase.Deployment));
            var input = For(source, Active, deployment);

            Assert.That(CombatCommandCalculator.GetAttackRange(input).FailureReason, Is.EqualTo(AttackFailureReason.WrongPhase));
            var unit = CombatCommandCalculator.GetUnitAttackRange(input);
            Assert.That(unit.Succeeded, Is.True, unit.FailureReason.ToString());
            Assert.That(unit.RangeCells.ToArray(),
                Is.EqualTo(CombatCommandCalculator.GetUnitAttackRange(source).RangeCells.ToArray()));
        }

        [Test]
        public void HandUnit_NotInCombat()
        {
            var source = CombatCommandTestData.Input();
            var input = CombatCommandTestData.With(source,
                source.Characters.Select(c => c.EntityId == Active ? InHand(c) : c).ToArray());

            AssertFailure(CombatCommandCalculator.GetUnitAttackRange(input), AttackFailureReason.NotInCombat, Active);
        }

        [Test]
        public void DeadUnit_NotInCombat()
        {
            var source = CombatCommandTestData.Input();
            var input = For(CombatCommandTestData.With(source, source.Characters
                .Select(c => c.EntityId == Enemy ? CombatCommandTestData.With(c, hp: 0) : c).ToArray()), Enemy);

            AssertFailure(CombatCommandCalculator.GetUnitAttackRange(input), AttackFailureReason.NotInCombat, Enemy);
        }

        [Test]
        public void UnknownUnit_EntityNotFound()
        {
            var unknown = new EntityId(99);

            AssertFailure(CombatCommandCalculator.GetUnitAttackRange(For(CombatCommandTestData.Input(), unknown)),
                AttackFailureReason.EntityNotFound, unknown);
        }

        [Test]
        public void BasicAttackDisabled_Rejected()
        {
            var source = CombatCommandTestData.Input();
            var noAttack = new UnitBehaviorSpec(true, false, false);
            var input = For(CombatCommandTestData.With(source, profiles: source.Profiles
                .Select(p => p.Definition.Key != EntityKey.Unit_Demon ? p
                    : new CombatAttackProfile(WithBehavior(p.Definition, noAttack), p.Method)).ToArray()), Enemy);

            AssertFailure(CombatCommandCalculator.GetUnitAttackRange(input), AttackFailureReason.BasicAttackDisabled, Enemy);
        }

        [Test]
        public void FinishedBattle_StageEnded()
        {
            var source = CombatCommandTestData.Input();
            var s = source.State;
            var finished = new CombatCommandState(s.BoardWidth,s.BoardHeight,CombatFlowPhase.Finished,s.Structures,s.ThornFields,s.Turn);

            AssertFailure(CombatCommandCalculator.GetUnitAttackRange(For(source, Enemy, finished)),
                AttackFailureReason.StageEnded, Enemy);
        }

        [Test]
        public void EnemyOnlyKnownSandstorm_DoesNotShortenEnemyRange()
        {
            var source = For(CombatCommandTestData.Input(), Enemy);
            var cell = source.Characters.Single(c => c.EntityId == Enemy).Anchor.Value;
            var plain = CombatCommandCalculator.GetUnitAttackRange(source).RangeCells.ToArray();

            var hidden = CombatCommandCalculator.GetUnitAttackRange(WithSandstorm(source, cell, Owner.Enemy));
            var shared = CombatCommandCalculator.GetUnitAttackRange(WithSandstorm(source, cell, Owner.Enemy, Owner.Player));

            Assert.That(hidden.Succeeded, Is.True, hidden.FailureReason.ToString());
            Assert.That(hidden.RangeCells.ToArray(), Is.EqualTo(plain), "플레이어가 모르는 지형은 사거리에 반영하지 않는다.");
            Assert.That(shared.RangeCells.Length, Is.LessThan(plain.Length), "플레이어가 아는 모래폭풍은 사거리를 줄인다.");
        }

        [Test]
        public void Result_CopiesCellsAndRejectsInconsistentValues()
        {
            var cells = new[] { new CellCoord(1, 1) };
            var moveCells = new[] { new CellCoord(2, 1) };
            var result = UnitAttackRangeResult.Success(Enemy, cells, moveCells);
            cells[0] = new CellCoord(5, 5);
            moveCells[0] = new CellCoord(5, 5);

            Assert.That(result.RangeCells.ToArray(), Is.EqualTo(new[] { new CellCoord(1, 1) }));
            Assert.That(result.MoveCells.ToArray(), Is.EqualTo(new[] { new CellCoord(2, 1) }));
            Assert.Throws<ArgumentException>(() => UnitAttackRangeResult.Failure(AttackFailureReason.None, Enemy));
            Assert.Throws<ArgumentNullException>(() => UnitAttackRangeResult.Success(Enemy, null));
        }

        [Test]
        public void PreviewResult_HoldsOnlyUnitRangeAndRejectsUninitialized()
        {
            var preview = CombatPreviewResult.ForUnitAttackRange(3, UnitAttackRangeResult.Success(Enemy, new[] { new CellCoord(1, 1) }));

            Assert.That(preview.RequestNumber, Is.EqualTo(3));
            Assert.That(preview.UnitRange.HasValue, Is.True);
            Assert.That(preview.Range.HasValue || preview.Attack.HasValue || preview.Move.HasValue ||
                preview.MoveRange.HasValue || preview.Placement.HasValue || preview.ActionAvailability != null, Is.False);
            Assert.Throws<ArgumentException>(() => CombatPreviewResult.ForUnitAttackRange(3, default));
            Assert.Throws<ArgumentException>(() => CombatPreviewResult.ForUnitAttackRange(3,
                UnitAttackRangeResult.Success(EntityId.Empty, Array.Empty<CellCoord>())));
        }

        private static void AssertFailure(UnitAttackRangeResult result, AttackFailureReason reason, EntityId unit)
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo(reason));
            Assert.That(result.UnitId, Is.EqualTo(unit));
            Assert.That(result.RangeCells.Length, Is.Zero);
        }

        private static CombatCommandInput For(CombatCommandInput input, EntityId unit, CombatCommandState state = null,
            CombatProcessingKind? processingKind = null, EnvironmentSemanticBindings bindings = null)
        {
            var kind = processingKind ?? input.ProcessingKind;
            // 자동 진행 입력은 명령 번호가 없다(0).
            int number = kind == CombatProcessingKind.Command ? Math.Max(1, input.Number) : 0;
            return new CombatCommandInput(number, input.Kind, unit, input.Aim, state ?? input.State, input.Characters,
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, input.TargetCell,
                input.Rotation, input.Battle, kind, input.Path, input.MovementContext,
                input.BattleInstanceId, input.ArtifactItems, bindings ?? input.EnvironmentBindings, input.ObstacleContents,
                input.ItemKey, input.ItemInventory, input.ItemDefinitions, input.GoldBalance, input.FameBalance,
                expectedBattleInstanceId: input.BattleInstanceId, expectedControllerTurnSequence: (state ?? input.State).Turn.ControllerTurnSequence);
        }

        private static CombatCommandInput WithSandstorm(CombatCommandInput input, CellCoord cell, params Owner[] knownTo)
        {
            var s = input.State;
            var environment = new MovementEnvironmentFacts(new MovementTileFact[]
            {
                new StaticTerrainFact(11, cell, StaticTerrainKind.Sandstorm, true, knownTo)
            });
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.Structures,s.ThornFields,s.Turn,environment);
            var bindings = new EnvironmentSemanticBindings(new long[] { 11 }, Array.Empty<long>(), Array.Empty<EntityId>());
            return For(input, input.ActorId, state, bindings: bindings);
        }

        private static CharacterCombatFacts InHand(CharacterCombatFacts c) =>
            new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                CharacterRuntimeArea.Hand, c.Health, c.Stats, 0, null, null, Array.Empty<CellCoord>(), UnitActionUse.Available);

        private static UnitSpec WithBehavior(UnitSpec s, UnitBehaviorSpec behavior) => new UnitSpec(s.Key, s.Role, s.FootprintWidth,
            s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod, s.AttackRange,
            s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority, s.Traits.ToArray(), s.OwnerEligibility, s.Occlusion, behavior);
    }
}
