using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item
{
    /// <summary>RecoveryOptionData의 입력 SO입니다. 회복 스테이지 SO가 이 에셋을 목록으로 참조합니다.</summary>
    [CreateAssetMenu(fileName = "NewRecoveryOptionData", menuName = "ProjectL/Database/InGame/RecoveryOptionData")]
    internal sealed class RecoveryOptionDataSO : DatabaseItem
    {
        [SerializeField, Min(1)] private int optionId = 1;
        [SerializeField, Min(0)] private int cost;
        [SerializeField] private RecoveryHealMode healMode = RecoveryHealMode.Flat;
        [Tooltip("Flat: 회복량, PercentOfMax: 최대 체력 대비 %(1~100), Full: 쓰지 않음")]
        [SerializeField, Min(0)] private int healValue;

        /// <summary>다른 SO가 이 선택지를 참조할 때 쓰는 키입니다.</summary>
        internal int OptionId => optionId;

        public override IData GetData() => new RecoveryOptionData(optionId, cost, healMode, healValue);
    }
}
