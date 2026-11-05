using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop
{
    public sealed class TrainingPurchaseDecision
    {
        public ShopFailureReason FailureReason { get; }
        public bool Succeeded => FailureReason == ShopFailureReason.None;
        public OwnedCharacterRecord Final { get; }
        public int FinalBalance { get; }
        public TrainingReceiptSnapshot Receipt { get; }
        private TrainingPurchaseDecision(ShopFailureReason failure, OwnedCharacterRecord final, int balance, TrainingReceiptSnapshot receipt)
        {
            FailureReason = failure;
            Final = final;
            FinalBalance = balance;
            Receipt = receipt;
        }
        internal static TrainingPurchaseDecision Failure(ShopFailureReason reason) => new(reason, null, 0, null);
        internal static TrainingPurchaseDecision Success(OwnedCharacterRecord final, int balance, TrainingReceiptSnapshot receipt) =>
            new(ShopFailureReason.None, final, balance, receipt);
    }
}
