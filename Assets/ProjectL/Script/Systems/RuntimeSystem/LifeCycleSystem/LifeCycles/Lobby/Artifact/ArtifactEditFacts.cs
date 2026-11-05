using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact
{
    /// <summary>
    /// 아티팩트 편집 입력 가운데 다음에 처리할 번호를 고르는 규칙의 유일한 자리입니다(로비 재구성 설계 P3·R1).
    /// 로비 아티팩트 도메인(로비 판단)이 부릅니다. 상태를 갖지 않습니다.
    /// </summary>
    internal static class ArtifactEditFacts
    {
        /// <summary>Core 처리 번호 다음 첫 편집 입력의 번호입니다. 없으면 0입니다.</summary>
        internal static int NextCommandNumber(IArtifactViewBufferReadSink input, IArtifactCommandResultReadSink results)
        {
            return input.TryGetNextCommand(results.HandledCommandNumber, out var command) ? command.Number : 0;
        }

        /// <summary>아직 답하지 않은 최신 프리뷰의 번호입니다. 없으면 0입니다.</summary>
        internal static int PendingPreviewNumber(IArtifactViewBufferReadSink input, IArtifactCommandResultReadSink results)
        {
            return input.HasPreview && input.Preview.Number > results.AnsweredPreviewNumber ? input.Preview.Number : 0;
        }
    }
}
