using System;
using System.Collections.Generic;
using ProjectL.Framework.Composition;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Category.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.Factory.Runtime.InGame.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.ServiceKeys.Runtime;
using ProjectL.Systems.ServiceKeys.Runtime.InGame;

namespace ProjectL.Tests.CoreIntegration
{
    internal sealed class RunStartMainSelectionTestRuntime : IDisposable
    {
        private readonly FrameworkRuntime _runtime;
        internal TestDatabase Database { get; }
        internal IInGameCoreSystemGroupService Core { get; }
        internal IBufferQuery Buffers { get; }

        internal RunStartMainSelectionTestRuntime() : this(new TestDatabase())
        {
        }

        internal RunStartMainSelectionTestRuntime(TestDatabase database, bool initialize = true)
        {
            Database = database ?? throw new ArgumentNullException(nameof(database));
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            InGameCoreSystemFactory.Create().ExportServices(builder);
            _runtime = new FrameworkRuntime(builder.BuildSystemReader());
            try
            {
                if (initialize)
                {
                    InitData();
                    BindServices();
                }
                Core = _runtime.GetService(InGameSystemGroupServiceKey.Core);
                Buffers = _runtime.GetService(BufferSystemKey.Buffer);
            }
            catch
            {
                _runtime.Shutdown();
                throw;
            }
        }

        internal void InitData() => _runtime.InitData(Database);
        internal void BindServices() => _runtime.PostBindService();
        public void Dispose() => _runtime.Shutdown();

        internal sealed class TestDatabase : IDataBaseQueryContext, IInGameQuery,
            IUnitTableQuery, IPlayerLoadoutTableQuery, IHasPlayerActiveSkillTable
        {
            private readonly CoreIntegrationTestData _shared = new(new CategoryConfigData(3, 4, 3, 2, 0));
            private readonly Dictionary<EntityKey, UnitData> _units = new();
            internal PlayerLoadoutData Loadout { get; set; }
            internal Action BeforeNextUnitLookup { get; set; }

            internal TestDatabase(EntityKey defaultMain = EntityKey.Unit_Akimbo)
            {
                Loadout = new PlayerLoadoutData(1, defaultMain, new[]
                {
                    new PlayerLoadoutEntry(EntityKey.Unit_ShotGun, 1),
                    new PlayerLoadoutEntry(EntityKey.Unit_Cowboy, 1),
                    new PlayerLoadoutEntry(EntityKey.Unit_DynamiteHammer, 1)
                }, 4, startingRunCurrency: 137, initialDeckGrantIndices: new[] { 0, 1, 2, 3 });
                SetUnit(Main(EntityKey.Unit_Sniper, 10, 5, 2));
                SetUnit(Main(EntityKey.Unit_Akimbo, 30, 10, 5));
                SetUnit(new UnitData(EntityKey.Unit_ShotGun, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly,
                    1, 1, 10, 0, 2, CategoryKey.Heavy, 5, 1, null,
                    targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded));
                SetUnit(_shared.GetUnit(EntityKey.Unit_Cowboy));
                SetUnit(_shared.GetUnit(EntityKey.Unit_DynamiteHammer));
                SetUnit(_shared.GetUnit(EntityKey.Unit_Demon));
            }

            internal static UnitData Main(EntityKey key, int health, int attack, int defense,
                int attackRange = 1, UnitTraitData[] traits = null) =>
                new UnitData(key, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly,
                    1, 1, health, defense, attackRange, CategoryKey.Main, attack, 1, traits,
                    targetingMethodKey: 1, killReward: UnitKillRewardSpec.Excluded);

            // 조회 경계에서 정의 오류를 확인할 수 있도록 픽스처는 입력을 그대로 보관한다.
            internal void SetUnit(UnitData data) => _units[data.Key] = data;
            internal void SetStage(StageRootData data) => _shared.SetStage(data);
            internal void SetTargetingMethod(TargetingMethodData data) => _shared.SetTargetingMethod(data);

            public IInGameQuery InGameQuery => this;
            public IRenderQuery RenderQuery => _shared.RenderQuery;
            public IViewQuery ViewQuery => _shared.ViewQuery;
            public IUnitTableQuery UnitTableQuery => this;
            public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => this;
            public IPlayerActiveSkillTableQuery PlayerActiveSkills
            {
                get => _shared.PlayerActiveSkills;
                internal set => _shared.PlayerActiveSkills = value;
            }
            public IStageMapTableQuery StageMapTableQuery => _shared.StageMapTableQuery;
            public IStageTableQuery StageTableQuery => _shared.StageTableQuery;
            public IFieldTableQuery FieldTableQuery => _shared.FieldTableQuery;
            public ITargetingMethodTableQuery TargetingMethodTableQuery => _shared.TargetingMethodTableQuery;
            public IStructureTableQuery StructureTableQuery => _shared.StructureTableQuery;
            public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => _shared.EnemyLoadoutTableQuery;
            public IUnitRosterTableQuery UnitRosterTableQuery => _shared.UnitRosterTableQuery;
            public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => _shared.ArtifactDefinitionTableQuery;
            public ICategoryConfigTableQuery CategoryConfigTableQuery => _shared.CategoryConfigTableQuery;
            public IStageEconomyTableQuery StageEconomyTableQuery => _shared.StageEconomyTableQuery;
            public IRecoveryOptionTableQuery RecoveryOptionTableQuery => _shared.RecoveryOptionTableQuery;

            public UnitData GetUnit(EntityKey key) => _units[key];
            public bool TryGetUnit(EntityKey key, out UnitData data)
            {
                var beforeRead = BeforeNextUnitLookup;
                BeforeNextUnitLookup = null;
                beforeRead?.Invoke();
                return _units.TryGetValue(key, out data);
            }
            public IReadOnlyCollection<UnitData> GetAllUnits() => _units.Values;

            public PlayerLoadoutData GetPlayerLoadout(int key) => TryGetPlayerLoadout(key, out var data)
                ? data
                : throw new KeyNotFoundException($"테스트 로드아웃 정의가 없습니다. 키: {key}");

            public bool TryGetPlayerLoadout(int key, out PlayerLoadoutData data)
            {
                data = key == Loadout.LoadoutKey ? Loadout : null;
                return data != null;
            }

            public IReadOnlyCollection<PlayerLoadoutData> GetAllPlayerLoadouts() => new[] { Loadout };
        }
    }
}
