using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class RampageTraitTests
    {
        [TestCase(39, 100, true)]
        [TestCase(40, 100, false)]
        [TestCase(41, 100, false)]
        [TestCase(2, 7, true)]
        [TestCase(3, 7, false)]
        public void ThresholdIsStrict(int hp, int maximum, bool active)
        {
            var actor = Facts(hp, maximum);
            Assert.That(RampageTraitRule.IsActive(actor), Is.EqualTo(active));
            Assert.That(RampageTraitRule.AttackBonus(actor), Is.EqualTo(active ? 10 : 0));
            Assert.That(RampageTraitRule.MovementBonus(actor), Is.EqualTo(active ? 1 : 0));
        }

        [TestCase(0, 1, 10)]
        [TestCase(5, 1, 15)]
        [TestCase(7, 2, 17)]
        public void RankBonuses_UseResolvedConfiguration(int offset, int movement, int attack)
        {
            var actor = Facts(39, 100, offset: offset, movement: movement);
            actor = RampageApplication.Reconcile(actor, 0);
            Assert.That(actor.Stats.TraitAttackBonus, Is.EqualTo(attack));
            Assert.That(CombatEffectiveStatsCalculator.AttackPower(actor), Is.EqualTo(20 + attack));
            Assert.That(VoluntaryMovementRules.Allowance(actor), Is.EqualTo(3 + movement));
            Assert.That(RampageApplication.Reconcile(actor, 0), Is.SameAs(actor));
            Assert.That(StatusEffectReadModel.Capture(actor).OfType<RampageEffectSnapshot>().Single().AttackBonus, Is.EqualTo(attack));
        }

        [Test]
        public void KillUsesBaseAttackAndSettlesOnce()
        {
            var input = Equip(CombatCommandTestData.Input());
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value is 2 or 3
                ? c.With(health: new HealthStateSnapshot(1, 100), stats: c.Stats.With(attackPower: 8, artifactAttackBonus: 20)) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var source = TraitCombatFixtures.Final(result, 1);
            Assert.That(Status(source).KillAttackBonus, Is.EqualTo(16));
            Assert.That(source.Stats.TraitAttackBonus, Is.EqualTo(26));
            Assert.That(source.Stats.AttackPower, Is.EqualTo(20));
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(t => t.TraitKey == TraitKey.Rampage && t.TraitActivationKind == TraitActivationKind.AttackGranted)
                .Select(t => t.Magnitude), Is.EqualTo(new[] { 8, 8 }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HealingPastThresholdClearsHistory_RegardlessOfTraitOrder(bool bloodFirst)
        {
            var specs = new TraitSpec[] { new RampageTraitSpec(10, 0, 1), new CallOfBloodTraitSpec(10) };
            if (bloodFirst) Array.Reverse(specs);
            var input = Equip(TraitMovementAttackTests.SingleTarget(), specs: specs);
            input = ThornHeartTestData.Replace(input, 2, c => c.With(health: new HealthStateSnapshot(1, 10), stats: c.Stats.With(attackPower: 8)));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var actor = TraitCombatFixtures.Final(result, 1);
            Assert.That(actor.Health.CurrentHealth, Is.EqualTo(40));
            Assert.That(Status(actor).KillAttackBonus, Is.Zero);
            Assert.That(actor.Stats.TraitAttackBonus, Is.Zero);
            Assert.That(StatusEffectReadModel.Capture(actor).OfType<RampageEffectSnapshot>(), Is.Empty);
            Assert.That(result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Single(t => t.TraitKey == TraitKey.Rampage && t.TraitActivationKind == TraitActivationKind.AttackGranted).Magnitude, Is.EqualTo(8));
        }

        [Test]
        public void ReentryStartsAtZero()
        {
            var actor = RampageApplication.Reconcile(Facts(39, 100, kills: 16), 0);
            var healed = CharacterSettlementCalculator.SetHealth(actor, null, 40).Characters.Single().Final;
            Assert.That(Status(healed).KillAttackBonus, Is.Zero);
            Assert.That(healed.Stats.TraitAttackBonus, Is.Zero);
            var damaged = CharacterSettlementCalculator.SetHealth(healed, null, 39).Characters.Single().Final;
            Assert.That(Status(damaged).KillAttackBonus, Is.Zero);
            Assert.That(damaged.Stats.TraitAttackBonus, Is.EqualTo(10));
            Assert.That(Status(actor).KillAttackBonus, Is.EqualTo(16));
        }

        [Test]
        public void NextHitReadsPriorKillGain()
        {
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy, 8).WithHealth(new HealthStateSnapshot(1, 100))).Append(
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy)).ToArray());
            var result = TraitCombatFixtures.Attack(Equip(input));
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(62));
            Assert.That(Status(TraitCombatFixtures.Final(result, 1)).KillAttackBonus, Is.EqualTo(8));
        }

        [Test]
        public void HunterAndRampageGainsCoexist()
        {
            var input = Equip(TraitMovementAttackTests.SingleTarget(), specs: new TraitSpec[]
                { new RampageTraitSpec(10, 0, 1), new HunterTraitSpec(2, 3) });
            input = ThornHeartTestData.Replace(input, 2, c => c.With(health: new HealthStateSnapshot(1, 100), stats: c.Stats.With(attackPower: 8)));
            input = CombatCommandTestData.NextPlayerTurn(input);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var after = CombatCommandComposition.Project(result);
            Assert.That(after.Owned.Single(o => o.Id == after.Characters.Single(c => c.EntityId.Value == 1).OwnedId).HunterAttackBonus, Is.EqualTo(2));
            Assert.That(TraitCombatFixtures.Final(result, 1).Stats.TraitAttackBonus, Is.EqualTo(20));
        }

        [Test]
        public void InvalidHistoryClearsForDeadAndReserveHolders()
        {
            var source = RampageApplication.Reconcile(Facts(39, 100, kills: 8), 0);
            var reserve = source.WithPlacement(CharacterRuntimeArea.Hand, 0, null, null, Array.Empty<CellCoord>());
            var dead = CombatCommandCalculator.Hurt(source, 100);
            foreach (var actor in new[] { reserve, dead })
            {
                var final = RampageApplication.Reconcile(actor, 0);
                Assert.That(Status(final).KillAttackBonus, Is.Zero);
                Assert.That(RampageTraitRule.IsActive(final), Is.False);
                Assert.That(final.Stats.TraitAttackBonus, Is.Zero);
            }
        }

        [Test]
        public void ConfigurationAndRuntimeHistoryRejectInvalidOrOverflowingValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RampageTraitSpec(-1, 0, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RampageTraitStatusSnapshot(0, 0, 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RampageRuntimeState(-1));
            Assert.Throws<OverflowException>(() => new RampageTraitStatusSnapshot(int.MaxValue, 1, 1));
            Assert.Throws<OverflowException>(() => new RampageTraitStatusSnapshot(int.MaxValue, 0, 1, 1));
        }

        [TestCase(39, 8)]
        [TestCase(40, 0)]
        [TestCase(0, 0)]
        public void PeriodicKill_UsesCurrentSourceEligibility(int hp, int expectedGain)
        {
            var input = Equip(CombatCommandTestData.Input(), hp: hp);
            if (hp == 0) input = ThornHeartTestData.Replace(input, 1, c => CombatCommandCalculator.Hurt(c, 0));
            input = ThornHeartTestData.Replace(input, 2, c => c.With(stats: c.Stats.With(attackPower: 8)));
            input = PeriodicStatusTurnTests.WithStatus(input, 2, new StatusEffectSet(new AppliedStatus[]
                { new FireStatus(PeriodicStatusTurnTests.Origin(), 2) }), 1);
            var result = PeriodicStatusTurnTests.Start(input, Owner.Enemy);
            Assert.That(PeriodicStatusTurnTests.Final(result, 2).Health.IsDead, Is.True);
            Assert.That(Status(PeriodicStatusTurnTests.Final(result, 1)).KillAttackBonus, Is.EqualTo(expectedGain));
        }

        [Test]
        public void RuntimeHistoryCopiesAndCodecRoundTrips()
        {
            var original = new RampageRuntimeState(8);
            var clone = (RampageRuntimeState)original.Clone();
            clone.CopyFinalValuesFrom(new RampageRuntimeState(16));
            Assert.That(original.KillAttackBonus, Is.EqualTo(8));
            Assert.That(clone.KillAttackBonus, Is.EqualTo(16));
            var restored = (RampageRuntimeState)TraitEffectStateCodec.Restore(TraitEffectStateCodec.Capture(original));
            Assert.That(restored.KillAttackBonus, Is.EqualTo(8));
            Assert.That(restored, Is.Not.SameAs(original));
        }

        internal static RampageTraitStatusSnapshot Status(CharacterCombatFacts actor) => actor.Stats.TraitStatuses.ToArray().OfType<RampageTraitStatusSnapshot>().Single();

        internal static CharacterCombatFacts Facts(int hp, int maximum, int offset = 0, int movement = 1, int kills = 0) =>
            CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Player, 20)
                .With(health: new HealthStateSnapshot(hp, maximum), stats: CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo,
                    new CellCoord(1, 3), Owner.Player, 20).Stats.With(traitStatuses: new TraitStatusSnapshot[]
                        { new RampageTraitStatusSnapshot(10, offset, movement, kills) }));

        internal static CombatCommandInput Equip(CombatCommandInput input, int hp = 39, int kills = 0, TraitSpec[] specs = null)
        {
            specs ??= new TraitSpec[] { new RampageTraitSpec(10, 0, 1) };
            var old = input.Profiles.Single(p => p.Definition.Key == EntityKey.Unit_Akimbo);
            var d = old.Definition;
            var definition = new UnitSpec(d.Key, d.Role, d.FootprintWidth, d.FootprintHeight, d.FootprintLocalCells.ToArray(),
                d.MaxHealth, d.MoveMethod, d.AttackRange, d.CategoryKey, d.AttackPower, d.PhysicalDefense, d.Priority,
                specs, d.OwnerEligibility, d.Occlusion, d.Behavior, d.KillReward);
            var snapshots = specs.Select(spec => spec switch
            {
                RampageTraitSpec rampage => (TraitStatusSnapshot)new RampageTraitStatusSnapshot(rampage.BaseAttackBonus,
                    rampage.RankAttackBonus, rampage.MovementBonus, kills),
                HunterTraitSpec => new HunterTraitStatusSnapshot(),
                _ => (TraitStatusSnapshot)Activator.CreateInstance(typeof(TraitStatusSnapshot).Assembly.GetType(
                    typeof(TraitStatusSnapshot).Namespace + "." + spec.TraitKey + "TraitStatusSnapshot"))
            }).ToArray();
            return CombatCommandTestData.With(input, profiles: input.Profiles.Select(p => p == old ? new CombatAttackProfile(definition, p.Method, p.OwnedId) : p).ToArray(),
                characters: input.Characters.Select(c => c.CharacterKey != EntityKey.Unit_Akimbo ? c : RampageApplication.Reconcile(
                    c.With(health: new HealthStateSnapshot(hp, c.Health.MaxHealth), stats: c.Stats.With(traitStatuses: snapshots)),
                    c.OwnedId.HasValue ? input.Owned.Single(o => o.Id == c.OwnedId).HunterAttackBonus : 0)).ToArray());
        }
    }
}
