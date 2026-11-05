using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>강화의 게시된 내용과 대상 View 이름·얼굴만 카드로 옮깁니다.</summary>
    internal sealed class EnhancementShopOfferContentBuilder : IShopOfferContentBuilder
    {
        private readonly Func<EntityKey, string> _findName;
        private readonly Func<EntityKey, Sprite> _findIcon;

        internal EnhancementShopOfferContentBuilder(Func<EntityKey, string> findName, Func<EntityKey, Sprite> findIcon)
        {
            _findName = findName ?? throw new ArgumentNullException(nameof(findName));
            _findIcon = findIcon ?? throw new ArgumentNullException(nameof(findIcon));
        }

        public ShopProductKind Kind => ShopProductKind.Enhancement;

        public StageOfferCardContent Build(ShopPurchaseSnapshot offer, bool purchased, bool hasPurchase, StageOfferTextData text)
        {
            string name = _findName(offer.CharacterKey) ?? offer.CharacterKey.ToString();
            return new StageOfferCardContent(_findIcon(offer.CharacterKey), offer.EnhancementName,
                text.FormatPrice(offer.Cost), text.FormatLine(StageOfferLineKind.EnhancementDetails, name, offer.EnhancementDescription),
                purchased ? text.CompletedButtonText : text.SelectButtonText, !hasPurchase, purchased);
        }
    }
}
