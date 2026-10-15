using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item
{
    /// <summary>
    /// StageRootData의 입력 SO입니다. 종류별 상세는 StageBaseData SO를 참조하고,
    /// GetData에서 순수 C# 상세 데이터로 변환합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "StageRootData", menuName = "ProjectL/Database/InGame/Stage Root Data")]
    public sealed class StageRootDataSO : DatabaseItem
    {
        [SerializeField, Min(1)] private int stageKey = 1;
        [SerializeField, Min(1)] private int mapKey = 1;
        [SerializeField] private StageMapRole mapRole = StageMapRole.Regular;
        [SerializeField] private StageBaseData stageData;

        public override IData GetData()
        {
            IStageData detail = stageData != null ? stageData.GetData() as IStageData : null;
            return new StageRootData(stageKey, mapKey, mapRole, detail);
        }
    }
}
