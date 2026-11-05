using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>
    /// 훈련소의 유닛 카드와 특성 카드 값을 만듭니다. 등급·가격·특성 이름과 설명은 Core 값을 옮기고, 그림·낱말은 View 표에서 읽습니다.
    /// </summary>
    internal sealed class TrainingCardContentBuilder
    {
        private readonly Func<EntityKey, string> _findName;
        private readonly Func<EntityKey, Sprite> _findIcon;
        private readonly ITraitVisualTableQuery _traits;

        internal TrainingCardContentBuilder(Func<EntityKey, string> findName, Func<EntityKey, Sprite> findIcon,
            ITraitVisualTableQuery traits)
        {
            _findName = findName ?? throw new ArgumentNullException(nameof(findName));
            _findIcon = findIcon ?? throw new ArgumentNullException(nameof(findIcon));
            _traits = traits ?? throw new ArgumentNullException(nameof(traits));
        }

        internal string UnitName(OwnedCharacterRecord unit) => _findName(unit.CharacterKey) ?? unit.CharacterKey.ToString();

        /// <summary>유닛 단계 카드입니다. 훈련할 특성이 없으면 잠깁니다.</summary>
        internal StageOfferCardContent Unit(OwnedCharacterRecord unit, IReadOnlyList<string> traitNames, StageOfferTextData text)
        {
            if (unit == null) throw new ArgumentNullException(nameof(unit));
            if (traitNames == null) throw new ArgumentNullException(nameof(traitNames));
            if (text == null) throw new ArgumentNullException(nameof(text));
            bool canTrain = traitNames.Count > 0;
            string details = canTrain
                ? text.FormatLine(StageOfferLineKind.TrainingUnitTraits, string.Join("\n", traitNames))
                : text.FormatLine(StageOfferLineKind.TrainingUnitNoTrait);
            return new StageOfferCardContent(_findIcon(unit.CharacterKey), UnitName(unit), string.Empty, details,
                canTrain ? text.SelectButtonText : text.UnavailableButtonText, canTrain, false,
                text.FormatLine(StageOfferLineKind.TrainingUnitLevel, unit.Level));
        }

        /// <param name="purchased">이 카드가 이번 방문에 산 특성인지입니다.</param>
        /// <param name="locked">이번 방문의 구매가 끝나 더 고를 수 없는지입니다.</param>
        internal StageOfferCardContent Trait(TrainingChoiceSnapshot offer, bool purchased, bool locked, StageOfferTextData text)
        {
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            if (text == null) throw new ArgumentNullException(nameof(text));
            // 외형 행이 없으면 멈춥니다(증강 Q-TG4 A와 같은 규칙). 훈련 대상 특성마다 외형 행이 필요합니다.
            if (!_traits.TryGetTraitVisual(offer.TraitKey, out var visual))
                throw new InvalidOperationException($"[ShopPopup] 특성 외형 행이 없습니다: {offer.TraitKey}");
            return new StageOfferCardContent(visual.Icon, offer.Name, text.FormatPrice(offer.Price),
                text.FormatLine(StageOfferLineKind.TrainingDetails, ValueLine(visual, offer), offer.Description),
                purchased ? text.CompletedButtonText : text.SelectButtonText, !locked, purchased,
                text.FormatLine(StageOfferLineKind.TrainingRank, offer.CurrentRank, offer.NextRank));
        }

        // 수치는 Core 값을 옮기고 낱말은 특성 외형 표의 글입니다. 산 카드도 산 단계의 값을 그대로 씁니다.
        private static string ValueLine(TraitVisualData visual, TrainingChoiceSnapshot offer)
        {
            if (string.IsNullOrWhiteSpace(visual.RankValueFormat))
                throw new InvalidOperationException($"[ShopPopup] 특성 외형의 등급 수치 글이 비었습니다: {offer.TraitKey}");
            TraitRankValues current = offer.CurrentValues
                ?? throw new InvalidOperationException($"[ShopPopup] 훈련 상품에 지금 수치가 없습니다: {offer.TraitKey}");
            TraitRankValues next = offer.NextValues
                ?? throw new InvalidOperationException($"[ShopPopup] 훈련 상품에 다음 수치가 없습니다: {offer.TraitKey}");
            int second = current.SecondAmount ?? 0;
            int nextSecond = next.SecondAmount ?? 0;
            return string.Format(visual.RankValueFormat, current.Amount, next.Amount - current.Amount, next.Amount,
                second, nextSecond - second, nextSecond);
        }
    }
}
