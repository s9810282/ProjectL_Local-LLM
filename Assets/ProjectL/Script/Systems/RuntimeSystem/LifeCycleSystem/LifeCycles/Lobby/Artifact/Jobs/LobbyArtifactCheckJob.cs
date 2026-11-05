using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact.Jobs
{
    /// <summary>
    /// 아티팩트 편집·프리뷰 호출 Job 바로 뒤에서, 그 호출이 처리 기록을 남겼는지 확인합니다(로비 재구성 설계 §4.5, R4).
    /// </summary>
    /// <remarks>
    /// 호출 뒤에도 같은 번호가 처리 안 된 다음 요청으로 남아 있으면 기록 누락이므로 아티팩트 도메인만 멈춥니다(D11, Q-LJ2 A).
    /// 입력이 지워지거나 더 새 입력으로 바뀌었으면 기다림을 끝낸 것으로 봅니다.
    /// </remarks>
    internal sealed class LobbyArtifactCheckJob : IJobService
    {
        private readonly LobbyArtifactDomain _domain;
        private readonly LobbyArtifactRequestKind _kind;
        private readonly int _number;

        /// <summary>호출 Job과 같은 우선순위입니다. 같은 우선순위는 등록 순서를 지키므로 호출 바로 뒤에 돕니다(계획 LP3).</summary>
        public int Priority => LobbyJobPriority.Artifact;
        public bool IsCompleted { get; private set; }

        internal LobbyArtifactCheckJob(LobbyArtifactDomain domain, LobbyArtifactRequestKind kind, int number)
        {
            _domain = domain ?? throw new ArgumentNullException(nameof(domain));
            // 종류·번호는 유일한 생성 입구 LobbyArtifactDomain.CreateCheck가 검사합니다(로비 리팩토링 2차 설계 N-20).
            _kind = kind;
            _number = number;
        }

        public void Execute()
        {
            if (IsCompleted)
                return;

            if (_domain.IsStillPending(_kind, _number))
            {
                string request = _kind == LobbyArtifactRequestKind.Edit ? "편집" : "프리뷰";
                _domain.Stop($"[LobbyArtifactCheckJob] {request} 요청 {_number}의 Core 처리가 기록되지 않았습니다.");
            }
            IsCompleted = true;
        }
    }
}
