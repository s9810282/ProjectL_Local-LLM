using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>아이템 상품의 게시된 이름·설명·가격과 View 아이콘을 표시합니다.</summary>
    internal sealed class ItemShopOfferContentBuilder : IShopOfferContentBuilder
    {
        private readonly Func<int, Sprite> _findIcon;

        internal ItemShopOfferContentBuilder(Func<int, Sprite> findIcon)
        {
            _findIcon = findIcon;
        }

        public ShopProductKind Kind => ShopProductKind.ConsumableItem;

        public StageOfferCardContent Build(ShopPurchaseSnapshot offer, bool purchased, bool hasPurchase, StageOfferTextData text)
        {
            return new StageOfferCardContent(_findIcon?.Invoke(offer.ItemKey), offer.ItemName,
                text.FormatPrice(offer.Cost), text.FormatLine(StageOfferLineKind.ItemDetails, offer.ItemDescription),
                purchased ? text.CompletedButtonText : text.SelectButtonText, !hasPurchase, purchased);
        }
    }
}
