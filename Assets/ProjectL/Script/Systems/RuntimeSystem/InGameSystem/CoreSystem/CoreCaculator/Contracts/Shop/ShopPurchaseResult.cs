using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop
{
    public sealed class ShopPurchaseResult
    {
        public bool Succeeded => FailureReason == ShopFailureReason.None;
        public ShopFailureReason FailureReason { get; }
        public ShopProductKind Kind { get; }
        public RosterAcquireResult UnitAcquisition { get; }
        public int ItemKey { get; }
        public CharacterInstanceId TargetOwnedId { get; }
        public OwnedEnhancement Enhancement { get; }
        public TrainingReceiptSnapshot Training { get; }

        private ShopPurchaseResult(TrainingReceiptSnapshot training)
        {
            Training = training ?? throw new ArgumentNullException(nameof(training));
            TargetOwnedId = training.OwnedId;
            Kind = ShopProductKind.Training;
        }

        public static ShopPurchaseResult ForTraining(TrainingReceiptSnapshot receipt) => new(receipt);

        private ShopPurchaseResult(ShopFailureReason reason, ShopProductKind kind,
            RosterAcquireResult acquisition, int itemKey, CharacterInstanceId targetId = default, OwnedEnhancement enhancement = null)
        {
            FailureReason = reason;
            Kind = kind;
            UnitAcquisition = acquisition;
            ItemKey = itemKey;
            TargetOwnedId = targetId;
            Enhancement = enhancement;
        }

        public static ShopPurchaseResult ForUnit(RosterAcquireResult acquisition)
        {
            if (acquisition == null) throw new ArgumentNullException(nameof(acquisition));
            if (!acquisition.Succeeded || acquisition.Acquired.Count != 1)
                throw new ArgumentException("캐릭터 한 명의 성공한 획득 결과가 필요합니다.", nameof(acquisition));
            return new ShopPurchaseResult(ShopFailureReason.None, ShopProductKind.Unit, acquisition, 0);
        }

        public static ShopPurchaseResult ForItem(int key)
        {
            if (key < 1) throw new ArgumentOutOfRangeException(nameof(key));
            return new ShopPurchaseResult(ShopFailureReason.None, ShopProductKind.ConsumableItem, null, key);
        }

        public static ShopPurchaseResult ForEnhancement(CharacterInstanceId targetId, OwnedEnhancement enhancement)
        {
            if (targetId.IsEmpty) throw new ArgumentException(nameof(targetId));
            if (enhancement == null) throw new ArgumentNullException(nameof(enhancement));
            if (enhancement.Level != 1) throw new ArgumentOutOfRangeException(nameof(enhancement));
            return new ShopPurchaseResult(ShopFailureReason.None, ShopProductKind.Enhancement, null, 0, targetId, enhancement);
        }

        public static ShopPurchaseResult Failure(ShopFailureReason reason)
        {
            if (reason == ShopFailureReason.None || !Enum.IsDefined(typeof(ShopFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            return new ShopPurchaseResult(reason, ShopProductKind.None, null, 0);
        }
    }
}
