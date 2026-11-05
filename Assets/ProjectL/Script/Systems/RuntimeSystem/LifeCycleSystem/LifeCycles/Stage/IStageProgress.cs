using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>
    /// 이번 Stage 방문의 진행 사실과 상태 바꿈 입구입니다(검사 후속 정리 계획 TP-R9). <c>StagePlan</c>만 씁니다.
    /// Core 호출은 Job이 하고, 이 입구는 전투 진입 요청·시도 닫기·도메인 멈춤·확인 Job 만들기만 합니다.
    /// </summary>
    public interface IStageProgress : IServiceModule
    {
        /// <summary>지금 사실을 한 번 읽습니다. 부를 때마다 새로 읽습니다.</summary>
        StageProgressFacts Read();

        /// <summary>
        /// 전투 노드의 맵 명령으로 전투 진입을 요청하고 시도로 기억합니다. 진입 서비스는 Runner가 빈 순간에만 받으므로
        /// Job이 아니라 판단 직후에 부릅니다(옛 노드 진입 Flow와 같은 순간).
        /// </summary>
        void BeginCombatEntry(StageViewCommand command);

        /// <summary>끝난 전투 진입 시도를 닫습니다.</summary>
        void CloseCombatEntry();

        /// <summary>기록 누락으로 도메인 하나를 멈추고 진단을 남깁니다. 다른 도메인은 계속합니다.</summary>
        void Stop(StageDomain domain, string reason);

        /// <summary>처리 Job 바로 뒤에서 그 처리가 끝났는지 확인하는 Job입니다. 아직 남았으면 그 도메인을 멈춥니다.</summary>
        IJobService CreateCheck(StageDomain domain, Func<bool> isStillPending, string reason);

        /// <summary>증강 제안 준비 Job 바로 뒤에서, 준비 뒤에도 원본이 그대로면 증강 도메인을 멈추는 Job입니다.</summary>
        IJobService CreateGrowthOfferCheck();
    }
}
