using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Recovery.Item
{
    /// <summary>회복 선택지의 표시 외형입니다. 키는 InGame RecoveryOptionTable과 같은 OptionId입니다. 조회마다 새로 만드는 읽기 전용 값입니다.</summary>
    internal sealed class RecoveryOptionVisualData : IData
    {
        internal RecoveryOptionVisualData(int optionId, string displayName, Sprite icon, string description)
        {
            OptionId = optionId;
            DisplayName = displayName ?? string.Empty;
            Icon = icon;
            Description = description ?? string.Empty;
        }

        public int OptionId { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public string Description { get; }

        internal void Validate()
        {
            if (OptionId < 1)
                throw new InvalidOperationException("[RecoveryOptionVisualData] OptionId must be positive.");
            if (string.IsNullOrWhiteSpace(DisplayName))
                throw new InvalidOperationException($"[RecoveryOptionVisualData] Display name is required. OptionId: {OptionId}");
        }
    }
}
