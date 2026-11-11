using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Shop
{
    /// <summary>훈련 카드의 숫자 줄 사용 여부와 공개된 효과 값 읽기입니다. 강화 수치는 보관하지 않습니다.</summary>
    internal readonly struct TrainingTraitValueBinding
    {
        internal bool ShowAmount { get; }
        internal Func<TraitRankValues, bool> ReadEffect { get; }

        internal TrainingTraitValueBinding(bool showAmount, Func<TraitRankValues, bool> readEffect)
        {
            ShowAmount = showAmount;
            ReadEffect = readEffect ?? throw new ArgumentNullException(nameof(readEffect));
        }
    }

    /// <summary>효과 적용 여부를 표시하는 특성의 연결입니다. 등록이 없는 특성은 기존 숫자 전용 표시를 사용합니다.</summary>
    internal static class TrainingTraitValueBindings
    {
        private static readonly IReadOnlyDictionary<TraitKey, TrainingTraitValueBinding> Entries =
            new ReadOnlyDictionary<TraitKey, TrainingTraitValueBinding>(new Dictionary<TraitKey, TrainingTraitValueBinding>
            {
                [TraitKey.Sacrifice] = new(true, value => value.PreventsLethalSacrificeDamage),
                [TraitKey.DustScreen] = new(false, value => value.ExemptsAlliedAttackRangePenalty)
            });

        internal static bool TryGet(TraitKey key, out TrainingTraitValueBinding binding) =>
            Entries.TryGetValue(key, out binding);
    }
}
