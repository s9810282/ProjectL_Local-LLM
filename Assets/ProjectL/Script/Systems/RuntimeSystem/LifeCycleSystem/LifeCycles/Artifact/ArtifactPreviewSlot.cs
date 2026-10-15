namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact
{
    /// <summary>
    /// 프리뷰 요청을 최신 하나만 보관합니다. 대기 중 Job은 최대 하나이며, 그 Job은 실행될 때 가장 최근 요청을 가져갑니다.
    /// </summary>
    /// <remarks>
    /// Runner에는 개별 Job 제거가 없으므로 요청이 바뀔 때마다 Job을 쌓지 않습니다(아티팩트 팝업 계획 D8·D8a).
    /// </remarks>
    internal sealed class ArtifactPreviewSlot
    {
        private ArtifactPreviewRequest _pending;
        private bool _isJobQueued;

        /// <summary>마지막으로 끝난 유효한 프리뷰입니다. 무효화되면 null입니다.</summary>
        internal ArtifactPreviewRequest Latest { get; private set; }

        /// <summary>대기 요청을 교체합니다. 새 Job을 등록해야 하면 true입니다.</summary>
        internal bool Offer(ArtifactPreviewRequest request)
        {
            _pending?.Cancel();
            _pending = request;
            if (_isJobQueued)
                return false;

            _isJobQueued = true;
            return true;
        }

        /// <summary>Job이 실행될 때 호출합니다. 대기 요청이 없으면(무효화됨) false입니다.</summary>
        internal bool TryTakePending(out ArtifactPreviewRequest request)
        {
            _isJobQueued = false;
            request = _pending;
            _pending = null;
            return request != null;
        }

        internal void Publish(ArtifactPreviewRequest request)
        {
            Latest = request;
        }

        /// <summary>대기 요청을 취소하고 마지막 결과를 버립니다. 이미 등록된 Job은 실행될 때 아무것도 하지 않습니다.</summary>
        internal void Invalidate()
        {
            _pending?.Cancel();
            _pending = null;
            Latest = null;
        }
    }
}
