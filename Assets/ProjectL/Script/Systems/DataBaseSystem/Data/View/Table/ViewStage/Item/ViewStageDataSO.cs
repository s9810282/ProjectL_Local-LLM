using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageEnvironment.Item;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.ViewStage.Item
{
    /// <summary>한 전투 스테이지를 표시하는 데 필요한 View 전용 사실 묶음입니다.</summary>
    internal sealed class ViewStageData : IData
    {
        internal ViewStageData(StageViewPropData prop, StageEnvironmentVisualData environment)
        {
            Prop = prop ?? throw new ArgumentNullException(nameof(prop));
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            if (Prop.StageKey < 1 || Prop.StageKey != Environment.StageKey)
                throw new InvalidOperationException("[ViewStageData] 프랍과 환경 외형의 StageKey가 같아야 합니다.");
            // 배경 방식별 칸(프리팹·맵 SO·바닥 타일)은 프랍 한 곳이 검사한다.
            Prop.Validate();
        }

        public int StageKey => Prop.StageKey;
        public StageViewPropData Prop { get; }
        public StageEnvironmentVisualData Environment { get; }
    }

    [CreateAssetMenu(fileName = "NewViewStageData", menuName = "ProjectL/Database/View/ViewStageData")]
    internal sealed class ViewStageDataSO : DatabaseItem
    {
        [Serializable]
        private struct RotationInput
        {
            [SerializeField, Min(1)] private long sourceId;
            [SerializeField] private Rotation4 rotation;

            internal EnvironmentVisualRotation ToData() => new(sourceId, rotation);
        }

        [Tooltip("StageKey와 맵 프랍·변환 보정입니다.")]
        [SerializeField] private StageViewPropData stageViewPropData;
        [Tooltip("Terrain 원본 ID별 외형 회전입니다. 비어 있으면 R0입니다.")]
        [SerializeField] private RotationInput[] terrainRotations = Array.Empty<RotationInput>();
        [Tooltip("Object 원본 ID별 외형 회전입니다. 비어 있으면 R0입니다.")]
        [SerializeField] private RotationInput[] objectRotations = Array.Empty<RotationInput>();

        public override IData GetData()
        {
            if (stageViewPropData == null)
                throw new InvalidOperationException("[ViewStageDataSO] StageViewPropData가 필요합니다.");

            EnvironmentVisualRotation[] terrain = (terrainRotations ?? Array.Empty<RotationInput>())
                .Select(input => input.ToData())
                .ToArray();
            EnvironmentVisualRotation[] objects = (objectRotations ?? Array.Empty<RotationInput>())
                .Select(input => input.ToData())
                .ToArray();
            var environment = new StageEnvironmentVisualData(stageViewPropData.StageKey, terrain, objects);
            return new ViewStageData(stageViewPropData, environment);
        }
    }
}
