using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Economy.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Enhancement.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Field.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Map.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Structure.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.StageMap;

namespace ProjectL.Tests.CoreIntegration
{
    // 선택된 검사의 입력만 제공한다. 변환·계산·설치는 운영 어댑터와 서비스가 맡는다.
    internal sealed class CoreIntegrationTestData : IDataBaseQueryContext, IInGameQuery,
        IUnitTableQuery, IPlayerLoadoutTableQuery, ITargetingMethodTableQuery,
        IStageTableQuery, IStageMapTableQuery, IFieldTableQuery, IUnitRosterTableQuery,
        IStructureTableQuery, IStageEconomyTableQuery, ICategoryConfigTableQuery, IArtifactDefinitionTableQuery,
        IHasEnhancementTable, IEnhancementTableQuery, IHasTraitGrowthTables
    {
        public ICategoryTraitProgressionTableQuery CategoryTraitProgressions
        {
            get; internal set;
        }
        public ITraitGrowthDefinitionTableQuery TraitGrowthDefinitions
        {
            get; internal set;
        }
        private readonly Dictionary<EntityKey, UnitData> _units = new();
        private readonly Dictionary<EnhancementKey, EnhancementData> _enhancements;
        private readonly PlayerLoadoutData _loadout = new(1, EntityKey.Unit_Akimbo, new[]
        {
            new PlayerLoadoutEntry(EntityKey.Unit_Sniper, 1),
            new PlayerLoadoutEntry(EntityKey.Unit_Cowboy, 1),
            new PlayerLoadoutEntry(EntityKey.Unit_DynamiteHammer, 1)
        }, 4, initialDeckGrantIndices: new[] { 0, 1, 2, 3 });
        private readonly Dictionary<int, TargetingMethodData> _targeting = new()
        {
            [1] = new TargetingMethodData(1, TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0)
        };
        // 보스 스테이지가 없어 그래프를 만들지 않는 맵이라 출현 규칙은 비워 둔다.
        private readonly StageMapData _map = new(1, 2, 1, 1, false, 0, 0, 0, StageConnectionAlgorithm.Nearest, 1, 1, 42, false, 0,
            Array.Empty<ChapterStageDistributionRule>());
        private readonly FieldData _field = new("field", 8, 6, 8, 2, 2);
        private readonly UnitRosterData _roster = new(1, new[] { EntityKey.Unit_Akimbo, EntityKey.Unit_Demon });
        private readonly StructureData _headquarters;
        private readonly StageEconomyData _economy = new(1, 27, 6, new StageClearRewardSpec(7, 8, 9));
        private StageRootData _stage;
        internal void SetStage(StageRootData stage) => _stage = stage ?? throw new ArgumentNullException(nameof(stage));
        internal IRecoveryOptionTableQuery RecoveryOptions { get; set; }
        internal IEnemyLoadoutTableQuery EnemyLoadouts { get; set; }

        /// <param name="playerDeploymentCells">스테이지의 아군 배치 칸이다. 비우면 필드의 아군 줄 수(2)를 쓴다.</param>
        internal CoreIntegrationTestData(CategoryConfigData config, int headquartersHealth = 20,
            CellCoord[] playerDeploymentCells = null, IReadOnlyList<EnhancementData> enhancements = null)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            _enhancements = enhancements?.ToDictionary(row => row.Key);
            _stage = new StageRootData(1101, 1, StageMapRole.Regular,
                new CombatStageDetail(42, "field", 1, 0, 1,
                    new[] { new EntityPlacement(EntityKey.Unit_Demon, new CellCoord(2, 5), Rotation4.R180) },
                    Array.Empty<EntityKey>(), Array.Empty<EntityPlacement>(), EntityKey.Structure_Headquarters,
                    new CellCoord(1, 0), Rotation4.R0, new CellCoord(3, 5), Rotation4.R180, null,
                    playerDeploymentCells: playerDeploymentCells));
            _headquarters = new StructureData(EntityKey.Structure_Headquarters, 1, 1, headquartersHealth, false, 0);
            _headquarters.Validate();
            AddUnit(EntityKey.Unit_Akimbo, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly, CategoryKey.Main, 1);
            AddUnit(EntityKey.Unit_Cowboy, UnitRole.Regular, UnitOwnerEligibility.Both, CategoryKey.Rider, 2);
            AddUnit(EntityKey.Unit_Sniper, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly, CategoryKey.Cannon, 2);
            AddUnit(EntityKey.Unit_DynamiteHammer, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly, CategoryKey.Rider, 2);
            AddUnit(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly, CategoryKey.Main, 2);
            _loadout.Validate();
        }

