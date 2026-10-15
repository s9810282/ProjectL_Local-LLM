using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Recovery.Item
{
    /// <summary>회복 선택지의 표시 이름입니다. 키는 InGame RecoveryOptionTable과 같은 OptionId입니다.</summary>
    [Serializable]
    internal sealed class RecoveryOptionVisualData : IData
    {
        [SerializeField, Min(1)] private int optionId = 1;
        [SerializeField] private string displayName;

        public int OptionId => optionId;
        public string DisplayName => displayName ?? string.Empty;

        internal void Validate()
        {
            if (optionId < 1)
                throw new InvalidOperationException("[RecoveryOptionVisualData] OptionId must be positive.");
            if (string.IsNullOrWhiteSpace(displayName))
                throw new InvalidOperationException($"[RecoveryOptionVisualData] Display name is required. OptionId: {optionId}");
        }
    }

    [CreateAssetMenu(fileName = "NewRecoveryOptionVisualData", menuName = "ProjectL/Database/View/RecoveryOptionVisualData")]
    internal sealed class RecoveryOptionVisualDataSO : DatabaseItem
    {
        [SerializeField] private RecoveryOptionVisualData recoveryOptionVisualData = new();

        public override IData GetData() => recoveryOptionVisualData;
    }
}
