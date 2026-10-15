using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item
{
    /// <summary>
    /// DB에서 조회하는 아티팩트 규칙 데이터입니다.
    /// 기존 Core 정의의 데이터 복사와 검증을 재사용합니다.
    /// </summary>
    public sealed class ArtifactDefinitionData : IData
    {
        public ArtifactDefinition Definition { get; }

        public int ArtifactId => Definition.ArtifactId;
        public int InitialCount => Definition.InitialCount;

        public IReadOnlyList<CellCoord> Cells => Definition.Cells;

        public IReadOnlyList<ArtifactEffectData> Effects => Definition.Effects;

        public ArtifactDefinitionData(
            int artifactId, int initialCount, 
            IReadOnlyList<CellCoord> cells, IReadOnlyList<ArtifactEffectData> effecs)
        {
            Definition = new ArtifactDefinition(artifactId, initialCount, cells, effecs);
        }
    }
}
