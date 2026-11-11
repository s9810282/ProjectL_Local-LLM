using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class DustScreenUpgradeTests
    {
        private static readonly Owner[] BothSides = { Owner.Player, Owner.Enemy };

        [TestCase(false)]
        [TestCase(true)]
        public void TrailCapturesCreationSettings(bool upgraded)
        {
            var input = MovementTestData.SafeRoute(CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new DustScreenTraitSpec(1, 20, 2, upgraded));
            var move = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(move.Accepted, Is.True, move.Reason);
            var trail = move.FinalState.Environment.Tiles.OfType<TraitTrailFact>().Single();
            Assert.That((trail.Origin.OwnerInstanceId, trail.CreatingSide, trail.Cell),
                Is.EqualTo((input.ActorId.Value, Owner.Player, CellCoord.Right)));
            Assert.That((trail.AttackRangeReduction, trail.AttackDamageReductionPercent, trail.RemainingTurnEnds,
                trail.ExemptsAlliedAttackRangePenalty), Is.EqualTo((1, 20, 2, upgraded)));
        }

        [Test]
        public void TrailCopiesRetainExemption()
        {
            var trail = Dust(10, CellCoord.Zero, Owner.Player);
            var visible = (TraitTrailFact)trail.WithKnownTo(new[] { Owner.Enemy });
            var shortened = visible.WithRemainingOpposingTurnEnds(1);
            var state = TraitTrailTestData.Environment(CombatCommandTestData.Input(), shortened).State;
            var expired = TraitTrailCalculator.EndControllerTurn(state, Owner.Enemy).Environment.Tiles.OfType<TraitTrailFact>().Single();

            Assert.That(visible.KnownTo, Is.EqualTo(new[] { Owner.Enemy }));
            Assert.That(visible.ExemptsAlliedAttackRangePenalty, Is.True);
            Assert.That(shortened.ExemptsAlliedAttackRangePenalty, Is.True);
            Assert.That(expired.ExemptsAlliedAttackRangePenalty, Is.True);
            Assert.That(expired.Active, Is.False);
            Assert.That(TraitTrailCalculator.RangeReductionAt(expired.Cell, new MovementEnvironmentFacts(new[] { expired }), Owner.Enemy), Is.Zero);
            Assert.That(visible.SuccessorFailure(shortened), Is.Null);
            Assert.That(shortened.SuccessorFailure(expired), Is.Null);
        }

        [Test]
        public void ChangingExemptionIsNotSameConfiguration()
        {
            var upgraded = Dust(10, CellCoord.Zero, Owner.Player);
            var basic = Dust(10, CellCoord.Zero, Owner.Player, upgraded: false);

            Assert.That(basic.ExemptsAlliedAttackRangePenalty, Is.False);
            Assert.That(basic.SameValues(upgraded), Is.False);
            Assert.That(basic.SuccessorFailure(upgraded), Is.Not.Null);
            Assert.That(upgraded.SuccessorFailure(basic), Is.Not.Null);
            Assert.That(new DustScreenTraitSpec(1, 20, 2).SameValues(new DustScreenTraitSpec(1, 20, 2, true)), Is.False);
        }

        [Test]
        public void GroundCompactionCannotCarryDustFlag()
        {
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.GroundCompaction, "main");
            Assert.Throws<ArgumentException>(() => new TraitTrailFact(10, CellCoord.Zero, origin,
                Owner.Player, 1, 0, 0, 2, true, BothSides, exemptsAlliedAttackRangePenalty: true));
        }

        [Test]
        public void DustRankRejectsNumericPayload()
        {
            Assert.Throws<ArgumentException>(() => new TraitRankValues(TraitKey.DustScreen, 1, exemptsAlliedAttackRangePenalty: true));
            Assert.Throws<ArgumentException>(() => new TraitRankValues(TraitKey.DustScreen, 0, 1, exemptsAlliedAttackRangePenalty: true));
        }


        [TestCase(Owner.Player, Owner.Player, false, 1)]
        [TestCase(Owner.Player, Owner.Enemy, false, 1)]
        [TestCase(Owner.Enemy, Owner.Player, false, 1)]
        [TestCase(Owner.Enemy, Owner.Enemy, false, 1)]
        [TestCase(Owner.Player, Owner.Player, true, 0)]
        [TestCase(Owner.Player, Owner.Enemy, true, 1)]
        [TestCase(Owner.Enemy, Owner.Player, true, 1)]
        [TestCase(Owner.Enemy, Owner.Enemy, true, 0)]
        public void RangePenaltyUsesCreatingAndQueryingSides(Owner creator, Owner queryingSide, bool upgraded, int penalty)
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters.First(c => c.Owner == queryingSide);
            var trail = Dust(10, actor.Anchor.Value, creator, upgraded);
            input = TraitTrailTestData.Environment(input, trail);

            Assert.That(EnvironmentTerrainRules.AppointRange(input, actor), Is.EqualTo(actor.Stats.AppointRange - penalty));
            Assert.That(TraitTrailCalculator.RangeReductionAt(trail.Cell, input.State.Environment, queryingSide), Is.EqualTo(penalty));
            Assert.That(TraitTrailCalculator.RangeReductionAt(trail.Cell, input.State.Environment), Is.EqualTo(1));
        }

        [Test]
        public void OverlapKeepsApplicableMaximumAndOrderedTies()
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters.First(c => c.Owner == Owner.Player);
            var cell = actor.Anchor.Value;
            input = TraitTrailTestData.Environment(input, Dust(13, cell, Owner.Player, range: 3),
                Dust(12, cell, Owner.Enemy), Dust(11, cell, Owner.Player, upgraded: false));

            Assert.That(EnvironmentTerrainRules.AppointRange(input, actor), Is.EqualTo(actor.Stats.AppointRange - 1));
            Assert.That(DustScreenTraitRule.RangeSourcesAt(cell, input.State.Environment, actor.Owner).Select(t => t.Id),
                Is.EqualTo(new long[] { 11, 12 }));
        }

        [TestCase(Owner.Player, Owner.Player)]
        [TestCase(Owner.Player, Owner.Enemy)]
        [TestCase(Owner.Enemy, Owner.Player)]
        [TestCase(Owner.Enemy, Owner.Enemy)]
        public void UpgradedDustStillReducesAttackDamageForBothFactions(Owner creator, Owner recipientSide)
        {
            var input = CombatCommandTestData.Input();
            var recipient = input.Characters.First(c => c.Owner == recipientSide);
            var attacker = input.Characters.First(c => c.Owner != recipientSide);
            int Damage(CombatCommandInput current) =>
                TraitTrailTestData.Damage(current, attacker.EntityId, recipient.EntityId, CombatDamageKind.OrdinaryAttack);
            Assert.That(Damage(input), Is.EqualTo(100));
            Assert.That(Damage(TraitTrailTestData.Environment(input, Dust(10, recipient.Anchor.Value, creator))), Is.EqualTo(80));
        }

        [Test]
        public void CreatorDeathDoesNotChangeTrailSettings()
        {
            var move = MovementUndoTestData.Run(MovementEffectUsageTests.ProducerAcrossAlly(new DustScreenTraitSpec(1, 20, 2, true)));
            var current = MovementUndoTestData.After(move);
            var trail = current.State.Environment.Tiles.OfType<TraitTrailFact>().Single();
            Assert.That(trail.Origin.OwnerInstanceId, Is.EqualTo(current.ActorId.Value));
            var afterDeath = CombatCommandTestData.With(current, characters: current.Characters.Select(c =>
                c.EntityId == current.ActorId ? CombatCommandCalculator.Hurt(c, c.Health.CurrentHealth) : c).ToArray());
            var ally = afterDeath.Characters.Single(c => c.EntityId == new EntityId(20));
            var afterRemoval = TraitCombatFixtures.Characters(afterDeath,
                afterDeath.Characters.Where(c => c.EntityId != current.ActorId).ToArray());

            Assert.That(afterDeath.Characters.Single(c => c.EntityId == current.ActorId).Health.IsDead, Is.True);
            Assert.That(afterRemoval.Characters.Any(c => c.EntityId == current.ActorId), Is.False);
            Assert.That(trail.SameValues(afterRemoval.State.Environment.Tiles.Single(t => t.Id == trail.Id)), Is.True);
            Assert.That(EnvironmentTerrainRules.AppointRange(afterDeath, ally), Is.EqualTo(ally.Stats.AppointRange));
            Assert.That(EnvironmentTerrainRules.AppointRange(afterRemoval, ally), Is.EqualTo(ally.Stats.AppointRange));
        }

        [TestCase(Owner.Enemy, true, true)]
        [TestCase(Owner.Player, true, false)]
        [TestCase(Owner.Enemy, false, false)]
        public void LegalAttackPreviewAndAiAgreeOnExemption(Owner creator, bool upgraded, bool canAttackFromDust)
        {
            var start = new CellCoord(1, 1);
            var dustCell = new CellCoord(2, 1);
            var clearCell = new CellCoord(1, 2);
            var target = new CellCoord(2, 2);
            var input = EnemyMovementPathTests.Input(actorCell: start, targetCell: target);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.With(stats: c.Stats.With(moveRange: 1, appointRange: 1))
                : c.With(stats: c.Stats.With(appointRange: 0))).ToArray(), profiles: input.Profiles.Select(p =>
                p.Definition.Key == EntityKey.Unit_Demon
                    ? new CombatAttackProfile(p.Definition,
                        new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0), p.OwnedId)
                    : p).ToArray());
            input = TraitTrailTestData.Environment(input, Dust(10, dustCell, creator, upgraded));

            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var context = new EnemyDecisionContext(input, new EnemyDecisionSettings(moveCandidateLimit: 1, refinedMoveLimit: 1));
            using var work = new EnemyActionCandidates(context, actor, EnemyActionCandidateTests.MakeForecast(input));
            while (!work.IsComplete) work.Step();
            Assert.That(work.Attacks, Is.Not.Empty);
            Assert.That(work.Moves.Single().Path.Endpoint, Is.EqualTo(canAttackFromDust ? dustCell : clearCell));

            var moved = actor.WithPlacement(CharacterRuntimeArea.Field, null, dustCell, Rotation4.R0, new[] { dustCell });
            var atEndpoint = CombatCommandTestData.With(input, characters: input.Characters.Select(c =>
                c.EntityId == actor.EntityId ? moved : c).ToArray());
            var aim = AttackAim.ForCell(target);
            var aimed = CombatCommandComposition.Intent(atEndpoint, CombatCommandKind.Attack, aim);
            Assert.That(CombatCommandCalculator.GetAttackRange(aimed).AttackAims.ToArray(), canAttackFromDust ? Has.Member(aim) : Has.No.Member(aim));
            Assert.That(CombatCommandCalculator.Preview(aimed, Array.Empty<IMovementTileRule>()).CanAttack, Is.EqualTo(canAttackFromDust));
            Assert.That(CombatCommandCalculator.Calculate(aimed, Array.Empty<IMovementTileRule>()).Accepted, Is.EqualTo(canAttackFromDust));
        }

        [Test]
        public void ExemptRangeDoesNotCreateUndoConsumption()
        {
            var move = MoveAcrossAlly();
            var current = MovementUndoTestData.After(move);
            var request = MovementUndoTestData.Command(current, CombatCommandKind.Attack,
                actor: new EntityId(20), aim: AttackAim.ForCell(new CellCoord(3, 2)));
            var attack = MovementUndoTestData.Run(request);

            Assert.That(attack.Accepted, Is.True, attack.Reason);
            Assert.That(attack.MovementEffectUses, Is.Empty);
            Assert.That(attack.FinalState.MovementUndo.TryGet(move.Input.ActorId, out _), Is.True);
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(attack),
                CombatCommandKind.UndoMove, move.Input.Number, actor: move.Input.ActorId));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            var removed = undo.FinalState.Environment.Tiles.OfType<TraitTrailFact>().Single();
            Assert.That(removed.Active, Is.False);
            Assert.That(removed.ExemptsAlliedAttackRangePenalty, Is.True);
        }

        [Test]
        public void DustDefenseStillBlocksUnsafeUndo()
        {
            var move = MoveAcrossAlly();
            var current = MovementUndoTestData.After(move);
            var enemy = current.Characters.Single(c => c.EntityId == new EntityId(2));
            var ally = current.Characters.Single(c => c.EntityId == new EntityId(20));
            var attack = CombatAttackCalculator.Reaction(current, Array.Empty<IMovementTileRule>(), enemy, AttackAim.ForCell(ally.Anchor.Value));
            var committed = MovementUndoPolicy.Reconcile(attack);

            Assert.That(committed.Accepted, Is.True, committed.Reason);
            var trail = current.State.Environment.Tiles.OfType<TraitTrailFact>().Single();
            Assert.That(trail.ExemptsAlliedAttackRangePenalty, Is.True);
            var use = committed.MovementEffectUses.Single(u => u.Kind == MovementEffectUseKind.DustDefense);
            Assert.That(use.SourceId, Is.EqualTo(trail.Id));
            Assert.That(TraitCombatFixtures.Final(committed, 20).Health.CurrentHealth, Is.LessThan(ally.Health.CurrentHealth));
            Assert.That(committed.FinalState.MovementUndo.TryGet(move.Input.ActorId, out _), Is.False);
        }

        private static CombatCommandTransition MoveAcrossAlly()
        {
            var input = MovementEffectUsageTests.ProducerAcrossAlly(new DustScreenTraitSpec(1, 20, 1, true));
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 2), Owner.Enemy)).ToArray());
            return MovementUndoTestData.Run(input);
        }

        private static TraitTrailFact Dust(long id, CellCoord cell, Owner side, bool upgraded = true, int source = 99, int range = 1) =>
            new(id, cell, new EffectOriginKey(EffectOwnerKind.Trait, source, (int)TraitKey.DustScreen, "main"),
                side, 0, range, 20, 2, true, BothSides, exemptsAlliedAttackRangePenalty: upgraded);
    }
}
