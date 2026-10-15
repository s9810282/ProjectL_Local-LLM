using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Framework
{
    public sealed class WallAttackPublicationTests
    {
        [Test]
        public void RejectedWallAttack_PublishesEvidenceWithoutStateChange()
        {
            var target = new CellCoord(3, 1);
            var b = CombatPreviewPublicationTests.Create(out var service, Array.Empty<IMovementTileRule>(),
                new[] { new CellCoord(2, 1) }, target);
            var actor = b.CharacterReadSink.Characters.Single().ToFacts();
            var state = b.CombatStateReadSink.Current;
            var souls = b.CombatStateReadSink.SoulField;
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.AttackRange(actor.EntityId));
            Assert.That(service.ProcessLatestPreview(), Is.True);
            b.CombatStateReadSink.TryGetLatestPreview(out var rangeResponse);
            var range = rangeResponse.Range.Value;
            Assert.That(range.RangeCells.ToArray(), Has.Member(target));
            Assert.That(range.InRangeTargetIds.ToArray(), Has.Member(new EntityId(2)));
            Assert.That(range.AttackAims.ToArray(), Has.No.Member(AttackAim.ForCell(target)));
            Assert.That(range.BlockedContacts.All(c => c.Phase == AttackOcclusionPhase.Direct), Is.True);
            var rangeContact = range.BlockedContacts.Single(c => c.Contact == target);
            b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(actor.EntityId, AttackAim.ForCell(target)));
            service.ProcessLatestPreview();
            b.CombatStateReadSink.TryGetLatestPreview(out var response);
            Assert.That(response.Attack.Value.FailureReason, Is.EqualTo(AttackFailureReason.BlockedByWall));
            SameContact(rangeContact, response.Attack.Value.BlockedContacts.Single());
            b.CombatViewWriteSink.Submit(CombatViewCommandData.Attack(actor.EntityId, AttackAim.ForCell(target)));
            Assert.That(service.ProcessNextCommand(), Is.True);
            b.CombatResultReadSink.TryGetLatest(out var published);
            Assert.That(published.Accepted, Is.False);
            Assert.That(published.Reason, Is.EqualTo(AttackFailureReason.BlockedByWall.ToString()));
            Assert.That(published.Events, Is.Empty);
            SameContact(rangeContact, published.BlockedContacts.Single());
            Assert.That(b.CombatStateReadSink.Current.CommandState.SameValues(state.CommandState), Is.True);
            Assert.That(b.CombatStateReadSink.SoulField.SameValues(souls), Is.True);
            var after = b.CharacterReadSink.Characters.Single().ToFacts();
            Assert.That(after.Health, Is.EqualTo(actor.Health));
            Assert.That(after.Stats.CurrentActionCost, Is.EqualTo(actor.Stats.CurrentActionCost));
            Assert.That(after.Anchor, Is.EqualTo(actor.Anchor));
            Assert.That(after.PendingMoveActionCost, Is.EqualTo(actor.PendingMoveActionCost));
        }

        [Test]
        public void AcceptedSplash_PreviewAttackAndBufferKeepPropagationEvidence()
        {
            var target = new CellCoord(3, 1);
            var b = CombatPreviewPublicationTests.Create(out _, Array.Empty<IMovementTileRule>(), new[] { new CellCoord(4, 1) }, target);
            var source = CombatCommandPublicationTests.Input(b);
            var profiles = CharacterBattlePreparationTests.Initialization().Units.Select(u => new CombatAttackProfile(u,
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 70))).ToArray();
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, AttackAim.ForCell(target), source.State,
                source.Characters, profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                environmentBindings: source.EnvironmentBindings);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var contact = preview.BlockedContacts.Single();
            Assert.That(contact.Phase, Is.EqualTo(AttackOcclusionPhase.Propagation));
            Assert.That(contact.Origin, Is.EqualTo(target));
            Assert.That(contact.Contact, Is.EqualTo(new CellCoord(4, 1)));
            SameContact(contact, transition.BlockedContacts.Single());
            SameContact(contact, transition.Attack.Value.BlockedContacts.Single());
            b.BattleWriteSink.Apply(transition);
            b.CombatResultReadSink.TryGetLatest(out var published);
            SameContact(contact, published.BlockedContacts.Single());
            SameContact(contact, published.Attack.Value.BlockedContacts.Single());
        }

        [Test]
        public void CopiesAndCompletion_PreserveDiagnostics()
        {
            var b = CombatPreviewPublicationTests.Create(out _, Array.Empty<IMovementTileRule>());
            var input = CombatCommandPublicationTests.Input(b);
            var contact = new AttackBlockedContact(new CellCoord(1, 1), new CellCoord(4, 1),
                AttackOcclusionPhase.Direct, new long[] { 71 }, new[] { new CellCoord(2, 1) });
            var source = new[] { contact };
            var attack = AttackResult.FromFacts(input.ActorId, EntityId.Empty, Rotation4.R0,
                new ActionCostSettlementSnapshot(4, 2, 0, 2), new[] { contact.Contact }, Array.Empty<AttackHitResult>(),
                new AttackCellSelection(contact.Contact, new[] { contact.Contact }, Array.Empty<CellCoord>()), blockedContacts: source);
            var preview = AttackPreview.FromFacts(attack, Array.Empty<AttackTargetPreview>(), Array.Empty<TraitStatusSnapshot>(),
                input.Characters.Single().Stats.Category);
            var failure = AttackPreview.Failure(AttackFailureReason.BlockedByWall, input.ActorId, EntityId.Empty, source);
            var outcome = new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.EnemyHeadquartersDestroyed);
            var state = new CombatCommandState(8, 6, CombatFlowPhase.Finished, EntityId.Empty, false,
                input.State.Structures, input.State.ThornFields, input.State.Timeline);
            var transition = new CombatCommandTransition(input, true, "", new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(input.Souls, input.Souls), state, outcome, attack, blockedContacts: source);
            var completed = transition.WithCompletion(new BattleCompletionChanges(transition.Characters, outcome,
                ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(10), true)));
            var published = new CombatResult(1, CombatProcessingKind.Command, 1, false, "BlockedByWall", blockedContacts: source);
            var range = AttackRangeResult.Success(input.ActorId, new[] { contact.Contact }, Array.Empty<EntityId>(), true,
                Array.Empty<AttackAim>(), source);
            var cancelled = AttackResult.FromFacts(input.ActorId, EntityId.Empty, Rotation4.R0,
                new ActionCostSettlementSnapshot(4, 0, 0, 4), Array.Empty<CellCoord>(), Array.Empty<AttackHitResult>(),
                new AttackCellSelection(null, Array.Empty<CellCoord>(), Array.Empty<CellCoord>()),
                AttackFailureReason.AttackerDiedOnMovementSettlement, blockedContacts: source);
            var cancelledPreview = AttackPreview.FromFacts(cancelled, Array.Empty<AttackTargetPreview>(), Array.Empty<TraitStatusSnapshot>(),
                input.Characters.Single().Stats.Category);
            source[0] = null;
            foreach (var contacts in new[] { attack.BlockedContacts, preview.BlockedContacts, failure.BlockedContacts,
                         transition.BlockedContacts, completed.BlockedContacts, published.BlockedContacts,
                         range.BlockedContacts, cancelled.BlockedContacts, cancelledPreview.BlockedContacts })
            {
                Assert.That(contacts.Single(), Is.SameAs(contact));
                Assert.Throws<NotSupportedException>(() => ((IList<AttackBlockedContact>)contacts)[0] = null);
            }
            Assert.That(default(AttackResult).BlockedContacts, Is.Empty);
            Assert.That(default(AttackPreview).BlockedContacts, Is.Empty);
            Assert.That(default(AttackRangeResult).BlockedContacts, Is.Empty);
            Assert.Throws<ArgumentException>(() => AttackPreview.Failure(AttackFailureReason.BlockedByWall, input.ActorId, EntityId.Empty, source));
            Assert.Throws<ArgumentException>(() => new CombatResult(1, CombatProcessingKind.Command, 1, false, "BlockedByWall", blockedContacts: source));
        }

        private static void SameContact(AttackBlockedContact a, AttackBlockedContact b)
        {
            Assert.That(b.Origin, Is.EqualTo(a.Origin));
            Assert.That(b.Contact, Is.EqualTo(a.Contact));
            Assert.That(b.Phase, Is.EqualTo(a.Phase));
            Assert.That(b.WallSourceIds, Is.EqualTo(a.WallSourceIds));
            Assert.That(b.WallCells, Is.EqualTo(a.WallCells));
        }
    }
}
