using System;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.ArtifactPreparation;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby.Artifact;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.LifeCycle
{
    #region 시스템 모듈

    internal static class LifeCycleSystemFactory
    {
        /// <summary>
        /// 시험용 입구: LifeCycle 그룹만 조립합니다(하위 Factory를 따로 부르는 테스트).
        /// 오류 경계를 생략하면 로그를 남기는 기본 경계를 만듭니다. 운영 조립은 <see cref="CreateComposition"/>을 씁니다.
        /// </summary>
        internal static IExportSystemServices Create(RuntimeFaultBoundary faultBoundary = null)
            => CreateComposition(faultBoundary ?? new RuntimeFaultBoundary(Debug.LogException)).Group;

        /// <summary>
        /// LifeCycle 그룹과 덱 편집 창(C1)을 함께 만듭니다. 창은 InGame을 만들기 전에 있어야 Core 덱 서비스에 넘길 수 있습니다.
        /// 여기 있는 객체는 InGame 객체를 받지 않고 런타임 의존성은 PostBindService에서 묶으므로 InGame보다 먼저 만들 수 있습니다.
        /// 운영 조립 자리라 런타임 공용 오류 경계를 반드시 받습니다(대체 생성은 시험용 입구 <see cref="Create"/>만, 전투 개선 설계 §7.2 E3).
        /// </summary>
        internal static LifeCycleSystemComposition CreateComposition(RuntimeFaultBoundary faultBoundary)
        {
            RuntimeFaultBoundary faults = faultBoundary ?? throw new ArgumentNullException(nameof(faultBoundary));
            SystemBuilderHelper builder = new();
            LifeCycleManager manager = new();
            manager.ExportServices(builder);

            // 생명주기 콜백이 없으므로 서비스로만 등록합니다(FW-001/002). 바인드·해제는 StageLifeCycle이 맡습니다.
            var deckEditWindow = new StageDeckEditWindow();
            builder.AddService<IDeckEditAvailabilityRead>(deckEditWindow);

            // 전투 진행 사실의 읽기 자리. Plan(JobRegister가 그룹으로 받음)과 진입 확인이 함께 쓴다(재구성 설계 D5).
            var progress = new CombatProgressReader();
            builder.AddService<ICombatProgressReader>(progress);
            builder.AddModule<ISystemModule>(progress);

            // 전투 진입 요청의 수명(설계 J5·D3). CombatLifeCycle은 씬 진입 때 이 서비스에 활성화만 맡긴다.
            var entry = new CombatEntryService(faults, progress);
            builder.AddService<ICombatEntryService>(entry);
            builder.AddModule<ISystemModule>(entry);

            var boardPreparation = new ArtifactBoardPreparation(faults);
            builder.AddService<IArtifactBoardPreparation>(boardPreparation);
            builder.AddModule<ISystemModule>(boardPreparation);

            // 로비 판단 사실 읽기와 아티팩트 도메인 정지(로비 재구성 설계 P1). 정지 손잡이는 어떤 인터페이스에도 내지 않고,
            // 그룹에는 읽기와 확인 Job 입구만 낸다. 기록 누락은 공용 오류가 아니라 오류 로그다(D11).
            // LobbyLifeCycle은 진입 때 정지를 풀고 이탈 검사에 사실을 읽으므로 둘 다 생성자로 받는다(§4.9).
            var lobbyArtifacts = new LobbyArtifactDomain(message => Debug.LogError(message));
            builder.AddService<ILobbyArtifactChecks>(lobbyArtifacts);
            builder.AddModule<ISystemModule>(lobbyArtifacts);
            var lobbyProgress = new LobbyProgress(lobbyArtifacts);
            builder.AddService<ILobbyProgressReader>(lobbyProgress);
            builder.AddModule<ISystemModule>(lobbyProgress);

            Register(builder, manager, new LobbyLifeCycle(faults, lobbyProgress, lobbyArtifacts));
            Register(builder, manager, new StageLifeCycle(faults, deckEditWindow));
            Register(builder, manager, new ArtifactLifeCycle());
            Register(builder, manager, new CombatLifeCycle(faults, entry));

            return new LifeCycleSystemComposition(new LifeCycleSystemGroup(builder.BuildSystemReader()), deckEditWindow);
        }

        private static void Register(
            ISystemWriteSink sink,
            LifeCycleManager manager,
            LifeCycleBase lifeCycle)
        {
            manager.Register(lifeCycle);
            lifeCycle.ExportServices(sink);
        }
    }

    #endregion
}
