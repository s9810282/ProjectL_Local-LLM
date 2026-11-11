using System;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.Factory.Runtime.InGame.View;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Framework.SystemBase;
using UnityEngine;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem;

namespace ProjectL.Systems.Factory.Runtime.InGame
{
    #region 시스템 모듈

    internal static class InGameSystemFactory
    {
        /// <summary>
        /// 시험용 입구: Core에는 준비 입력만, View에는 오류 경계만 전달합니다(하위 Factory를 따로 부르는 테스트).
        /// 오류 경계를 생략하면 로그를 남기는 기본 경계를 만듭니다. 운영 조립은 <see cref="CreateComposition"/>을 씁니다.
        /// </summary>
        public static IExportSystemServices Create(
            IBattleInitializationSource battleSource = null,
            RuntimeFaultBoundary faultBoundary = null)
            => Compose(battleSource, null, faultBoundary ?? TestBoundary()).Group;

        /// <summary>
        /// 같은 인게임 조립에서 그룹과 View 입력 등록을 함께 얻습니다. 운영 조립 자리라 런타임 공용 오류 경계를 반드시 받습니다
        /// (대체 생성은 시험용 입구만, 전투 개선 설계 §7.2 E3).
        /// </summary>
        /// <param name="deckEditAvailability">LifeCycle이 만든 덱 편집 창(C1). InGame은 등록하지 않고 참조로만 Core에 넘깁니다.</param>
        public static InGameSystemComposition CreateComposition(
            IBattleInitializationSource battleSource,
            RuntimeFaultBoundary faultBoundary,
            IDeckEditAvailabilityRead deckEditAvailability = null
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , bool includeDevelopmentTools = false
#endif
            )
            => Compose(battleSource, null, faultBoundary, deckEditAvailability
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                , includeDevelopmentTools
#endif
                , usePreparedEnemyAdvance: true
                );

        internal static IExportSystemServices CreateWithBattleSource(IBattleInitializationSource battleSource)
            => CreateWithSources(battleSource, null);

        /// <summary>시험용 입구: 준비·챕터 연결 입력을 직접 줍니다. 오류 경계를 생략하면 기본 경계를 만듭니다.</summary>
        internal static IExportSystemServices CreateWithSources(IBattleInitializationSource battleSource,
            IChapterContinuationSource continuationSource,
            RuntimeFaultBoundary faultBoundary = null)
            => Compose(battleSource, continuationSource, faultBoundary ?? TestBoundary()).Group;

        // 시험용 입구가 경계를 생략할 때만 만드는 기본 경계입니다. 운영 조립은 런타임 공용 경계를 넘깁니다.
        private static RuntimeFaultBoundary TestBoundary() => new(Debug.LogException);

        private static InGameSystemComposition Compose(IBattleInitializationSource battleSource,
            IChapterContinuationSource continuationSource,
            RuntimeFaultBoundary faultBoundary,
            IDeckEditAvailabilityRead deckEditAvailability = null
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            , bool includeDevelopmentTools = false
#endif
            , bool usePreparedEnemyAdvance = false
            )
        {
            RuntimeFaultBoundary faults = faultBoundary ?? throw new ArgumentNullException(nameof(faultBoundary));
            SystemBuilderHelper builder = new();

            InGameCoreSystemFactory.Create(battleSource, continuationSource, deckEditAvailability
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                , includeDevelopmentTools
#endif
                , usePreparedEnemyAdvance: usePreparedEnemyAdvance
                ).ExportServices(builder);
            InGameViewSystemComposition view = InGameViewSystemFactory.CreateComposition(faults
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                , includeDevelopmentTools
#endif
                );
            view.Group.ExportServices(builder);

            var group = new InGameSystemGroup(builder.BuildSystemReader());
            return new InGameSystemComposition(group, view.Registration);
        }
    }

    #endregion
}
