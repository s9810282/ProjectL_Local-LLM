using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.TraitGrowth.Item
{
    public sealed class TraitGrowthDefinitionData : IData
    {
        public TraitKey Key => Normal.TraitKey;
        public TraitGrowthGroup Group { get; }
        public UnitTraitData Normal { get; }
        public UnitTraitData Enhanced { get; }
        public TraitRankValues Rank2 { get; }
        public TraitRankValues Rank3 { get; }
        public bool AllowUnchangedRankValues { get; }
        public bool IsTrainable => Rank2 != null;
        public string Name { get; }
        public string Description { get; }
        public string EnhancedName { get; }
        public string EnhancedDescription { get; }

        public TraitGrowthDefinitionData(UnitTraitData normal, UnitTraitData enhanced, string name, string description,
            string enhancedName, string enhancedDescription, TraitRankValues rank2 = null, TraitRankValues rank3 = null,
            bool allowUnchangedRankValues = false)
            : this(TraitGrowthGroup.Common, normal, enhanced, name, description, enhancedName, enhancedDescription, rank2, rank3,
                allowUnchangedRankValues)
        {
        }

        public TraitGrowthDefinitionData(TraitGrowthGroup group, UnitTraitData normal, UnitTraitData enhanced, string name,
            string description, string enhancedName, string enhancedDescription, TraitRankValues rank2 = null,
            TraitRankValues rank3 = null, bool allowUnchangedRankValues = false)
        {
            if (group == TraitGrowthGroup.Unspecified || !System.Enum.IsDefined(typeof(TraitGrowthGroup), group))
                throw new ArgumentOutOfRangeException(nameof(group), group, "특성 성장 그룹을 명시해야 합니다.");
            if (normal == null) throw new ArgumentNullException(nameof(normal));
            normal.Validate();
            enhanced?.Validate();
            if (enhanced != null && normal.TraitKey != enhanced.TraitKey)
                throw new InvalidOperationException("강화형은 기본형과 같은 특성 키여야 합니다.");
            try
            {
                TraitGrowthDefinition.ValidateRankSlots(normal.TraitKey, rank2, rank3);
            }
            catch (ArgumentException exception)
            {
                throw new InvalidOperationException(exception.Message, exception);
            }
            Rank2 = rank2;
            Rank3 = rank3;
            AllowUnchangedRankValues = allowUnchangedRankValues;
            Group = group;
            Normal = normal;
            Enhanced = enhanced;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Description = description ?? throw new ArgumentNullException(nameof(description));
            EnhancedName = enhancedName ?? throw new ArgumentNullException(nameof(enhancedName));
            EnhancedDescription = enhancedDescription ?? throw new ArgumentNullException(nameof(enhancedDescription));
        }
    }
}
