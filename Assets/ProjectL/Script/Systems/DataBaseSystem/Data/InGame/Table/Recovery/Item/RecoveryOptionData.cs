using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item
{
    /// <summary>
    /// 회복 스테이지에서 고를 수 있는 선택지 하나입니다. Core가 받는 순수 C# 데이터이며 RecoveryOptionDataSO가 생성합니다.
    /// 효과 종류에 따라 Core의 체력 회복형 또는 최대 체력 증가형 선택지로 변환됩니다. 표시 이름은 ViewGroup이 가집니다.
    /// </summary>
    public sealed class RecoveryOptionData : IData
    {
        public int OptionId { get; }
        public int Cost { get; }
        public RecoveryHealMode HealMode { get; }
        /// <summary>Flat이면 회복량, PercentOfMax면 최대 체력 대비 %(1~100), Full이면 쓰지 않습니다.</summary>
        public int HealValue { get; }
        public RecoveryEffectKind EffectKind { get; }
        public int MaxHealthIncrease { get; }

        public RecoveryOptionData(int optionId, int cost, RecoveryHealMode healMode, int healValue,
            RecoveryEffectKind effectKind = RecoveryEffectKind.Heal, int maxHealthIncrease = 0)
        {
            OptionId = optionId;
            Cost = cost;
            HealMode = healMode;
            HealValue = healValue;
            EffectKind = effectKind;
            MaxHealthIncrease = maxHealthIncrease;
        }

        public void Validate()
        {
            if (OptionId < 1)
                throw new InvalidOperationException("[RecoveryOptionData] OptionId must be positive.");
            if (Cost < 0)
                throw new InvalidOperationException($"[RecoveryOptionData] Cost must not be negative. OptionId: {OptionId}");
            if (!System.Enum.IsDefined(typeof(RecoveryEffectKind), EffectKind))
                throw new InvalidOperationException($"회복 효과 종류가 올바르지 않습니다. OptionId: {OptionId}");
            if (EffectKind == RecoveryEffectKind.MaxHealthGrowth)
            {
                if (MaxHealthIncrease < 1)
                    throw new InvalidOperationException($"최대 체력 증가량은 양수여야 합니다. OptionId: {OptionId}");
                return;
            }
            if (MaxHealthIncrease != 0)
                throw new InvalidOperationException($"체력 회복 효과에는 최대 체력 증가를 함께 지정할 수 없습니다. OptionId: {OptionId}");
            if (!System.Enum.IsDefined(typeof(RecoveryHealMode), HealMode))
                throw new InvalidOperationException($"[RecoveryOptionData] HealMode is invalid. OptionId: {OptionId}");
            // Core RecoveryHealSpec과 같은 규칙입니다.
            if (HealMode == RecoveryHealMode.PercentOfMax && (HealValue < 1 || HealValue > 100))
                throw new InvalidOperationException($"[RecoveryOptionData] Percent heal must be between 1 and 100. OptionId: {OptionId}");
            if (HealMode == RecoveryHealMode.Flat && HealValue < 0)
                throw new InvalidOperationException($"[RecoveryOptionData] Flat heal must not be negative. OptionId: {OptionId}");
        }
    }
}
