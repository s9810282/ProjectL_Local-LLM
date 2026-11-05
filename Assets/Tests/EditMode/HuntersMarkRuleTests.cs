using System;
using NUnit.Framework;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class HuntersMarkRuleTests
    {
        [TestCase(CombatDamageKind.OrdinaryAttack, true)]
        [TestCase(CombatDamageKind.ExtraAttack, true)]
        [TestCase(CombatDamageKind.ReactionAttack, false)]
        [TestCase(CombatDamageKind.EnvironmentDamage, false)]
        [TestCase(CombatDamageKind.ReflectedDamage, false)]
        [TestCase(CombatDamageKind.DamageOverTime, false)]
        public void GrantEligibility_RemainsOrdinaryAndExtraLivingHostileFieldContact(CombatDamageKind kind, bool eligible)
        {
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, CellCoord.Zero, Owner.Player);
            var victim = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, CellCoord.Right, Owner.Enemy);
            Assert.That(HuntersMarkTraitRule.CanApplyMark(actor, victim, kind), Is.EqualTo(eligible));
            Assert.That(HuntersMarkTraitRule.CanApplyMark(actor, actor, kind), Is.False);
            Assert.That(HuntersMarkTraitRule.CanApplyMark(actor, CombatCommandTestData.With(victim, hp: 0), kind), Is.False);
            Assert.That(HuntersMarkTraitRule.CanApplyMark(actor,
                victim.WithPlacement(CharacterRuntimeArea.EnemyWaiting, null, null, null, Array.Empty<CellCoord>()), kind), Is.False);
        }
        [Test]
        public void RegistryFactoriesCodecAndStatus_PreserveValueAndRejectWrongKinds()
        {
            var registry = TraitRegistry.CreateDefault();
            var registration = registry.GetRequiredRegistration(TraitKey.HuntersMark);
            var spec = new HuntersMarkTraitSpec(25, 1);
            var definition = registration.TraitDefinitionFactory.BuildDefinition(spec);
            var state = registration.TraitRuntimeStateFactory.BuildRuntimeState(definition);
            var clone = state.Clone();
            Assert.That(clone, Is.Not.SameAs(state));
            state.CopyFinalValuesFrom(clone);
            Assert.That(TraitEffectStateCodec.Restore(TraitEffectStateCodec.Capture(state)), Is.TypeOf<HuntersMarkRuntimeState>());
            Assert.That(TraitEffectStateCodec.Capture(state).ValueEquals(TraitEffectStateCodec.Capture(clone)), Is.True);
            Assert.Throws<ArgumentNullException>(() => state.CopyFinalValuesFrom(null));
            Assert.Throws<ArgumentException>(() => state.CopyFinalValuesFrom(new KnockbackRuntimeState()));
            Assert.Throws<ArgumentException>(() => registration.TraitDefinitionFactory.BuildDefinition(new KnockbackTraitSpec(1)));
            Assert.Throws<ArgumentException>(() => registration.TraitRuntimeStateFactory.BuildRuntimeState(new KnockbackTraitDefinition(1)));
            var unit = new UnitCatalog(new[] { CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, spec) }, registry)
                .GetRequired(EntityKey.Unit_Akimbo);
            var status = (HuntersMarkTraitStatusSnapshot)new TraitEffectProvider(registry)
                .CreateStatuses(unit, new UnitTraitStateSet(new[] { state })).Single();
            Assert.That(status.BonusDamagePercent, Is.EqualTo(25));
            Assert.That(status.SameValues(new HuntersMarkTraitStatusSnapshot(25, 1)), Is.True);
            Assert.That(status.SameValues(new HuntersMarkTraitStatusSnapshot(10, 1)), Is.False);
            Assert.That(status.SameValues(new KnockbackTraitStatusSnapshot(1)), Is.False);
            var unavailable = (HuntersMarkTraitStatusSnapshot)status.WithAvailability("Pending");
            Assert.That(unavailable.BonusDamagePercent, Is.EqualTo(25));
            Assert.That(status.IsAvailable, Is.True);
            Assert.That(unavailable.IsAvailable, Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new HuntersMarkTraitSpec(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HuntersMarkTraitDefinition(-1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new HuntersMarkTraitStatusSnapshot(-1, 1));
        }

        [Test]
        public void MarkEvent_RequiresRealChangeAndKeepsSerializedIdsStable()
        {
            var mark = new HuntersMarkState(new EntityId(90), Owner.Player, 25);
            Assert.Throws<ArgumentOutOfRangeException>(() => new CombatHuntersMarkEvent(default, mark, null));
            Assert.Throws<ArgumentException>(() => new CombatHuntersMarkEvent(new EntityId(2), null, null));
            Assert.Throws<ArgumentException>(() => new CombatHuntersMarkEvent(new EntityId(2), mark,
                new HuntersMarkState(mark.SourceId, mark.SourceOwner, mark.BonusDamagePercent)));
            var entry = new CombatHuntersMarkEvent(new EntityId(2), mark, null);
            Assert.That(entry.Expected, Is.SameAs(mark));
            Assert.That((int)entry.Kind, Is.EqualTo(39));
            Assert.That((int)CombatEventKind.EnemyPhaseEnded, Is.EqualTo(38));
            Assert.That(Enum.IsDefined(typeof(CombatEventKind), 30), Is.False);
            Assert.That((int)TraitKey.HuntersMark, Is.EqualTo(190));
        }

    }
}
