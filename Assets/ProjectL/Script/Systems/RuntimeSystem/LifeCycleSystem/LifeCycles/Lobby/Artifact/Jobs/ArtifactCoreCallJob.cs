using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact.Jobs
{
    /// <summary>
    /// 기대 번호만 Core에 전달합니다. 요청 조회·취소 확인·계산·게시는 Core가 맡습니다.
    /// 편집과 프리뷰는 부르는 Core 입구 하나만 달라 한 Job으로 둡니다(로비 리팩토링 2차 설계 N-48, 옛 ArtifactCommandJob·ArtifactPreviewJob).
    /// </summary>
    internal sealed class ArtifactCoreCallJob : IJobService
    {
        private readonly LobbyArtifactRequestKind _kind;
        private readonly int _expectedRequestNumber;
        private readonly IArtifactCommandProcessingService _core;

        public int Priority => LobbyJobPriority.Artifact;
        public bool IsCompleted { get; private set; }
        private bool _started;

        internal ArtifactCoreCallJob(LobbyArtifactRequestKind kind, int expectedRequestNumber,
            IArtifactCommandProcessingService core)
        {
            if (!System.Enum.IsDefined(typeof(LobbyArtifactRequestKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (expectedRequestNumber <= 0) throw new ArgumentOutOfRangeException(nameof(expectedRequestNumber));
            _kind = kind;
            _expectedRequestNumber = expectedRequestNumber;
            _core = core ?? throw new ArgumentNullException(nameof(core));
        }

        public void Execute()
        {
            // 시작 가드와 완료 표지를 나눕니다. 완료는 Core가 정상 반환한 뒤에만 올립니다(옛 두 Job과 같음).
            if (_started) return;
            _started = true;
            // 취소·교체·이미 처리된 요청의 false도 정상 완료이며 처리 기록 확인은 LobbyArtifactCheckJob이 맡습니다.
            if (_kind == LobbyArtifactRequestKind.Edit) _core.ProcessNextCommand(_expectedRequestNumber);
            else _core.ProcessLatestPreview(_expectedRequestNumber);
            IsCompleted = true;
        }
    }
}
