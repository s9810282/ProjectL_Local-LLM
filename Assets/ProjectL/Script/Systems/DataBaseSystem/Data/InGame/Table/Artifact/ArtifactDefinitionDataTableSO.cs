using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact
{
    /// <summary>
    /// 아티팩트 규칙 SO 목록을 보관하는 테이블입니다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ArtifactDefinitionDataTable",
        menuName = "ProjectL/Database/InGame/Artifact Definition Table")]
    public sealed class ArtifactDefinitionDataTableSO : DatabaseTable
    {
    }
}
