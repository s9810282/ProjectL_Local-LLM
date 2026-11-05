using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.StageMap;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;
using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.StageSystem
{
    #region 서비스

    public interface IStageGeneratorSystem : IServiceModule
    {
        /// <summary>
        /// 그래프를 만들기만 한다. 등록은 챕터 진입·계속 제안과 함께 Buffer가 설치한다.
        /// pin이 있으면 그 노드에 그 스테이지를 놓는다. 그래프 모양은 같지만 다른 노드의 스테이지는 달라질 수 있고,
        /// 종류별 최소 개수는 고정 노드를 포함해 지킨다. 노드가 중간 층이 아니거나 스테이지가 이 맵의 일반 스테이지가 아니면 예외다.
        /// </summary>
        IReadOnlyList<StageProgressData> PrepareGraph(int mapKey, ChapterStagePin pin = null);
    }

    #endregion

    #region 시스템 모듈

    /// <summary>
    /// DB의 맵·스테이지 정의를 그래프 생성 입력(ChapterGraphSpec)으로 바꾸고, 생성 결과를 진행 기록으로 바꾸는 어댑터다.
    /// 생성 알고리즘은 Core 계산 계층의 ChapterGraphGenerator가 소유한다. Buffer에 직접 쓰지 않는다.
    /// </summary>
    internal sealed class StageGeneratorSystem : IStageGeneratorSystem,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private IInGameQuery _inGameQuery;
        private IRunProgressBufferReadSink _runRead;

        public void InitData(IDataBaseQueryContext data)
        {
            _inGameQuery = data?.InGameQuery ?? throw new ArgumentNullException(nameof(data));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            _runRead = buffers.RunProgressReadSink;
        }

        public IReadOnlyList<StageProgressData> PrepareGraph(int mapKey, ChapterStagePin pin = null)
        {
            if (mapKey < 1) throw new ArgumentOutOfRangeException(nameof(mapKey));
            if (_inGameQuery == null || _runRead == null)
                throw new InvalidOperationException("StageGeneratorSystem must be initialized before use.");
            EnsureRunInProgress();

            StageMapData map = _inGameQuery.StageMapTableQuery.GetStageMap(mapKey);
            IReadOnlyList<StageRootData> stages = _inGameQuery.StageTableQuery.GetStages(mapKey);
            var regularByKind = new Dictionary<ChapterStageKind, List<int>>();
            int boss = 0;
            var stagesByKey = new Dictionary<int, StageRootData>();
            foreach (StageRootData stage in stages)
            {
                stagesByKey[stage.StageKey] = stage;
                if (stage.MapRole == StageMapRole.Regular)
                {
                    ChapterStageKind kind = KindOf(stage, mapKey);
                    if (!regularByKind.TryGetValue(kind, out List<int> keys)) regularByKind[kind] = keys = new List<int>();
                    keys.Add(stage.StageKey);
                }
                else if (boss == 0) boss = stage.StageKey;
                else throw new InvalidOperationException($"Map {mapKey} has multiple boss stages.");
            }
            if (boss == 0)
                throw new InvalidOperationException($"Map {mapKey} requires one boss stage.");

            IReadOnlyList<ChapterGraphNode> nodes;
            try
            {
                // 매 Run 시드를 바꾸는 설정은 여기서 시드로 확정한다. 생성기는 받은 시드만 쓴다.
                var spec = new ChapterGraphSpec(map.DepthCount, map.WidthMax, map.WidthMin, map.SingleFirstLayer,
                    map.RandomizeSeedEachRun ? Environment.TickCount : map.Seed, ToConnection(map.ConnectionAlgorithm),
                    map.NearestConnectionWeight, map.OtherConnectionWeight, map.AdditionalConnectionChance,
                    BuildPools(map, regularByKind), boss, pin, map.FirstStageKey);
                nodes = ChapterGraphGenerator.Generate(spec);
            }
            catch (ArgumentException exception)
            {
                // 출현 설정·후보·고정 입력이 맞지 않는 경우다. 어느 맵인지 덧붙이고 원래 예외는 안쪽에 둔다.
                throw new InvalidOperationException($"Map {mapKey} cannot generate its chapter graph: {exception.Message}", exception);
            }

            var result = new List<StageProgressData>();
            foreach (ChapterGraphNode node in nodes)
            {
                // View가 노드 외형을 고를 수 있도록 스테이지 종류와 맵 역할을 함께 기록합니다. 시작 노드는 기본값입니다.
                StageRootData root = node.StageKey == 0 ? null : stagesByKey[node.StageKey];
                result.Add(new StageProgressData(node.NodeId, node.StageKey, node.Depth, node.WidthIndex, node.NextNodeIds,
                    stageType: root?.StageType ?? default, mapRole: root?.MapRole ?? default));
            }
            EnsureRunInProgress();
            return result.AsReadOnly();
        }

        // 상세 형식이 생성 종류의 유일한 근거다. 훈련도 기존 강화 출현 설정을 사용하며 일반 상점과 구분한다.
        private static ChapterStageKind KindOf(StageRootData stage, int mapKey)
        {
            return stage.StageData switch
            {
                CombatStageDetail => ChapterStageKind.Combat,
                ShopStageDetail => ChapterStageKind.Shop,
                EventStageDetail => ChapterStageKind.Recovery,
                EnhancementStageDetail => ChapterStageKind.Enhancement,
                _ => throw new InvalidOperationException(
                    $"Map {mapKey} regular stage {stage.StageKey} has unsupported detail {stage.StageData?.GetType().Name ?? "null"}.")
            };
        }

        // 맵의 출현 규칙마다 그 종류의 후보를 묶는다. 후보가 없는 종류는 빈 목록이며, 활성 규칙이면 묶음이 거절한다.
        private static List<ChapterStagePool> BuildPools(StageMapData map,
            Dictionary<ChapterStageKind, List<int>> regularByKind)
        {
            var pools = new List<ChapterStagePool>(map.StageDistribution.Count);
            foreach (ChapterStageDistributionRule rule in map.StageDistribution)
            {
                IReadOnlyList<int> keys = regularByKind.TryGetValue(rule.Kind, out List<int> found) ? found : Array.Empty<int>();
                pools.Add(new ChapterStagePool(rule, keys));
            }
            return pools;
        }

        private static ChapterGraphConnection ToConnection(StageConnectionAlgorithm algorithm)
        {
            return algorithm switch
            {
                StageConnectionAlgorithm.Nearest => ChapterGraphConnection.Nearest,
                StageConnectionAlgorithm.Random => ChapterGraphConnection.Random,
                StageConnectionAlgorithm.WeightedRandom => ChapterGraphConnection.WeightedRandom,
                _ => throw new InvalidOperationException($"Unknown stage connection algorithm {algorithm}.")
            };
        }

        private void EnsureRunInProgress()
        {
            if (_runRead.Outcome != RunOutcome.InProgress)
                throw new InvalidOperationException("RunEnded: 이미 종료된 Run입니다.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IStageGeneratorSystem>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    #endregion
}
