using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels
{
    internal sealed class UnitCatalog
    {
        private readonly Dictionary<EntityKey, UnitDefinition> _definitions;
        private readonly TraitRegistry _traitRegistry;
        private readonly CategoryRegistry _categoryRegistry;

        public UnitCatalog(IReadOnlyList<UnitSpec> specs, TraitRegistry traitRegistry)
            : this(specs, traitRegistry, CategoryRegistry.CreateDefault())
        {
        }

        internal UnitCatalog(
            IReadOnlyList<UnitSpec> specs,
            TraitRegistry traitRegistry,
            CategoryRegistry categoryRegistry)
        {
            if (specs == null)
                throw new ArgumentNullException(nameof(specs));
            if (traitRegistry == null)
                throw new ArgumentNullException(nameof(traitRegistry));
            if (categoryRegistry == null)
                throw new ArgumentNullException(nameof(categoryRegistry));
            if (specs.Count == 0)
                throw new ArgumentException("유닛의 카탈로그는 비어있을 수 없습니다.", nameof(specs));

            _traitRegistry = traitRegistry;
            _categoryRegistry = categoryRegistry;
            _definitions = new Dictionary<EntityKey, UnitDefinition>(specs.Count);

            for (int i = 0; i < specs.Count; i++)
            {
                UnitSpec spec = specs[i];
                if (spec == null)
                    throw new ArgumentException("spec는 null일 수 없습니다.", nameof(specs));

                if (_definitions.ContainsKey(spec.Key) == true)
                    throw new ArgumentException($"중복된 키입니다 : {spec.Key}", nameof(specs));

                _definitions.Add(spec.Key, CreateDefinition(spec));
            }
        }

        public UnitDefinition GetRequired(EntityKey key)
        {
            if (_definitions.TryGetValue(key, out var definition) == false)
                throw new ArgumentException($"유닛의 키가 레지스터에 없습니다 : {key}", nameof(key));

            return definition;
        }

        public bool TryGet(EntityKey key, out UnitDefinition definition) =>
            _definitions.TryGetValue(key, out definition);

        public void ValidateCombatReady()
        {
            foreach (UnitDefinition definition in _definitions.Values)
            {
                if (definition.HasTimelineConfiguration == false)
                    throw new InvalidOperationException(
                        $"Unit '{definition.Key}' has no Timeline Priority/Delay configuration.");

                if (definition.MoveMethod != MoveMethod.LiftAndPlace)
                    throw new InvalidOperationException(
                        $"Unit '{definition.Key}'의 MoveMethod는 LiftAndPlace여야 합니다.");

                if (definition.MoveCost <= 0)
                    throw new InvalidOperationException(
                        $"Unit '{definition.Key}'의 MoveCost는 1 이상이어야 합니다.");

                if (definition.AttackCost <= 0)
                    throw new InvalidOperationException(
                        $"Unit '{definition.Key}'의 AttackCost는 1 이상이어야 합니다.");
            }
        }

        private UnitDefinition CreateDefinition(UnitSpec spec)
        {
            TraitDefinition[] traitDefinitions = new TraitDefinition[spec.Traits.Length];
            ReadOnlySpan<TraitSpec> traitSpecs = spec.Traits.Span;

            for (int i = 0; i < traitSpecs.Length; i++)
            {
                TraitSpec traitSpec = traitSpecs[i];

                TraitRegistration registration = _traitRegistry.GetRequiredRegistration(traitSpec.TraitKey);

                if (traitSpec.GetType() != registration.SpecType)
                    throw new InvalidOperationException($"{traitSpec.TraitKey}의 Spec 타입이 Registration과 일치하지 않습니다.");

                TraitDefinition traitDefinition = registration.TraitDefinitionFactory.BuildDefinition(traitSpec);

                ValidateTraitDefinition(traitDefinition);

                traitDefinitions[i] = traitDefinition;
            }

            Array.Sort(traitDefinitions, (left, right) => left.TraitKey.CompareTo(right.TraitKey));

            CellCoord[] cells = spec.FootprintLocalCells.ToArray();
            TileFootprint footprint = new TileFootprint(spec.FootprintWidth, spec.FootprintHeight, cells);
            CategoryDefinition category = _categoryRegistry.GetRequired(spec.CategoryKey);

            return new UnitDefinition(
                spec.Key,
                spec.Role,
                footprint,
                spec.MaxHealth,
                spec.MoveMethod,
                spec.MoveRange,
                spec.MoveCost,
                spec.AttackRange,
                category,
                spec.AttackPower,
                spec.MaxActionCost,
                spec.AttackCost,
                spec.PhysicalDefense,
                spec.Priority,
                spec.Delay,
                traitDefinitions,
                hasTimelineConfiguration: true,
                ownerEligibility: spec.OwnerEligibility,
                occlusion: spec.Occlusion);
        }

        private static void ValidateTraitDefinition(TraitDefinition definition)
        {
            if (definition is FocusTraitDefinition focus)
            {
                long maxBonusDamage = (long)focus.DamagePerStack * focus.MaxStacks;

                if (maxBonusDamage > int.MaxValue)
                    throw new ArgumentException("Focus 특성의 최대 추가 피해가 int 범위를 초과합니다.");
            }
        }
    }
}
