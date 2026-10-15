#nullable enable
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact
{
    /// <summary>호출별 사실로 초기 상태, 편집 제안과 공간 미리보기를 계산하는 순수 계약입니다.</summary>
    public interface IArtifactEditCalculation
    {
        ArtifactState CreateInitialState(int width, int height, IReadOnlyList<ArtifactDefinition> definitions);
        ArtifactEditProposal Place(ArtifactState state, int artifactId,
            ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation);
        ArtifactEditProposal Move(ArtifactState state, int instanceId,
            ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation);
        ArtifactEditProposal ReturnToPool(ArtifactState state, int instanceId, ArtifactDefinition? definition);
        ArtifactEditProposal ExpandBoard(ArtifactState state, int width, int height,
            IReadOnlyList<ArtifactDefinition> placedDefinitions);
        ArtifactPlacementPreview PreviewPlace(ArtifactState state, int artifactId,
            ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation);
        ArtifactPlacementPreview PreviewMove(ArtifactState state, int instanceId,
            ArtifactDefinition? definition, CellCoord anchor, Rotation4 rotation);
    }
}
