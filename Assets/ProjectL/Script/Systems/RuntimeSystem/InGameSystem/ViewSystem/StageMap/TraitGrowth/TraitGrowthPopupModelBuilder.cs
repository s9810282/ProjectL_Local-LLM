using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.TraitGrowth
{
    /// <summary>
    /// 증강 제안과 View 외형·글을 공통 레이아웃 값으로 바꿉니다. 특성 이름·설명은 제안에서만 읽습니다(Core 인계 §3).
    /// 카드 수와 순서는 제안 그대로이며 채우거나 거르지 않습니다. 모델 하나를 다시 채워 씁니다.
    /// </summary>
    internal sealed class TraitGrowthPopupModelBuilder
    {
        private readonly IUnitVisualTableQuery _units;
        private readonly ITraitVisualTableQuery _traits;
        private readonly ICharacterBufferReadSink _characters;
        private readonly IStageOfferTextTableQuery _texts;
        private readonly StageOfferPopupModel _model = new();

        internal TraitGrowthPopupModelBuilder(IUnitVisualTableQuery units, ITraitVisualTableQuery traits,
            ICharacterBufferReadSink characters, IStageOfferTextTableQuery texts)
        {
            _units = units ?? throw new ArgumentNullException(nameof(units));
            _traits = traits ?? throw new ArgumentNullException(nameof(traits));
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        }

        internal StageOfferPopupModel Offer(TraitGrowthOffer offer)
        {
            if (offer == null) throw new ArgumentNullException(nameof(offer));
            StageOfferTextData text = _texts.TraitGrowth;
            // 유닛을 모르면 줄과 얼굴 없이 설명만 그립니다. 외형 행이 없으면 이름은 키, 얼굴은 없음입니다.
            OwnedCharacterRecord record = _characters.TryGetOwned(offer.OwnedId, out var owned) ? owned : null;
            UnitVisualData visual = null;
            if (record != null) _units.TryGetUnitVisual(record.CharacterKey, out visual);
            // 골드를 쓰지 않고 반드시 골라야 하므로 골드 칸과 나가기를 끕니다(R4).
            _model.Reset(text.EventName, Description(text, offer, record, visual), string.Empty, false, false);
            // 얼굴은 덱 편집·유닛 카드와 같은 유닛 외형 InfoIcon입니다(비어 있으면 얼굴 칸이 꺼집니다).
            _model.SetPortrait(visual?.InfoIcon);
            for (int i = 0; i < offer.Choices.Count; i++)
                _model.Add(Card(text, offer.Choices[i]));
            return _model;
        }

        /// <summary>다음 제안을 준비하는 동안의 화면입니다. 카드 없이 이름·설명만 보입니다.</summary>
        internal StageOfferPopupModel Preparing()
        {
            StageOfferTextData text = _texts.TraitGrowth;
            _model.Reset(text.EventName, text.EventDescription, string.Empty, false, false);
            return _model;
        }

        /// <summary>Core 실패 사유의 안내 글입니다. 옛 요청을 자동으로 다시 보내지 않으므로 다시 고르라고 안내합니다(설계 §14 TG4).</summary>
        internal string ErrorText(TraitGrowthFailureReason reason) => _texts.TraitGrowth.ErrorOf(ErrorOf(reason));

        private static StageOfferErrorKind ErrorOf(TraitGrowthFailureReason reason)
        {
            switch (reason)
            {
                case TraitGrowthFailureReason.NotCandidate:
                case TraitGrowthFailureReason.StaleOffer:
                case TraitGrowthFailureReason.NotOwned:
                case TraitGrowthFailureReason.NotUnlocked:
                case TraitGrowthFailureReason.SourceChanged:
                    return StageOfferErrorKind.SelectionChanged;
                case TraitGrowthFailureReason.InBattle:
                case TraitGrowthFailureReason.RunEnded:
                    return StageOfferErrorKind.CannotChooseNow;
                default:
                    return StageOfferErrorKind.None; // 사유별 글이 없어 기본 글입니다.
            }
        }

        private StageOfferCardContent Card(StageOfferTextData text, TraitGrowthChoice choice)
        {
            // 외형 행이 없으면 멈춥니다(Q-TG4 A). 후보 풀에 넣는 특성마다 외형 행이 필요합니다.
            if (!_traits.TryGetTraitVisual(choice.TraitKey, out var visual))
                throw new InvalidOperationException($"[TraitGrowthPopup] 특성 외형 행이 없습니다: {choice.TraitKey}");
            // 증강은 돈을 쓰지 않아 가격을 비웁니다(가격 글·그림이 꺼집니다).
            return new StageOfferCardContent(visual.Icon, choice.Name, string.Empty, choice.Description,
                text.SelectButtonText, true, false, KindLabel(text, choice.Kind));
        }

        // 설명 글 아래 줄에 제안마다 다른 값(대상 유닛·레벨·종류)을 붙입니다. 유닛을 모르면 설명만입니다.
        private static string Description(StageOfferTextData text, TraitGrowthOffer offer, OwnedCharacterRecord record,
            UnitVisualData visual)
        {
            if (record == null) return text.EventDescription;
            string name = visual != null ? visual.DisplayName : record.CharacterKey.ToString();
            return text.EventDescription + "\n" +
                text.FormatLine(StageOfferLineKind.TraitGrowthHeader, name, record.Level, KindLabel(text, offer.Kind));
        }

        private static string KindLabel(StageOfferTextData text, TraitGrowthKind kind) => text.FormatLine(
            kind == TraitGrowthKind.Enhance ? StageOfferLineKind.TraitEnhanceLabel : StageOfferLineKind.TraitAcquireLabel);
    }
}
