using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class TraitActivationContractTests
    {
        [Test]
        public void NewPayloads_SurviveFactoryAndLegacyCopies()
        {
            var input = SacrificeTraitTests.Input();
            var attack = TraitCombatFixtures.Attack(input).Attack.Value;
            var hits = attack.Hits.ToArray();
            var activations = attack.TraitActivations.ToArray();
            var expected = hits.Single();
            var reaction = activations.Single(t => t.Interception != null);
            var selection = new AttackCellSelection(attack.PrimaryCell, attack.DesignatedCells.ToArray(), attack.AdditionalCells.ToArray());
            var copied = AttackResult.FromFacts(attack.AttackerId, attack.PrimaryTargetId, attack.AttackDirection, attack.ActionUseSettlement,
                attack.AffectedCells.ToArray(), hits, selection, traitActivations: activations, attackGroup: attack.AttackGroup);
            var legacy = AttackExecutionResult.Success(attack.AttackerId,attack.PrimaryTargetId,attack.AttackDirection,                attack.AffectedCells.ToArray(),hits,attack.TargetCell);
            hits[0] = default; activations[0] = null;
            Assert.That(copied.Hits.ToArray().Single().Contacts, Is.SameAs(expected.Contacts));
            Assert.That(legacy.Hits.ToArray().Single().Contacts, Is.SameAs(expected.Contacts));
            Assert.That(copied.TraitActivations.ToArray().Single(t => t.Interception != null), Is.SameAs(reaction));
            var originalPreview = new AttackTargetPreview(expected, expected.DamageDealt, new[] { new CellCoord(3, 4) }, 20, 3, 2, 4);
            var preview = originalPreview.WithContacts(expected.Contacts);
            Assert.That(preview.TargetId, Is.EqualTo(originalPreview.TargetId));
            Assert.That(preview.TargetHealthBefore, Is.EqualTo(originalPreview.TargetHealthBefore));
            Assert.That(preview.TargetHealthAfter, Is.EqualTo(originalPreview.TargetHealthAfter));
            Assert.That(preview.DamageBeforeMatchup, Is.EqualTo(20));
            Assert.That(preview.MatchupNumerator, Is.EqualTo(3));
            Assert.That(preview.MatchupDenominator, Is.EqualTo(2));
            Assert.That(preview.IncomingReduction, Is.EqualTo(4));
            Assert.That(preview.TargetOccupiedCells.ToArray(), Is.EqualTo(originalPreview.TargetOccupiedCells.ToArray()));
            Assert.That(preview.IsDirectHit, Is.True);
        }

        [Test]
        public void NativeRoot_RejectsScopeAndDuplicateReactions()
        {
            var attack = TraitCombatFixtures.Attack(SacrificeTraitTests.Input()).Attack.Value;
            var selection = new AttackCellSelection(attack.PrimaryCell, attack.DesignatedCells.ToArray(), attack.AdditionalCells.ToArray());
            var hits = attack.Hits.ToArray();
            var reactions = attack.TraitActivations.ToArray().Where(t => t.Interception != null).ToArray();
            Assert.Throws<ArgumentException>(() => AttackResult.FromFacts(attack.AttackerId, attack.PrimaryTargetId, attack.AttackDirection,
                attack.ActionUseSettlement, attack.AffectedCells.ToArray(), hits, selection, traitActivations: reactions, attackGroup: attack.AttackGroup + 1));
            Assert.Throws<ArgumentException>(() => AttackResult.FromFacts(attack.AttackerId, attack.PrimaryTargetId, attack.AttackDirection,
                attack.ActionUseSettlement, attack.AffectedCells.ToArray(), hits, selection, traitActivations: reactions.Concat(reactions).ToArray(), attackGroup: attack.AttackGroup));
            Assert.Throws<ArgumentException>(() => new AttackOutcomeSnapshot(hits.Append(new AttackHitResult(new EntityId(8), 10, 5)).ToArray(), reactions));
            var wrongOrigin = new TraitActivationSnapshot(reactions[0].SourceUnitId, reactions[0].TargetId, TraitKey.Sacrifice,
                TraitActivationKind.DamageRedirected, reactions[0].Magnitude, resultOrigin: new CombatDamageOrigin(new CellCoord(7, 7), attack.AttackerId, attack.AttackGroup))
                .WithInterception(reactions[0].Interception);
            Assert.Throws<ArgumentException>(() => new AttackOutcomeSnapshot(hits, new[] { wrongOrigin }));
        }

        [Test]
        public void NativeOutcome_RejectsUnexplainedRedirect()
        {
            var contact = new AttackHitContactSnapshot(0, new EntityId(2), AttackTargetRole.Primary, 100,
                new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 1));
            var hit = new AttackHitResult(new EntityId(5), 100, 80, AttackTargetRole.Primary, 100, 0, 0, new[] { contact });
            Assert.Throws<ArgumentException>(() => new AttackOutcomeSnapshot(new[] { hit }, Array.Empty<TraitActivationSnapshot>()));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NativePreview_RejectsDuplicateOrForeignContacts(bool foreign)
        {
            var input = SacrificeTraitTests.Input();
            var result = TraitCombatFixtures.Attack(input).Attack.Value;
            var hit = result.Hits.ToArray().Single();
            var contact = hit.Contacts.Single();
            var badContact = foreign ? new AttackHitContactSnapshot(0, contact.OriginalTargetId, contact.Role, contact.AppliedSplashPercent,
                new CombatDamageOrigin(contact.Origin.PhysicalCell, new EntityId(999), contact.Origin.AttackGroup)) : contact;
            var target = new AttackTargetPreview(hit, hit.DamageDealt, new[] { new CellCoord(3, 4) }, 20, 1, 1, 0).WithContacts(new[] { badContact });
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            Assert.Throws<ArgumentException>(() => AttackPreview.FromFacts(result, foreign ? new[] { target } : new[] { target, target },
                actor.Stats.TraitStatuses.ToArray(), actor.Stats.Category));
        }

        [Test]
        public void InterceptionPayloads_AreDefensiveAndValidateIdentity()
        {
            var origin = new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 2);
            var relation = new AttackInterceptionSnapshot(new EntityId(5), new EntityId(2), new EntityId(5), 0);
            var reaction = new TraitActivationSnapshot(new EntityId(5), new EntityId(2), TraitKey.Sacrifice,
                TraitActivationKind.DamageRedirected, 20, resultOrigin: origin).WithInterception(relation);
            var contacts = new[] { new AttackHitContactSnapshot(0, new EntityId(2), AttackTargetRole.Designated, 100, origin) };
            var hits = new[] { new AttackHitResult(new EntityId(5), 100, 80, AttackTargetRole.Designated, 100, 0, 0, contacts) };
            var reactions = new[] { reaction };
            var outcome = new AttackOutcomeSnapshot(hits, reactions);
            var start = new TraitActivationSnapshot(new EntityId(1), new EntityId(2), TraitKey.LuckyBullet,
                TraitActivationKind.FollowUpAttack, 20, resultOrigin: origin).WithChildAttack(outcome);
            contacts[0] = new AttackHitContactSnapshot(1, new EntityId(3), AttackTargetRole.Designated, 100, origin);
            hits[0] = default; reactions[0] = null;
            Assert.That(start.ChildAttack.Hits[0].Contacts[0].OriginalTargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(start.ChildAttack.Reactions[0], Is.SameAs(reaction));
            Assert.Throws<NotSupportedException>(() => ((IList<AttackHitResult>)outcome.Hits)[0] = default);
            Assert.Throws<NotSupportedException>(() => ((IList<TraitActivationSnapshot>)outcome.Reactions)[0] = null);
            Assert.Throws<NotSupportedException>(() => ((IList<AttackHitContactSnapshot>)outcome.Hits[0].Contacts)[0] = null);
            Assert.Throws<ArgumentException>(() => new AttackInterceptionSnapshot(new EntityId(2), new EntityId(2), new EntityId(2), 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackInterceptionSnapshot(new EntityId(5), new EntityId(2), new EntityId(5), -1));
            Assert.Throws<ArgumentException>(() => new TraitActivationSnapshot(new EntityId(6), new EntityId(2), TraitKey.Sacrifice,
                TraitActivationKind.DamageRedirected, 20, resultOrigin: origin).WithInterception(relation));
            Assert.Throws<ArgumentException>(() => new CombatEffectsEvent(new[] { reaction }, Array.Empty<CategoryActivationSnapshot>()));
            Assert.Throws<ArgumentException>(() => new TraitActivationSnapshot(new EntityId(1), new EntityId(2), TraitKey.LuckyBullet,
                TraitActivationKind.FollowUpAttack, 20, resultOrigin: new CombatDamageOrigin(CellCoord.Zero, new EntityId(1), 3)).WithChildAttack(outcome));
            Assert.That(typeof(AttackHitResult).GetConstructors().Any(c => c.GetParameters().Length == 7), Is.True);
            Assert.That(typeof(TraitActivationSnapshot).GetConstructors().Any(c => c.GetParameters().Length == 10), Is.True);
        }

        [Test]
        public void CauseCopy_IsImmutable()
        {
            var cause = new CombatMovementCause(CombatMovementAgency.Forced, TraitKey.Pull);
            var steps = new[] { new AttackPullStepSnapshot(CellCoord.Zero, CellCoord.Right, cause) };
            var movement = AttackPullSnapshot.FromSteps(new EntityId(2), CellCoord.Zero, steps);
            steps[0] = new AttackPullStepSnapshot(CellCoord.Right, new CellCoord(2, 0), cause);
            Assert.That(movement.Steps.Single().From, Is.EqualTo(CellCoord.Zero));
            Assert.That(movement.Path, Is.EqualTo(new[] { CellCoord.Right }));
            Assert.That(movement.FinalAnchor, Is.EqualTo(CellCoord.Right));
            Assert.That(movement.Steps.Single().Cause, Is.SameAs(cause));
            Assert.Throws<NotSupportedException>(() => ((IList<AttackPullStepSnapshot>)movement.Steps)[0] = steps[0]);
            Assert.That(typeof(CombatMovementCause).GetProperties().All(p => !p.CanWrite), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatMovementCause((CombatMovementAgency)0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatMovementCause(CombatMovementAgency.Forced, TraitKey.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatMovementCause(CombatMovementAgency.Forced, (TraitKey)9999));
            Assert.Throws<ArgumentException>(() => new AttackPullStepSnapshot(CellCoord.Zero, CellCoord.Right,
                new CombatMovementCause(CombatMovementAgency.Voluntary, TraitKey.Pull)));
            Assert.Throws<ArgumentException>(() => AttackPullSnapshot.FromSteps(new EntityId(2), CellCoord.Zero, steps));
            Assert.That(typeof(CombatCharacterEvent).GetConstructors().Any(c => c.GetParameters().Length == 6), Is.True);
            Assert.That(typeof(CombatStructureEvent).GetConstructors().Any(c => c.GetParameters().Length == 4), Is.True);
            Assert.That(typeof(AttackPullSnapshot).GetConstructors().Any(c => c.GetParameters().Length == 4), Is.True);
        }

        [Test]
        public void LegacyConstructor_PreservesSingleTarget()
        {
            var value = new TraitActivationSnapshot(new EntityId(1), new EntityId(2), TraitKey.Vanguard, TraitActivationKind.AttackGranted, 3);
            Assert.That(value.TargetId, Is.EqualTo(new EntityId(2)));
            Assert.That(Read<IReadOnlyList<EntityId>>(value, "TargetIds"), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(Read<IReadOnlyList<CellCoord>>(value, "TargetCells"), Is.Empty);
            Assert.That(Read<CombatDamageOrigin>(value, "ResultOrigin"), Is.Null);
            Assert.That(value.Magnitude, Is.EqualTo(3));
        }

        [Test]
        public void Collections_AreDefensiveCopies()
        {
            var ids = new[] { new EntityId(2), new EntityId(3) };
            var cells = new[] { new CellCoord(1, 2), new CellCoord(3, 4) };
            var value = Create(ids: ids, cells: cells);
            ids[0] = new EntityId(9); cells[0] = new CellCoord(9, 9);
            var actualIds = Read<IReadOnlyList<EntityId>>(value, "TargetIds");
            var actualCells = Read<IReadOnlyList<CellCoord>>(value, "TargetCells");
            Assert.That(actualIds, Is.EqualTo(new[] { new EntityId(2), new EntityId(3) }));
            Assert.That(actualCells, Is.EqualTo(new[] { new CellCoord(1, 2), new CellCoord(3, 4) }));
            Assert.That(value.TargetId, Is.Null);
            Assert.Throws<NotSupportedException>(() => ((IList<EntityId>)actualIds)[0] = new EntityId(9));
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)actualCells)[0] = new CellCoord(9, 9));
            var input = new[] { value };
            var result = new CombatEffectsEvent(input, Array.Empty<CategoryActivationSnapshot>());
            input[0] = new TraitActivationSnapshot(new EntityId(9), null, TraitKey.Hunter, TraitActivationKind.HunterTargetSelected, 0);
            Assert.That(Read<IReadOnlyList<EntityId>>(result.Traits.Single(), "TargetIds"), Is.EqualTo(actualIds));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        public void ConflictingTargets_AreRejected(int invalid)
        {
            Assert.Catch<ArgumentException>(() => Create(
                target: invalid == 2 || invalid == 3 ? new EntityId(2) : (EntityId?)null,
                ids: invalid == 0 ? new[] { EntityId.Empty } : invalid == 1 ? new[] { new EntityId(2), new EntityId(2) }
                    : invalid == 2 ? new[] { new EntityId(3) } : invalid == 3 ? new[] { new EntityId(2), new EntityId(3) } : null,
                cells: invalid == 4 ? new[] { CellCoord.Zero, CellCoord.Zero } : null,
                direction: invalid == 5 ? (Rotation4)99 : (Rotation4?)null));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OwnerAndCreditor_AreDistinct(bool nullCredit)
        {
            var origin = new CombatDamageOrigin(new CellCoord(3, 4), nullCredit ? (EntityId?)null : new EntityId(7), nullCredit ? 0 : 8);
            var value = Create(origin: origin);
            Assert.That(value.SourceUnitId, Is.EqualTo(new EntityId(1)));
            var result = Read<CombatDamageOrigin>(value, "ResultOrigin");
            Assert.That(result.CreditedCharacterId, Is.EqualTo(origin.CreditedCharacterId));
            Assert.That(result.AttackGroup, Is.EqualTo(origin.AttackGroup));
            Assert.That(result.PhysicalCell, Is.EqualTo(origin.PhysicalCell));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TargetlessFollowUp_RetainsDirectionAndSelectedCell(bool hasAim)
        {
            Rotation4? direction = hasAim ? Rotation4.R90 : (Rotation4?)null;
            CellCoord? cell = hasAim ? new CellCoord(3, 4) : (CellCoord?)null;
            var value = Create(direction: direction, selectedCell: cell);
            Assert.That(value.TargetId, Is.Null);
            Assert.That(Read<IReadOnlyList<EntityId>>(value, "TargetIds"), Is.Empty);
            Assert.That(Read<Rotation4?>(value, "AttackDirection"), Is.EqualTo(direction));
            Assert.That(Read<CellCoord?>(value, "TargetCell"), Is.EqualTo(cell));
        }

        [Test]
        public void ExistingEnumValues_AreUnchanged()
        {
            var expected = new Dictionary<string, int> { ["None"] = 0, ["FocusGainScheduled"] = 10,
                ["FocusStackApplied"] = 11, ["FocusReset"] = 12, ["AttackDiscountArmed"] = 20,
                ["AttackCostReduced"] = 21, ["AttackDiscountExpired"] = 22, ["KillChainAttack"] = 70,
                ["FollowUpAttack"] = 80, ["AttackGranted"] = 90, ["HealthRecovered"] = 100,
                ["DamageRedirected"] = 110, ["ArmorBroken"] = 120, ["HunterTargetSelected"] = 130 };
            foreach (var entry in expected)
                Assert.That((int)Enum.Parse(typeof(TraitActivationKind), entry.Key), Is.EqualTo(entry.Value), entry.Key);
        }

        [Test]
        public void PublicShape_HasCanonicalTargetsAndImmutableCorrelation()
        {
            Assert.That(typeof(TraitActivationSnapshot).GetProperties().Select(p => p.Name), Is.EquivalentTo(new[] {
                "SourceUnitId", "TargetId", "TraitKey", "TraitActivationKind", "Magnitude", "TargetIds", "TargetCells",
                "ResultOrigin", "AttackDirection", "TargetCell", "Interception", "ChildAttack" }));
            Assert.That(typeof(TraitActivationSnapshot).GetProperties().All(p => !p.CanWrite), Is.True);
        }

        private static T Read<T>(TraitActivationSnapshot value, string property)
        {
            var member = typeof(TraitActivationSnapshot).GetProperty(property);
            Assert.That(member, Is.Not.Null, property);
            return (T)member.GetValue(value);
        }

        private static TraitActivationSnapshot Create(EntityId? target = null, IReadOnlyList<EntityId> ids = null,
            IReadOnlyList<CellCoord> cells = null, CombatDamageOrigin origin = null, Rotation4? direction = null, CellCoord? selectedCell = null)
        {
            var constructor = typeof(TraitActivationSnapshot).GetConstructors().SingleOrDefault(c => c.GetParameters().Length == 10);
            Assert.That(constructor, Is.Not.Null, "확장 불변 발동 계약이 필요합니다.");
            try
            {
                return (TraitActivationSnapshot)constructor.Invoke(new object[] { new EntityId(1), target, TraitKey.DoubleShot,
                    TraitActivationKind.FollowUpAttack, 0, ids, cells, origin, direction, selectedCell });
            }
            catch (TargetInvocationException error) when (error.InnerException != null)
            {
                throw error.InnerException;
            }
        }
    }
}
