using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageNode.Item
{
    /// <summary>
    /// 스테이지 맵 노드의 외형입니다. StageKey 지정이 없으면 (StageType, StageMapRole) 공통 외형을 사용합니다.
    /// View는 조회에 필요한 키와 종류·역할을 Buffer에서 받습니다.
    /// 시작 노드 외형은 StageMapViewData.StartVisualPrefab을 사용합니다. 조회마다 새로 만드는 읽기 전용 값입니다.
    /// </summary>
    internal sealed class StageNodeVisualData : IData
    {
        internal StageNodeVisualData(int stageKey, StageType stageType, StageMapRole mapRole, GameObject visualPrefab,
            string displayName, Vector3 rotationOffset = default)
        {
            StageKey = stageKey;
            StageType = stageType;
            MapRole = mapRole;
            VisualPrefab = visualPrefab;
            RotationOffset = rotationOffset;
            DisplayName = string.IsNullOrWhiteSpace(displayName) && visualPrefab != null
                ? visualPrefab.name
                : displayName;
        }

        public int StageKey { get; }
        public StageType StageType { get; }
        public StageMapRole MapRole { get; }
        public GameObject VisualPrefab { get; }
        public Vector3 RotationOffset { get; }
        public string DisplayName { get; }

        internal void Validate()
        {
            if (!float.IsFinite(RotationOffset.x) || !float.IsFinite(RotationOffset.y) || !float.IsFinite(RotationOffset.z))
                throw new InvalidOperationException($"[StageNodeVisualData] StageKey {StageKey}: rotationOffset은 유한한 값이어야 합니다.");
            if (StageKey < 0)
                throw new InvalidOperationException($"[StageNodeVisualData] StageKey는 0 이상이어야 합니다. 값: {StageKey}");
            if (!System.Enum.IsDefined(typeof(StageType), StageType) ||
                !System.Enum.IsDefined(typeof(StageMapRole), MapRole))
                throw new InvalidOperationException("[StageNodeVisualData] Stage type or map role is invalid.");
        }
    }
}
