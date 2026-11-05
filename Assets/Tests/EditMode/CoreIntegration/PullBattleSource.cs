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
    internal sealed class PullBattleSource : IBattleInitializationSource
    {
        internal InitialObstaclePlacementSpec Obstacle { get; set; }
        private readonly TraitSpec _trait;
        private readonly int _range;
        private readonly CellCoord _target;

        internal PullBattleSource(TraitSpec trait, int range = 1, CellCoord? target = null)
        {
            _trait = trait;
            _range = range;
            _target = target ?? new CellCoord(0, 3);
        }

        public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
        {
            var source = new ConfiguredMovementBattle.EncounterSource(_target).Read(database, chapterKey, stageKey);
            var units = source.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight,
                s.FootprintLocalCells.ToArray(), s.Key == EntityKey.Unit_Demon ? 100 : s.MaxHealth, s.MoveMethod,
                s.Key == EntityKey.Unit_Cowboy ? _range : s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense,
                s.Priority, s.Key == EntityKey.Unit_Cowboy ? new[] { _trait } : s.Traits.ToArray(),
                s.OwnerEligibility, s.Occlusion, s.Behavior, killReward: s.KillReward)).ToArray();
            var profiles = source.AttackProfiles.Select(p => new CombatAttackProfile(
                units.Single(u => u.Key == p.Definition.Key), p.Definition.Key == EntityKey.Unit_Cowboy
                    ? new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0)
                    : p.Method, p.OwnedId)).ToArray();
            return new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation,
                units, source.Structures, source.Categories, profiles,
                obstaclePlacements: Obstacle == null ? null : new[] { Obstacle });
        }
    }
}
