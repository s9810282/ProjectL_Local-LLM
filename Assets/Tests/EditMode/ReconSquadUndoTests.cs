using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class ReconSquadUndoTests
    {
        [TestCase(10)]
        [TestCase(15)]
        [TestCase(20)]
        public void Undo_PreservesEarlierAndOtherSourceGrants_AndUnrelatedHealth(int percent)
        {
            var input = ReconSquadMovementTests.Route(percent);
            var earlier = new WeaknessExposureStatus(new EffectOriginKey(EffectOwnerKind.Trait, 90, (int)TraitKey.ReconSquad, "main"),
                20, WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath);
            var hunters = new WeaknessExposureStatus(new EffectOriginKey(EffectOwnerKind.Trait, 91, (int)TraitKey.HuntersMark, "main"), 30, 2);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { earlier, hunters })) : c).ToArray());
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.FinalState.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
            var current = MovementUndoTestData.After(move);
            var target = current.Characters.Single(c => c.EntityId.Value == 2);
            var changed = target.WithHealth(new HealthStateSnapshot(99, 100));
            current = CombatCommandTestData.With(current, characters: current.Characters.Select(c => c.EntityId == target.EntityId ? changed : c).ToArray(),
                state: current.State.With(movementUndo: current.State.MovementUndo.WithoutAffected(
                    CombatChangeEvidenceReader.ReadCharacterChanges(new[] { target }, new[] { changed }))));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Command(current, CombatCommandKind.UndoMove, move.Input.Number));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            var final = TraitCombatFixtures.Final(undo, 2);
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(99));
            Assert.That(final.StatusEffects.SameValues(new StatusEffectSet(new AppliedStatus[] { earlier, hunters })), Is.True);
            Assert.That(undo.UndoMovement.RemovedEffects.OfType<MovementExposureReceipt>().Count(), Is.EqualTo(3));
            Assert.That(undo.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Any(t => t.TraitKey == TraitKey.ReconSquad && t.TraitActivationKind == TraitActivationKind.StatusApplied), Is.False);
        }

        [Test]
        public void ShieldOnlyReaction_UsesActualRecipientAndCommits_WhilePreviewDoesNot()
        {
            var input = ReconSquadMovementTests.Route();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? WeaknessExposureDamageTests.Shield(c, 1000) : c.EntityId.Value == 10
                    ? c.WithPlacement(c.Area, null, new CellCoord(0, 2), c.Rotation, new[] { new CellCoord(0, 2) }) : c).ToArray());
            var move = MovementUndoTestData.Run(input);
            Assert.That(move.FinalState.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
            var current = MovementUndoTestData.After(move);
            var attacker = current.Characters.Single(c => c.OwnershipRole == ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.CharacterOwnershipRole.Main);
            var request = MovementUndoTestData.Command(current, CombatCommandKind.Attack, actor: attacker.EntityId, aim: AttackAim.ForCell(new CellCoord(1, 2)));
            CombatCommandCalculator.Preview(request, Array.Empty<IMovementTileRule>());
            Assert.That(current.State.MovementUndo.Checkpoints, Has.Count.EqualTo(1));
            var attack = CombatAttackCalculator.Reaction(request, Array.Empty<IMovementTileRule>(), attacker, request.Aim);
            Assert.That(attack, Is.Not.Null);
            var result = MovementUndoPolicy.Reconcile(attack);
            Assert.That(result.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.ExposureDamage && u.ConsumerId.Value == 2), Is.True);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(10)]
        [TestCase(15)]
        [TestCase(20)]
        public void WaitingAndReentry_ClearMovementExposureWithoutASecondGrant(int percent)
        {
            var move = MovementUndoTestData.Run(ReconSquadMovementTests.Route(percent));
            var before = TraitCombatFixtures.Final(move, 2);
            Assert.That(ReconSquadMovementTests.Grants(before).Single().IncreasePercent, Is.EqualTo(percent));
            var waiting = CombatPlacementCalculator.Location(before, CharacterRuntimeArea.EnemyWaiting, null, null, null);
            Assert.That(ReconSquadMovementTests.Grants(waiting), Is.Empty);
            var redeployed = CombatPlacementCalculator.Location(waiting, CharacterRuntimeArea.Field, null, before.Anchor, before.Rotation);
            Assert.That(ReconSquadMovementTests.Grants(redeployed), Is.Empty);
        }

        [Test]
        public void ClearedGrant_IsNotResurrectedByUndo()
        {
            var move = MovementUndoTestData.Run(ReconSquadMovementTests.Route());
            var input = MovementUndoTestData.After(move);
            var before = input.Characters.Single(c => c.EntityId.Value == 2);
            var after = before.WithStatusEffects(StatusEffectSet.Empty);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == before.EntityId ? after : c).ToArray(),
                state: input.State.With(movementUndo: input.State.MovementUndo.WithoutAffected(
                    CombatChangeEvidenceReader.ReadCharacterChanges(new[] { before }, new[] { after }))));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Command(input, CombatCommandKind.UndoMove, move.Input.Number));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(TraitCombatFixtures.Final(undo, 2).StatusEffects.Entries, Is.Empty);
            Assert.That(undo.UndoMovement.RemovedEffects.OfType<MovementExposureReceipt>().Count(), Is.EqualTo(2));
        }

        [TestCase(CombatDamageKind.OrdinaryAttack)]
        [TestCase(CombatDamageKind.ExtraAttack)]
        [TestCase(CombatDamageKind.ReactionAttack)]
        [TestCase(CombatDamageKind.EnvironmentDamage)]
        [TestCase(CombatDamageKind.ReflectedDamage)]
        [TestCase(CombatDamageKind.DamageOverTime)]
        public void NumericBoundary_CommitsEvenWhenRoundingAndShieldHideDifference(CombatDamageKind kind)
        {
            var input = ReconSquadMovementTests.Route();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? WeaknessExposureDamageTests.Shield(c, 1000) : c).ToArray());
            var move = MovementUndoTestData.Run(input);
            var result = Receive(MovementUndoTestData.After(move), kind, 1);
            Assert.That(result.FinalState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(result.MovementEffectUses.Count(u => u.Kind == MovementEffectUseKind.ExposureDamage), Is.EqualTo(1));
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void UsedTiedSources_CommitButSuppressedSourceDoesNot()
        {
            var input = ReconSquadMovementTests.Route();
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] {
                CombatCommandTestData.Character(20, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player),
                CombatCommandTestData.Character(30, EntityKey.Unit_Shielder, new CellCoord(0, 4), Owner.Player)
            }).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Append(new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Shielder, 1), input.Profiles[0].Method)).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, ReconSquadMovementTests.Spec());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Shielder, ReconSquadMovementTests.Spec(5, 2));
            var first = MovementUndoTestData.Run(input);
            var second = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(first), CombatCommandKind.Move,
                actor: new EntityId(20), path: new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3) })));
            var third = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(second), CombatCommandKind.Move,
                actor: new EntityId(30), path: new MovementPath(new[] { new CellCoord(0, 4), new CellCoord(1, 4), new CellCoord(2, 4) })));
            Assert.That(third.FinalState.MovementUndo.Checkpoints, Has.Count.EqualTo(3));
            var damaged = Receive(MovementUndoTestData.After(third), CombatDamageKind.EnvironmentDamage, 10);
            Assert.That(damaged.MovementEffectUses.Where(u => u.Kind == MovementEffectUseKind.ExposureDamage)
                .Select(u => u.Movement.Value.ActorId.Value), Is.EquivalentTo(new[] { 1, 20 }));
            Assert.That(damaged.FinalState.MovementUndo.Checkpoints.Select(c => c.ActorId.Value), Is.EqualTo(new[] { 30 }));
            var undo = MovementUndoTestData.Run(MovementUndoTestData.Command(MovementUndoTestData.After(damaged), CombatCommandKind.UndoMove,
                third.Input.Number, actor: new EntityId(30)));
            Assert.That(undo.Accepted, Is.True, undo.Reason);
            Assert.That(undo.FinalState.MovementUndo.Checkpoints, Is.Empty);
            Assert.That(TraitCombatFixtures.Final(undo, 2).Health.CurrentHealth, Is.EqualTo(89));
        }

        [Test]
        public void ZeroInputAndInstantDefeat_DoNotFabricateMultiplierUse()
        {
            var move = MovementUndoTestData.Run(ReconSquadMovementTests.Route());
            var input = MovementUndoTestData.After(move);
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var damage = CombatDamageApplication.For(input, characters, new List<CombatEvent>());
            var origin = new CombatDamageOrigin(new CellCoord(1, 2), null, 1);
            damage.Apply(new EntityId(2), CombatDamageKind.EnvironmentDamage, origin, 0);
            Assert.That(damage.MovementEffectUses, Is.Empty);
            damage.Defeat(new EntityId(2), origin);
            Assert.That(damage.MovementEffectUses, Is.Empty);
        }

        [Test]
        public void SacrificeUsesActualRecipient_AndDiscardedPreviewInstallsNoUsage()
        {
            var input = SacrificeTraitTests.Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2 || c.EntityId.Value == 5
                ? c.WithStatusEffects(new StatusEffectSet(c.StatusEffects.Entries.Append(new WeaknessExposureStatus(
                    new EffectOriginKey(EffectOwnerKind.Trait, 90, (int)TraitKey.ReconSquad, "main"), 10,
                    WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath, movement: new MovementEffectId(1, 1, new EntityId(90), 1))).ToArray())) : c).ToArray());
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var projected);
            Assert.That(preview.CanAttack, Is.True);
            Assert.That(projected.MovementEffectUses, Is.Empty);
            var attack = MovementUndoTestData.Run(input);
            var uses = attack.MovementEffectUses.Where(u => u.Kind == MovementEffectUseKind.ExposureDamage).ToArray();
            Assert.That(uses.Select(u => u.ConsumerId.Value), Is.EqualTo(new[] { 5 }));
        }

        [TestCase(CombatDamageKind.OrdinaryAttack, 10)]
        [TestCase(CombatDamageKind.ExtraAttack, 10)]
        [TestCase(CombatDamageKind.ReactionAttack, 10)]
        [TestCase(CombatDamageKind.EnvironmentDamage, 10)]
        [TestCase(CombatDamageKind.OrdinaryAttack, 0)]
        [TestCase(CombatDamageKind.ExtraAttack, 0)]
        [TestCase(CombatDamageKind.EnvironmentDamage, 0)]
        public void BadgeReceivingBoundary_CommitsPositiveInputButZeroInputOnlyConsumesBadge(CombatDamageKind kind, int amount)
        {
            var input = ReconSquadMovementTests.Route();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? HeldEnvironmentEffects.AcquireBadge(c, 91) : c).ToArray());
            var move = MovementUndoTestData.Run(input);
            var result = Receive(MovementUndoTestData.After(move), kind, amount, held: true);
            Assert.That(TraitCombatFixtures.Final(result, 2).HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(result.MovementEffectUses.Count(u => u.Kind == MovementEffectUseKind.ExposureDamage), Is.EqualTo(amount > 0 ? 1 : 0));
            Assert.That(result.FinalState.MovementUndo.Checkpoints.Count, Is.EqualTo(amount > 0 ? 0 : 1));
            Assert.That(result.HealthExecutions, Is.Not.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PullDamage_UsesActualReachedRecipientFacts(bool intermediateHazard)
        {
            var input = PullTestData.Input(extra: 2, distance: 3);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == PullTestData.Target
                ? c.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { new WeaknessExposureStatus(
                    new EffectOriginKey(EffectOwnerKind.Trait, 90, (int)TraitKey.ReconSquad, "main"), 10,
                    WeaknessExposureLifetimeKind.UntilReserveReturnOrDeath, movement: new MovementEffectId(1, 1, new EntityId(90), 1)) })) : c).ToArray());
            if (intermediateHazard) input = CombatCommandTestData.With(input, state: EnvironmentDamageTests.WithDamageObjects(input.State, (new CellCoord(9, 7), 7)));
            var result = PullTestData.Run(input, EnvironmentDamageTests.DamageRules);
            Assert.That(PullTestData.Final(result).Anchor, Is.EqualTo(new CellCoord(8, 7)));
            Assert.That(PullTestData.Final(result).Health.CurrentHealth, Is.EqualTo(intermediateHazard ? 71 : 78));
            Assert.That(result.MovementEffectUses.Where(u => u.Kind == MovementEffectUseKind.ExposureDamage)
                .Select(u => u.ConsumerBefore.Anchor.Value), Is.EqualTo(intermediateHazard
                    ? new[] { new CellCoord(9, 7), new CellCoord(8, 7) } : new[] { new CellCoord(8, 7) }));
        }

        private static CombatCommandTransition Receive(CombatCommandInput input, CombatDamageKind kind, int amount, bool held = false)
        {
            var current = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var damage = CombatDamageApplication.For(input, current, events);
            damage.Apply(new EntityId(2), kind, new CombatDamageOrigin(new CellCoord(1, 2), null, 1), amount,
                heldDefenseGroups: held ? new CommandAttackGroups() : null);
            return MovementUndoPolicy.Reconcile(new CombatCommandTransition(input, true, "", new CharacterChanges(input.Characters
                .Where(c => !c.SameValues(current[c.EntityId])).Select(c => new CharacterStateChange(c, current[c.EntityId])).ToArray(),
                Array.Empty<SoulCountChange>()), new SoulFieldChange(input.Souls, input.Souls), input.State,
                new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None), events: events, movementEffectUses: damage.MovementEffectUses, healthExecutions: damage.HealthExecutions));
        }
    }
}
