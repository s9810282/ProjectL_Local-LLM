using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class StatusEffectPublicationTests
    {
        [Test]
        public void FreshBattle_RejectsCanonicalStateThatLegacyDtoCannotRepresent()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new TraitAttackGrantPublicationTests.GrantSource());
            var fresh = battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            var membership = battle.Buffers.CombatStateReadSink.Current.Membership;
            battle.Buffers.BattleWriteSink.EndBattle();
            var store = (ICharacterBufferStore)battle.Buffers.CharacterReadSink;
            var stale = fresh.Select(c => c.CharacterKey == EntityKey.Unit_Cowboy
                ? c.WithStatusEffects(new StatusEffectSet(new AppliedStatus[] { new ProbeStatus() })) : c).ToArray();
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => store.PrepareBattle(stale, membership));
            before.AssertUnchanged(battle.Buffers);
            Assert.DoesNotThrow(() => store.PrepareBattle(fresh, membership));
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void SuccessfulGrantAndExpiry_PreservePreviouslyPublishedSnapshots()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new TraitAttackGrantPublicationTests.GrantSource());
            var initial = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == battle.Main.EntityId);
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(initial.StatusEffects.Entries, Is.Empty);
            battle.Buffers.BattleWriteSink.Apply(transition);

            var granted = battle.Buffers.CharacterReadSink.Characters.Single(c => c.EntityId == battle.Main.EntityId);
            Assert.That(granted.StatusEffects.Entries.OfType<AttackIncreaseStatus>().Select(g => g.Amount), Is.EqualTo(new[] { 3, 3 }));
            Assert.That(granted.StatusEffects.Entries.Select(g => g.Origin.Value.DefinitionKey),
                Is.EquivalentTo(new[] { (int)TraitKey.Vanguard, (int)TraitKey.CleanSweep }));
            Assert.That(granted.StatusEffects, Is.SameAs(granted.ToFacts().StatusEffects));
            Assert.That(initial.StatusEffects.Entries, Is.Empty);
            var stateEvent = transition.Events.OfType<CombatTraitStateEvent>().Last(e => e.Final.EntityId == granted.EntityId);
            Assert.That(stateEvent.Final.StatusEffects.SameValues(granted.StatusEffects), Is.True);

            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Main.StatusEffects.Entries, Is.Empty);
            Assert.That(granted.StatusEffects.Entries.Count, Is.EqualTo(2));
            Assert.That(stateEvent.Final.StatusEffects.Entries.Count, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StaleStatusOnly_RejectsAllStoresAndResultPublication(bool legacyCanRepresent)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new TraitAttackGrantPublicationTests.GrantSource());
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var main = battle.Main;
            AppliedStatus status = legacyCanRepresent
                ? new AttackIncreaseStatus(new EffectOriginKey(EffectOwnerKind.Trait, main.EntityId.Value, (int)TraitKey.Vanguard, "grant"), 0, 2)
                : new ProbeStatus();
            var changed = main.WithStatusEffects(new StatusEffectSet(new[] { status }));
            Assert.That(changed.Stats.SameValues(main.Stats), Is.True);
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(main, changed) }, Array.Empty<SoulCountChange>()));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(battle.Main.StatusEffects.SameValues(changed.StatusEffects), Is.True);
            Assert.That(battle.Main.Health.CurrentHealth, Is.EqualTo(main.Health.CurrentHealth));
            Assert.That(main.StatusEffects.Entries, Is.Empty);
        }

        [TestCase(StatusEffectKey.AttackDecrease)]
        [TestCase(StatusEffectKey.Disarm)]
        [TestCase(StatusEffectKey.Bind)]
        [TestCase(StatusEffectKey.DefenseDecrease)]
        public void StaleCanonicalDebuff_RejectsEveryStore(StatusEffectKey key)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new ArmorBreakerPublicationTests.ArmorSource());
            var victim = battle.Character(EntityKey.Unit_Demon);
            var transition = CombatCommandCalculator.Calculate(battle.Publication().Attack(battle.Actor, victim.Anchor.Value), Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, battle.Actor.EntityId.Value, (int)TraitKey.Vanguard, "other-producer");
            AppliedStatus status = key switch
            {
                StatusEffectKey.AttackDecrease => new AttackDecreaseStatus(origin, 20),
                StatusEffectKey.DefenseDecrease => new DefenseDecreaseStatus(origin, 20),
                _ => new ActionRestrictionStatus(key, origin, 2)
            };
            var changed = victim.WithStatusEffects(new StatusEffectSet(new[] { status }));
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(victim, changed) }, Array.Empty<SoulCountChange>()));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(battle.Character(EntityKey.Unit_Demon).StatusEffects.SameValues(changed.StatusEffects), Is.True);
            Assert.That(victim.StatusEffects.Entries, Is.Empty);
        }

        [Test]
        public void LateCommandPreparationFailure_PublishesNeitherArmorNorHealthNorResult()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new ArmorBreakerPublicationTests.ArmorSource());
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            input = input.WithWorld(input.State.With(traitTurn: new TraitTurnFacts(999, 0)), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(transition.Characters.Characters.Any(c => c.Final.StatusEffects.Entries.OfType<DefenseDecreaseStatus>().Any()), Is.True);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
        }

        private sealed class ProbeStatus : AppliedStatus
        {
            public ProbeStatus() : base(StatusEffectKey.DefenseIncrease, null) { }
            public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.BattleEnd;
            public override bool SameValues(AppliedStatus other) => other is ProbeStatus;
        }
    }
}
