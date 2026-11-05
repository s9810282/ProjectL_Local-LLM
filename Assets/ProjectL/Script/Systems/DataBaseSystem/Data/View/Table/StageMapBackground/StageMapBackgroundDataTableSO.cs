using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.StageMapBackground
{
    /// <summary>챕터별 배경 설정을 모읍니다. 챕터 키 중복은 조회 테이블을 만들 때 검사합니다.</summary>
    [CreateAssetMenu(fileName = "NewStageMapBackgroundDataTable", menuName = "ProjectL/Database/View/StageMapBackgroundDataTable")]
    internal sealed class StageMapBackgroundDataTableSO : DatabaseTable
    {
    }
}
