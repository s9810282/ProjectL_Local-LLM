using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageViewProp
{
    /// <summary>StageKey별 스테이지 프롭을 등록하는 View 테이블입니다. InGameGroup에서 옮겨 왔습니다.</summary>
    [CreateAssetMenu(
        fileName = "NewStageViewPropDataTable",
        menuName = "ProjectL/Database/View/StageViewPropDataTable")]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.ViewProp", sourceAssembly: "ProjectL.Systems", sourceClassName: "StageViewPropDataTableSO")]
    internal sealed class StageViewPropDataTableSO : DatabaseTable
    {
    }
}
