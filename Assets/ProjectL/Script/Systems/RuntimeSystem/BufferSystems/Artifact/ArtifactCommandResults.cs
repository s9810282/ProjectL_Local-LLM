using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact
{
    /// <summary>편집·프리뷰의 처리 번호와 결과를 함께 읽습니다. 입력 번호와 같이 Buffer 수명 동안 유지합니다.</summary>
    public interface IArtifactCommandResultReadSink : IServiceModule
    {
        int HandledCommandNumber { get; }
        int AnsweredPreviewNumber { get; }
        ArtifactCommandResult? LastCommand { get; }
        ArtifactPreviewResult? LastAnsweredPreview { get; }
    }

    /// <summary>Core만 완성한 계산 제안과 그 요청 번호를 함께 게시합니다.</summary>
    internal interface IArtifactCommandResultStore : IServiceModule
    {
        void PublishCommand(int number, ArtifactViewCommandData data, ArtifactEditProposal proposal);
        void PublishAnswer(int number, ArtifactViewPreviewData data, ArtifactPlacementPreview preview);
    }

    public readonly struct ArtifactCommandResult
    {
        public int RequestNumber { get; }
        public ArtifactViewCommandData Data { get; }
        public ArtifactActionResult Result { get; }

        internal ArtifactCommandResult(int number, ArtifactViewCommandData data, ArtifactActionResult result)
        {
            RequestNumber = number;
            Data = data;
            Result = result;
        }
    }

    public readonly struct ArtifactPreviewResult
    {
        public int RequestNumber { get; }
        public ArtifactViewPreviewData Data { get; }
        /// <summary>유효한 요청이지만 이동 대상이 배치돼 있지 않아 Core가 계산을 생략했습니다.</summary>
        public bool IsSkipped => Preview == null;
        public ArtifactPlacementPreview Preview { get; }

        internal ArtifactPreviewResult(int number, ArtifactViewPreviewData data, ArtifactPlacementPreview preview)
        {
            RequestNumber = number;
            Data = data;
            Preview = preview;
        }
    }
}
