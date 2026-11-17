using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerSkillStunTests
    {
        private static readonly EffectOriginKey First = new(EffectOwnerKind.PlayerSkill, 3, (int)PlayerSkillKey.PowderBlossom, "condemnation");
        private static readonly EffectOriginKey Second = new(EffectOwnerKind.PlayerSkill, 4, (int)PlayerSkillKey.PowderBlossom, "condemnation");

        [TestCase(Owner.Player)]
        [TestCase(Owner.Enemy)]
        public void StunExpiresExactlyAtAffectedSideEnd(Owner affected)
        {
            var statuses = new StatusEffectSet(new AppliedStatus[] { Stun(First, 1) });
            var opposite = affected == Owner.Player ? Owner.Enemy : Owner.Player;
            Assert.That(AppliedStatusRules.EndControllerTurn(statuses, affected, opposite), Is.SameAs(statuses));
            Assert.That(AppliedStatusRules.EndControllerTurn(statuses, affected, affected).Entries, Is.Empty);
        }

        [TestCase(1, 1)]
        [TestCase(2, 2)]
        public void StunReapplicationKeepsTheMaximumDuration(int outstanding, int expected)
        {
            var statuses = new StatusEffectSet(new AppliedStatus[] { Stun(First, outstanding) });
            var renewed = AppliedStatusRules.Apply(statuses, Stun(Second, 1));

            Assert.That(renewed.Entries.Cast<ActionRestrictionStatus>().Single().RemainingAffectedSideTurnEnds, Is.EqualTo(expected));
        }

        [Test]
        public void StunBlocksOrdinaryMoveOrdinaryAttackAndSkillAndAppearsInReadFacts()
        {
            var input = PlayerSkillTestData.Input();
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId)
                .WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { Stun(First, 1) }));
            input = PlayerSkillAdmissionTests.ReplaceActor(input, actor);

            var ordinary = CombatCommandCalculator.GetActionAvailability(input);
            Assert.That(ordinary.MoveFailure, Is.EqualTo(MovementFailureReason.Stunned));
            Assert.That(ordinary.AttackFailure, Is.EqualTo(AttackFailureReason.Stunned));
            Assert.That(PlayerSkillAdmissionTests.Query(input).FailureReason, Is.EqualTo(PlayerSkillFailureReason.Stunned));
            var projection = StatusEffectReadModel.Capture(actor).OfType<ActionRestrictionEffectSnapshot>().Single();
            Assert.That(projection.Key, Is.EqualTo(StatusEffectKey.Stun));
            Assert.That(projection.Sources.Single().Status.Origin, Is.EqualTo(First));
        }

        [Test]
        public void LegacyDebuffReplacementPreservesStunAndExistingCleanupRemovesIt()
        {
            var input = PlayerSkillTestData.Input();
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId)
                .WithStatusEffects(new StatusEffectSet(new AppliedStatus[]
                {
                    Stun(First, 1),
                    new ActionRestrictionStatus(StatusEffectKey.Disarm, new EffectOriginKey(EffectOwnerKind.Item, 5, 1, "disarm"), 1)
                }));
            var updated = actor.With(debuffs: actor.Debuffs.WithRestrictions(null, null));

            Assert.That(AppliedStatusRules.IsRestricted(updated.StatusEffects, StatusEffectKey.Stun), Is.True);
            Assert.That(AppliedStatusRules.IsRestricted(updated.StatusEffects, StatusEffectKey.Disarm), Is.False);
            Assert.That(CombatTemporaryStateCleanup.Clear(updated).StatusEffects.Entries, Is.Empty);
            Assert.That(CombatReserveReturnCleanup.Clear(updated, new List<CombatEvent>()).StatusEffects.Entries, Is.Empty);
        }

        private static ActionRestrictionStatus Stun(EffectOriginKey origin, int duration) =>
            new(StatusEffectKey.Stun, origin, duration);
    }
}
