using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerSkillEvidenceTests
    {
        [Test]
        public void SkillOccurrence_OwnsActualRecipientHealthAndSurvivesTransitionCopies()
        {
            var transition = PlayerSkillEvidenceTestData.Calculate();
            var occurrence = transition.Events.OfType<CombatPlayerSkillAttackEvent>().Single();
            Assert.That(occurrence.Attack.Hits.Single().DamageDealt, Is.EqualTo(20));
            Assert.That(transition.HealthClaims.Single(c => c.Kind == CombatHealthClaimKind.AttackHit).Event, Is.SameAs(occurrence));
            Assert.DoesNotThrow(() => CombatHealthEvidenceValidator.Validate(transition));
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(transition));
            Assert.That(transition.WithFinalState(transition.FinalState).PlayerSkill, Is.SameAs(transition.PlayerSkill));
            Assert.That(CombatCommandComposition.WithEvents(transition, transition.Events).PlayerSkill, Is.SameAs(transition.PlayerSkill));
            Assert.That(CombatCommandComposition.Merge(transition.Input, new[] { transition }).PlayerSkill, Is.SameAs(transition.PlayerSkill));
            Assert.That(transition.WithMovementEffectUses(transition.MovementEffectUses).PlayerSkill, Is.SameAs(transition.PlayerSkill));
        }

        [Test]
        public void HealthClaim_RejectsAnotherOccurrenceEvenWithEqualPayload()
        {
            var transition = PlayerSkillEvidenceTestData.Calculate();
            var claim = transition.HealthClaims.Single(c => c.Kind == CombatHealthClaimKind.AttackHit);
            var another = new CombatPlayerSkillAttackEvent(((CombatPlayerSkillAttackEvent)claim.Event).Attack);
            Assert.Throws<ArgumentException>(() => PlayerSkillEvidenceTestData.Claim(another,
                claim.HitIndex, claim.RecipientId, claim.Steps, claim.Scope));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HealthEvidence_RejectsDuplicateClaimOrApplication(bool duplicateExecution)
        {
            var transition = PlayerSkillEvidenceTestData.Calculate();
            var claim = transition.HealthClaims.Single(c => c.Kind == CombatHealthClaimKind.AttackHit);
            Assert.Catch<Exception>(() =>
            {
                var forged = PlayerSkillEvidenceTestData.Copy(transition,
                    claims: duplicateExecution ? transition.HealthClaims : transition.HealthClaims.Append(claim).ToArray(),
                    executions: duplicateExecution ? transition.HealthExecutions.Append(transition.HealthExecutions[0]).ToArray() : transition.HealthExecutions);
                CombatHealthEvidenceValidator.Validate(forged);
            });
        }

        [Test]
        public void SecondPrimaryOccurrence_CannotClaimTheSameHealthStepsAgain()
        {
            var transition = PlayerSkillEvidenceTestData.Calculate();
            var first = transition.Events.OfType<CombatPlayerSkillAttackEvent>().Single();
            var other = new CombatPlayerSkillAttackEvent(new PlayerSkillAttackResult(first.Attack.ActorId, 1, 2,
                first.Attack.AffectedCells, first.Attack.Hits.Select(hit => new AttackHitResult(hit.TargetId,
                    hit.TargetHealthBefore, hit.TargetHealthAfter)).ToArray()));
            var old = transition.HealthClaims.Single(c => c.Kind == CombatHealthClaimKind.AttackHit);
            var scope = PlayerSkillEvidenceTestData.Scope(other, old.Scope.Steps);
            var borrowed = PlayerSkillEvidenceTestData.Claim(other, 0, old.RecipientId, old.Steps, scope);
            Assert.Catch<Exception>(() =>
            {
                var events = transition.Events.Append(other).ToArray();
                var forged = PlayerSkillEvidenceTestData.Copy(transition, events: events,
                    claims: transition.HealthClaims.Append(borrowed).ToArray(),
                    skill: new PlayerSkillResult(transition.Input.State.PlayerSkill.Definition, PlayerSkillUpgradeKey.None,
                        events, transition.PlayerSkill.UseSettlement));
                CombatHealthEvidenceValidator.Validate(forged);
            });
        }

        [Test]
        public void ProjectilePath_DoesNotBecomeCharacterMovementOrActionSettlement()
        {
            var input = PlayerSkillEvidenceTestData.Input();
            var path = new[] { new CellCoord(3, 2), new CellCoord(4, 2), new CellCoord(4, 3) };
            var occurrence = new CombatPlayerSkillAttackEvent(new PlayerSkillAttackResult(input.ActorId, 0, 1,
                Array.Empty<CellCoord>(), Array.Empty<AttackHitResult>(), projectilePath: path));
            var transition = PlayerSkillEvidenceTestData.Empty(input, occurrence);
            var result = MovementUndoPolicy.Reconcile(transition);
            Assert.DoesNotThrow(() => CombatChangeEvidenceReader.ReadAll(result));
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Movement, Is.Null);
            Assert.That(result.PathMovement, Is.Null);
            Assert.That(result.ActionUseSettlement, Is.Null);
            Assert.That(result.Events.OfType<CombatCharacterEvent>(), Is.Empty);
        }

        [Test]
        public void ForcedMovementAndDustDefense_PublishConsistentHealthAndUndoEvidence()
        {
            var input = PlayerSkillEvidenceTestData.Input();
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = input.Profiles.Single(p => p.Definition.Key == actor.CharacterKey);
            input = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, input.State,
                input.Characters.Select(c => c.EntityId == input.ActorId ? c.With(stats: c.Stats.With(traitStatuses:
                    new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) })) : c).ToArray(),
                input.Profiles.Select(p => p == profile ? new CombatAttackProfile(
                    CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Main, new KnockbackTraitSpec(1)), p.Method) : p).ToArray(),
                input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                battleInstanceId: input.BattleInstanceId, membership: input.Membership,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence,
                playerSkillRequest: input.PlayerSkillRequest, playerSkillSource: input.PlayerSkillSource);
            var target = input.Characters.Single(c => c.EntityId.Value == 2);
            var environment = new MovementEnvironmentFacts(new[] { PlayerSkillAttackTestData.Dust(target) });
            input = input.WithWorld(input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment)),
                input.Characters, input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = PlayerSkillEvidenceTestData.Calculate(input);
            var move = transition.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Moved);
            Assert.That(move.Final.EntityId, Is.EqualTo(target.EntityId));
            Assert.That(move.MovementCause.Agency, Is.EqualTo(CombatMovementAgency.Forced));
            Assert.That(transition.Events.OfType<CombatPlayerSkillAttackEvent>().Single().Attack.Knockbacks.Single().Path.Count, Is.EqualTo(1));
            var reconciled = MovementUndoPolicy.Reconcile(transition);
            Assert.That(reconciled.MovementEffectUses.Any(u => u.Kind == MovementEffectUseKind.DustDefense &&
                u.DamageKind == CombatDamageKind.PlayerSkillAttack && u.ConsumerId == target.EntityId), Is.True);
            Assert.DoesNotThrow(() => MovementUndoPolicy.ValidatePublication(reconciled, CombatCommandComposition.Project(reconciled).Characters));
        }

        [Test]
        public void SkillPayload_CannotBorrowAbsentOrReorderedOccurrences()
        {
            var transition = PlayerSkillEvidenceTestData.Empty(PlayerSkillEvidenceTestData.Input());
            var occurrence = transition.Events.Single();
            Assert.Throws<ArgumentException>(() => PlayerSkillEvidenceTestData.Copy(transition,
                events: new[] { new CombatPlayerSkillAttackEvent(((CombatPlayerSkillAttackEvent)occurrence).Attack) }));
        }

        [Test]
        public void KnockbackSummary_RequiresActualForcedMovementSteps()
        {
            var input = PlayerSkillEvidenceTestData.Input();
            var target = input.Characters.Single(c => c.EntityId.Value == 2);
            var movement = new AttackKnockbackSnapshot(target.EntityId, AttackKnockbackTargetKind.Character,
                target.Anchor.Value, CellCoord.Up, 1, new[] { target.Anchor.Value + CellCoord.Up }, AttackKnockbackStop.Completed);
            var occurrence = new CombatPlayerSkillAttackEvent(new PlayerSkillAttackResult(input.ActorId, 0, 1,
                target.OccupiedCells, Array.Empty<AttackHitResult>(), knockbacks: new[] { movement }));
            var forged = PlayerSkillEvidenceTestData.Empty(input, occurrence);
            Assert.Throws<InvalidOperationException>(() => CombatChangeEvidenceReader.ReadAll(forged));
        }

        [TestCase("origin")]
        [TestCase("order")]
        public void PrimarySkillClaim_CannotRelabelOrPrecedeItsActualDamage(string mutation)
        {
            var transition = PlayerSkillEvidenceTestData.Calculate();
            var original = transition.Events.OfType<CombatPlayerSkillAttackEvent>().Single();
            var hit = original.Attack.Hits.Single();
            long group = mutation == "origin" ? 2 : 1;
            var contacts = mutation == "origin" ? hit.Contacts.Select(contact => new AttackHitContactSnapshot(contact.DeliveryIndex,
                contact.OriginalTargetId, contact.Role, contact.AppliedSplashPercent,
                new CombatDamageOrigin(contact.Origin.PhysicalCell, original.Attack.ActorId, group))).ToArray() : hit.Contacts;
            var replacement = new CombatPlayerSkillAttackEvent(new PlayerSkillAttackResult(original.Attack.ActorId, 0, group,
                original.Attack.AffectedCells, new[] { new AttackHitResult(hit.TargetId, hit.TargetHealthBefore, hit.TargetHealthAfter,
                    hit.Role, hit.AppliedSplashPercent, hit.HealthNormalizationLoss, hit.HealthRecovery, contacts) }));
            var events = mutation == "order" ? transition.Events.Where(e => !ReferenceEquals(e, original)).Append(replacement).ToArray()
                : transition.Events.Select(e => ReferenceEquals(e, original) ? replacement : e).ToArray();
            var prior = transition.HealthClaims.Single(c => c.Kind == CombatHealthClaimKind.AttackHit);
            var scope = PlayerSkillEvidenceTestData.Scope(replacement, prior.Scope.Steps);
            var claim = PlayerSkillEvidenceTestData.Claim(replacement, 0, prior.RecipientId, prior.Steps, scope);
            var skill = new PlayerSkillResult(transition.Input.State.PlayerSkill.Definition, PlayerSkillUpgradeKey.None, events,
                transition.PlayerSkill.UseSettlement);
            var forged = PlayerSkillEvidenceTestData.Copy(transition, events: events, claims: new[] { claim }, skill: skill);
            Assert.Throws<InvalidOperationException>(() => CombatHealthEvidenceValidator.Validate(forged));
        }
    }

    internal static class PlayerSkillEvidenceTestData
    {
        internal static CombatCommandInput Input() => PlayerSkillTestData.Input()
            .WithIntent(CombatCommandKind.UsePlayerSkill, default, default, null, null);

        internal static CombatCommandTransition Calculate(CombatCommandInput input = null)
        {
            input ??= Input();
            var work = PlayerSkillAttackTestData.Prepare(input, CombatDamageKind.PlayerSkillAttack);
            var arrival = AttackDeliveryExecutor.Execute(work.Plan, work.World, work.Ledger, new HashSet<EntityId>());
            var attack = AttackResultFactory.CreatePlayerSkill(work.Plan, work.Ledger, 0,
                work.World.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits).ToArray());
            var occurrence = new CombatPlayerSkillAttackEvent(attack);
            work.World.Events.Insert(0, occurrence);
            work.Ledger.BindHealthClaims(work.World.Damage, occurrence);
            var resolved = arrival.Complete();
            return Settle(resolved);
        }

        internal static CombatCommandTransition Empty(CombatCommandInput input, CombatPlayerSkillAttackEvent occurrence = null)
        {
            occurrence ??= new CombatPlayerSkillAttackEvent(new PlayerSkillAttackResult(input.ActorId, 0, 1,
                Array.Empty<CellCoord>(), Array.Empty<AttackHitResult>()));
            return Settle(new CombatCommandTransition(input, true, "",
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(input.Souls, input.Souls), input.State, default, events: new[] { occurrence }));
        }

        internal static CombatCommandTransition Settle(CombatCommandTransition resolved)
        {
            var state = resolved.Input.State.PlayerSkill;
            var settlement = new PlayerSkillUseSettlement(state.ActorId, state.RemainingUses,
                state.LastUsedControllerTurnSequence, resolved.Input.State.Turn.ControllerTurnSequence);
            var skill = new PlayerSkillResult(state.Definition, state.UpgradeKey, resolved.Events, settlement);
            return Copy(resolved, finalState: resolved.FinalState.With(playerSkill:
                state.WithUsage(settlement.FinalRemainingUses, settlement.FinalLastUsedControllerTurnSequence)), skill: skill);
        }

        internal static CombatCommandTransition Copy(CombatCommandTransition t, CombatCommandState finalState = null,
            IReadOnlyList<CombatEvent> events = null, IReadOnlyList<CombatHealthClaim> claims = null,
            IReadOnlyList<CombatHealthExecution> executions = null, PlayerSkillResult skill = null) =>
            new(t.Input, t.Accepted, t.Reason, t.Characters, t.Field, finalState ?? t.FinalState, t.Outcome, t.Attack,
                t.ConsumedSoulIds, t.Completion, t.Movement, events ?? t.Events, t.PathMovement, t.EnvironmentBindings,
                t.BlockedContacts, t.InventoryChange, t.RewardChange, t.RewardSettlement, t.ActionUseSettlement,
                t.UndoMovement, t.MovementEffectUses, executions ?? t.HealthExecutions, claims ?? t.HealthClaims,
                skill ?? t.PlayerSkill);

        // 체력 근거 생성은 Application 전용입니다. 위조 입력 검사는 시험에서만 비공개 생성자를 호출합니다.
        internal static CombatHealthScope Scope(CombatEvent occurrence, IEnumerable<CombatHealthStep> steps) =>
            Construct<CombatHealthScope>(occurrence, -1, steps);

        internal static CombatHealthClaim Claim(CombatEvent occurrence, int index, EntityId recipient,
            IReadOnlyList<CombatHealthStep> steps, CombatHealthScope scope) =>
            Construct<CombatHealthClaim>(CombatHealthClaimKind.AttackHit, occurrence, -1, index, recipient, steps, scope, null);

        private static T Construct<T>(params object[] arguments)
        {
            try { return (T)typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single().Invoke(arguments); }
            catch (TargetInvocationException exception)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }
    }
}
