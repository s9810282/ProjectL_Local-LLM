using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects
{
    internal static class TraitEffectOrigin
    {
        internal static EffectOriginKey Create(EntityId unitId, TraitKey key)
        {
            ValidateUnitId(unitId);
            ValidateTraitKey(key);
            return new EffectOriginKey(EffectOwnerKind.Trait, unitId.Value, (int)key, "main");
        }

        internal static TraitKey GetRequiredTraitKey(EffectOriginKey origin, EntityId unitId)
        {
            ValidateUnitId(unitId);
            if (origin.IsValid == false ||
                origin.Kind != EffectOwnerKind.Trait ||
                origin.OwnerInstanceId != unitId.Value ||
                string.Equals(origin.RuleKey, "main", StringComparison.Ordinal) == false)
            {
                throw new ArgumentException("Invalid Trait effect origin", nameof(origin));
            }

            var key = (TraitKey)origin.DefinitionKey;
            ValidateTraitKey(key);
            return key;
        }

        internal static TraitKey[] GetTraitContributors(
            EffectOriginKey boundOrigin,
            IReadOnlyList<EffectOriginKey> origins)
        {
            if (origins == null) throw new ArgumentNullException(nameof(origins));
            if (boundOrigin.OwnerInstanceId <= 0)
                throw new ArgumentException("Trait owner must be a valid unit.", nameof(boundOrigin));

            var unitId = new EntityId(boundOrigin.OwnerInstanceId);
            var contributors = new List<TraitKey>();
            for (int i = 0; i < origins.Count; i++)
            {
                if (origins[i].Kind != EffectOwnerKind.Trait) continue;
                contributors.Add(GetRequiredTraitKey(origins[i], unitId));
            }
            return contributors.ToArray();
        }

        private static void ValidateUnitId(EntityId unitId)
        {
            if (unitId.IsEmpty) throw new ArgumentException("Unit ID cannot be empty.", nameof(unitId));
        }

        private static void ValidateTraitKey(TraitKey key)
        {
            if (key == TraitKey.None || Enum.IsDefined(typeof(TraitKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
        }
    }
}
