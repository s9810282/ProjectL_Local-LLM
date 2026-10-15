using System;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item
{
    /// <summary>
    /// 아티팩트 규칙을 입력하는 SO입니다.
    /// GetData 호출 시 새로운 규칙 데이터를 생성합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtifactDefinitionData",
        menuName = "ProjectL/Database/InGame/Artifact Definition Data")]
    public sealed class ArtifactDefinitionDataSO : DatabaseItem
    {
        [Header("Identity")]
        [SerializeField, Min(0)]
        private int artifactId;

        [Header("Initial Settings")]
        [SerializeField, Min(0)]
        private int initialCount;

        [Header("Shape")]
        [SerializeField]
        private CellInput[] cells =
        {
            new CellInput(0, 0)
        };

        [Header("Effects")]
        [SerializeField]
        private ArtifactEffectData[] effects =
            Array.Empty<ArtifactEffectData>();

        public int ArtifactId => artifactId;

        public override IData GetData()
        {
            return new ArtifactDefinitionData(
                artifactId, initialCount,
                CellInput.ToCellCoords(cells),
                effects ?? Array.Empty<ArtifactEffectData>());
               
        }
    }
}
