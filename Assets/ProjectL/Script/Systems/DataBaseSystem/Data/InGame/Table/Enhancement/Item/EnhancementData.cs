using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Enhancement.Item
{
    public sealed class EnhancementData : IData
    {
        public EnhancementKey Key { get; }
        public EntityKey CharacterKey { get; }
        public int Level { get; }
        public int Cost { get; }
        public int Weight { get; }
        public string Name { get; }
        public string Description { get; }
        public IReadOnlyList<UnitTraitData> AddedTraits { get; }
        public IReadOnlyList<UnitTraitData> ReplacedTraits { get; }

        public EnhancementData(EnhancementKey key, EntityKey characterKey, int level, int cost, int weight,
            string name, string description, IReadOnlyList<UnitTraitData> addedTraits,
            IReadOnlyList<UnitTraitData> replacedTraits)
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

            Validate();
        }

        public void Validate()
        {
            var traits = AddedTraits.Concat(ReplacedTraits).ToArray();
            if (traits.Any(t => t == null) || traits.Select(t => t.TraitKey).Distinct().Count() != traits.Length)
                throw new ArgumentException("추가·교체 특성 키는 비어 있거나 중복될 수 없습니다.");
            foreach (var trait in traits) trait.Validate();
        }
    }
}
