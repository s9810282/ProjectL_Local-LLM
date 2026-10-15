using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup
{
    /// <summary>팝업에 보일 문구입니다. 요청 상태와 Core 실패 사유를 사람이 읽는 문장으로 바꿉니다.</summary>
    internal static class ArtifactPopupText
    {
        internal const string Waiting = "보드를 준비하는 중입니다.";

        /// <summary>끝난 편집 요청의 문구입니다. 성공이면 빈 문자열입니다.</summary>
        internal static string ForEdit(ArtifactEditRequest request)
        {
            switch (request.Status)
            {
                case ArtifactRequestStatus.Completed:
                    ArtifactActionResult result = request.Result.Value;
                    return result.Succeeded ? string.Empty : ForFailure(result.FailureReason);
                case ArtifactRequestStatus.Faulted:
                    return "처리 중 오류가 발생했습니다.";
                default:
                    return string.Empty;
            }
        }

        internal static string ForFailure(ArtifactActionFailureReason reason)
        {
            switch (reason)
            {
                case ArtifactActionFailureReason.LayoutLocked: return "전투가 끝나야 배치를 바꿀 수 있습니다.";
                case ArtifactActionFailureReason.DefinitionNotFound: return "알 수 없는 아티팩트입니다.";
                case ArtifactActionFailureReason.NotInPool: return "배치할 아티팩트가 남아 있지 않습니다.";
                case ArtifactActionFailureReason.InstanceNotFound: return "배치된 아티팩트를 찾을 수 없습니다.";
                case ArtifactActionFailureReason.OutOfBounds: return "보드 밖에는 놓을 수 없습니다.";
                case ArtifactActionFailureReason.CellOccupied: return "다른 아티팩트와 겹칩니다.";
                case ArtifactActionFailureReason.InvalidBoardSize: return "보드 크기가 올바르지 않습니다.";
                case ArtifactActionFailureReason.BoardShrinkNotAllowed: return "보드는 줄일 수 없습니다.";
                case ArtifactActionFailureReason.BoardSizeUnchanged: return "보드 크기가 그대로입니다.";
                case ArtifactActionFailureReason.BoardRebuildFailed: return "보드를 다시 구성하지 못했습니다.";
                default: return "요청을 처리하지 못했습니다.";
            }
        }
    }
}
