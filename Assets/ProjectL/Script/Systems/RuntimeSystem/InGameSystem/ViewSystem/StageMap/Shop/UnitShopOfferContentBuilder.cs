using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>유닛 상품의 이름·얼굴·레벨·보유 수를 표시합니다.</summary>
    internal sealed class UnitShopOfferContentBuilder : IShopOfferContentBuilder
    {
        private readonly ICharacterBufferReadSink _characters;
        private readonly Func<EntityKey, string> _findName;
        private readonly Func<EntityKey, Sprite> _findIcon;

        internal UnitShopOfferContentBuilder(ICharacterBufferReadSink characters,
            Func<EntityKey, string> findName, Func<EntityKey, Sprite> findIcon)
        {
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _findName = findName ?? throw new ArgumentNullException(nameof(findName));
            _findIcon = findIcon ?? throw new ArgumentNullException(nameof(findIcon));
        }

        public ShopProductKind Kind => ShopProductKind.Unit;

        public StageOfferCardContent Build(ShopPurchaseSnapshot offer, bool purchased, bool hasPurchase, StageOfferTextData text)
        {
            return new StageOfferCardContent(_findIcon(offer.CharacterKey),
                _findName(offer.CharacterKey), text.FormatPrice(offer.Cost),
                text.FormatLine(StageOfferLineKind.UnitDetails, offer.InitialLevel, CountOwned(offer.CharacterKey)),
                purchased ? text.CompletedButtonText : text.SelectButtonText, !hasPurchase, purchased);
        }

        private int CountOwned(EntityKey key)
        {
            IReadOnlyList<OwnedCharacterRecord> owned = _characters.Owned;
            int count = 0;
            for (int i = 0; i < owned.Count; i++)
            {
                if (owned[i].CharacterKey == key)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
