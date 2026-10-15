using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.ArtifactPopup;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    #region 시스템 모듈

    internal static class InGameViewSystemFactory
    {
        /// <summary>View 그룹을 조립합니다.</summary>
        /// <remarks>
        /// CombatViewSystem에 실행 모드와 런타임 공통 오류 경계를 넘기고, 경계의 입력 정리 콜백에 StopInput을 연결합니다(설계 §10.1).
        /// 오류 경계를 생략하면(하위 Factory를 따로 부르는 테스트) 로그를 남기는 기본 경계를 만듭니다.
        /// 사건 순서대로 연출한 뒤 기존 View가 결과를 한 번 확인합니다.
        /// </remarks>
        public static IExportSystemServices Create(
            CombatExecutionMode executionMode = CombatExecutionMode.Live,
            RuntimeFaultBoundary faultBoundary = null)
        {
            RuntimeFaultBoundary faults = faultBoundary ?? new RuntimeFaultBoundary(Debug.LogException);
            SystemBuilderHelper builder = new();

            var combatView = new CombatViewSystem(executionMode, faults, new OrderedCombatPresentation());
            faults.ConnectStopInput(combatView.StopInput);
            combatView.ExportServices(builder);
            new StageMapViewSystem(faults).ExportServices(builder);
            new ArtifactViewSystem(faults).ExportServices(builder);

            return new InGameViewSystemGroup(builder.BuildSystemReader(), combatView);
        }
    }

    #endregion
}
