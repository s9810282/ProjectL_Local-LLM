using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Enhancement;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.CoreIntegration
{
    // 챕터 맵·스테이지 표만 이 시험이 소유한다. 나머지 조회는 공용 시험 데이터에 그대로 맡긴다.
    internal sealed class ChapterDistributionTestData : IDataBaseQueryContext, IInGameQuery, IStageTableQuery, IStageMapTableQuery
    {
        internal const int Boss = 2101;
        private readonly CoreIntegrationTestData _inner = new(ConfiguredMovementBattle.Config);
        private readonly Dictionary<int, StageMapData> _maps = new();
        private readonly Dictionary<int, StageRootData> _stages = new();

        // 맵 1: 깊이 7·폭 3, 운영 Chapter1과 같은 출현 설정. 전투 101~103, 상점 1201, 강화 1202, 회복 1301, 보스 2101.
        internal static ChapterDistributionTestData Standard(int seed = 192)
        {
            var data = new ChapterDistributionTestData();
            data.SetMap(1, seed, Rules((80, 1), (9, 1), (7, 1), (4, 1)));
            data.Add(CombatStage(101, 1, StageMapRole.Regular));
            data.Add(CombatStage(102, 1, StageMapRole.Regular));
            data.Add(CombatStage(103, 1, StageMapRole.Regular));
            data.Add(ShopStage(1201, 1));
            data.Add(EnhancementStage(1202, 1));
            data.Add(RecoveryStage(1301, 1));
            data.Add(CombatStage(Boss, 1, StageMapRole.Boss));
            return data;
        }

        internal static ChapterStageDistributionRule[] Rules((int weight, int minimum) combat, (int weight, int minimum) shop,
            (int weight, int minimum) recovery, (int weight, int minimum) enhancement) => new[]
        {
            new ChapterStageDistributionRule(ChapterStageKind.Combat, combat.weight, combat.minimum, 1),
            new ChapterStageDistributionRule(ChapterStageKind.Shop, shop.weight, shop.minimum, 1),
            new ChapterStageDistributionRule(ChapterStageKind.Recovery, recovery.weight, recovery.minimum, 1),
            new ChapterStageDistributionRule(ChapterStageKind.Enhancement, enhancement.weight, enhancement.minimum, 1)
        };

        internal void SetMap(int key, int seed, IReadOnlyList<ChapterStageDistributionRule> rules, int depth = 7, int widthMin = 1,
            bool singleFirstLayer = false, int firstStageKey = 0)
            => _maps[key] = new StageMapData(key, depth, 3, widthMin, singleFirstLayer, firstStageKey, 0.3f, StageConnectionAlgorithm.WeightedRandom, 3, 1,
                seed, false, 0, rules);

        internal void Add(StageRootData stage) => _stages[stage.StageKey] = stage;
        internal void Remove(int stageKey) => _stages.Remove(stageKey);

        internal static StageRootData CombatStage(int key, int mapKey, StageMapRole role)
            => new(key, mapKey, role, new CombatStageDetail(42, "field", 1, 0, 1,
                Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(),
                EntityKey.Structure_Headquarters, new CellCoord(1, 0), Rotation4.R0, new CellCoord(3, 5), Rotation4.R180, null));
        internal static StageRootData ShopStage(int key, int mapKey)
            => new(key, mapKey, StageMapRole.Regular, new ShopStageDetail(new[] { EntityKey.Unit_Cowboy }, Array.Empty<int>(), 5));
        internal static StageRootData EnhancementStage(int key, int mapKey)
            => new(key, mapKey, StageMapRole.Regular, new EnhancementStageDetail());
        internal static StageRootData RecoveryStage(int key, int mapKey)
            => new(key, mapKey, StageMapRole.Regular, new EventStageDetail(new[] { 1 }));

        public IInGameQuery InGameQuery => this;
        public IRenderQuery RenderQuery => _inner.RenderQuery;
        public IViewQuery ViewQuery => _inner.ViewQuery;
        public IStageMapTableQuery StageMapTableQuery => this;
        public IStageTableQuery StageTableQuery => this;
        public IFieldTableQuery FieldTableQuery => _inner.FieldTableQuery;
        public IUnitTableQuery UnitTableQuery => _inner.UnitTableQuery;
        public ITargetingMethodTableQuery TargetingMethodTableQuery => _inner.TargetingMethodTableQuery;
        public IStructureTableQuery StructureTableQuery => _inner.StructureTableQuery;
        public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => _inner.EnemyLoadoutTableQuery;
        public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => _inner.PlayerLoadoutTableQuery;
        public IUnitRosterTableQuery UnitRosterTableQuery => _inner.UnitRosterTableQuery;
        public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => _inner.ArtifactDefinitionTableQuery;
        public ICategoryConfigTableQuery CategoryConfigTableQuery => _inner.CategoryConfigTableQuery;
        public IStageEconomyTableQuery StageEconomyTableQuery => _inner.StageEconomyTableQuery;
        public IRecoveryOptionTableQuery RecoveryOptionTableQuery => _inner.RecoveryOptionTableQuery;

        public StageRootData GetStage(int stageKey) => _stages[stageKey];
        public IReadOnlyList<StageRootData> GetStages(int mapKey)
            => _stages.Values.Where(s => s.MapKey == mapKey).OrderBy(s => s.StageKey).ToArray();
        public StageMapData GetStageMap(int mapKey) => _maps[mapKey];
        public IReadOnlyCollection<StageMapData> GetAllStageMaps() => _maps.Values.ToArray();
    }
}
