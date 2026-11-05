using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item
{
    /// <summary>스테이지 맵 노드 외형을 입력하고 조회마다 새 읽기 전용 데이터를 만듭니다(DB 규칙 P2).</summary>
    [CreateAssetMenu(fileName = "NewStageNodeVisualData", menuName = "ProjectL/Database/View/StageNodeVisualData")]
    internal sealed class StageNodeVisualDataSO : DatabaseItem
    {
        // 칸 이름은 옛 직렬화 경로 그대로라 에셋 값이 유지됩니다.
        [SerializeField] private Input stageNodeVisualData = new();

        public override IData GetData() => stageNodeVisualData.ToData();

        [Serializable]
        private sealed class Input
        {
            [Tooltip("0은 종류·역할의 공통 외형, 양수는 해당 StageKey의 전용 외형입니다.")]
            [SerializeField] private int stageKey = 0;
            [SerializeField] private StageType stageType = StageType.Combat;
            [SerializeField] private StageMapRole mapRole = StageMapRole.Regular;
            [Tooltip("노드 자리에 표시하고 눌러서 고르는 건물 프리팹입니다. 켜진 MeshRenderer가 있어야 합니다.")]
            [SerializeField] private GameObject visualPrefab;
            [Tooltip("건물의 원래 회전에 노드 로컬 축 기준으로 더하는 회전입니다(X·Y·Z, 도). 0이면 원래 방향을 유지합니다.")]
            [SerializeField] private Vector3 rotationOffset = Vector3.zero;
            [Tooltip("비워 두면 프리팹 이름을 표시합니다.")]
            [SerializeField] private string displayName;

            internal StageNodeVisualData ToData() =>
                new(stageKey, stageType, mapRole, visualPrefab, displayName, rotationOffset);
        }
    }
}
