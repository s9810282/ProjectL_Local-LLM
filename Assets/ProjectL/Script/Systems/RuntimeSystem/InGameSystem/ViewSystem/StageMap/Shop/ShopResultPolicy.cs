using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>상점 요청 결과의 처리 분류입니다(설계 §2.1).</summary>
    internal enum ShopResultCategory
    {
        /// <summary>성공입니다.</summary>
        Success,
        /// <summary>정상 거절입니다. 문구를 보여 주고 팝업을 유지합니다.</summary>
        NormalRejection,
        /// <summary>상태 불일치입니다. 팝업을 닫고 현재 Buffer 상태로 복구합니다.</summary>
        Mismatch
    }

    /// <summary>
    /// Core가 돌려준 사유 값만으로 분류합니다. View가 규칙을 다시 계산하지 않습니다(상점 계획 S3.3, 2026-10-10 사용자 확인).
    /// </summary>
    internal static class ShopResultPolicy
    {
        /// <summary>구매 결과를 분류합니다. 구매 조건에 따른 거절은 정상 거절이고 나머지 실패는 상태 불일치입니다.</summary>
        internal static ShopResultCategory ClassifyPurchase(ShopFailureReason reason)
        {
            switch (reason)
            {
                case ShopFailureReason.None:
                    return ShopResultCategory.Success;
                case ShopFailureReason.InsufficientFunds:
                case ShopFailureReason.AlreadyOwned:
                case ShopFailureReason.ItemQuantityOverflow:
                case ShopFailureReason.PurchaseAlreadyUsed:
                case ShopFailureReason.SoldOut:
                case ShopFailureReason.DeckCountOverflow:
                    return ShopResultCategory.NormalRejection;
                case ShopFailureReason.TargetNotOwned:
                // 훈련: 그린 뒤 보유·등급이 바뀌었습니다. 닫고 지금 Buffer 상태로 다시 그립니다.
                case ShopFailureReason.TraitNotTrainable:
                case ShopFailureReason.RankLimitReached:
                case ShopFailureReason.StaleRank:
                case ShopFailureReason.StaleRoster:
                    return ShopResultCategory.Mismatch;
                default:
                    return ShopResultCategory.Mismatch;
            }
        }

        /// <summary>Core 거절 사유가 어떤 안내 글인지입니다.</summary>
        internal static StageOfferErrorKind ErrorOf(ShopFailureReason reason)
        {
            switch (reason)
            {
                case ShopFailureReason.InsufficientFunds:
                    return StageOfferErrorKind.InsufficientFunds;
                case ShopFailureReason.AlreadyOwned:
                    return StageOfferErrorKind.AlreadyOwned;
                case ShopFailureReason.ItemQuantityOverflow:
                    return StageOfferErrorKind.QuantityOverflow;
                case ShopFailureReason.PurchaseAlreadyUsed:
                    return StageOfferErrorKind.PurchaseUsed;
                case ShopFailureReason.SoldOut:
                    return StageOfferErrorKind.SoldOut;
                case ShopFailureReason.DeckCountOverflow:
                    return StageOfferErrorKind.DeckFull;
                default:
                    return StageOfferErrorKind.StateChanged;
            }
        }

        /// <summary>상태 불일치 중 내부 이상으로 보고 로그를 남겨야 하는 구매 사유입니다.</summary>
        internal static bool IsUnexpected(ShopFailureReason reason)
        {
            switch (reason)
            {
                case ShopFailureReason.UnknownOffer:
                case ShopFailureReason.DeckRevisionOverflow:
                case ShopFailureReason.RunRevisionOverflow:
                case ShopFailureReason.Disposed:
                case ShopFailureReason.InvalidItem:
                case ShopFailureReason.InvalidCharacter:
                case ShopFailureReason.IdentityOverflow:
                    return true;
                default:
                    return false;
            }
        }
    }
}
