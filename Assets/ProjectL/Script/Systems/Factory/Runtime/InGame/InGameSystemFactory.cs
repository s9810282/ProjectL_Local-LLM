using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.Factory.Runtime.InGame.View;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Framework.SystemBase;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;

namespace ProjectL.Systems.Factory.Runtime.InGame
{
    #region 시스템 모듈

    internal static class InGameSystemFactory
    {
        /// <summary>
        /// Core에는 준비 입력만, View에는 실행 모드와 오류 경계만 전달합니다.
        /// 오류 경계를 생략하면(하위 Factory를 따로 부르는 테스트) 로그를 남기는 기본 경계를 만듭니다.
        /// </summary>
        public static IExportSystemServices Create(
            IBattleInitializationSource battleSource = null,
            CombatExecutionMode executionMode = CombatExecutionMode.Live,
            RuntimeFaultBoundary faultBoundary = null)
            => CreateWithSources(battleSource, null, executionMode, faultBoundary);

        internal static IExportSystemServices CreateWithBattleSource(IBattleInitializationSource battleSource)
            => CreateWithSources(battleSource, null);

        internal static IExportSystemServices CreateWithSources(IBattleInitializationSource battleSource,
            IChapterContinuationSource continuationSource, CombatExecutionMode executionMode = CombatExecutionMode.Live,
            RuntimeFaultBoundary faultBoundary = null)
        {
            RuntimeFaultBoundary faults = faultBoundary ?? new RuntimeFaultBoundary(Debug.LogException);
            SystemBuilderHelper builder = new();

            InGameCoreSystemFactory.Create(battleSource, continuationSource).ExportServices(builder);
            InGameViewSystemFactory.Create(executionMode, faults).ExportServices(builder);

            return new InGameSystemGroup(builder.BuildSystemReader());
        }
    }

    #endregion
}
