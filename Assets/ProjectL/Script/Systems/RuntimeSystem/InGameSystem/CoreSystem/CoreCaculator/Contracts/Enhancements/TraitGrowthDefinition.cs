using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements
{
    public sealed class TraitGrowthDefinition
    {
        public TraitKey Key => Normal.TraitKey;
        public TraitSpec Normal { get; }
        public TraitSpec Enhanced { get; }
        public TraitRankValues Rank2 { get; }
        public TraitRankValues Rank3 { get; }
        public bool IsTrainable => Rank2 != null;
        public string Name { get; }
        public string Description { get; }
        public string EnhancedName { get; }
        public string EnhancedDescription { get; }
        internal TraitRankValues ValuesAt(int rank) => rank switch
        {
            1 => TraitRankValues.FromNormal(Normal),
            2 => Rank2,
            3 => Rank3,
            _ => throw new InvalidOperationException("특성 등급이 범위를 벗어났습니다.")
        };

        internal TraitSpec ResolveRank(TraitSpec source, int rank) => ValuesAt(rank).Apply(source, Normal);

        public TraitGrowthDefinition(TraitSpec normal, TraitSpec enhanced, string name, string description,
            string enhancedName, string enhancedDescription, TraitRankValues rank2 = null, TraitRankValues rank3 = null)
        {
            if (normal == null) throw new ArgumentNullException(nameof(normal));
            if (enhanced != null && normal.TraitKey != enhanced.TraitKey)
                throw new ArgumentException("강화형은 기본형과 같은 특성 키여야 합니다.", nameof(enhanced));
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (description == null) throw new ArgumentNullException(nameof(description));
            if (enhancedName == null) throw new ArgumentNullException(nameof(enhancedName));
            if (enhancedDescription == null) throw new ArgumentNullException(nameof(enhancedDescription));
            if ((rank2 == null) != (rank3 == null) || rank2 != null &&
                (rank2.Key != normal.TraitKey || rank3.Key != normal.TraitKey))
                throw new ArgumentException("2·3등급 정의는 같은 특성 키로 함께 지정해야 합니다.");
            if (rank2 != null) _ = TraitRankValues.FromNormal(normal);
            Rank2 = rank2;
            Rank3 = rank3;
            Normal = normal;
            Enhanced = enhanced;
            Name = name;
            Description = description;
            EnhancedName = enhancedName;
            EnhancedDescription = enhancedDescription;
        }
    }
}
