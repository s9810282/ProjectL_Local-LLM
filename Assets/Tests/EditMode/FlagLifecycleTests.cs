using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class FlagLifecycleTests
    {
        internal static readonly IMovementTileRule[] Rules = {
            new SheriffBadgeMovementTileRule(), new MineMovementTileRule(), new FlagMovementTileRule() };
        internal static FlagSourceFact Flag(long id = 10, long ordinal = 1, int bonus = 7, CellCoord? cell = null) =>
            new(id, cell ?? MineEnvironmentTests.Center, ordinal, bonus, true, new[] { Owner.Player, Owner.Enemy });

        [Test]
        public void Arrival_LeavesFlagActiveWithoutPriorityOrControllerChange()
        {
            var input = MineEnvironmentTests.Input(Flag());
            var before = input.Characters.Single(c => c.EntityId == input.ActorId);
            var preview = CombatMovementCalculator.Preview(input, Rules);
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(preview.PublicEffects.OfType<CombatHeldEnvironmentEvent>(), Is.Empty);
            var after = MovementTestData.Actor(result);
            Assert.That(after.HeldEnvironment.Flag, Is.Null);
            Assert.That(CombatEffectiveStatsCalculator.Priority(after), Is.EqualTo(before.Stats.Priority));
            Assert.That(result.FinalState.Environment.SameValues(input.State.Environment), Is.True);
            Assert.That(result.FinalState.EnvironmentProgress.SameValues(input.State.EnvironmentProgress), Is.True);
            Assert.That(result.FinalState.Turn.SameValues(input.State.Turn), Is.True);
            Assert.That(result.Events.OfType<CombatObjectCreatedEvent>(), Is.Empty);
        }

        [Test]
        public void FlagBeforeLethalMine_DoesNotPreventDamageOrCreateDeathDrop()
        {
            var input = MineEnvironmentTests.Input(Flag(), MineEnvironmentTests.Mine(11, 2, 100));
            var result = CombatCommandCalculator.Calculate(input, Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.IsDead, Is.True);
            Assert.That(result.FinalState.Environment.Tiles.Single(t => t.Id == 10).Active, Is.True);
            Assert.That(result.Events.OfType<CombatObjectCreatedEvent>(), Is.Empty);
            Assert.That(result.FinalState.EnvironmentProgress.NextSourceId, Is.EqualTo(input.State.EnvironmentProgress.NextSourceId));
            Assert.That(result.FinalState.EnvironmentProgress.RandomState, Is.EqualTo(input.State.EnvironmentProgress.RandomState));
        }

        [Test]
        public void ExplicitCreation_DoesNotAllocateConsumeRandomOrGrantFlag()
        {
            var input = MineEnvironmentTests.Input(Flag());
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var environment = input.State.Environment;
            var bindings = input.EnvironmentBindings;
            var progress = input.State.EnvironmentProgress;
            var events = new System.Collections.Generic.List<CombatEvent>();
            FlagDropCalculator.CreateAt(HeldFlag.From(Flag()), actor.Anchor.Value, characters,
                ref environment, ref bindings, ref progress, events);
            Assert.That(characters[actor.EntityId], Is.SameAs(actor));
            Assert.That(environment, Is.SameAs(input.State.Environment));
            Assert.That(bindings, Is.SameAs(input.EnvironmentBindings));
            Assert.That(progress, Is.SameAs(input.State.EnvironmentProgress));
            Assert.That(events, Is.Empty);
        }

        [Test]
        public void FlagPickup_DoesNotChangeBadgeOrSupplies()
        {
            var actor = CombatCommandTestData.Input().Characters[0];
            var held = HeldEnvironmentEffects.GrantSupplies(HeldEnvironmentEffects.AcquireBadge(actor, 31), 4);
            var acquired = HeldEnvironmentEffects.AcquireFlag(held, Flag());
            Assert.That(acquired, Is.SameAs(held));
            Assert.That(acquired.HeldEnvironment.Flag, Is.Null);
            Assert.That(acquired.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(acquired.HeldEnvironment.SuppliesAttackBonus, Is.EqualTo(4));
            Assert.That(CombatEffectiveStatsCalculator.Priority(acquired), Is.EqualTo(actor.Stats.Priority));
            var expired = HeldEnvironmentEffects.ExpireSupplies(acquired);
            Assert.That(expired.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(expired.HeldEnvironment.SuppliesAttackBonus, Is.Zero);
        }

        [Test]
        public void DropCandidates_UseStraightLineDistance_AvoidUnitsAndStructures()
        {
            var i = MineEnvironmentTests.Input(Flag());
            var occupied = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(2, 2), Owner.Enemy);
            var nearest = FlagDropCalculator.NearestCells(i.State, new[] { occupied }, i.State.Structures, new CellCoord(2, 2), i.EnvironmentBindings);
            Assert.That(nearest, Is.EquivalentTo(new[] { new CellCoord(1, 2), new CellCoord(3, 2), new CellCoord(2, 1), new CellCoord(2, 3) }));
            Assert.That(nearest, Has.No.Member(new CellCoord(3, 3)));
        }

        [Test]
        public void RandomTieSequence_IsDeterministic_AndSingleCandidateDoesNotNeedRandom()
        {
            uint state = 1;
            Assert.That(EnvironmentRandom.NextIndex(ref state, 4), Is.EqualTo(0));
            Assert.That(state, Is.EqualTo(1015568748u));
            Assert.That(EnvironmentRandom.NextIndex(ref state, 4), Is.EqualTo(3));
            Assert.That(EnvironmentRandom.NextIndex(ref state, 4), Is.EqualTo(2));
            var i = MineEnvironmentTests.Input(Flag());
            Assert.That(FlagDropCalculator.NearestCells(i.State, Array.Empty<CharacterCombatFacts>(),
                i.State.Structures, new CellCoord(-1, -1), i.EnvironmentBindings), Is.EqualTo(new[] { CellCoord.Zero }));
        }
    }
}
