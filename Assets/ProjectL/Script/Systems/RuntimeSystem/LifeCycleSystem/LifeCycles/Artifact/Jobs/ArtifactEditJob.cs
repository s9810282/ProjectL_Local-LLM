using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Jobs
{
    /// <summary>
    /// 편집 요청 하나를 Core에 한 번 전달하고, 그 호출이 게시한 결과를 요청에 기록합니다.
    /// 취소된 요청은 Core를 부르지 않고 완료합니다. 정상 거절도 한 번 완료합니다.
    /// </summary>
    internal struct ArtifactEditJob : IJobService
    {
        private readonly ArtifactEditRequest _request;
        private readonly IArtifactEditingService _artifacts;
        private readonly IArtifactEditResultReadSink _results;

        public int Priority { get; }
        public bool IsCompleted { get; private set; }

        internal ArtifactEditJob(ArtifactEditRequest request, IArtifactEditingService artifacts,
            IArtifactEditResultReadSink results, int priority)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            Priority = priority;
            IsCompleted = false;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            if (!_request.TryStart())
            {
                IsCompleted = true;
                return;
            }

            try
            {
                switch (_request.Kind)
                {
                    case ArtifactEditKind.Place:
                        _artifacts.Place(_request.ArtifactId, _request.Anchor, _request.Rotation);
                        break;
                    case ArtifactEditKind.Move:
                        _artifacts.Move(_request.InstanceId, _request.Anchor, _request.Rotation);
                        break;
                    case ArtifactEditKind.Return:
                        _artifacts.ReturnToPool(_request.InstanceId);
                        break;
                    default:
                        throw new InvalidOperationException($"[ArtifactEditJob] Unknown edit kind: {_request.Kind}");
                }

                // 정상 반환한 호출은 결과를 게시합니다. 이 값이 이번 요청의 결과입니다(인계 §5).
                ArtifactActionResult result = _results.LastEditResult
                    ?? throw new InvalidOperationException("[ArtifactEditJob] Core returned without publishing an edit result.");
                _request.Complete(result);
            }
            catch
            {
                _request.Fail();
                throw;
            }

            IsCompleted = true;
        }
    }
}
