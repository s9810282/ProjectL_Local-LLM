using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class TrainingChoiceSnapshot
    {
        public CharacterInstanceId OwnedId { get; }
        public TraitKey TraitKey { get; }
        public int CurrentRank { get; }
        public int NextRank => CurrentRank + 1;
        public int Price { get; }
        public bool CanPurchase { get; }
        /// <summary>지금 등급의 수치입니다. 표시용이며 훈련 선택지가 아니면 null입니다.</summary>
        public TraitRankValues CurrentValues { get; }
        /// <summary>다음 등급의 수치입니다. 표시용이며 훈련 선택지가 아니면 null입니다.</summary>
        public TraitRankValues NextValues { get; }
        /// <summary>특성 이름입니다. 증강 제안과 같은 Core 글입니다.</summary>
        public string Name { get; }
        /// <summary>특성 설명입니다. 증강 제안과 같은 Core 글입니다.</summary>
        public string Description { get; }
        public TrainingChoiceSnapshot(CharacterInstanceId ownedId, TraitKey traitKey, int currentRank, int price, bool canPurchase,
            TraitRankValues currentValues = null, TraitRankValues nextValues = null,
            string name = null, string description = null)
        {
            if (ownedId.IsEmpty || traitKey == TraitKey.None || !Enum.IsDefined(typeof(TraitKey), traitKey) ||
                currentRank < 1 || currentRank > 2 || price < 1)
                throw new ArgumentException("훈련 선택지의 개체·특성·등급·가격이 유효해야 합니다.");
            OwnedId = ownedId;
            TraitKey = traitKey;
            CurrentRank = currentRank;
            Price = price;
            CanPurchase = canPurchase;
            CurrentValues = currentValues;
            NextValues = nextValues;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }
}
