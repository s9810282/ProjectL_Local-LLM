using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item
{
    /// <summary>
    /// 스테이지 맵 노드의 외형입니다. 키는 (StageType, StageMapRole)이고 View는 이 값을 Buffer에서 받습니다.
    /// 시작 노드 외형은 StageMapViewData.StartVisualPrefab을 사용합니다.
    /// </summary>
    [Serializable]
    internal sealed class StageNodeVisualData : IData
    {
        [SerializeField] private StageType stageType = StageType.Combat;
        [SerializeField] private StageMapRole mapRole = StageMapRole.Regular;
        [Tooltip("노드 위에 표시할 건물/장식 프리팹입니다.")]
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private Sprite icon;
        [Tooltip("비워 두면 프리팹 이름을 표시합니다.")]
        [SerializeField] private string displayName;

        public StageType StageType => stageType;
        public StageMapRole MapRole => mapRole;
        public GameObject VisualPrefab => visualPrefab;
        public Sprite Icon => icon;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) && visualPrefab != null
            ? visualPrefab.name
            : displayName;

        internal void Validate()
        {
            if (!System.Enum.IsDefined(typeof(StageType), stageType) ||
                !System.Enum.IsDefined(typeof(StageMapRole), mapRole))
                throw new InvalidOperationException("[StageNodeVisualData] Stage type or map role is invalid.");
            if (visualPrefab == null)
                throw new InvalidOperationException(
                    $"[StageNodeVisualData] Visual prefab is required. Key: ({stageType}, {mapRole})");
        }
    }

    [CreateAssetMenu(fileName = "NewStageNodeVisualData", menuName = "ProjectL/Database/View/StageNodeVisualData")]
    internal sealed class StageNodeVisualDataSO : DatabaseItem
    {
        [SerializeField] private StageNodeVisualData stageNodeVisualData = new();

        public override IData GetData() => stageNodeVisualData;
    }
}
