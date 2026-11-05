using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.Global;
using ProjectL.Systems.Factory.Runtime.InGame;
using ProjectL.Systems.Factory.Runtime.LifeCycle;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using UnityEngine;

namespace ProjectL.Systems.Factory.Runtime
{
    #region 시스템 모듈

    internal static class FrameworkRuntimeFactory
    {
        /// <summary>
        /// 루트 런타임을 조립합니다. 인자를 생략하면 기본 조립(준비 입력 없음, 로그를 남기는 기본 오류 경계)입니다.
        /// </summary>
        /// <remarks>
        /// 오류 경계는 런타임당 하나이며 같은 인스턴스를 InGame·LifeCycle에 전달합니다(설계 §8.3).
        /// 준비 입력은 Core에만, 오류 경계는 View·LifeCycle에만 전달합니다.
        /// 리플렉션으로 호출할 때는 선택 인자가 자동으로 채워지지 않으므로 인자 배열을 모두 넘깁니다.
        /// </remarks>
        public static FrameworkRuntime Create(
            IBattleInitializationSource battleSource = null,
            RuntimeFaultBoundary faultBoundary = null)
        {
            RuntimeFaultBoundary faults = faultBoundary ?? new RuntimeFaultBoundary(Debug.LogException);
            SystemBuilderHelper systemBuilderHelper = new();

            // 덱 편집 창(C1)을 Core에 넘기려고 LifeCycle을 먼저 만듭니다. 만드는 순서만 바뀌고 등록·초기화 순서는 그대로입니다.
            LifeCycleSystemComposition lifeCycle = LifeCycleSystemFactory.CreateComposition(faults);
            // View 입력은 생성 때 준비하고, 그룹의 등록·초기화 순서는 그대로 유지합니다.
            InGameSystemComposition inGame = InGameSystemFactory.CreateComposition(battleSource, faults,
                lifeCycle.DeckEditAvailability
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                , includeDevelopmentTools: true
#endif
                );

            BufferSystemFactory.Create().ExportServices(systemBuilderHelper);
            GlobalSystemFactory.CreateWithInputRegistration(inGame.Registration, faults).ExportServices(systemBuilderHelper);
            inGame.Group.ExportServices(systemBuilderHelper);
            lifeCycle.Group.ExportServices(systemBuilderHelper);

            return new FrameworkRuntime(systemBuilderHelper.BuildSystemReader());
        }
    }

    #endregion
}
