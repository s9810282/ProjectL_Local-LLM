using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public sealed class TraitGrowthDefinition
    {
        public TraitKey Key => Normal.TraitKey;
        public TraitGrowthGroup Group { get; }
        public TraitSpec Normal { get; }
        public TraitSpec Enhanced { get; }
        public TraitRankValues Rank2 { get; }
        public TraitRankValues Rank3 { get; }
        public bool AllowUnchangedRankValues { get; }
        public int MaxRank { get; }
        public bool IsTrainable => MaxRank > 1;
        public string Name { get; }
        public string Description { get; }
        public string EnhancedName { get; }
        public string EnhancedDescription { get; }
        public bool CanAdvanceFrom(int rank)
        {
            if (rank < 1 || rank > 3)
                throw new ArgumentOutOfRangeException(nameof(rank), rank, $"지원하지 않는 특성 등급입니다: {Key}.");
            return rank < MaxRank;
        }

        public static int ValidateRankSlots(TraitKey key, TraitRankValues rank2, TraitRankValues rank3)
        {
            if (rank2 == null)
            {
                if (rank3 != null)
                    throw new ArgumentException($"3등급을 지정하려면 2등급이 필요합니다: {key}.", nameof(rank3));
                return 1;
            }
            if (rank2.Key != key)
                throw new ArgumentException($"2등급과 기본 특성의 키가 다릅니다: {key}, {rank2.Key}.", nameof(rank2));
            if (rank3 == null)
                return 2;
            if (rank3.Key != key)
                throw new ArgumentException($"3등급과 기본 특성의 키가 다릅니다: {key}, {rank3.Key}.", nameof(rank3));
            return 3;
        }

        internal TraitRankValues ValuesAt(int rank)
        {
            if (rank < 1 || rank > MaxRank)
                throw new InvalidOperationException($"정의된 특성 등급 범위를 벗어났습니다: {Key}, Rank={rank}, MaxRank={MaxRank}.");
            return rank switch
            {
                1 => TraitRankValues.FromNormal(Normal),
                2 => Rank2,
                _ => Rank3
            };
        }

        internal TraitSpec ResolveRank(TraitSpec source, int rank) => ValuesAt(rank).Apply(source, Normal);

        public TraitGrowthDefinition(TraitSpec normal, TraitSpec enhanced, string name, string description,
            string enhancedName, string enhancedDescription, TraitRankValues rank2 = null, TraitRankValues rank3 = null,
            bool allowUnchangedRankValues = false)
            : this(TraitGrowthGroup.Common, normal, enhanced, name, description, enhancedName, enhancedDescription, rank2, rank3,
                allowUnchangedRankValues)
        {
        }

        public TraitGrowthDefinition(TraitGrowthGroup group, TraitSpec normal, TraitSpec enhanced, string name,
            string description, string enhancedName, string enhancedDescription, TraitRankValues rank2 = null,
            TraitRankValues rank3 = null, bool allowUnchangedRankValues = false)
        {
            if (group == TraitGrowthGroup.Unspecified || !Enum.IsDefined(typeof(TraitGrowthGroup), group))
                throw new ArgumentOutOfRangeException(nameof(group), group, "특성 성장 그룹을 명시해야 합니다.");
            if (normal == null) throw new ArgumentNullException(nameof(normal));
            if (enhanced != null && normal.TraitKey != enhanced.TraitKey)
                throw new ArgumentException("강화형은 기본형과 같은 특성 키여야 합니다.", nameof(enhanced));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (enhancedName == null) throw new ArgumentNullException(nameof(enhancedName));
            if (enhancedDescription == null) throw new ArgumentNullException(nameof(enhancedDescription));
            MaxRank = ValidateRankSlots(normal.TraitKey, rank2, rank3);
            Rank2 = rank2;
            Rank3 = rank3;
            AllowUnchangedRankValues = allowUnchangedRankValues;
            Group = group;
            Normal = normal;
            Enhanced = enhanced;
            Name = name;
            Description = description;
            EnhancedName = enhancedName;
            EnhancedDescription = enhancedDescription;

            if (!IsTrainable)
                return;
            var previous = ResolveRank(Normal, 1);
            for (int rank = 2; rank <= MaxRank; rank++)
            {
                var current = ResolveRank(Normal, rank);
                if (!AllowUnchangedRankValues && previous.SameValues(current))
                    throw new ArgumentException($"이전 등급과 특성 효과가 같습니다: {Key}, Rank={rank}.");
                previous = current;
            }
        }
    }
}
