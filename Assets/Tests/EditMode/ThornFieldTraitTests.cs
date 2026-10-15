using System;
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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
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
    public sealed class ThornFieldTraitTests
    {
        [Test]
        public void DefaultRegistry_RoundTripsDefinitionRuntimeCodecAndStatus()
        {
            var traitSpec = new ThornFieldTraitSpec(5, 5);

            TraitRegistry registry = TraitRegistry.CreateDefault();
            TraitRegistration registration = registry.GetRequiredRegistration(TraitKey.ThornField);
            Assert.That(registration.SpecType, Is.EqualTo(typeof(ThornFieldTraitSpec)));
            Assert.That(registration.DefinitionType, Is.EqualTo(typeof(ThornFieldTraitDefinition)));
            Assert.That(registration.RuntimeStateType, Is.EqualTo(typeof(ThornFieldRuntimeState)));

            var catalog = new UnitCatalog(new[] { CreateCactusSpec(traitSpec) }, registry);
            catalog.ValidateCombatReady();
            UnitDefinition definition = catalog.GetRequired(EntityKey.Unit_CactusCannon);
            Assert.That(definition.TraitDefinitions.Length, Is.EqualTo(1));
            var traitDefinition = (ThornFieldTraitDefinition)
                definition.TraitDefinitions.Span[0];
            Assert.That(traitDefinition.ThornDamage, Is.EqualTo(5));
            Assert.That(traitDefinition.LifetimeUnitTurns, Is.EqualTo(5));

            TraitRegistration entry = registry.GetRequiredRegistration(TraitKey.ThornField);
            var stateSet = new UnitTraitStateSet(new[]
            {
                entry.TraitRuntimeStateFactory.BuildRuntimeState(definition.TraitDefinitions.Span[0]),
            });
            var state = stateSet.GetRequired<ThornFieldRuntimeState>(
                TraitKey.ThornField);

            var clone = (ThornFieldRuntimeState)state.Clone();
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
            Assert.That(restored, Is.TypeOf<ThornFieldRuntimeState>());
            Assert.That(restored, Is.Not.SameAs(state));

            var provider = new TraitEffectProvider(registry);
            TraitStatusSnapshot[] statuses = provider.CreateStatuses(definition, stateSet);
            Assert.That(statuses.Length, Is.EqualTo(1));
            var status = (ThornFieldTraitStatusSnapshot)statuses[0];
            Assert.That(status.Key, Is.EqualTo(TraitKey.ThornField));
            Assert.That(status.ThornDamage, Is.EqualTo(5));
            Assert.That(status.LifetimeUnitTurns, Is.EqualTo(5));
        }

        [Test]
        public void StaticContracts_RejectNonPositiveValues()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThornFieldTraitSpec(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThornFieldTraitSpec(5, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThornFieldTraitDefinition(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThornFieldTraitDefinition(5, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThornFieldTraitStatusSnapshot(0, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new ThornFieldTraitStatusSnapshot(5, 0));
        }

        [Test]
        public void ApplicationCells_ExcludeCenter_AreOrdered_AndUseCheckedArithmetic()
        {
            CellCoord[] cells = ThornFieldTraitRule.CalculateApplicationCells(
                new CellCoord(2, 2));

            Assert.That(cells, Is.EqualTo(new[]
            {
                new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1),
                new CellCoord(1, 2),                      new CellCoord(3, 2),
                new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3),
            }));
            Assert.Throws<OverflowException>(() =>
                ThornFieldTraitRule.CalculateApplicationCells(
                    new CellCoord(int.MaxValue, 0)));
        }

        [Test]
        public void ApplicationCapability_EmptyBindings_ReturnsNoContribution()
        {
            EffectThornFieldEvaluation evaluation =
                new CombatEffectEngine().EvaluateThornFieldApplication(
                    Array.Empty<EffectRuleBinding>(),
                    new ThornFieldApplicationEffectContext(new CellCoord(2, 2)));

            Assert.That(evaluation.HasContribution, Is.False);
            Assert.That(evaluation.Contributions, Is.Empty);
            Assert.That(evaluation.Expectations, Is.Empty);
        }

        [Test]
        public void ApplicationCapability_TwoContributors_PreserveValuesInOriginOrder()
        {
            EffectOriginKey later = Origin(EffectOwnerKind.Artifact, 0, 2, "later");
            EffectOriginKey earlier = Origin(EffectOwnerKind.Category, 2, 1, "earlier");
            CellCoord[] laterCells = { new CellCoord(3, 2), new CellCoord(3, 3) };
            CellCoord[] earlierCells = { new CellCoord(1, 1) };
            IEffectStateSnapshot laterState = StatelessEffectStateSnapshot.Instance;
            IEffectStateSnapshot earlierState = new TestEffectStateSnapshot();

            EffectThornFieldEvaluation evaluation =
                new CombatEffectEngine().EvaluateThornFieldApplication(
                    new[]
                    {
                        new EffectRuleBinding(
                            later,
                            new FixedThornFieldRule(9, 4, laterCells),
                            laterState),
                        new EffectRuleBinding(
                            earlier,
                            new FixedThornFieldRule(5, 7, earlierCells),
                            earlierState),
                    },
                    new ThornFieldApplicationEffectContext(new CellCoord(2, 2)));

            Assert.That(evaluation.HasContribution, Is.True);
            Assert.That(evaluation.Contributions.Count, Is.EqualTo(2));
            Assert.That(evaluation.Contributions[0].Origin, Is.EqualTo(earlier));
            Assert.That(evaluation.Contributions[0].ThornDamage, Is.EqualTo(5));
            Assert.That(evaluation.Contributions[0].LifetimeUnitTurns, Is.EqualTo(7));
            Assert.That(evaluation.Contributions[0].ApplicationCells,
                Is.EqualTo(earlierCells));
            Assert.That(evaluation.Contributions[1].Origin, Is.EqualTo(later));
            Assert.That(evaluation.Contributions[1].ThornDamage, Is.EqualTo(9));
            Assert.That(evaluation.Contributions[1].LifetimeUnitTurns, Is.EqualTo(4));
            Assert.That(evaluation.Contributions[1].ApplicationCells,
                Is.EqualTo(laterCells));
            Assert.That(evaluation.Expectations.Count, Is.EqualTo(2));
            Assert.That(evaluation.Expectations[0].Origin, Is.EqualTo(earlier));
            Assert.That(evaluation.Expectations[0].ExpectedState, Is.SameAs(earlierState));
            Assert.That(evaluation.Expectations[1].Origin, Is.EqualTo(later));
            Assert.That(evaluation.Expectations[1].ExpectedState, Is.SameAs(laterState));
        }

        private static UnitSpec CreateCactusSpec(TraitSpec trait)
        {
            return UnitSpecTestFactory.Create(
                EntityKey.Unit_CactusCannon,
                footprintWidth: 1,
                footprintHeight: 1,
                footprintLocalCells: new[] { CellCoord.Zero },
                maxHealth: 20,
                MoveMethod.LiftAndPlace,
                moveRange: 3,
                moveCost: 1,
                3,
                attackPower: 15,
                maxActionCost: 5,
                attackCost: 4,
                priority: 9,
                delay: 50,
                traits: new[] { trait },
                categoryKey: CategoryKey.Cannon,
                ownerEligibility: UnitOwnerEligibility.PlayerOnly);
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

        private sealed class FixedThornFieldRule : IEffectThornFieldRule
        {
            private readonly int _thornDamage;
            private readonly int _lifetimeUnitTurns;
            private readonly CellCoord[] _applicationCells;

            internal FixedThornFieldRule(
                int thornDamage,
                int lifetimeUnitTurns,
                CellCoord[] applicationCells)
            {
                _thornDamage = thornDamage;
                _lifetimeUnitTurns = lifetimeUnitTurns;
                _applicationCells = applicationCells;
            }

            public EffectThornFieldContribution EvaluateThornFieldApplication(
                in EffectRuleInput input,
                in ThornFieldApplicationEffectContext context)
            {
                return new EffectThornFieldContribution(
                    input.Origin,
                    _thornDamage,
                    _lifetimeUnitTurns,
                    _applicationCells);
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
