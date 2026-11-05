using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerTurnMovementTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [Test]
        public void MovementFailureReason_ValuesAreStable()
        {
            Assert.That((int)MovementFailureReason.None, Is.EqualTo(0));
            Assert.That((int)MovementFailureReason.EntityNotFound, Is.EqualTo(1));
            Assert.That((int)MovementFailureReason.NotUnit, Is.EqualTo(2));
            Assert.That((int)MovementFailureReason.WrongPhase, Is.EqualTo(3));
            Assert.That((int)MovementFailureReason.NotInCombat, Is.EqualTo(4));
            Assert.That((int)MovementFailureReason.Dead, Is.EqualTo(5));
            Assert.That((int)MovementFailureReason.UnsupportedMoveMethod, Is.EqualTo(7));
            Assert.That((int)MovementFailureReason.SameAnchor, Is.EqualTo(8));
            Assert.That((int)MovementFailureReason.OutOfBounds, Is.EqualTo(9));
            Assert.That((int)MovementFailureReason.DestinationOccupied, Is.EqualTo(10));
            Assert.That((int)MovementFailureReason.MoveRangeExceeded, Is.EqualTo(11));
            Assert.That((int)MovementFailureReason.ActionCostInsufficient, Is.EqualTo(12));
            Assert.That((int)MovementFailureReason.StateInconsistent, Is.EqualTo(13));
            Assert.That((int)MovementFailureReason.StageEnded, Is.EqualTo(14));
            Assert.That((int)MovementFailureReason.PathOriginMismatch, Is.EqualTo(15));
            Assert.That((int)MovementFailureReason.InvalidPath, Is.EqualTo(16));
            Assert.That((int)MovementFailureReason.RepeatedCell, Is.EqualTo(17));
            Assert.That((int)MovementFailureReason.StaleContext, Is.EqualTo(19));
            Assert.That((int)MovementFailureReason.VoluntaryMoveDisabled, Is.EqualTo(20));
            Assert.That((int)MovementFailureReason.WrongController, Is.EqualTo(30));
            Assert.That((int)MovementFailureReason.ActionAlreadyUsed, Is.EqualTo(31));
            Assert.That((int)MovementFailureReason.ActionDeferred, Is.EqualTo(32));
            Assert.That((int)MovementFailureReason.StaleTurn, Is.EqualTo(33));
            Assert.That(System.Enum.IsDefined(typeof(MovementFailureReason), 6), Is.False);
            Assert.That(System.Enum.IsDefined(typeof(MovementFailureReason), 18), Is.False);
        }

        [TestCase(1)]
        [TestCase(5)]
        public void InstalledMovement_PreviewAndExecutionAgree(int edges)
        {
            var input = ConfiguredRoute(edges);
            var availability = CombatActionAdmission.GetAvailability(input);
            Assert.That(availability.MovementAllowance, Is.EqualTo(5));
            Assert.That(availability.CanMove, Is.True);
            var preview = CombatMovementCalculator.Preview(input, NoRules);
            var move = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(preview.CanMove, Is.True, preview.FailureReason.ToString());
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(preview.QuotedDistance, Is.EqualTo(edges));
            Assert.That(move.PathMovement.EnteredCells, Is.EqualTo(preview.PathMovement.EnteredCells));
            Assert.That(MovementTestData.Actor(move).ActionUse, Is.EqualTo(UnitActionUse.Moved));
        }

        [TestCase(6, 5, MovementFailureReason.MoveRangeExceeded)]
        [TestCase(1, 0, MovementFailureReason.VoluntaryMoveDisabled)]
        public void InstalledMovement_RejectsInvalidRangeOrDisabledMovement(int edges, int allowance,
            MovementFailureReason reason)
        {
            var input = MovementTestData.WithMovement(ConfiguredRoute(edges),allowance);
            var preview = CombatMovementCalculator.Preview(input, NoRules);
            var move = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(preview.FailureReason, Is.EqualTo(reason));
            Assert.That(move.Accepted, Is.False);
            Assert.That(move.Reason, Is.EqualTo(reason.ToString()));
            Assert.That(move.FinalState, Is.SameAs(input.State));
            Assert.That(move.Characters.Characters, Is.Empty);
            Assert.That(move.Events, Is.Empty);
            var availability = CombatActionAdmission.GetAvailability(input);
            if (allowance == 0) Assert.That(availability.MovementAllowance, Is.Null);
            if (reason != MovementFailureReason.MoveRangeExceeded)
                Assert.That(availability.MoveFailure, Is.EqualTo(reason));
        }

        [Test]
        public void MovementConsumesOnceAndLeavesAttackAvailable()
        {
            var input = MovementTestData.WithMovement(ConfiguredRoute(1),5);
            var preview = CombatMovementCalculator.Preview(input, NoRules);
            var move = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(preview.CanMove, Is.True);
            Assert.That(MovementTestData.Actor(move).ActionUse, Is.EqualTo(UnitActionUse.Moved));
            var after = MovementTestData.After(move);
            var next = MovementTestData.Route(after, new CellCoord(2, 1), new CellCoord(3, 1));
            Assert.That(CombatCommandCalculator.Calculate(next, NoRules).Reason,
                Is.EqualTo(MovementFailureReason.ActionAlreadyUsed.ToString()));
            var availability = CombatActionAdmission.GetAvailability(after);
            Assert.That(availability.CanAttack, Is.True);
            Assert.That(availability.AttackFailure, Is.EqualTo(AttackFailureReason.None));
        }

        [TestCase(6)]
        public void ChangedInstalledMovement_InvalidatesCapturedContext(int allowance)
        {
            var input = ConfiguredRoute(1);
            var expected = MovementContextCalculator.Capture(input, Owner.Player, NoRules);
            var changed = MovementTestData.WithMovement(input,allowance);
            Assert.That(MovementContextCalculator.Matches(expected, changed, Owner.Player, NoRules), Is.False);
            var result = MovementContextCalculator.RejectStale(changed);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo(MovementFailureReason.StaleContext.ToString()));
            Assert.That(result.FinalState, Is.SameAs(changed.State));
            Assert.That(result.Events, Is.Empty);
            Assert.That(result.Characters.Characters, Is.Empty);
        }

        private static CombatCommandInput ConfiguredRoute(int edges)
        {
            var input = CombatCommandTestData.Input(empty: true, category: CategoryKey.Rider);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == actor.EntityId
                ? CombatCommandTestData.With(c, position: new CellCoord(1, 1)) : c).ToArray(),
                state: input.State.With());
            return MovementTestData.WithMovement(MovementTestData.Route(input,
                Enumerable.Range(1, edges + 1).Select(x => new CellCoord(x, 1)).ToArray()),5);
        }

        [Test]
        public void OneLegalLCornerSucceeds()
        {
            var input = Diagonal();
            input = Block(input, new CellCoord(2, 1));
            var preview = CombatMovementCalculator.Preview(input, NoRules);
            var move = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(preview.PathMovement.RequestedPath.Cells, Is.EqualTo(new[]
                { new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(2, 2) }));
            Assert.That(move.PathMovement.RequestedPath.Cells, Is.EqualTo(preview.PathMovement.RequestedPath.Cells));
            Assert.That(MovementTestData.Actor(move).ActionUse, Is.EqualTo(UnitActionUse.Moved));
        }

        [Test]
        public void BothLCornersBlockedReject()
        {
            var input = Block(Block(Diagonal(), new CellCoord(2, 1)), new CellCoord(1, 2));
            var move = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(move.Accepted, Is.False);
            Assert.That(move.Characters.Characters, Is.Empty);
            Assert.That(move.Events, Is.Empty);
        }

        [Test]
        public void PreviewedMiddleCellIsNotSwapped()
        {
            var input = Diagonal();
            var expected = MovementContextCalculator.Capture(input, Owner.Player, NoRules);
            Assert.That(expected.Path.Cells[1], Is.EqualTo(new CellCoord(2, 1)));
            var changed = Block(input, new CellCoord(2, 1));
            Assert.That(CombatMovementCalculator.Preview(changed, NoRules).CanMove, Is.True);
            Assert.That(MovementContextCalculator.Matches(expected, changed, Owner.Player, NoRules), Is.False);
            var move = CombatCommandCalculator.Calculate(changed.WithIntent(changed.Kind, changed.Aim,
                changed.TargetCell, changed.Path, expected), NoRules);
            Assert.That(move.Accepted, Is.False);
            Assert.That(move.Events, Is.Empty);
        }

        [Test]
        public void LaterRepeatedCellMakesNormalizerTryOtherCorner()
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1),                new CellCoord(2, 2),new CellCoord(2, 1));
            var move = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(move.PathMovement.RequestedPath.Cells, Is.EqualTo(new[]
                { new CellCoord(1, 1), new CellCoord(1, 2), new CellCoord(2, 2), new CellCoord(2, 1) }));
        }

        [Test]
        public void MiddleMineStopsBeforeDiagonalEndpoint()
        {
            var input = WithMine(MovementTestData.WithMovement(Diagonal(),5), new CellCoord(2, 1), lethal: true);
            var rules = new IMovementTileRule[] { new MineRule() };
            var move = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(move.PathMovement.EnteredCells, Is.EqualTo(new[] { new CellCoord(2, 1) }));
            Assert.That(move.PathMovement.FinalStop, Is.EqualTo(new CellCoord(2, 1)));
            Assert.That(move.PathMovement.StopReason, Is.EqualTo(MovementStopReason.Dead));
        }

        [Test]
        public void UnchosenMiddleHasNoEffects()
        {
            var input = WithMine(Diagonal(), new CellCoord(1, 2), lethal: true);
            var move = CombatCommandCalculator.Calculate(input, new IMovementTileRule[] { new MineRule() });
            Assert.That(move.Accepted, Is.True, move.Reason);
            Assert.That(MovementTestData.Actor(move).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(move.PathMovement.EnteredCells, Has.No.Member(new CellCoord(1, 2)));
        }

        [Test]
        public void HiddenMiddleMineDoesNotChangePreviewButResolvesOnCommit()
        {
            var clear = Diagonal();
            var hidden = WithMine(clear, new CellCoord(2, 1), lethal: true, hidden: true);
            var rules = new IMovementTileRule[] { new MineRule() };
            var expected = MovementContextCalculator.Capture(clear, Owner.Player, rules);
            Assert.That(MovementContextCalculator.Matches(expected, hidden, Owner.Player, rules), Is.True);
            Assert.That(CombatMovementCalculator.Preview(hidden, rules).ProjectedActor.Health.IsDead, Is.False);
            var move = CombatCommandCalculator.Calculate(hidden, rules);
            Assert.That(MovementTestData.Actor(move).Health.IsDead, Is.True);
            Assert.That(move.PathMovement.DistanceSpent, Is.EqualTo(1));
        }

        [Test]
        public void DetourUsesActualSteps()
        {
            var input = MovementTestData.SafeRoute(new CellCoord(1, 1),                new CellCoord(1, 2),new CellCoord(2, 2),new CellCoord(3, 2),new CellCoord(3, 1));
            var move = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(move.Accepted, Is.False);
            Assert.That(move.Reason, Is.EqualTo(MovementFailureReason.MoveRangeExceeded.ToString()));
            Assert.That(move.Events, Is.Empty);
        }

        [Test]
        public void MoveCompletesWithoutAttackAndCannotActAgain()
        {
            var first = CombatCommandCalculator.Calculate(Diagonal(), NoRules);
            Assert.That(first.Accepted, Is.True, first.Reason);
            Assert.That(MovementTestData.Actor(first).Anchor, Is.EqualTo(new CellCoord(2, 2)));
            var after = MovementTestData.After(first);
            Assert.That(after.Characters.Single(c => c.EntityId != after.ActorId).ActionUse, Is.EqualTo(UnitActionUse.Available));
            var next = MovementTestData.Route(after, new CellCoord(2, 2), new CellCoord(3, 2));
            var rejected = CombatCommandCalculator.Calculate(next, NoRules);
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo(MovementFailureReason.ActionAlreadyUsed.ToString()));
        }

        [Test]
        public void ZeroPathRejectsWithoutCharacterChanges()
        {
            var move = CombatCommandCalculator.Calculate(MovementTestData.SafeRoute(new CellCoord(1, 1)), NoRules);
            Assert.That(move.Accepted, Is.False);
            Assert.That(move.Reason, Is.EqualTo(MovementFailureReason.SameAnchor.ToString()));
            Assert.That(move.Characters.Characters, Is.Empty);
        }

        private static CombatCommandInput Diagonal() => MovementTestData.SafeRoute(            new CellCoord(1, 1),new CellCoord(2, 2));

        private static CombatCommandInput Block(CombatCommandInput input, CellCoord cell)
        {
            int id = 20 + input.Characters.Count;
            return CombatCommandTestData.With(input, characters: input.Characters.Append(
                CombatCommandTestData.Character(id, EntityKey.Unit_Demon, cell, Owner.Enemy)).ToArray());
        }

        private static CombatCommandInput WithMine(CombatCommandInput input, CellCoord cell, bool lethal, bool hidden = false)
        {
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] { new MineFact(cell, lethal, hidden) });
            return CombatCommandTestData.With(input, state: input.State.With(environment: environment,
                environmentProgress: EnvironmentProgressFacts.Initial(environment)));
        }

        private sealed class MineFact : MovementTileFact
        {
            internal bool Lethal { get; }
            internal MineFact(CellCoord cell, bool lethal, bool hidden) : base(71, cell, "phase-b-mine", 0,
                true, new[] { hidden ? Owner.Enemy : Owner.Player }) { Lethal = lethal; }
            public override bool SameValues(MovementTileFact other) => other is MineFact mine &&
                Lethal == mine.Lethal && SameSourceValues(other);
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => this;
        }

        private sealed class MineRule : IMovementTileRule
        {
            public string Key => "phase-b-mine";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile) =>
                input.Phase == MovementEffectPhase.Hazard ? new MovementStepEffects(damage: new[]
                    { new MovementHealthRequest(input.MoverId, ((MineFact)tile).Lethal ? 1000 : 1) }) : new MovementStepEffects();
        }
    }
}
