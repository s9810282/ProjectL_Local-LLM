using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Shop;
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ShopView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>
    /// 현재 방문·Run 재화와 상품별 내용 생성기를 연결해 재사용 표시 모델을 채우고, 카드 번호가 가리키는 상품을 기억합니다.
    /// </summary>
    /// <remarks>
    /// 규칙 판단은 하지 않습니다. 골드가 부족해도 버튼을 끄지 않고, 구매가 확정된 방문의 모든 상품을 끕니다(설계 §4, 판단은 Core).
    /// 열 때와 요청이 끝났을 때만 부릅니다(성능 규칙 R1). 모델 객체는 재사용합니다(R5).
    /// </remarks>
    internal sealed class ShopPopupModelBuilder
    {
        private readonly ICurrentProgressBufferReadSink _current;
        private readonly ICurrencyBufferReadSink _currency;
        private readonly ICharacterBufferReadSink _characters;
        private readonly Dictionary<ShopProductKind, IShopOfferContentBuilder> _contents = new();
        private readonly IStageOfferTextTableQuery _texts;
        private readonly StageOfferPopupModel _model = new();
        private readonly TrainingCardContentBuilder _training;
        // 카드 번호 순서의 제출 명령입니다. 낼 수 없는 칸은 기본값(Kind = None)입니다.
        private readonly List<ShopViewCommandData> _commands = new();
        // 훈련소 유닛 단계의 카드 번호 순서입니다. 고를 수 없는 칸은 비어 있습니다.
        private readonly List<CharacterInstanceId> _unitCards = new();
        private readonly List<string> _traitNames = new();
        private readonly List<TrainingChoiceSnapshot> _traitCards = new();
        // 고른 유닛은 화면 상태입니다. 방문이 바뀌면 비웁니다.
        private CharacterInstanceId _trainee;
        private StageEntryId _traineeEntry;

        internal ShopPopupModelBuilder(ICurrentProgressBufferReadSink current, ICurrencyBufferReadSink currency,
            ICharacterBufferReadSink characters,
            IStageOfferTextTableQuery texts, IReadOnlyList<IShopOfferContentBuilder> contentBuilders,
            TrainingCardContentBuilder training)
        {
            _current = current ?? throw new ArgumentNullException(nameof(current));
            _currency = currency ?? throw new ArgumentNullException(nameof(currency));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            _training = training ?? throw new ArgumentNullException(nameof(training));
            if (contentBuilders == null)
            {
                throw new ArgumentNullException(nameof(contentBuilders));
            }
            for (int i = 0; i < contentBuilders.Count; i++)
            {
                IShopOfferContentBuilder content = contentBuilders[i];
                if (content == null)
                {
                    throw new ArgumentException("상품 표시 구현이 필요합니다.", nameof(contentBuilders));
                }
                if (content.Kind == ShopProductKind.None || !Enum.IsDefined(typeof(ShopProductKind), content.Kind))
                {
                    throw new InvalidOperationException($"지원하지 않는 상품 표시 종류입니다: {content.Kind}");
                }
                if (!_contents.TryAdd(content.Kind, content))
                {
                    throw new InvalidOperationException($"상품 표시 종류가 중복됐습니다: {content.Kind}");
                }
            }
        }

        /// <summary>마지막으로 그린 방문입니다.</summary>
        internal ShopVisitContext Context { get; private set; }

        /// <summary>마지막으로 그린 카드 번호가 낼 명령입니다. 명령이 없는 칸이면 false입니다.</summary>
        internal bool TryGetCommand(int index, out ShopViewCommandData command)
        {
            command = index >= 0 && index < _commands.Count ? _commands[index] : default;
            return command.Kind != ShopViewCommandKind.None;
        }

        /// <summary>훈련소 유닛 단계에서 고를 수 있는 유닛 카드면 그 유닛을 고릅니다. 고쳐 그리는 일은 부르는 쪽이 합니다.</summary>
        internal bool TrySelectUnit(int index)
        {
            if (index < 0 || index >= _unitCards.Count || _unitCards[index].IsEmpty) return false;
            _trainee = _unitCards[index];
            return true;
        }

        /// <summary>구매 전 특성 단계면 유닛 단계로 돌아갑니다. 고쳐 그리는 일은 부르는 쪽이 합니다.</summary>
        internal bool TryBack()
        {
            CurrentShopData shop = _current.Shop;
            if (shop == null || shop.VisitKind != ShopVisitKind.Training || shop.TrainingReceipt != null || _trainee.IsEmpty) return false;
            _trainee = default;
            return true;
        }

        /// <summary>현재 Buffer 기준으로 모델을 다시 채웁니다. 부르는 쪽이 방문 문맥을 확인한 뒤에만 부릅니다.</summary>
        internal StageOfferPopupModel Build()
        {
            CurrentShopData shop = _current.Shop;
            if (shop == null || !_current.HasSelection)
                throw new InvalidOperationException("[ShopPopup] 현재 상점 방문이 없어 화면을 그릴 수 없습니다.");

            int stageKey = _current.CurrentStage.StageKey;
            StageOfferTextData text = _texts.GetForStage(stageKey);
            Context = new ShopVisitContext(_current.CurrentMapKey, new StageNodeId(_current.CurrentStage.NodeId),
                new StageKey(stageKey), shop.EntryId);
            _commands.Clear();
            _unitCards.Clear();
            if (shop.VisitKind == ShopVisitKind.Training)
            {
                BuildTraining(shop, text);
                return _model;
            }

            _model.Reset(text.EventName, text.EventDescription, text.FormatGold(_currency.Amount), true, true, text.ExitButtonText);
            IReadOnlyList<ShopPurchaseSnapshot> offers = shop.Offers;
            for (int i = 0; i < offers.Count; i++)
            {
                ShopPurchaseSnapshot offer = offers[i];
                bool purchased = shop.IsPurchased(offer.OfferId);
                if (!_contents.TryGetValue(offer.Kind, out IShopOfferContentBuilder content))
                {
                    throw new InvalidOperationException($"상품 표시 구현이 연결되지 않았습니다: {offer.Kind}");
                }
                _model.Add(content.Build(offer, purchased, shop.HasPurchase, text));
                _commands.Add(shop.HasPurchase ? default : ShopViewCommandData.Purchase(Context, offer.OfferId));
            }

            return _model;
        }

        private void BuildTraining(CurrentShopData shop, StageOfferTextData text)
        {
            if (_traineeEntry != shop.EntryId)
            {
                _traineeEntry = shop.EntryId;
                _trainee = default;
            }
            // 산 뒤에는 그 유닛의 특성 단계에 머뭅니다. 영수증이 Buffer에 있어 다시 연결돼도 같은 화면입니다.
            if (shop.TrainingReceipt != null) _trainee = shop.TrainingReceipt.OwnedId;
            else if (!_trainee.IsEmpty && !HasChoice(shop, _trainee)) _trainee = default;

            if (_trainee.IsEmpty) BuildUnits(shop, text);
            else BuildTraits(shop, text);
        }

        private void BuildUnits(CurrentShopData shop, StageOfferTextData text)
        {
            string description = shop.TrainingChoices.Count == 0
                ? text.EventDescription + "\n" + text.FormatLine(StageOfferLineKind.TrainingEmpty)
                : text.EventDescription;
            _model.Reset(text.EventName, description, text.FormatGold(_currency.Amount), true, true, text.ExitButtonText);
            IReadOnlyList<OwnedCharacterRecord> owned = _characters.Owned;
            for (int i = 0; i < owned.Count; i++)
            {
                OwnedCharacterRecord unit = owned[i];
                _traitNames.Clear();
                foreach (TrainingChoiceSnapshot choice in shop.TrainingChoices)
                {
                    if (choice.OwnedId == unit.Id) _traitNames.Add(choice.Name);
                }
                _model.Add(_training.Unit(unit, _traitNames, text));
                _commands.Add(default);
                _unitCards.Add(_traitNames.Count > 0 ? unit.Id : default);
            }
        }

        // 카드 자리는 특성 키 순이라 구매 뒤에도 움직이지 않습니다. 산 카드는 구매 직전 값으로 그립니다.
        private void BuildTraits(CurrentShopData shop, StageOfferTextData text)
        {
            if (!_characters.TryGetOwned(_trainee, out OwnedCharacterRecord unit))
                throw new InvalidOperationException($"[ShopPopup] 훈련 대상 유닛이 보유 목록에 없습니다: {_trainee}");
            TrainingChoiceSnapshot bought = shop.TrainingPurchase;
            _model.Reset(text.EventName, text.FormatLine(StageOfferLineKind.TrainingTraitPrompt, _training.UnitName(unit)),
                text.FormatGold(_currency.Amount), true, true, bought != null ? text.ExitButtonText : text.BackButtonText);
            _traitCards.Clear();
            foreach (TrainingChoiceSnapshot choice in shop.TrainingChoices)
            {
                if (choice.OwnedId == _trainee && (bought == null || choice.TraitKey != bought.TraitKey)) _traitCards.Add(choice);
            }
            if (bought != null) _traitCards.Add(bought);
            _traitCards.Sort((a, b) => a.TraitKey.CompareTo(b.TraitKey));
            for (int i = 0; i < _traitCards.Count; i++)
            {
                TrainingChoiceSnapshot offer = _traitCards[i];
                _model.Add(_training.Trait(offer, offer == bought, bought != null, text));
                _commands.Add(bought != null ? default : ShopViewCommandData.PurchaseTraining(
                    Context, offer.OwnedId, offer.TraitKey, offer.CurrentRank));
            }
        }

        private static bool HasChoice(CurrentShopData shop, CharacterInstanceId owned)
        {
            foreach (TrainingChoiceSnapshot choice in shop.TrainingChoices)
            {
                if (choice.OwnedId == owned) return true;
            }
            return false;
        }
    }
}
