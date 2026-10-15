#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.Factory.Runtime.Development
{
    /// <summary>엑셀 데이터 도착 전 Core 실행을 위한 명시적 개발용 진입점이다. 씬·입력·Job은 조립하지 않는다.</summary>
    public static class TemporaryCoreRuntimeFactory
    {
        public const int ChapterKey = 1;
        public const int FirstBattleNodeId = 2;
        public const int RecoveryNodeId = 3;
        public const int ShopNodeId = 5;
        public const int BossBattleNodeId = 4;
        public const int SecondChapterKey = 2;
        public const int SecondChapterFirstBattleNodeId = 2;
        public const int SecondChapterBossNodeId = 3;

        /// <summary>데이터·서비스 초기화와 시작 로스터·챕터 준비를 마친 런타임을 반환한다. 사용 후 Shutdown을 호출한다.</summary>
        public static FrameworkRuntime Create() => CreateScenario(false);

        /// <summary>회복 경로와 분리된 임시 상점 경로를 만든다. 정식 DB 연결 뒤 제거할 개발용 데이터다.</summary>
        public static FrameworkRuntime CreateWithShop() => CreateScenario(true);

        /// <summary>상점 경로 뒤 두 번째 챕터와 최종 Run 종료를 검증하는 별도 개발 런타임을 만든다.</summary>
        public static FrameworkRuntime CreateWithChapterContinuation() => CreateScenario(true, true);

        private static FrameworkRuntime CreateScenario(bool withShop, bool withChapterContinuation = false)
        {
            var data = new TemporaryCoreDatabase(withChapterContinuation);
            var builder = new SystemBuilderHelper();
            var bufferGroup = BufferSystemFactory.Create();
            bufferGroup.ExportServices(builder);
            var buffers = (IBufferQuery)bufferGroup;
            var inGameBuilder = new SystemBuilderHelper();
            ProjectL.Systems.Factory.Runtime.InGame.Core.InGameCoreSystemFactory.Create(new TemporaryBattleInitializationSource(buffers.CharacterReadSink),
                withChapterContinuation ? new TemporaryChapterContinuationSource() : null)
                .ExportServices(inGameBuilder);
            new ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.InGameViewSystemGroup(
                new SystemBuilderHelper().BuildSystemReader(), null).ExportServices(inGameBuilder);
            new ProjectL.Systems.RuntimeSystem.InGameSystem.InGameSystemGroup(inGameBuilder.BuildSystemReader()).ExportServices(builder);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());
            try
            {
                runtime.InitData(data);
                runtime.PostBindService();
                runtime.Initialize();
                var core = runtime.GetService(SystemGroupsService.InGame).CoreGroup;
                if (!core.Characters.InitializeRoster(1).Succeeded)
                    throw new InvalidOperationException("임시 시작 로스터를 구성하지 못했습니다.");
                // Run 재화는 운영처럼 0으로 시작한다. 스테이지 경제의 시작 골드는 전투 준비 때 스테이지 골드로 들어간다.
                core.Artifacts.InitializeBoard(4, 4);
                // 고정 경로는 개발 시나리오 데이터이며 실제 맵 생성 규칙이나 Run 종료 정책을 대체하지 않는다.
                int nonCombatNodeId = withShop ? ShopNodeId : RecoveryNodeId;
                buffers.StageProgressWriteSink.RegisterStages(ChapterKey, new[] {
                    new StageProgressData(1, 0, 0, 0, new[] { FirstBattleNodeId }),
                    new StageProgressData(FirstBattleNodeId, 101, 1, 0, new[] { nonCombatNodeId },
                        stageType: withChapterContinuation ? StageType.Combat : default),
                    new StageProgressData(nonCombatNodeId, withShop ? 30 : 20, 2, 0, new[] { BossBattleNodeId },
                        false, withShop ? StageType.Shop : StageType.Event),
                    new StageProgressData(BossBattleNodeId, 102, 3, 0, Array.Empty<int>(), false, StageType.Combat, StageMapRole.Boss) });
                if (!core.ChapterProgression.EnterChapter(ChapterKey).Accepted)
                    throw new InvalidOperationException("임시 시작 챕터에 진입하지 못했습니다.");
                return runtime;
            }
            catch
            {
                runtime.Shutdown();
                throw;
            }
        }
    }
}
#endif
