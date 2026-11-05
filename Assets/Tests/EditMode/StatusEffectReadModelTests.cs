using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Tests.Core
{
    public sealed class StatusEffectReadModelTests
    {
        private static readonly EffectOriginKey First = new(EffectOwnerKind.Trait, 1, (int)TraitKey.Vanguard, "grant");
        private static readonly EffectOriginKey Second = new(EffectOwnerKind.Trait, 2, (int)TraitKey.CleanSweep, "grant");

        [Test]
        public void Capture_OrdersFamiliesButPreservesApplicationOrderAndIndependentSourceClocks()
        {
            var statuses = new StatusEffectSet(new AppliedStatus[]
            {
                new ActionRestrictionStatus(StatusEffectKey.Bind, First, 2), new AttackIncreaseStatus(Second, 4, 3),
                new AttackDecreaseStatus(First, 20), new ShieldStatus(Second, 20, true, 4),
                new AttackIncreaseStatus(First, 3, 1), new ActionRestrictionStatus(StatusEffectKey.Disarm, Second, 1),
                new DefenseDecreaseStatus(First, 20)
            });
            var captured = StatusEffectReadModel.Capture(Facts(statuses));
            Assert.That(captured.Select(s => s.Key), Is.EqualTo(new[] { StatusEffectKey.AttackIncrease, StatusEffectKey.Shield,
                StatusEffectKey.AttackDecrease, StatusEffectKey.DefenseDecrease, StatusEffectKey.Disarm, StatusEffectKey.Bind }));
            var attack = captured.OfType<AttackIncreaseEffectSnapshot>().Single();
            Assert.That(attack.AppliedAmount, Is.EqualTo(7));
            Assert.That(attack.Sources.Select(s => s.Origin), Is.EqualTo(new EffectOriginKey?[] { Second, First }));
            Assert.That(attack.Sources.Select(s => s.RemainingControllerTurnEnds), Is.EqualTo(new int?[] { 3, 1 }));
            var later = StatusEffectReadModel.Capture(Facts(AppliedStatusRules.EndControllerTurn(statuses, Owner.Player, Owner.Enemy)));
            Assert.That(later.OfType<AttackIncreaseEffectSnapshot>().Single().AppliedAmount, Is.EqualTo(4));
            Assert.That(attack.AppliedAmount, Is.EqualTo(7));
            Assert.That(attack.Sources[1].RemainingControllerTurnEnds, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() => ((IList<StatusEffectSnapshot>)captured).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<StatusEffectSourceSnapshot>)attack.Sources).Clear());
        }

        [Test]
        public void ShieldRead_PreservesSuppressedBudgetsAndHistoricalResetWithoutInventedExpiration()
        {
            var captured = StatusEffectReadModel.Capture(Facts(new StatusEffectSet(new AppliedStatus[]
            {
                new ShieldStatus(First, 30, false, 4), new ShieldStatus(Second, 20, true, 2)
            }))).OfType<ShieldEffectSnapshot>().Single();
            Assert.That(captured.RemainingEligibleBudget, Is.EqualTo(20));
            Assert.That(captured.Sources.Count, Is.EqualTo(2));
            Assert.That(((ShieldStatus)captured.Sources[0].Status).RemainingBudget, Is.EqualTo(30));
            Assert.That(captured.Sources[0].Eligible, Is.False);
            Assert.That(captured.Sources[0].LastResetControllerTurnCount, Is.EqualTo(4));
            Assert.That(captured.Sources.All(s => s.LifetimeKind == StatusEffectLifetimeKind.ReconciledBudget), Is.True);
            Assert.That(captured.Sources.All(s => s.RemainingControllerTurnEnds == null && s.RemainingAffectedSideTurnEnds == null), Is.True);
        }

        [Test]
        public void DerivedDefenseAndHeldAttack_DoNotBecomeAnotherStoredGrantOrTimer()
        {
            var original = CombatCommandTestData.Input().Characters[0];
            var facts = original.With(traitEffects: new CharacterTraitEffects(new[] { new TraitAttackGrant(First, 3, 2) }, Array.Empty<TraitShieldGrant>(), 5),
                stats: original.Stats.With(traitStatuses: new TraitStatusSnapshot[] { new AccelerationTraitStatusSnapshot(4) }));
            var captured = StatusEffectReadModel.Capture(facts);
            var attack = captured.OfType<AttackIncreaseEffectSnapshot>().Single();
            Assert.That((attack.AppliedAmount, attack.HeldForAttackAmount, attack.TotalAmount), Is.EqualTo((3, 4, 7)));
            Assert.That(attack.HeldLifetimeKind, Is.EqualTo(StatusEffectLifetimeKind.AttackCompletion));
            var defense = captured.OfType<DefenseIncreaseEffectSnapshot>().Single();
            Assert.That(defense.Amount, Is.EqualTo(5));
            Assert.That(defense.LifetimeKind, Is.EqualTo(StatusEffectLifetimeKind.CurrentEligibility));
            Assert.That(defense.Sources, Is.Empty);
            Assert.That(facts.StatusEffects.Entries.Count, Is.EqualTo(1));
            Assert.That(facts.StatusEffects.Entries.Single(), Is.TypeOf<AttackIncreaseStatus>());
        }

        [Test]
        public void ConditionalTraits_DoNotCreateUnconditionalActorBadges()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo,
                new TrackingShotTraitSpec(50, 30), new FocusedBombardmentTraitSpec(3, 30),
                new GroundCompactionTraitSpec(1, 2), new DustScreenTraitSpec(1, 20, 2));
            Assert.That(StatusEffectReadModel.Capture(input.Characters[0]), Is.Empty);
        }

        [Test]
        public void ContextualContributionKeys_FollowActualEligibilityWithoutChangingDamageFactors()
        {
            var rule = new TrackingShotEffectRule(new TrackingShotTraitDefinition(50, 30), new TrackingShotTraitRule());
            var source = new EffectRuleInput(TraitEffectOrigin.Create(new EntityId(1), TraitKey.TrackingShot), StatelessEffectStateSnapshot.Instance);
            var healthy = rule.EvaluateTargetDamage(source, new TargetDamageEffectContext(100, null, 1, 0, new HealthStateSnapshot(20, 20)));
            var injured = rule.EvaluateTargetDamage(source, new TargetDamageEffectContext(100, null, 1, 0, new HealthStateSnapshot(4, 20)));
            Assert.That(healthy.EffectKey, Is.EqualTo(StatusEffectKey.OutgoingDamageIncrease));
            Assert.That(injured.EffectKey, Is.Null);
            Assert.That((healthy.Numerator, healthy.Denominator), Is.EqualTo((130, 100)));
            var aura = new EntrenchmentEffectRule(new EntrenchmentTraitDefinition(1, 20), new EntrenchmentTraitRule());
            var holder = Facts(StatusEffectSet.Empty, 1);
            var ally = Facts(StatusEffectSet.Empty, 2).WithPlacement(CharacterRuntimeArea.Field, null, CellCoord.Right, Rotation4.R0, new[] { CellCoord.Right });
            var auraInput = new EffectRuleInput(TraitEffectOrigin.Create(new EntityId(1), TraitKey.Entrenchment), StatelessEffectStateSnapshot.Instance);
            var protectedAlly = aura.EvaluateIncomingDamage(auraInput, new IncomingDamageEffectContext(100, CombatDamageKind.OrdinaryAttack, ally, new[] { holder, ally }));
            var self = aura.EvaluateIncomingDamage(auraInput, new IncomingDamageEffectContext(100, CombatDamageKind.OrdinaryAttack, holder, new[] { holder, ally }));
            Assert.That(protectedAlly.EffectKey, Is.EqualTo(StatusEffectKey.IncomingDamageDecrease));
            Assert.That(protectedAlly.RemainingDamagePercent, Is.EqualTo(80));
            Assert.That(self.EffectKey, Is.Null);
        }

        private static CharacterCombatFacts Facts(StatusEffectSet statuses, int id = 9) =>
            new(new EntityId(id), null, EntityKey.Unit_Akimbo, Owner.Player, null, CharacterRuntimeArea.Field,
                new HealthStateSnapshot(10, 20), default, null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, statusEffects: statuses);
    }
}
