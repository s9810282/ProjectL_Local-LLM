using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Framework
{
    // 실제 로스터 정의를 재사용하고 조회 중 변경은 테스트 경계에서만 주입한다.
    internal sealed class ShopStageTestDatabase : IDataBaseQueryContext, IInGameQuery,
        IStageTableQuery, IStageMapTableQuery, IUnitTableQuery
    {
        private readonly CharacterRosterDatabase _roster = new();
        private readonly StageMapData _map = new(1, 3, 1, 0, StageConnectionAlgorithm.Nearest, 1, 1, 42, false);
        internal readonly Dictionary<int, StageRootData> Stages = new();
        internal readonly List<EntityKey> UnitReads = new();
        internal Action<EntityKey> OnUnitRead;
        internal Action<int> OnStageRead;
        internal int StageReads;

        internal ShopStageTestDatabase()
        {
            Stages[100] = Shop();
            Stages[101] = new StageRootData(101, 1, StageMapRole.Regular, Shop().StageData);
        }

        internal static StageRootData Shop(params ShopCandidate[] candidates)
            => new(100, 1, StageMapRole.Regular, new ShopStageDetail(candidates.Length == 0
                ? new[] { new ShopCandidate(EntityKey.Unit_Akimbo, 5) } : candidates));

        public StageRootData GetStage(int key)
        {
            StageReads++;
            OnStageRead?.Invoke(key);
            return Stages[key];
        }
        public IReadOnlyList<StageRootData> GetStages(int mapKey)
            => Stages.Values.Where(x => x != null && x.MapKey == mapKey).ToArray();
        public StageMapData GetStageMap(int key) => key == 1 ? _map : throw new KeyNotFoundException();
        public IReadOnlyCollection<StageMapData> GetAllStageMaps() => new[] { _map };
        public UnitData GetUnit(EntityKey key)
        {
            UnitReads.Add(key); OnUnitRead?.Invoke(key);
            return _roster.UnitTableQuery.GetUnit(key);
        }
        public bool TryGetUnit(EntityKey key, out UnitData data)
        {
            UnitReads.Add(key); OnUnitRead?.Invoke(key);
            return _roster.UnitTableQuery.TryGetUnit(key, out data);
        }
        public IReadOnlyCollection<UnitData> GetAllUnits() => _roster.UnitTableQuery.GetAllUnits();
        public IInGameQuery InGameQuery => this;
        public IStageTableQuery StageTableQuery => this;
        public IStageMapTableQuery StageMapTableQuery => this;
        public IUnitTableQuery UnitTableQuery => this;
        public ITargetingMethodTableQuery TargetingMethodTableQuery => throw new NotSupportedException();
        public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => _roster.PlayerLoadoutTableQuery;
        public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => _roster.ArtifactDefinitionTableQuery;
        public IFieldTableQuery FieldTableQuery => throw new NotSupportedException();
        public IStructureTableQuery StructureTableQuery => throw new NotSupportedException();
        public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => throw new NotSupportedException();
        public IUnitRosterTableQuery UnitRosterTableQuery => throw new NotSupportedException();
        public ICategoryConfigTableQuery CategoryConfigTableQuery => throw new NotSupportedException();
        public IStageEconomyTableQuery StageEconomyTableQuery => throw new NotSupportedException();
        public IRecoveryOptionTableQuery RecoveryOptionTableQuery => throw new NotSupportedException();
        public IRenderQuery RenderQuery => throw new NotSupportedException();
        public IViewQuery ViewQuery => throw new NotSupportedException();
    }
}
