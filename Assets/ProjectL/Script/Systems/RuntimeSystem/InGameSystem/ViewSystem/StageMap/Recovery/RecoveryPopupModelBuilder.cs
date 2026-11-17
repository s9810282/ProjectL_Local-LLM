using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Recovery
{
    /// <summary>Core 회복 상태와 View 외형·글을 표시 모델로 옮기고, 카드 번호가 가리키는 선택지를 기억합니다.</summary>
    internal sealed class RecoveryPopupModelBuilder
    {
        private readonly IRecoveryOptionVisualTableQuery _visuals;
        private readonly IStageOfferTextTableQuery _texts;
        private readonly StageOfferPopupModel _model = new();
        private readonly List<RecoveryOptionId> _optionIds = new();

        internal RecoveryPopupModelBuilder(IRecoveryOptionVisualTableQuery visuals, IStageOfferTextTableQuery texts)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
        }

        /// <summary>마지막으로 그린 카드 순서대로의 선택지입니다.</summary>
        internal IReadOnlyList<RecoveryOptionId> OptionIds => _optionIds;

        /// <summary>열기와 요청 완료 때만 호출하며, 같은 모델의 표시 값을 다시 채웁니다.</summary>
        internal StageOfferPopupModel Build(RecoveryStateSnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            StageOfferTextData text = _texts.GetForStage(snapshot.Context.StageKey.Value);
            _model.Reset(text.EventName, string.Empty, text.FormatGold(snapshot.Gold), true, snapshot.CanComplete, text.ExitButtonText);
            _optionIds.Clear();

            for (int i = 0; i < snapshot.Options.Count; i++)
            {
                RecoveryOptionState option = snapshot.Options[i];
                RecoveryOptionVisualData visual = VisualOf(option.Option.Id);
                bool applied = snapshot.Receipt.AppliedOptionId.HasValue &&
                    snapshot.Receipt.AppliedOptionId.Value == option.Option.Id;

                // 골드 부족은 Core 결과 안내를 받을 입력만 허용합니다. 실제 적용 가능 여부는 재판정하지 않습니다.
                _model.Add(new StageOfferCardContent(visual.Icon, visual.DisplayName,
                    text.FormatPrice(option.Option.Cost),
                    Details(text, visual.Description, option),
                    applied ? text.CompletedButtonText :
                    option.UnavailableReason == RecoveryFailureReason.NoEffect ?
                        text.UnavailableButtonText : text.SelectButtonText,
                    !snapshot.IsCompleted && !snapshot.Receipt.AppliedOptionId.HasValue &&
                    (option.CanSelect || option.UnavailableReason == RecoveryFailureReason.InsufficientFunds), applied));
                _optionIds.Add(option.Option.Id);
            }

            return _model;
        }

        /// <summary>Core 거절 사유가 어떤 안내 글인지입니다.</summary>
        internal static StageOfferErrorKind ErrorOf(RecoveryFailureReason reason)
        {
            switch (reason)
            {
                case RecoveryFailureReason.InsufficientFunds:
                    return StageOfferErrorKind.InsufficientFunds;
                case RecoveryFailureReason.NoEffect:
                    return StageOfferErrorKind.NoEffect;
                case RecoveryFailureReason.AlreadyResolved:
                    return StageOfferErrorKind.AlreadyResolved;
                default:
                    return StageOfferErrorKind.StateChanged;
            }
        }

        private RecoveryOptionVisualData VisualOf(RecoveryOptionId optionId)
        {
            if (_visuals.TryGetRecoveryOptionVisual(optionId.Value, out RecoveryOptionVisualData visual) &&
                !string.IsNullOrWhiteSpace(visual.DisplayName))
            {
                return visual;
            }

            throw new InvalidOperationException($"[RecoveryPopup] 회복 선택지 {optionId.Value}의 이름이 View 표에 없습니다.");
        }

        /// <summary>설명 본문과 종류에 맞는 Core 예상량·불가 사유를 같은 선택지에서 읽습니다.</summary>
        private static string Details(StageOfferTextData text, string description, RecoveryOptionState option)
        {
            string body = RecoveryDescriptionBindings.Format(description, option);
            string details = option.Option.EffectKind switch
            {
                RecoveryEffectKind.Heal => text.FormatLine(StageOfferLineKind.RecoveryProjected, option.ProjectedHealAmount),
                RecoveryEffectKind.MaxHealthGrowth => text.FormatLine(StageOfferLineKind.RecoveryMaxHealthProjected, option.ProjectedMaxHealthIncrease),
                _ => throw new InvalidOperationException($"[RecoveryPopup] 선택지 {option.Option.Id.Value}의 효과 종류가 맞지 않습니다.")
            };
            if (!string.IsNullOrWhiteSpace(body))
            {
                details = body + "\n" + details;
            }

            return option.UnavailableReason == RecoveryFailureReason.None ? details :
                details + "\n" + text.ErrorOf(ErrorOf(option.UnavailableReason));
        }
    }
}
