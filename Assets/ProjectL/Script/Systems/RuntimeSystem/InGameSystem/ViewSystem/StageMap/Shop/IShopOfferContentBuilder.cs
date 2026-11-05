using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>한 상품 종류의 공개 사실을 카드 내용으로 바꾸는 내부 표시 계약입니다.</summary>
    internal interface IShopOfferContentBuilder
    {
        ShopProductKind Kind { get; }
        StageOfferCardContent Build(ShopPurchaseSnapshot offer, bool purchased, bool hasPurchase, StageOfferTextData text);
    }
}
