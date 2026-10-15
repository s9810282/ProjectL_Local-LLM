using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Framework
{
    /// <summary>회복 테스트에 필요한 DB 경계만 대체하고 실제 로스터와 데이터 형식은 유지한다.</summary>
    internal sealed class RecoveryStageTestDatabase : IDataBaseQueryContext, IInGameQuery,
        IStageTableQuery, IRecoveryOptionTableQuery, IStageMapTableQuery
    {
        private readonly CharacterRosterDatabase _roster = new();
        private readonly StageMapData _map = new(1, 3, 1, 0, StageConnectionAlgorithm.Nearest, 1, 1, 42, false);
        internal readonly Dictionary<int, StageRootData> Stages = new();
        internal readonly Dictionary<int, RecoveryOptionData> Options = new();
        internal readonly int[] OptionKeys = { 2, 1 };
        internal readonly List<int> OptionReads = new();
        internal Action<int> OnOptionRead;
        internal Action<int, int> OnStageRead;
        internal int StageReads;
        internal bool MissingInGame, MissingStages, MissingOptions;

        internal RecoveryStageTestDatabase()
        {
            Stages[20] = new StageRootData(20, 1, StageMapRole.Regular, new EventStageDetail(OptionKeys));
            Stages[21] = new StageRootData(21, 1, StageMapRole.Regular, new EventStageDetail(new[] { 1 }));
            Stages[100] = new StageRootData(100, 1, StageMapRole.Regular,
                new ShopStageDetail(new[] { new ShopCandidate(EntityKey.Unit_Akimbo, 5) }));
            Options[2] = new RecoveryOptionData(2, 3, RecoveryHealMode.Flat, 2);
            Options[1] = new RecoveryOptionData(1, 0, RecoveryHealMode.Full, 0);
        }

        public StageRootData GetStage(int key)
        {
            OnStageRead?.Invoke(++StageReads, key);
            return Stages.TryGetValue(key, out var stage) ? stage : throw new KeyNotFoundException();
        }
        public IReadOnlyList<StageRootData> GetStages(int mapKey) =>
            Stages.Values.Where(s => s != null && s.MapKey == mapKey).ToArray();
        public RecoveryOptionData GetRecoveryOption(int key)
        {
            OptionReads.Add(key);
            OnOptionRead?.Invoke(key);
            return Options.TryGetValue(key, out var data) ? data : throw new KeyNotFoundException();
        }
        public bool TryGetRecoveryOption(int key, out RecoveryOptionData data) => Options.TryGetValue(key, out data);
        public IReadOnlyCollection<RecoveryOptionData> GetAllRecoveryOptions() => Options.Values;
        public StageMapData GetStageMap(int key) => key == 1 ? _map : throw new KeyNotFoundException();
        public IReadOnlyCollection<StageMapData> GetAllStageMaps() => new[] { _map };
        public IInGameQuery InGameQuery => MissingInGame ? null : this;
        public IStageTableQuery StageTableQuery => MissingStages ? null : this;
        public IRecoveryOptionTableQuery RecoveryOptionTableQuery => MissingOptions ? null : this;
        public IStageMapTableQuery StageMapTableQuery => this;
        public IUnitTableQuery UnitTableQuery => _roster.UnitTableQuery;
        public ITargetingMethodTableQuery TargetingMethodTableQuery => _roster.TargetingMethodTableQuery;
        public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => _roster.PlayerLoadoutTableQuery;
        public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => _roster.ArtifactDefinitionTableQuery;
        public IFieldTableQuery FieldTableQuery => throw new NotSupportedException();
        public IStructureTableQuery StructureTableQuery => throw new NotSupportedException();
        public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => throw new NotSupportedException();
        public IUnitRosterTableQuery UnitRosterTableQuery => throw new NotSupportedException();
        public ICategoryConfigTableQuery CategoryConfigTableQuery => throw new NotSupportedException();
        public IStageEconomyTableQuery StageEconomyTableQuery => throw new NotSupportedException();
        public IRenderQuery RenderQuery => throw new NotSupportedException();
        public IViewQuery ViewQuery => throw new NotSupportedException();
    }
}
