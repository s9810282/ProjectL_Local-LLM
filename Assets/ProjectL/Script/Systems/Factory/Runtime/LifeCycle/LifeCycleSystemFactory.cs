using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Artifact;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.Base;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Lobby;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage;
using ProjectL.Framework.SystemBase;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime.LifeCycle
{
    #region 시스템 모듈

    internal static class LifeCycleSystemFactory
    {
        /// <summary>
        /// LifeCycle 그룹을 조립합니다. 실행 모드와 오류 경계는 CombatLifeCycle에만 전달합니다.
        /// 오류 경계를 생략하면(하위 Factory를 따로 부르는 테스트) 로그를 남기는 기본 경계를 만듭니다.
        /// </summary>
        internal static IExportSystemServices Create(
            CombatExecutionMode executionMode = CombatExecutionMode.Live,
            RuntimeFaultBoundary faultBoundary = null)
        {
            RuntimeFaultBoundary faults = faultBoundary ?? new RuntimeFaultBoundary(Debug.LogException);
            SystemBuilderHelper builder = new();
            LifeCycleManager manager = new();
            manager.ExportServices(builder);

            Register(builder, manager, new LobbyLifeCycle());
            Register(builder, manager, new StageLifeCycle());
            Register(builder, manager, new ArtifactLifeCycle());
            Register(builder, manager, new CombatLifeCycle(executionMode, faults));

            // 전투 진행 사실의 읽기 자리. Plan(JobRegister가 그룹으로 받음)과 진입 확인이 함께 쓴다(재구성 설계 D5).
            var progress = new CombatProgressReader();
            builder.AddService<ICombatProgressReader>(progress);
            builder.AddModule<ISystemModule>(progress);

            return new LifeCycleSystemGroup(builder.BuildSystemReader());
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
