using System;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Recovery.Item
{
    /// <summary>회복 선택지의 표시 외형을 입력하고 조회마다 새 읽기 전용 데이터를 만듭니다(DB 규칙 P2).</summary>
    [CreateAssetMenu(fileName = "NewRecoveryOptionVisualData", menuName = "ProjectL/Database/View/RecoveryOptionVisualData")]
    internal sealed class RecoveryOptionVisualDataSO : DatabaseItem
    {
        // 칸 이름은 옛 직렬화 경로 그대로라 에셋 값이 유지됩니다.
        [SerializeField] private Input recoveryOptionVisualData = new();

        public override IData GetData() => recoveryOptionVisualData.ToData();

        [Serializable]
        private sealed class Input
        {
            [SerializeField, Min(1)] private int optionId = 1;
            [SerializeField] private string displayName;
            [SerializeField] private Sprite icon;
            [SerializeField, TextArea] private string description;

            internal RecoveryOptionVisualData ToData() => new(optionId, displayName, icon, description);
        }
    }
}