        private void AddUnit(EntityKey key, UnitRole role, UnitOwnerEligibility owner, CategoryKey category, int range)
        {
            var unit = new UnitData(key, role, owner, 1, 1, 10, 0, range, category, 5, 1,
                Array.Empty<UnitTraitData>(), targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded);
            unit.Validate();
            _units.Add(key, unit);
        }

        internal void SetUnit(UnitData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.Validate();
            _units[data.Key] = data;
        }

        internal void SetTargetingMethod(TargetingMethodData data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            data.Validate();
            _targeting[data.MethodKey] = data;
        }

        internal static void InstallDeck(IBufferQuery buffers)
        {
            if (buffers.RunProgressReadSink.RunId.IsEmpty)
            {
                ((IRunProgressBufferStore)buffers.RunProgressReadSink).InstallRunId(new RunId(Guid.NewGuid()));
            }
            var owned = buffers.CharacterReadSink.Owned;
            var main = owned.Single(c => c.Role == CharacterOwnershipRole.Main);
            var ids = new[] { main.Id }.Concat(owned.Where(c => c.Role == CharacterOwnershipRole.Sub)
                .Select(c => c.Id)).ToArray();
            var deck = (IRunDeckStore)buffers.RunDeckReadSink;
            deck.Install(deck.Prepare(buffers.RunProgressReadSink.RunId, ids));
        }

        internal static IBattleInitializationSource DatabaseSource(IBufferQuery buffers)
        {
            var source = new DatabaseBattleInitializationSource();
            var builder = new SystemBuilderHelper();
            builder.AddService<IBufferQuery>(buffers);
            source.PostBindService(builder.BuildSystemReader().ServiceStoreReader);
            return source;
        }

        public IInGameQuery InGameQuery => this;
        public IUnitTableQuery UnitTableQuery => this;
        public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => this;
        public ITargetingMethodTableQuery TargetingMethodTableQuery => this;
        public IStageTableQuery StageTableQuery => this;
        public IStageMapTableQuery StageMapTableQuery => this;
        public IFieldTableQuery FieldTableQuery => this;
        public IUnitRosterTableQuery UnitRosterTableQuery => this;
        public IStructureTableQuery StructureTableQuery => this;
        public IStageEconomyTableQuery StageEconomyTableQuery => this;
        public ICategoryConfigTableQuery CategoryConfigTableQuery => this;
        public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => this;
        public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => EnemyLoadouts ?? throw new NotSupportedException();
        public IRecoveryOptionTableQuery RecoveryOptionTableQuery => RecoveryOptions ?? throw new NotSupportedException();
        public IRenderQuery RenderQuery => throw new NotSupportedException();
        public IViewQuery ViewQuery => throw new NotSupportedException();
        public CategoryConfigData Config { get; internal set; }
        internal Action<int> BeforeStageRead
        {
            get; set;
        }
        internal Action BeforeEnhancementRead
        {
            get; set;
        }
        // 기본 fixture는 예전과 같이 강화 표를 제공하지 않는다.
        public IEnhancementTableQuery Enhancements => _enhancements == null ? null : this;

        public EnhancementData Get(EnhancementKey key)
        {
            BeforeEnhancementRead?.Invoke();
            return _enhancements[key];
        }

        public bool TryGet(EnhancementKey key, out EnhancementData data)
        {
            BeforeEnhancementRead?.Invoke();
            data = null;
            return _enhancements != null && _enhancements.TryGetValue(key, out data);
        }

