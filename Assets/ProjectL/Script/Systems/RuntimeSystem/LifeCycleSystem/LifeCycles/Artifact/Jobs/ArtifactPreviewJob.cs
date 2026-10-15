using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Jobs
{
    /// <summary>
    /// 실행 시점의 최신 프리뷰 요청 하나를 Core에 전달합니다. 무효화돼 요청이 없거나,
    /// 이동 대상 인스턴스가 더 이상 배치돼 있지 않으면 Core를 부르지 않습니다(아티팩트 팝업 계획 D8).
    /// </summary>
    internal struct ArtifactPreviewJob : IJobService
    {
        private readonly ArtifactPreviewSlot _slot;
        private readonly IArtifactEditingService _artifacts;
        private readonly IArtifactPreviewReadSink _previews;
        private readonly IArtifactBufferReadSink _board;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal ArtifactPreviewJob(ArtifactPreviewSlot slot, IArtifactEditingService artifacts,
            IArtifactPreviewReadSink previews, IArtifactBufferReadSink board, int priority)
        {
            _slot = slot ?? throw new ArgumentNullException(nameof(slot));
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
            _previews = previews ?? throw new ArgumentNullException(nameof(previews));
            _board = board ?? throw new ArgumentNullException(nameof(board));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            if (!_slot.TryTakePending(out ArtifactPreviewRequest request))
            {
                IsCompleted = true;
                return;
            }

            if (request.Kind == ArtifactPreviewKind.Move && !IsPlaced(request.InstanceId))
            {
                // 반환·교체된 인스턴스의 이동 프리뷰는 의미가 없으므로 실행하지 않습니다.
                request.Cancel();
                IsCompleted = true;
                return;
            }

            if (!request.TryStart())
            {
                IsCompleted = true;
                return;
            }

            try
            {
                if (request.Kind == ArtifactPreviewKind.Place)
                    _artifacts.PreviewPlace(request.ArtifactId, request.Anchor, request.Rotation);
                else
                    _artifacts.PreviewMove(request.InstanceId, request.Anchor, request.Rotation);

                ArtifactPlacementPreview result = _previews.LastPreview
                    ?? throw new InvalidOperationException("[ArtifactPreviewJob] Core returned without publishing a preview.");
                request.Complete(result);
                _slot.Publish(request);
            }
            catch
            {
                request.Fail();
                throw;
            }

            IsCompleted = true;
        }

        private bool IsPlaced(int instanceId)
        {
            var placed = _board.Current.PlacedItems;
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].InstanceId == instanceId)
                    return true;
            }

            return false;
        }
    }
}
