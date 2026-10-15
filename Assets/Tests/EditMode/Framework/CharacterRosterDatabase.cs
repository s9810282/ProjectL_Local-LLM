using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Loadout.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Shop;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Targeting.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.DataBaseSystem.Queries.RenderDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Framework
{
    /// <summary>실제 DB 데이터 형식을 사용하는 로스터·상점 테스트 조회다.</summary>
    internal sealed class CharacterRosterDatabase : IDataBaseQueryContext, IInGameQuery,
        IUnitTableQuery, IPlayerLoadoutTableQuery, IStageTableQuery, IArtifactDefinitionTableQuery, ITargetingMethodTableQuery
    {
        /// <summary>모든 테스트 유닛이 참조하는 지정 방식(키 1): 원형 사각 범위, 칸 지정, 스플래시 없음.</summary>
        private readonly TargetingMethodData _targeting = new(1, TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
        private readonly Dictionary<EntityKey, UnitData> _units = new();
        private readonly PlayerLoadoutData _loadout;
        private readonly StageRootData _shop;
        private readonly IArtifactDefinitionTableQuery _artifacts;
        /// <summary>공격력 0·지정 방식 키 0으로 만들 유닛(공격하지 않는 지원 유닛 경우). None이면 없음.</summary>
        private readonly EntityKey _nonAttacker;

        internal CharacterRosterDatabase(bool enemyStartingSub = false, int shopCost = 5,
            EntityKey shopCharacter = EntityKey.Unit_Akimbo, IArtifactDefinitionTableQuery artifacts = null,
            EntityKey nonAttacker = EntityKey.None)
        {
            _artifacts = artifacts ?? this;
            _nonAttacker = nonAttacker;
            AddUnit(EntityKey.Unit_MainCharacter, UnitRole.MainCharacter, UnitOwnerEligibility.PlayerOnly);
            AddUnit(EntityKey.Unit_Akimbo, UnitRole.Regular, UnitOwnerEligibility.Both);
            AddUnit(EntityKey.Unit_Sniper, UnitRole.Regular, UnitOwnerEligibility.PlayerOnly);
            AddUnit(EntityKey.Unit_Demon, UnitRole.Regular, shopCharacter == EntityKey.Unit_Demon
                ? UnitOwnerEligibility.PlayerOnly : UnitOwnerEligibility.EnemyOnly);
            _loadout = new PlayerLoadoutData(1, EntityKey.Unit_MainCharacter, new[]
            {
                new PlayerLoadoutEntry(enemyStartingSub ? EntityKey.Unit_Demon : EntityKey.Unit_Sniper, 1),
                new PlayerLoadoutEntry(EntityKey.Unit_Akimbo, 2)
            }, 4);
            _loadout.Validate();
            _shop = new StageRootData(100, 1, StageMapRole.Regular,
                new ShopStageDetail(new[] { new ShopCandidate(shopCharacter, shopCost) }));
            _shop.Validate();
        }

        private void AddUnit(EntityKey key, UnitRole role, UnitOwnerEligibility owner)
        {
            bool main = role == UnitRole.MainCharacter;
            var data = new UnitData(key, role, owner, 1, 1, 10, 0, 3, 1,
                main ? 1 : 2,
                CategoryKey.Base, key == _nonAttacker ? 0 : 5, 4, 1, 1, 1, Array.Empty<UnitTraitData>(),
                targetingMethodKey: key == _nonAttacker ? 0 : 1);
            data.Validate();
            _units.Add(key, data);
        }

        public UnitData GetUnit(EntityKey key) => _units[key];
        public bool TryGetUnit(EntityKey key, out UnitData data) => _units.TryGetValue(key, out data);
        public IReadOnlyCollection<UnitData> GetAllUnits() => _units.Values;
        public PlayerLoadoutData GetPlayerLoadout(int key) => key == 1 ? _loadout : throw new KeyNotFoundException();
        public bool TryGetPlayerLoadout(int key, out PlayerLoadoutData data)
        {
            data = key == 1 ? _loadout : null;
            return data != null;
        }
        public IReadOnlyCollection<PlayerLoadoutData> GetAllPlayerLoadouts() => new[] { _loadout };
        public StageRootData GetStage(int key) => key == 100 ? _shop : throw new KeyNotFoundException();
        public IReadOnlyList<StageRootData> GetStages(int mapKey) => mapKey == 1 ? new[] { _shop } : Array.Empty<StageRootData>();

        public IInGameQuery InGameQuery => this;
        public IUnitTableQuery UnitTableQuery => this;
        public ITargetingMethodTableQuery TargetingMethodTableQuery => this;
        public TargetingMethodData GetTargetingMethod(int key) => key == 1 ? _targeting : throw new KeyNotFoundException();
        public bool TryGetTargetingMethod(int key, out TargetingMethodData data) { data = key == 1 ? _targeting : null; return data != null; }
        public IReadOnlyCollection<TargetingMethodData> GetAllTargetingMethods() => new[] { _targeting };
        public IPlayerLoadoutTableQuery PlayerLoadoutTableQuery => this;
        public IStageTableQuery StageTableQuery => this;
        public IRenderQuery RenderQuery => throw new NotSupportedException();
        public IViewQuery ViewQuery => throw new NotSupportedException();
        public IStageMapTableQuery StageMapTableQuery => throw new NotSupportedException();
        public IFieldTableQuery FieldTableQuery => throw new NotSupportedException();
        public IStructureTableQuery StructureTableQuery => throw new NotSupportedException();
        public IEnemyLoadoutTableQuery EnemyLoadoutTableQuery => throw new NotSupportedException();
        public IUnitRosterTableQuery UnitRosterTableQuery => throw new NotSupportedException();
        public IArtifactDefinitionTableQuery ArtifactDefinitionTableQuery => _artifacts;
        public ArtifactDefinitionData GetDefinition(int artifactId) => throw new KeyNotFoundException();
        public bool TryGetDefinition(int artifactId, out ArtifactDefinitionData data) { data = null; return false; }
        public IReadOnlyCollection<ArtifactDefinitionData> GetAllDefinitions() => Array.Empty<ArtifactDefinitionData>();
        public ICategoryConfigTableQuery CategoryConfigTableQuery => throw new NotSupportedException();
        public IStageEconomyTableQuery StageEconomyTableQuery => throw new NotSupportedException();
        public IRecoveryOptionTableQuery RecoveryOptionTableQuery => throw new NotSupportedException();
    }
}
