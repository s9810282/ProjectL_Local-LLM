using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    public sealed class TrainingReceiptSnapshot
    {
        public CharacterInstanceId OwnedId { get; }
        public TraitKey TraitKey { get; }
        public int PreviousRank { get; }
        public int FinalRank => PreviousRank + 1;
        public int Price { get; }
        public TrainingReceiptSnapshot(CharacterInstanceId ownedId, TraitKey traitKey, int previousRank, int price)
        {
            _ = new TrainingChoiceSnapshot(ownedId, traitKey, previousRank, price, true);
            OwnedId = ownedId;
            TraitKey = traitKey;
            PreviousRank = previousRank;
            Price = price;
        }
    }
}
