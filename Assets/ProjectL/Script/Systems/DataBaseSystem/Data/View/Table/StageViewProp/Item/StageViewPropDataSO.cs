using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp.Item
{
    [System.Serializable]
    internal sealed class StageViewPropData : IData
    {
        [SerializeField, Min(1)] private int stageKey = 1;
        [SerializeField] private GameObject gameObject;
        [Tooltip("필드 기준 위치 보정값입니다.")]
        [SerializeField] private Vector3 positionOffset;
        [Tooltip("필드 기준 회전 보정값(오일러 각)입니다.")]
        [SerializeField] private Vector3 rotationOffset;
        [Tooltip("프리팹의 원래 로컬 크기에 축별로 곱하는 배율입니다. (1, 1, 1)은 원래 크기를 유지합니다.")]
        [SerializeField] private Vector3 scaleOffset = Vector3.one;

        public int StageKey => stageKey;
        public GameObject GameObject => gameObject;
        public Vector3 PositionOffset => positionOffset;
        public Vector3 RotationOffset => rotationOffset;
        public Vector3 ScaleOffset => scaleOffset;
    }

    [CreateAssetMenu(
        fileName = "NewStageViewPropData",
        menuName = "ProjectL/Database/View/StageViewPropData")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.ViewProp.Item", sourceAssembly: "ProjectL.Systems", sourceClassName: "StageViewPropDataSO")]
    internal sealed class StageViewPropDataSO : DatabaseItem
    {
        [SerializeField] private StageViewPropData stageViewPropData;

        public override IData GetData() => stageViewPropData;
    }
}
