using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.Artifact
{
    /// <summary>
    /// 아티팩트 표현 SO 목록을 보관하는 테이블입니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ArtifactViewDataTable",
        menuName = "ProjectL/Database/View/Artifact View Table")]
    public sealed class ArtifactViewDataTableSO : DatabaseTable
    {
    }
}