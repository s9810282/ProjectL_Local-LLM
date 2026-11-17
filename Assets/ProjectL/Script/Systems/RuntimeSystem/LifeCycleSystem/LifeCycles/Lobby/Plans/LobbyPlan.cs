using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact.Plans;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Jobs;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.RunStart;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Plans
{
    /// <summary>
    /// 로비의 다음 할 일을 정하고 그 Job 목록을 만듭니다. 판단의 유일한 원본은 <see cref="Decide"/>입니다(로비 재구성 설계 §4).
    /// </summary>
    internal static class LobbyPlan
    {
        // 할 일이 없을 때 돌려주는 목록. 로비 활성 동안 Runner가 빌 때마다 판단하므로 새로 만들지 않는다. JobRegister는 목록을 바꾸지 않는다.
        private static readonly List<IJobService> NoJobs = new(0);

        /// <param name="buffers">출발의 새 Run Job이 쓸 전투 상태 읽기·맵 입력 쓰기·로스터 읽기 싱크입니다.</param>
        /// <param name="lifeCycles">사실 읽기·아티팩트 확인 입구·전환 서비스를 꺼낼 LifeCycle 그룹입니다.</param>
        /// <param name="audio">로비 출발 소리를 재생할 서비스입니다.</param>
        internal static List<IJobService> Register(IInGameSystemGroupService service, JobType jobType, IBufferQuery buffers,
            ILifeCycleSystemGroupService lifeCycles, IAudioSystemService audio)
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service));

            switch (jobType)
            {
                case JobType.Next:
                    return Next(service, buffers, lifeCycles, audio);
                default:
                    // 출발·편집·프리뷰는 Next가 판단해 직접 만든다. 따로 등록하는 경로는 없다(설계 Q-LJ5).
                    throw new ArgumentOutOfRangeException(nameof(jobType), jobType, "[LobbyPlan] 알 수 없는 로비 Job 종류입니다.");
            }
        }

        /// <summary>사실을 한 번 읽어 다음 단계를 정하고 그 Job 목록을 만듭니다. 할 일이 없으면 공유 빈 목록입니다.</summary>
        private static List<IJobService> Next(IInGameSystemGroupService service, IBufferQuery buffers,
            ILifeCycleSystemGroupService lifeCycles, IAudioSystemService audio)
        {
            if (lifeCycles == null)
                throw new ArgumentNullException(nameof(lifeCycles));

            // Group Context의 속성은 만들 때 서비스 조회(없으면 예외)로 채워져 다시 검사하지 않습니다(로비 리팩토링 2차 설계 N-23).
            LobbyProgressFacts facts = lifeCycles.LobbyProgress.Read();
            LobbyStep step = Decide(in facts);
            switch (step.Kind)
            {
                case LobbyStepKind.None:
                    return NoJobs;
                case LobbyStepKind.ArtifactEdit:
                    return new List<IJobService>
                    {
                        ArtifactPlan.CreateCommand(step.Number, service.CoreGroup.ArtifactCommands),
                        lifeCycles.LobbyArtifactChecks.CreateCheck(LobbyArtifactRequestKind.Edit, step.Number)
                    };
                case LobbyStepKind.ArtifactPreview:
                    return new List<IJobService>
                    {
                        ArtifactPlan.CreatePreview(step.Number, service.CoreGroup.ArtifactCommands),
                        lifeCycles.LobbyArtifactChecks.CreateCheck(LobbyArtifactRequestKind.Preview, step.Number)
                    };
                case LobbyStepKind.Depart:
                {
                    if (buffers == null)
                        throw new ArgumentNullException(nameof(buffers));
                    ILifeCycleManager manager = lifeCycles.LifeCycleManager;
                    LobbyDepartureRequest request = facts.Departure;
                    // 같은 우선순위라 등록 순서대로 같은 Runner Tick에 이어 실행된다. 전환은 마지막 Job 안에서 한다(Q-LJ1).
                    // 메인 키는 제출 때 요청에 실린 값만 넘긴다. 실행 순간의 화면 선택은 읽지 않는다(로비 메인 선택 설계 LM5).
                    return new List<IJobService>
                    {
                        new RunStartJob(service.CoreGroup.RunStart, new LobbyRunStartOutcome(request), request.MainCharacterKey,
                            buffers.CombatStateReadSink, buffers.StageViewWriteSink, buffers.CharacterReadSink, LobbyJobPriority.Departure),
                        new LobbyTransitionJob(request, manager, audio)
                    };
                }
                case LobbyStepKind.Broken:
                    throw new InvalidOperationException(
                        $"[LobbyPlan] Runner가 빈 뒤에도 출발 요청이 진행 단계에 남았습니다({step}, 단계 {facts.Departure.Status}).");
                default:
                    throw new InvalidOperationException($"[LobbyPlan] 알 수 없는 단계입니다: {step}");
            }
        }

        /// <summary>
        /// 지금 사실로 로비의 다음 할 일을 정합니다. 판단의 유일한 원본입니다(로비 재구성 설계 §4.3).
        /// 위에서부터 처음 맞는 줄 하나만 고릅니다. 아티팩트가 출발보다 먼저입니다.
        /// </summary>
        internal static LobbyStep Decide(in LobbyProgressFacts facts)
        {
            // 0. 로딩 중에는 아무것도 하지 않는다.
            if (facts.IsLoading)
                return LobbyStep.Nothing;

            // 1~3. 아티팩트. 기록 누락으로 멈춘 아티팩트는 건너뛰고 출발을 계속 받는다(D11·D20).
            if (!facts.IsArtifactStopped)
            {
                if (facts.IsBoardInitialized)
                {
                    if (facts.ArtifactCommandNumber > 0)
                        return new LobbyStep(LobbyStepKind.ArtifactEdit, facts.ArtifactCommandNumber);
                    if (facts.ArtifactPreviewNumber > 0)
                        return new LobbyStep(LobbyStepKind.ArtifactPreview, facts.ArtifactPreviewNumber);
                }
                else if (facts.ArtifactCommandNumber > 0)
                {
                    // 보드가 준비되기 전의 편집은 등록하지 않고, 남은 편집을 버리지 않도록 출발도 기다린다.
                    return LobbyStep.Nothing;
                }
            }

            // 4~6. 출발. 제출만 된 요청은 출발을 걸고, Runner가 빈 뒤에도 진행 단계에 남은 요청은 어긋남이다.
            LobbyDepartureRequest departure = facts.Departure;
            if (departure == null || !departure.IsPending)
                return LobbyStep.Nothing;
            if (departure.IsQueued)
                return new LobbyStep(LobbyStepKind.Depart, departure.Number);
            return new LobbyStep(LobbyStepKind.Broken, departure.Number);
        }

        /// <summary>
        /// 로비를 떠나도 되는지의 유일한 정의입니다(로비 재구성 설계 §4.4). Runner가 비었는지는 보지 않습니다(R5).
        /// </summary>
        internal static bool CanLeave(in LobbyProgressFacts facts)
        {
            // 아티팩트: 멈췄거나 처리 안 된 편집이 없다. 답 안 된 프리뷰는 이탈을 막지 않는다.
            if (!facts.IsArtifactStopped && facts.ArtifactCommandNumber > 0)
                return false;

            // 출발: 요청이 없거나 대기 중이 아니거나, 새 Run이 성공한 정상 출발의 전환 중이다.
            LobbyDepartureRequest departure = facts.Departure;
            return departure == null || !departure.IsPending || departure.IsTransitionSucceeded;
        }
    }
}
