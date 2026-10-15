using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item
{
    /// <summary>
    /// 회복 스테이지에서 고를 수 있는 선택지 하나입니다. Core가 받는 순수 C# 데이터이며 RecoveryOptionDataSO가 생성합니다.
    /// Core RecoveryOptionSpec(id, cost, RecoveryHealSpec(mode, value))에 대응합니다. 표시 이름은 ViewGroup이 가집니다.
    /// </summary>
    public sealed class RecoveryOptionData : IData
    {
        public int OptionId { get; }
        public int Cost { get; }
        public RecoveryHealMode HealMode { get; }
        /// <summary>Flat이면 회복량, PercentOfMax면 최대 체력 대비 %(1~100), Full이면 쓰지 않습니다.</summary>
        public int HealValue { get; }

        public RecoveryOptionData(int optionId, int cost, RecoveryHealMode healMode, int healValue)
        {
            OptionId = optionId;
            Cost = cost;
            HealMode = healMode;
            HealValue = healValue;
        }

        public void Validate()
        {
            if (OptionId < 1)
                throw new InvalidOperationException("[RecoveryOptionData] OptionId must be positive.");
            if (Cost < 0)
                throw new InvalidOperationException($"[RecoveryOptionData] Cost must not be negative. OptionId: {OptionId}");
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
