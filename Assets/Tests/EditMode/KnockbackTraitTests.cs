using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States;

namespace ProjectL.Tests.Core
{
    public sealed class KnockbackTraitTests
    {
        [Test]
        public void DistanceContract_RoundTripsRegistryFactoryCodecAndStatus()
        {
            Assert.That(new KnockbackTraitSpec(1).DistanceCells, Is.EqualTo(1));
            Assert.That(new KnockbackTraitDefinition(3).DistanceCells, Is.EqualTo(3));
            Assert.That(new KnockbackTraitStatusSnapshot(3).DistanceCells, Is.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => new KnockbackTraitSpec(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new KnockbackTraitDefinition(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new KnockbackTraitStatusSnapshot(0));

            TraitRegistry registry = TraitRegistry.CreateDefault();
            TraitRegistration registration = registry.GetRequiredRegistration(TraitKey.Knockback);
            Assert.That(registration.SpecType, Is.EqualTo(typeof(KnockbackTraitSpec)));
            Assert.That(registration.DefinitionType, Is.EqualTo(typeof(KnockbackTraitDefinition)));
            Assert.That(registration.RuntimeStateType, Is.EqualTo(typeof(KnockbackRuntimeState)));

            var catalog = new UnitCatalog(
                new[] { CreateSpec(new KnockbackTraitSpec(3)) },
                registry);
            UnitDefinition definition = catalog.GetRequired(EntityKey.Unit_Akimbo);
            var traitDefinition = (KnockbackTraitDefinition)
                definition.TraitDefinitions.Span[0];
            Assert.That(traitDefinition.DistanceCells, Is.EqualTo(3));

            TraitRegistration entry = registry.GetRequiredRegistration(TraitKey.Knockback);
            var stateSet = new UnitTraitStateSet(new[]
            {
                entry.TraitRuntimeStateFactory.BuildRuntimeState(definition.TraitDefinitions.Span[0]),
            });
            var state = stateSet.GetRequired<KnockbackRuntimeState>(
                TraitKey.Knockback);
            var clone = (KnockbackRuntimeState)state.Clone();
            Assert.That(clone, Is.Not.SameAs(state));
            state.CopyFinalValuesFrom(clone);
            Assert.Throws<ArgumentNullException>(() => state.CopyFinalValuesFrom(null));
            Assert.Throws<ArgumentException>(() =>
                state.CopyFinalValuesFrom(new FocusRuntimeState()));

            IEffectStateSnapshot captured = TraitEffectStateCodec.Capture(state);
            IEffectStateSnapshot secondCapture = TraitEffectStateCodec.Capture(clone);
            Assert.That(captured.ValueEquals(secondCapture), Is.True);
            Assert.That(
                captured.ValueEquals(StatelessEffectStateSnapshot.Instance),
                Is.False);
            TraitRuntimeState restored = TraitEffectStateCodec.Restore(captured);
            Assert.That(restored, Is.TypeOf<KnockbackRuntimeState>());
            Assert.That(restored, Is.Not.SameAs(state));

            var provider = new TraitEffectProvider(registry);
            TraitStatusSnapshot[] statuses = provider.CreateStatuses(definition, stateSet);
            Assert.That(statuses, Has.Length.EqualTo(1));
            var status = (KnockbackTraitStatusSnapshot)statuses[0];
            Assert.That(status.Key, Is.EqualTo(TraitKey.Knockback));
            Assert.That(status.DistanceCells, Is.EqualTo(3));
        }

        [Test]
        public void Capability_IsOrderedOwnerNeutralAndRejectsMalformedContributions()
        {
            var engine = new CombatEffectEngine();
            EffectKnockbackEvaluation empty = engine.EvaluateKnockback(
                Array.Empty<EffectRuleBinding>());
            Assert.That(empty.HasContribution, Is.False);
            Assert.That(empty.Contributions, Is.Empty);
            Assert.That(empty.Expectations, Is.Empty);

            EffectOriginKey later = Origin(EffectOwnerKind.Artifact, 0, 2, "later");
            EffectOriginKey earlier = Origin(EffectOwnerKind.Category, 2, 1, "earlier");
            IEffectStateSnapshot laterState = StatelessEffectStateSnapshot.Instance;
            IEffectStateSnapshot earlierState = new TestEffectStateSnapshot();
            EffectKnockbackEvaluation evaluation = engine.EvaluateKnockback(new[]
            {
                new EffectRuleBinding(later, new FixedKnockbackRule(3), laterState),
                new EffectRuleBinding(earlier, new FixedKnockbackRule(1), earlierState),
            });

            Assert.That(evaluation.HasContribution, Is.True);
            Assert.That(
                evaluation.Contributions.Select(item => item.Origin),
                Is.EqualTo(new[] { earlier, later }));
            Assert.That(
                evaluation.Contributions.Select(item => item.DistanceCells),
                Is.EqualTo(new[] { 1, 3 }));
            Assert.That(
                evaluation.Contributions.Max(item => item.DistanceCells),
                Is.EqualTo(3));
            Assert.That(evaluation.Expectations[0].Origin, Is.EqualTo(earlier));
            Assert.That(evaluation.Expectations[0].ExpectedState, Is.SameAs(earlierState));
            Assert.That(evaluation.Expectations[1].Origin, Is.EqualTo(later));
            Assert.That(evaluation.Expectations[1].ExpectedState, Is.SameAs(laterState));

            Assert.Throws<InvalidOperationException>(() => engine.EvaluateKnockback(new[]
            {
                new EffectRuleBinding(
                    earlier,
                    new FixedKnockbackRule(1, later),
                    earlierState),
            }));
            Assert.Throws<InvalidOperationException>(() => engine.EvaluateKnockback(new[]
            {
                new EffectRuleBinding(earlier, new MalformedKnockbackRule(), earlierState),
            }));

            var definition = new KnockbackTraitDefinition(1);
            var boundRule = new KnockbackEffectRule(definition, new KnockbackTraitRule());
            EffectOriginKey traitOrigin = TraitEffectOrigin.Create(
                new EntityId(7),
                TraitKey.Knockback);
            Assert.Throws<ArgumentException>(() => engine.EvaluateKnockback(new[]
            {
                new EffectRuleBinding(
                    traitOrigin,
                    boundRule,
                    StatelessEffectStateSnapshot.Instance),
            }));
        }

        private static UnitSpec CreateSpec(TraitSpec trait)
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_Akimbo,
                1,
                1,
                new[] { CellCoord.Zero },
                10,
                MoveMethod.LiftAndPlace,
                5,
                1,
                4,
                3,
                8,
                2,
                30,
                35,
                new[] { trait });
        }

