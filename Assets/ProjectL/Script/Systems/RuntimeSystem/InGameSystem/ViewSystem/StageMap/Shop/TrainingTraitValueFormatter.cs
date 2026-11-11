using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageOfferText.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>훈련 상품의 현재·다음 값과 View 문구로 효과·수치 변화를 만듭니다. 구매 상태와 등급 판정은 소유하지 않습니다.</summary>
    internal static class TrainingTraitValueFormatter
    {
        internal static string Format(TraitVisualData visual, TrainingChoiceSnapshot offer, StageOfferTextData text)
        {
            if (visual == null)
            {
                throw new ArgumentNullException(nameof(visual));
            }
            if (offer == null)
            {
                throw new ArgumentNullException(nameof(offer));
            }
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            TraitRankValues current = offer.CurrentValues
                ?? throw new InvalidOperationException($"[TrainingTraitValueFormatter] 현재 수치가 없습니다. 특성: {offer.TraitKey}");
            TraitRankValues next = offer.NextValues
                ?? throw new InvalidOperationException($"[TrainingTraitValueFormatter] 다음 수치가 없습니다. 특성: {offer.TraitKey}");
            if (visual.Key != offer.TraitKey || current.Key != offer.TraitKey || next.Key != offer.TraitKey)
            {
                throw new InvalidOperationException(
                    $"[TrainingTraitValueFormatter] 특성 키가 다릅니다. 상품: {offer.TraitKey}, 외형: {visual.Key}, 현재: {current.Key}, 다음: {next.Key}");
            }

            if (!TrainingTraitValueBindings.TryGet(offer.TraitKey, out TrainingTraitValueBinding binding))
            {
                if (!string.IsNullOrWhiteSpace(visual.RankEffectFormat))
                {
                    throw new InvalidOperationException($"[TrainingTraitValueFormatter] 효과 표시 등록 없이 RankEffectFormat이 설정됐습니다. 특성: {offer.TraitKey}");
                }
                return FormatAmount(visual, current, next);
            }

            if (string.IsNullOrWhiteSpace(visual.RankEffectFormat))
            {
                throw new InvalidOperationException($"[TrainingTraitValueFormatter] RankEffectFormat이 비었습니다. 특성: {offer.TraitKey}");
            }
            if (!binding.ShowAmount && !string.IsNullOrWhiteSpace(visual.RankValueFormat))
            {
                throw new InvalidOperationException($"[TrainingTraitValueFormatter] 숫자 줄을 사용하지 않는 특성에 RankValueFormat이 설정됐습니다. 특성: {offer.TraitKey}");
            }

            string effect = FormatEffect(visual, binding.ReadEffect(current), binding.ReadEffect(next), text);
            return binding.ShowAmount ? effect + "\n" + FormatAmount(visual, current, next) : effect;
        }

        private static string FormatAmount(TraitVisualData visual, TraitRankValues current, TraitRankValues next)
        {
            if (string.IsNullOrWhiteSpace(visual.RankValueFormat))
            {
                throw new InvalidOperationException($"[TrainingTraitValueFormatter] RankValueFormat이 비었습니다. 특성: {visual.Key}");
            }

            int second = current.SecondAmount ?? 0;
            int nextSecond = next.SecondAmount ?? 0;
            try
            {
                return string.Format(visual.RankValueFormat, current.Amount, next.Amount - current.Amount, next.Amount,
                    second, nextSecond - second, nextSecond);
            }
            catch (FormatException error)
            {
                throw new InvalidOperationException($"[TrainingTraitValueFormatter] RankValueFormat의 꼴이 맞지 않습니다. 특성: {visual.Key}", error);
            }
        }

        private static string FormatEffect(TraitVisualData visual, bool current, bool next, StageOfferTextData text)
        {
            string enabled = ReadEffectText(text, StageOfferLineKind.TrainingEffectEnabled, visual.Key);
            string disabled = ReadEffectText(text, StageOfferLineKind.TrainingEffectDisabled, visual.Key);
            try
            {
                return string.Format(visual.RankEffectFormat, current ? enabled : disabled, next ? enabled : disabled);
            }
            catch (FormatException error)
            {
                throw new InvalidOperationException($"[TrainingTraitValueFormatter] RankEffectFormat의 꼴이 맞지 않습니다. 특성: {visual.Key}", error);
            }
        }

        private static string ReadEffectText(StageOfferTextData text, StageOfferLineKind kind, TraitKey key)
        {
            string value;
            try
            {
                value = text.FormatLine(kind);
            }
            catch (FormatException error)
            {
                throw new InvalidOperationException($"[TrainingTraitValueFormatter] 효과 상태 낱말 {kind}의 꼴이 맞지 않습니다. 특성: {key}", error);
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"[TrainingTraitValueFormatter] 효과 상태 낱말 {kind}이 비었습니다. 특성: {key}");
            }
            return value;
        }
    }
}
