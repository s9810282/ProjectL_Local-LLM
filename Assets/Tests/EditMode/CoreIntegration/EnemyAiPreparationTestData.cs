using System;
using System.Linq;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    internal static class EnemyAiPreparationTestData
    {
        internal static int EnemyCount(string fixtureId) => fixtureId switch
        {
            "N5" => 5,
            "N6" => 6,
            "N8" => 8,
            "G16" => 16,
            _ => throw new ArgumentOutOfRangeException(nameof(fixtureId))
        };

        internal static ConfiguredMovementBattle Create(string fixtureId, int seed, bool startEnemy = true)
        {
            var config = ConfiguredMovementBattle.Config;
            var database = new CoreIntegrationTestData(config);
            database.SetUnit(new UnitData(EntityKey.Unit_Demon, UnitRole.Regular, UnitOwnerEligibility.EnemyOnly,
                1, 1, 100, 0, 4, CategoryKey.Main, 10, 100, Array.Empty<UnitTraitData>(), 1,
                killReward: UnitKillRewardSpec.Excluded));
            var battle = new ConfiguredMovementBattle(config, new OpenEncounter(EnemyCount(fixtureId), seed), database: database);
            try
            {
                if (startEnemy)
                {
                    battle.Submit(battle.EndPlayerTurn());
                    if (!battle.Core.CombatAdvance.AdvanceCombat())
                        throw new InvalidOperationException("시험의 적 진영 시작이 게시되지 않았습니다.");
                }
                return battle;
            }
            catch
            {
                battle.Dispose();
                throw;
            }
        }

        private sealed class OpenEncounter : IBattleInitializationSource
        {
            private readonly int _count;
            private readonly int _seed;

            internal OpenEncounter(int count, int seed)
            {
                _count = count;
                _seed = seed;
            }

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var query = database.InGameQuery;
                var units = query.UnitTableQuery.GetAllUnits().Select(UnitSpecAdapter.ReadUnit).ToArray();
                var enemies = Enumerable.Range(0, _count).Select(index => new InitialEnemyPlacementSpec(
                    EntityKey.Unit_Demon, new CellCoord(5 + index % 4, 1 + index / 4), Rotation4.R0)).ToArray();
                var zone = PlayerDeploymentZone.Of(new[] { CellCoord.Zero, new CellCoord(0, 1), new CellCoord(2, 0), new CellCoord(4, 0) });
                var stage = new StageConfig(16, 16, 8, zone, _seed, enemies, Array.Empty<CellCoord>(),
                    Array.Empty<StageGimmickSpec>(), new[] { new InitialEnemyStructurePlacementSpec(
                        EntityKey.Structure_Headquarters, new CellCoord(15, 15), Rotation4.R180) });
                return new BattleInitialization(stage, CellCoord.Zero, Rotation4.R0, units,
                    new[] { new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 20, false, 0) },
                    UnitSpecAdapter.ReadCategories(query.CategoryConfigTableQuery.Config),
                    BattleDefinitionAdapter.ReadAttackProfiles(database, units),
                    clearRewards: query.StageEconomyTableQuery.GetStageEconomy(1).ClearRewards);
            }
        }
    }
}