        private static EffectOriginKey Origin(
            EffectOwnerKind kind,
            int ownerInstanceId,
            int definitionKey,
            string ruleKey)
        {
            return new EffectOriginKey(
                kind,
                ownerInstanceId,
                definitionKey,
                ruleKey);
        }

        private sealed class FixedKnockbackRule : IEffectKnockbackRule
        {
            private readonly int _distanceCells;
            private readonly EffectOriginKey? _originOverride;

            internal FixedKnockbackRule(
                int distanceCells,
                EffectOriginKey? originOverride = null)
            {
                _distanceCells = distanceCells;
                _originOverride = originOverride;
            }

            public EffectKnockbackContribution EvaluateKnockback(in EffectRuleInput input)
            {
                return new EffectKnockbackContribution(
                    _originOverride ?? input.Origin,
                    _distanceCells);
            }
        }

        private sealed class MalformedKnockbackRule : IEffectKnockbackRule
        {
            public EffectKnockbackContribution EvaluateKnockback(in EffectRuleInput input)
            {
                return default;
            }
        }

        private sealed class TestEffectStateSnapshot : IEffectStateSnapshot
        {
            public bool ValueEquals(IEffectStateSnapshot other)
            {
                return ReferenceEquals(this, other);
            }
        }
    }
}
