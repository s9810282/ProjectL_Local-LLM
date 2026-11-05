using System.Linq;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    internal sealed class ImpactBoltBattleSource : IBattleInitializationSource
    {
        private readonly TraitSpec _trait;
        private readonly TargetingMethodSpec _method;
        private readonly int _range;

        internal CategoryKey TargetCategory { get; set; } = CategoryKey.Main;
        internal bool TargetImmune
        {
            get; set;
        }

        internal ImpactBoltBattleSource(TraitSpec trait, TargetingMethodSpec method, int range = 4)
        {
            _trait = trait;
            _method = method;
            _range = range;
        }

        public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
        {
            var source = new ConfiguredMovementBattle.EncounterSource().Read(database, chapterKey, stageKey);
            var units = source.Units.Select(s => new UnitSpec(
                s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                s.Key == EntityKey.Unit_Demon ? 100 : s.MaxHealth, s.MoveMethod,
                s.Key == EntityKey.Unit_Cowboy ? _range : s.AttackRange,
                s.Key == EntityKey.Unit_Demon ? TargetCategory : s.CategoryKey,
                s.AttackPower, s.PhysicalDefense, s.Priority,
                s.Key == EntityKey.Unit_Cowboy ? new[] { _trait } : s.Traits.ToArray(),
                s.OwnerEligibility, s.Occlusion,
                s.Key == EntityKey.Unit_Demon
                    ? new UnitBehaviorSpec(s.Behavior.CanMoveVoluntarily, s.Behavior.CanBasicAttack, TargetImmune)
                    : s.Behavior,
                killReward: s.KillReward)).ToArray();
            var profiles = source.AttackProfiles.Select(p => new CombatAttackProfile(
                units.Single(u => u.Key == p.Definition.Key),
                p.Definition.Key == EntityKey.Unit_Cowboy ? _method : p.Method,
                p.OwnedId)).ToArray();

            // 본부와 배치 구역은 유지하고, 거리 2까지 좌우로 이동할 공간을 둔다.
            var stage = source.Stage;
            var encounter = new StageConfig(stage.MapWidth, stage.MapHeight, stage.ReserveCapacity,
                stage.PlayerDeploymentDepth, stage.EnemyDeploymentDepth, stage.TimelineSeed,
                new[]
                {
                    new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(3, 3), Rotation4.R180),
                    new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(2, 3), Rotation4.R180),
                    new InitialEnemyPlacementSpec(EntityKey.Unit_Demon, new CellCoord(4, 3), Rotation4.R180)
                },
                stage.RailCells.ToArray(), stage.StageGimmicks.ToArray(),
                stage.InitialEnemyStructurePlacements.ToArray());
            return new BattleInitialization(encounter, source.MainAnchor, source.MainRotation,
                units, source.Structures, source.Categories, profiles, source.Environment,
                source.EnvironmentBindings, source.ObstaclePlacements, source.WallBlocking,
                source.ObjectiveKind, source.ItemDefinitions, source.Progression,
                source.InitialRandomTornadoPlacements, source.ClearRewards, source.TraitGrowth);
        }
    }
}
