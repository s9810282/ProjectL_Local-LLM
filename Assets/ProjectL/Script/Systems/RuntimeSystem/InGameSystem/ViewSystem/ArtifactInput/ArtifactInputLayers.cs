using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput
{
    /// <summary>아티팩트 화면의 입력 Layer입니다. 비활성화에는 ClearLayer를 사용합니다.</summary>
    internal static class ArtifactInputLayers
    {
        internal static readonly InputLayer Artifact = new(
            ArtifactInputNames.Map,
            ArtifactInputNames.CommonMap,
            ArtifactInputNames.Map);
    }
}
