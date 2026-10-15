using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Jobs;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact.Plans
{
    /// <summary>세부 타입에 따라 서비스를 주입한 Job 목록을 구성합니다.</summary>
    /// <remarks>
    /// <see cref="Register"/>는 JobRegisterSystem이 등록한 기존 경로라 서명을 유지합니다.
    /// 아티팩트 팝업의 Job은 아래 생성 메서드로 만들고 호출자가 JobRunner에 직접 등록합니다(아티팩트 팝업 계획 D3).
    /// </remarks>
    internal static class ArtifactPlan
    {
        /// <summary>맵 준비 StageJob(0, 새 Run이면 1) 뒤에 실행되도록 정한 보드 초기화 우선순위입니다.</summary>
        internal const int BoardInitializationPriority = 2;

        /// <summary>보드를 처음 크기로 초기화하는 Job을 만듭니다. Core와 읽기 Sink만 받습니다(FW-101).</summary>
        internal static IJobService CreateBoardInitialization(IArtifactEditingService artifacts,
            IArtifactBufferReadSink board, int initialWidth, int initialHeight)
        {
            return new ArtifactBoardInitializeJob(artifacts, board, initialWidth, initialHeight,
                BoardInitializationPriority);
        }

        /// <summary>보드 초기화 뒤에 실행되도록 정한 편집·프리뷰 우선순위입니다.</summary>
        internal const int RequestPriority = 3;

        /// <summary>편집 요청 하나를 실행하는 Job을 만듭니다. Core와 편집 결과 Sink만 받습니다(FW-101).</summary>
        internal static IJobService CreateEditJob(ArtifactEditRequest request, IArtifactEditingService artifacts,
            IArtifactEditResultReadSink results)
        {
            return new ArtifactEditJob(request, artifacts, results, RequestPriority);
        }

        /// <summary>실행 시점의 최신 프리뷰 요청을 처리하는 Job을 만듭니다.</summary>
        internal static IJobService CreatePreviewJob(ArtifactPreviewSlot slot, IArtifactEditingService artifacts,
            IArtifactPreviewReadSink previews, IArtifactBufferReadSink board)
        {
            return new ArtifactPreviewJob(slot, artifacts, previews, board, RequestPriority);
        }

        internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            var jobs = new List<IJobService>();
            switch (jobType)
            {
                case JobType.None:
                    break;
                case JobType.Start:
                    // 임시 Job 골격입니다. 실제 작업 조합은 이 Plan에서 정의합니다.
                    jobs.Add(new ArtifactJob(service, 0));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "Unknown Artifact job type.");
            }

            return jobs;
        }
    }
}
