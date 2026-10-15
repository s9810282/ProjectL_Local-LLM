#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Recovery.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Event;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.Factory.Runtime.Development
{
    /// <summary>저작 형식은 그대로 두고 최소 개발 경로에 필요한 정의만 메모리에서 공급한다. 게임 상태는 저장하지 않는다.</summary>
    internal sealed class TemporaryCoreDatabase : IDataBaseQueryContext, IInGameQuery,
        IUnitTableQuery, IPlayerLoadoutTableQuery, IStageTableQuery, IStageMapTableQuery,
        IFieldTableQuery, IUnitRosterTableQuery, IStructureTableQuery, IStageEconomyTableQuery,
        ICategoryConfigTableQuery, IRecoveryOptionTableQuery, IArtifactDefinitionTableQuery
    {
        private readonly Dictionary<EntityKey, UnitData> _units = new();
        private readonly Dictionary<int, StageRootData> _stages = new();
        private readonly Dictionary<string, FieldData> _fields = new(StringComparer.Ordinal) {
            { "temporary-first", new FieldData("temporary-first", 4, 5, 8, 2, 2) },
            { "temporary-boss", new FieldData("temporary-boss", 5, 5, 8, 2, 2) } };
        private readonly Dictionary<int, RecoveryOptionData> _recovery = new() {
            { 1, new RecoveryOptionData(1, 5, RecoveryHealMode.Flat, 3) },
            { 2, new RecoveryOptionData(2, 10, RecoveryHealMode.Full, 0) } };
        private readonly PlayerLoadoutData _loadout = new(1, EntityKey.Unit_MainCharacter,
            new[] { new PlayerLoadoutEntry(EntityKey.Unit_Akimbo, 3) },
            ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.InitialDrawPlan.DefaultStartingHandSize);
        private readonly UnitRosterData _roster = new(1, new[] { EntityKey.Unit_MainCharacter, EntityKey.Unit_Akimbo });
        private readonly Dictionary<int, StageMapData> _maps = new() {
            { 1, new StageMapData(1, 3, 1, 0, StageConnectionAlgorithm.Nearest, 1, 1, 42, false) } };
        private readonly StageEconomyData _economy = new(1, 50, 2);
        private readonly StructureData _headquarters = new(EntityKey.Structure_Headquarters, 1, 1, 5, false, 0);
        public CategoryConfigData Config { get; } = new(50, Rotation4.R0);

        internal TemporaryCoreDatabase(bool withChapterContinuation = false)
        {
            AddUnit(EntityKey.Unit_MainCharacter, UnitRole.MainCharacter);
            AddUnit(EntityKey.Unit_Akimbo, UnitRole.Regular);
            _stages.Add(101, CombatStage(101, "temporary-first", StageMapRole.Regular));
            _stages.Add(102, CombatStage(102, "temporary-boss", StageMapRole.Boss));
            _stages.Add(20, new StageRootData(20, 1, StageMapRole.Regular, new EventStageDetail(new[] { 1, 2 })));
            _stages.Add(30, new StageRootData(30, 1, StageMapRole.Regular,
                new ShopStageDetail(new[] { new ShopCandidate(EntityKey.Unit_Akimbo, 5) })));
            if (withChapterContinuation)
            {
                _maps.Add(2, new StageMapData(2, 3, 1, 0, StageConnectionAlgorithm.Nearest, 1, 1, 42, false));
                _stages.Add(201, CombatStage(201, "temporary-first", StageMapRole.Regular, 2));
                _stages.Add(202, CombatStage(202, "temporary-boss", StageMapRole.Boss, 2));
            }
            _loadout.Validate(); _roster.Validate(); _economy.Validate(); _headquarters.Validate(); Config.Validate();
            foreach (var map in _maps.Values) map.Validate();
            foreach (var stage in _stages.Values) stage.Validate();
            foreach (var field in _fields.Values) field.Validate();
            foreach (var option in _recovery.Values) option.Validate();
        }

        private void AddUnit(EntityKey key, UnitRole role)
        {
            var unit = new UnitData(key, role, UnitOwnerEligibility.PlayerOnly, 1, 1, 10, 0, 3, 1,
                role == UnitRole.MainCharacter ? 1 : 2,
                CategoryKey.Base, 5, 4, 1, 1, 1, Array.Empty<UnitTraitData>(), 1);
            unit.Validate();
            _units.Add(key, unit);
        }

        private static StageRootData CombatStage(int key, string field, StageMapRole role, int mapKey = 1)
            => new(key, mapKey, role, new CombatStageDetail(42, field, 1, 0, 1, 100,
                Array.Empty<EntityPlacement>(), Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(),
                EntityKey.Structure_Headquarters, new CellCoord(1, 1), Rotation4.R0,
                new CellCoord(1, 3), Rotation4.R0, null));

        public UnitData GetUnit(EntityKey key) => _units[key];
        public bool TryGetUnit(EntityKey key, out UnitData data) => _units.TryGetValue(key, out data);
        public IReadOnlyCollection<UnitData> GetAllUnits() => _units.Values;
        public StageRootData GetStage(int key) => _stages[key];
        public IReadOnlyList<StageRootData> GetStages(int mapKey) => _stages.Values.Where(x => x.MapKey == mapKey).ToArray();
        public StageMapData GetStageMap(int key) => _maps[key];
        public IReadOnlyCollection<StageMapData> GetAllStageMaps() => _maps.Values;
        public FieldData GetField(string id) => _fields[id];
        public bool TryGetField(string id, out FieldData data) => _fields.TryGetValue(id, out data);
        public IReadOnlyCollection<FieldData> GetAllFields() => _fields.Values;
        public PlayerLoadoutData GetPlayerLoadout(int key) => key == 1 ? _loadout : throw new KeyNotFoundException();
        public bool TryGetPlayerLoadout(int key, out PlayerLoadoutData data) { data = key == 1 ? _loadout : null; return data != null; }
        public IReadOnlyCollection<PlayerLoadoutData> GetAllPlayerLoadouts() => new[] { _loadout };
        public UnitRosterData GetUnitRoster(int key) => key == 1 ? _roster : throw new KeyNotFoundException();
        public bool TryGetUnitRoster(int key, out UnitRosterData data) { data = key == 1 ? _roster : null; return data != null; }
        public IReadOnlyCollection<UnitRosterData> GetAllUnitRosters() => new[] { _roster };
        public StructureData GetStructure(EntityKey key) => key == _headquarters.Key ? _headquarters : throw new KeyNotFoundException();
        public bool TryGetStructure(EntityKey key, out StructureData data) { data = key == _headquarters.Key ? _headquarters : null; return data != null; }
        public IReadOnlyCollection<StructureData> GetAllStructures() => new[] { _headquarters };
        public StageEconomyData GetStageEconomy(int key) => key == 1 ? _economy : throw new KeyNotFoundException();
        public bool TryGetStageEconomy(int key, out StageEconomyData data) { data = key == 1 ? _economy : null; return data != null; }
        public IReadOnlyCollection<StageEconomyData> GetAllStageEconomies() => new[] { _economy };
        public RecoveryOptionData GetRecoveryOption(int key) => _recovery[key];
        public bool TryGetRecoveryOption(int key, out RecoveryOptionData data) => _recovery.TryGetValue(key, out data);
        public IReadOnlyCollection<RecoveryOptionData> GetAllRecoveryOptions() => _recovery.Values;
        public ArtifactDefinitionData GetDefinition(int id) => throw new KeyNotFoundException();
        public bool TryGetDefinition(int id, out ArtifactDefinitionData data) { data = null; return false; }
        public IReadOnlyCollection<ArtifactDefinitionData> GetAllDefinitions() => Array.Empty<ArtifactDefinitionData>();

        public IInGameQuery InGameQuery => this;
        public IUnitTableQuery UnitTableQuery => this;
        public ITargetingMethodTableQuery TargetingMethodTableQuery => throw new NotSupportedException();
        public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => this;
        public IStageTableQuery StageTableQuery => this;
        public IStageMapTableQuery StageMapTableQuery => this;
        public IFieldTableQuery FieldTableQuery => this;
        public IUnitRosterTableQuery UnitRosterTableQuery => this;
        public IStructureTableQuery StructureTableQuery => this;
        public IStageEconomyTableQuery StageEconomyTableQuery => this;
        public ICategoryConfigTableQuery CategoryConfigTableQuery => this;
        public IRecoveryOptionTableQuery RecoveryOptionTableQuery => this;
        public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => this;
        public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => throw new NotSupportedException("임시 경로는 적 유닛 편성을 제공하지 않습니다.");
        public IRenderQuery RenderQuery => throw new NotSupportedException("임시 Core 데이터는 Render 정의를 제공하지 않습니다.");
        public IViewQuery ViewQuery => throw new NotSupportedException("임시 Core 데이터는 View 정의를 제공하지 않습니다.");
    }
}
#endif
