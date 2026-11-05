using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>회복 표시용 사실을 검증하고 원본 목록과 분리해 보관하는 불변 스냅샷이다.</summary>
    public sealed class RecoveryStateSnapshot
    {
        public RecoveryEntryContext Context { get; }
        public HealthStateSnapshot MainCharacterHealth { get; }
        public int Gold { get; }
        public IReadOnlyList<RecoveryOptionState> Options { get; }
        public RecoveryReceiptSnapshot Receipt { get; }
        public bool IsCompleted { get; }
        public bool CanComplete => !IsCompleted;

        public RecoveryStateSnapshot(RecoveryEntryContext context, HealthStateSnapshot mainCharacterHealth,
            int gold, IReadOnlyList<RecoveryOptionState> options, RecoveryReceiptSnapshot receipt,
            bool isCompleted)
        {
            var validatedContext = new RecoveryEntryContext(context.EntryId, context.ChapterKey,
                context.NodeId, context.StageKey);
            if (mainCharacterHealth.MaxHealth < 1 || mainCharacterHealth.CurrentHealth < 1 ||
                mainCharacterHealth.CurrentHealth > mainCharacterHealth.MaxHealth)
            {
                throw new ArgumentException("살아 있는 메인 캐릭터의 체력이 필요합니다.", nameof(mainCharacterHealth));
            }
            if (gold < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gold));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (options.Count == 0)
            {
                throw new ArgumentException("회복 옵션이 하나 이상 필요합니다.", nameof(options));
            }
            if (receipt == null)
            {
                throw new ArgumentNullException(nameof(receipt));
            }

            var copiedOptions = new RecoveryOptionState[options.Count];
            var optionIds = new HashSet<RecoveryOptionId>();
            RecoveryOptionSpec appliedOption = null;
            int deficit = mainCharacterHealth.MaxHealth - mainCharacterHealth.CurrentHealth;
            for (int i = 0; i < copiedOptions.Length; i++)
            {
                RecoveryOptionState option = options[i];
                if (option == null || !optionIds.Add(option.Option.Id))
                {
                    throw new ArgumentException("회복 옵션은 null이거나 중복 식별자를 가질 수 없습니다.", nameof(options));
                }
                if (option.ProjectedHealAmount > deficit)
                {
                    throw new ArgumentException("예상 회복량은 현재 부족한 체력을 초과할 수 없습니다.", nameof(options));
                }

                // 전달된 사실의 일관성만 확인한다. 회복 규칙의 실제 계산은 Core가 맡는다.
                RecoveryFailureReason expectedReason = isCompleted ? RecoveryFailureReason.StaleEntry :
                    receipt.AppliedOptionId.HasValue ? RecoveryFailureReason.AlreadyResolved :
                    option.ProjectedHealAmount == 0 && option.ProjectedMaxHealthIncrease == 0 ? RecoveryFailureReason.NoEffect :
                    option.Option.Cost > gold ? RecoveryFailureReason.InsufficientFunds : RecoveryFailureReason.None;
                if (option.UnavailableReason != expectedReason)
                {
                    throw new ArgumentException("회복 선택 사유가 완료·영수증·효과·골드 상태와 일치하지 않습니다.", nameof(options));
                }
                if (receipt.AppliedOptionId.HasValue && option.Option.Id == receipt.AppliedOptionId.Value)
                {
                    appliedOption = option.Option;
                }
                copiedOptions[i] = option;
            }

            // 이미 적용한 효과는 과거 값이므로 현재 부족한 체력과 비교하지 않는다.
            if (receipt.AppliedOptionId.HasValue &&
                (appliedOption == null || receipt.SpentAmount != appliedOption.Cost ||
                    (appliedOption.EffectKind == RecoveryEffectKind.Heal
                        ? receipt.HealedAmount <= 0 || receipt.MaxHealthIncrease != 0
                        : receipt.HealedAmount != 0 || receipt.MaxHealthIncrease != appliedOption.MaxHealthIncrease)))
            {
                throw new ArgumentException("회복 영수증의 비용과 효과가 설치된 옵션과 일치해야 합니다.", nameof(receipt));
            }

            Context = validatedContext;
            MainCharacterHealth = mainCharacterHealth;
            Gold = gold;
            Options = Array.AsReadOnly(copiedOptions);
            Receipt = receipt;
            IsCompleted = isCompleted;
        }
    }
}