        public IReadOnlyList<EnhancementData> GetAll()
        {
            BeforeEnhancementRead?.Invoke();
            return _enhancements?.Values.ToArray() ?? Array.Empty<EnhancementData>();
        }

        public UnitData GetUnit(EntityKey key) => _units[key];
        public bool TryGetUnit(EntityKey key, out UnitData value) => _units.TryGetValue(key, out value);
        public IReadOnlyCollection<UnitData> GetAllUnits() => _units.Values;
        public PlayerLoadoutData GetPlayerLoadout(int key) => key == 1 ? _loadout : throw new KeyNotFoundException();
        public bool TryGetPlayerLoadout(int key, out PlayerLoadoutData value)
        {
            value = key == 1 ? _loadout : null;
            return value != null;
        }
        public IReadOnlyCollection<PlayerLoadoutData> GetAllPlayerLoadouts() => new[] { _loadout };
        public TargetingMethodData GetTargetingMethod(int key) => _targeting[key];
        public bool TryGetTargetingMethod(int key, out TargetingMethodData value) => _targeting.TryGetValue(key, out value);
        public IReadOnlyCollection<TargetingMethodData> GetAllTargetingMethods() => _targeting.Values;
        public StageRootData GetStage(int key)
        {
            BeforeStageRead?.Invoke(key);
            return key == _stage.StageKey ? _stage : throw new KeyNotFoundException();
        }
        public IReadOnlyList<StageRootData> GetStages(int mapKey) => mapKey == 1 ? new[] { _stage } : Array.Empty<StageRootData>();
        public StageMapData GetStageMap(int key) => key == 1 ? _map : throw new KeyNotFoundException();
        public IReadOnlyCollection<StageMapData> GetAllStageMaps() => new[] { _map };
        public FieldData GetField(string key) => key == "field" ? _field : throw new KeyNotFoundException();
        public bool TryGetField(string key, out FieldData value)
        {
            value = key == "field" ? _field : null;
            return value != null;
        }
        public IReadOnlyCollection<FieldData> GetAllFields() => new[] { _field };
        public UnitRosterData GetUnitRoster(int key) => key == 1 ? _roster : throw new KeyNotFoundException();
        public bool TryGetUnitRoster(int key, out UnitRosterData value)
        {
            value = key == 1 ? _roster : null;
            return value != null;
        }
        public IReadOnlyCollection<UnitRosterData> GetAllUnitRosters() => new[] { _roster };
        public StructureData GetStructure(EntityKey key) => key == _headquarters.Key ? _headquarters : throw new KeyNotFoundException();
        public bool TryGetStructure(EntityKey key, out StructureData value)
        {
            value = key == _headquarters.Key ? _headquarters : null;
            return value != null;
        }
        public IReadOnlyCollection<StructureData> GetAllStructures() => new[] { _headquarters };
        public StageEconomyData GetStageEconomy(int key) => key == 1 ? _economy : throw new KeyNotFoundException();
        public bool TryGetStageEconomy(int key, out StageEconomyData value)
        {
            value = key == 1 ? _economy : null;
            return value != null;
        }
        public IReadOnlyCollection<StageEconomyData> GetAllStageEconomies() => new[] { _economy };
        /// <summary>배치 Artifact 정의다. 비워 두면 정의가 하나도 없다.</summary>
        internal IReadOnlyList<ArtifactDefinitionData> ArtifactDefinitions { get; set; } = Array.Empty<ArtifactDefinitionData>();
        public ArtifactDefinitionData GetDefinition(int id) =>
            TryGetDefinition(id, out var value) ? value : throw new KeyNotFoundException();
        public bool TryGetDefinition(int id, out ArtifactDefinitionData value)
        {
            value = ArtifactDefinitions.FirstOrDefault(d => d.ArtifactId == id);
            return value != null;
        }
        public IReadOnlyCollection<ArtifactDefinitionData> GetAllDefinitions() => ArtifactDefinitions.ToArray();
    }
}
