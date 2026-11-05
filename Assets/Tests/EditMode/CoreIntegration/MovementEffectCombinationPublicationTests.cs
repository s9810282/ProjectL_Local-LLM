using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class MovementEffectCombinationPublicationTests
    {
        [TestCase("None")]
        [TestCase("Inventory")]
        [TestCase("Environment")]
        [TestCase("Passenger")]
        public void MixedEffectsAndPair_PublishAtomically_OrPreserveAllStores(string changed)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HuntersMarkBattleSource(
                new PositionSwapTraitSpec(), new ReconSquadTraitSpec(10, 2), new AccelerationTraitSpec(1),
                new GroundCompactionTraitSpec(1, 1), new DustScreenTraitSpec(1, 20, 1), new StationaryFireTraitSpec(20),
                new CohesionTraitSpec(2, 5, 1, 2)));
            var actorBefore = battle.Actor;
            var passengerBefore = battle.Character(EntityKey.Unit_DynamiteHammer);
            var oldEnemy = battle.Character(EntityKey.Unit_Demon);
            battle.Submit(battle.PreviewMove(battle.Actor, new CellCoord(0, 1), new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(2, 0)));
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.MovementUndo.Checkpoints.Single().Participants, Has.Count.EqualTo(2));
            var proposal = MovementEffectPublicationTests.Undo(battle);
            Assert.That(proposal.Accepted, Is.True, proposal.Reason);
            Assert.That(proposal.UndoMovement.RemovedEffects.Any(e => e is MovementExposureReceipt), Is.True);
            if (changed == "Inventory")
            {
                var input = proposal.Input;
                var counts = input.ItemInventory.Counts.ToDictionary(p => p.Key, p => p.Value);
                counts[1] = input.ItemInventory.GetCount(1) + 1;
                proposal = CombatCommandCalculator.Calculate(input.WithPersistentFacts(input.Owned, new ItemInventorySnapshot(counts),
                    input.GoldBalance, input.FameBalance), Array.Empty<IMovementTileRule>());
            }
            else if (changed == "Environment")
            {
                var input = proposal.Input;
                var progress = input.State.EnvironmentProgress;
                var state = input.State.With(environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal,
                    progress.NextSourceId, progress.RandomState ^ 1u));
                proposal = CombatCommandCalculator.Calculate(input.WithWorld(state, input.Characters, input.Main, input.SoulCounts,
                    input.Souls, input.EnvironmentBindings), Array.Empty<IMovementTileRule>());
            }
            else if (changed == "Passenger")
            {
                var passenger = battle.Character(EntityKey.Unit_DynamiteHammer);
                battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(passenger,
                    passenger.WithHealth(new HealthStateSnapshot(passenger.Health.CurrentHealth - 1, passenger.Health.MaxHealth))) }, Array.Empty<SoulCountChange>()));
            }
            Assert.That(proposal.Accepted, Is.True, proposal.Reason);
            var before = battle.CaptureStores();
            if (changed != "None")
            {
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(proposal));
                Assert.That(battle.CaptureStores(), Is.EqualTo(before));
                return;
            }
            var allocator = battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.NextSourceId;
            battle.Buffers.BattleWriteSink.Apply(proposal);
            Assert.That(battle.Actor.SameValues(actorBefore), Is.True);
            Assert.That(battle.Character(EntityKey.Unit_DynamiteHammer).SameValues(passengerBefore), Is.True);
            Assert.That(battle.Character(EntityKey.Unit_Demon).SameValues(oldEnemy), Is.True);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.EnvironmentProgress.NextSourceId, Is.EqualTo(allocator));
            var once = battle.CaptureStores();
            battle.Buffers.BattleWriteSink.Apply(proposal);
            Assert.That(battle.CaptureStores(), Is.EqualTo(once));
        }
    }
}
