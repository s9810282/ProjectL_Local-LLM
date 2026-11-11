#if UNITY_EDITOR || DEVELOPMENT_BUILD
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev;
#endif
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Configuration;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Input;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.LobbyInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageSelectionInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TutorialInput;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    #region 시스템 모듈

    internal static class InGameViewSystemFactory
    {
        // 튜토리얼 세션 진행을 남기는 개발 로그입니다. 화면이 붙기 전(T-6) 진행 확인에 씁니다(튜토리얼 구현 계획 §6).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private static readonly System.Action<string> TutorialDevLog = Debug.Log;
#else
        private static readonly System.Action<string> TutorialDevLog = null;
#endif

        /// <summary>View 그룹을 조립합니다.</summary>
        /// <remarks>
        /// 같은 오류 경계를 각 View에 넘기고 그룹이 소유하는 단일 입력 정리 콜백을 연결합니다(P3.9).
        /// 오류 경계를 생략하면 로그를 남기는 기본 경계를 만듭니다. 운영 조립(<c>InGameSystemFactory.CreateComposition</c>)은 늘 경계를 넘깁니다.
        /// 사건 순서대로 연출한 뒤 기존 View가 결과를 한 번 확인합니다.
        /// </remarks>
        public static IExportSystemServices Create(
            RuntimeFaultBoundary faultBoundary = null)
            => CreateComposition(faultBoundary).Group;

        /// <summary>View와 그 View가 소유하는 입력을 한 번 조립해 그룹 및 등록 정보를 반환합니다.</summary>
        public static InGameViewSystemComposition CreateComposition(
            RuntimeFaultBoundary faultBoundary = null
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , bool includeDevelopmentTools = false
#endif
            )
        {
            RuntimeFaultBoundary faults = faultBoundary ?? new RuntimeFaultBoundary(Debug.LogException);
            SystemBuilderHelper builder = new();

            StageInputComposition stage = StageInputFactory.Create();
            StageSelectionInputComposition selection = StageSelectionInputFactory.Create();
            ArtifactInputComposition artifact = ArtifactInputFactory.Create();
            LobbyInputComposition lobby = LobbyInputFactory.Create();
            TutorialInputComposition tutorialInput = TutorialInputFactory.Create();
            var input = new InGameViewInputContext(stage.Slot, selection.Slot, artifact.Slot, lobby.Slot);
            InputRegistrationSet registration = InputRegistrationSet.Combine(
                stage.Registration, selection.Registration, artifact.Registration, lobby.Registration,
                tutorialInput.Registration);

            var combatView = new CombatViewSystem(faults, new OrderedCombatPresentation(),
                CombatSceneFactory.Instance, CombatSceneFactory.Instance);
            var stageMapView = new StageMapViewSystem(faults, StageMapSceneFactory.Instance);
            var lobbyView = new LobbyViewSystem(faults, LobbySceneFactory.Instance);
            combatView.ExportServices(builder);
            stageMapView.ExportServices(builder);
            lobbyView.ExportServices(builder);
            var recovery = new RecoveryRequestLifetime(faults);
            recovery.ExportServices(builder);
            // 증강 선택 요청 소유자도 씬보다 오래 사는 런타임 수명이다(강화 View 설계 §14 TG6).
            var traitGrowth = new TraitGrowthRequestLifetime(faults);
            traitGrowth.ExportServices(builder);
            // 튜토리얼 실행 소유자는 씬보다 오래 사는 런타임 수명이다(튜토리얼 설계 §10).
            var tutorial = new TutorialViewSystem(faults, combatView.ReadInputWindow, TutorialDevLog, tutorialInput.Slot);
            tutorial.ExportServices(builder);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            DevelopmentTools developmentTools = includeDevelopmentTools
                ? new DevelopmentTools(() => stageMapView.CanUseDevelopmentTools, stageMapView.RequestMapRedraw, faults)
                : null;
            developmentTools?.ExportServices(builder);
#endif

            var reader = builder.BuildSystemReader();
            var cleanup = new InGameViewFaultCleanup(faults, combatView.StopInput,
                stageMapView.StopInput, lobbyView.StopInput,
                recovery: recovery.StopInput, tutorial: tutorial.StopInput);
            var group = new InGameViewSystemGroup(reader, combatView, cleanup, recovery, input, tutorial
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                , developmentTools
#endif
                , traitGrowth: traitGrowth);
            return new InGameViewSystemComposition(group, registration);
        }
    }

    #endregion
}
