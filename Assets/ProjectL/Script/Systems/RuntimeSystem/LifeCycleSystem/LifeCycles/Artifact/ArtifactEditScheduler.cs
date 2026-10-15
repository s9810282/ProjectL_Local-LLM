using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Plans;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact
{
    /// <summary>
    /// 아티팩트 편집·프리뷰 요청을 받아 Plan으로 Job을 만들고 공용 Runner에 등록합니다.
    /// 요청을 받을지(오류 상태·초기화·편집 대기)와 프리뷰 교체·무효화 규칙을 한 곳에서 지킵니다.
    /// </summary>
    /// <remarks>
    /// 규칙(아티팩트 팝업 계획): 편집은 한 번에 하나(D10), 프리뷰는 최신 하나·대기 Job 최대 하나(D8),
    /// 편집 요청이 생기면 프리뷰 무효화(D8), 취소된 요청은 Job이 Core를 부르지 않고 완료(D8a),
    /// 오류 상태·보드 미초기화면 요청 거부(D11·D7a). Runner.Clear는 쓰지 않습니다(다른 화면 Job까지 지움).
    /// </remarks>
    internal sealed class ArtifactEditScheduler
    {
        private readonly IJobRunner _runner;
        private readonly IArtifactEditingService _artifacts;
        private readonly IArtifactBufferReadSink _board;
        private readonly IArtifactEditResultReadSink _results;
        private readonly IArtifactPreviewReadSink _previews;
        private readonly RuntimeFaultBoundary _faults;
        private readonly ArtifactPreviewSlot _previewSlot = new();

        internal ArtifactEditScheduler(IJobRunner runner, IArtifactEditingService artifacts,
            IArtifactBufferReadSink board, IArtifactEditResultReadSink results,
            IArtifactPreviewReadSink previews, RuntimeFaultBoundary faults)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _previews = previews ?? throw new ArgumentNullException(nameof(previews));
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        /// <summary>마지막으로 받은 편집 요청입니다. 상태와 결과는 이 요청에서 읽습니다.</summary>
        internal ArtifactEditRequest CurrentEdit { get; private set; }

        /// <summary>마지막으로 끝난 유효한 프리뷰입니다. 무효화되면 null입니다.</summary>
        internal ArtifactPreviewRequest LatestPreview => _previewSlot.Latest;

        /// <summary>처리 전이거나 실행 중인 편집이 있습니다.</summary>
        internal bool IsEditPending => CurrentEdit != null && CurrentEdit.IsPending;

        /// <summary>새 요청을 받을 수 있는 상태입니다. 오류 상태이거나 보드가 초기화 전이면 false입니다.</summary>
        internal bool CanAcceptRequests => !_faults.IsFaulted && _board.Current.IsInitialized;

        /// <summary>편집 요청을 등록합니다. 받지 않으면 false이며 아무것도 등록하지 않습니다.</summary>
        internal bool TryRequestEdit(ArtifactEditRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (!CanAcceptRequests || IsEditPending || request.Status != ArtifactRequestStatus.Queued)
                return false;

            // 편집 뒤 보드가 바뀌므로 이전 프리뷰는 의미가 없습니다.
            _previewSlot.Invalidate();
            CurrentEdit = request;
            _runner.Register(ArtifactPlan.CreateEditJob(request, _artifacts, _results));
            return true;
        }

        /// <summary>
        /// 프리뷰 요청을 최신 값으로 둡니다. 대기 중 프리뷰 Job이 있으면 새로 등록하지 않습니다.
        /// 편집 대기 중에는 받지 않습니다(곧 보드가 바뀜).
        /// </summary>
        internal bool TryRequestPreview(ArtifactPreviewRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (!CanAcceptRequests || IsEditPending || request.Status != ArtifactRequestStatus.Queued)
                return false;

            if (_previewSlot.Offer(request))
                _runner.Register(ArtifactPlan.CreatePreviewJob(_previewSlot, _artifacts, _previews, _board));
            return true;
        }

        /// <summary>회전 변경·드래그 종료·닫기·다시 열기 때 부릅니다. 대기 프리뷰를 취소하고 마지막 결과를 버립니다.</summary>
        internal void InvalidatePreview()
        {
            _previewSlot.Invalidate();
        }

        /// <summary>
        /// 씬 이탈 때 부릅니다. 대기 프리뷰와 실행 전 편집을 취소합니다. 실행 중이거나 끝난 요청은 그대로 둡니다.
        /// </summary>
        internal void CancelPending()
        {
            _previewSlot.Invalidate();
            CurrentEdit?.Cancel();
        }
    }
}
