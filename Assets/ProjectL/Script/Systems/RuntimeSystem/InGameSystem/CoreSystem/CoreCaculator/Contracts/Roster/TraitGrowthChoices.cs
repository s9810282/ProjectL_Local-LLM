using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster
{
    public sealed class TraitGrowthChoice
    {
        public TraitKey TraitKey { get; }
        public TraitGrowthKind Kind { get; }
        public string Name { get; }
        public string Description { get; }
        public TraitGrowthChoice(TraitKey traitKey, TraitGrowthKind kind, string name, string description)
        {
            if (traitKey == TraitKey.None || !Enum.IsDefined(typeof(TraitKey), traitKey))
                throw new ArgumentOutOfRangeException(nameof(traitKey));
            if (kind != TraitGrowthKind.Acquire && kind != TraitGrowthKind.Enhance)
                throw new ArgumentOutOfRangeException(nameof(kind));
            TraitKey = traitKey;
            Kind = kind;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }
    }
    public sealed class TraitGrowthChoices
    {
        public CharacterInstanceId OwnedId { get; }
        public IReadOnlyList<TraitGrowthChoice> Items { get; }
        public TraitGrowthChoices(CharacterInstanceId ownedId, IReadOnlyList<TraitGrowthChoice> items)
        {
            var copy = (items ?? throw new ArgumentNullException(nameof(items))).ToArray();
            if (copy.Any(c => c == null) || copy.Select(c => c.TraitKey).Distinct().Count() != copy.Length)
                throw new ArgumentException("중복 없는 성장 후보가 필요합니다.", nameof(items));
            OwnedId = ownedId;
            Items = Array.AsReadOnly(copy);
        }
    }
}
