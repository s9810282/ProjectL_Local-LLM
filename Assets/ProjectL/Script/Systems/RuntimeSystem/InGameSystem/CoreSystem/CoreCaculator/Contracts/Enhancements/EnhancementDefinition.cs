using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public sealed class EnhancementDefinition
    {
        public EnhancementKey Key { get; }
        public EntityKey CharacterKey { get; }
        public int Level { get; }
        public int Cost { get; }
        public int Weight { get; }
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<TraitSpec> AddedTraits { get; }
        public IReadOnlyList<TraitSpec> ReplacedTraits { get; }

        public EnhancementDefinition(EnhancementKey key, EntityKey characterKey, int level, int cost, int weight,
            string name, string description, IReadOnlyList<TraitSpec> addedTraits,
            IReadOnlyList<TraitSpec> replacedTraits)
        {
            _ = new OwnedEnhancement(key, level);
            if (level != 1) throw new ArgumentOutOfRangeException(nameof(level));
            if (characterKey == EntityKey.None || !System.Enum.IsDefined(typeof(EntityKey), characterKey))
                throw new ArgumentOutOfRangeException(nameof(characterKey));
            if (cost < 0) throw new ArgumentOutOfRangeException(nameof(cost));
            if (weight < 1) throw new ArgumentOutOfRangeException(nameof(weight));
            Key = key;
            CharacterKey = characterKey;
            Level = level;
            Cost = cost;
            Weight = weight;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            AddedTraits = Array.AsReadOnly((addedTraits ?? throw new ArgumentNullException(nameof(addedTraits))).ToArray());
            ReplacedTraits = Array.AsReadOnly((replacedTraits ?? throw new ArgumentNullException(nameof(replacedTraits))).ToArray());

            var traits = AddedTraits.Concat(ReplacedTraits).ToArray();
            if (traits.Any(t => t == null) || traits.Select(t => t.TraitKey).Distinct().Count() != traits.Length)
                throw new ArgumentException("추가·교체 특성 키는 비어 있거나 중복될 수 없습니다.");
        }
    }
}
