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
    internal sealed class HuntersMarkBattleSource : IBattleInitializationSource
    {
        private readonly TraitSpec[] _traits;
        internal int? ActorAttackPower { get; set; }

        internal HuntersMarkBattleSource(params TraitSpec[] traits) => _traits = traits;

        public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
        {
            var source = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3))
                .Read(database, chapterKey, stageKey);
            var units = source.Units.Select(s =>
                new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.Key == EntityKey.Unit_Demon ? 100 : s.MaxHealth, s.MoveMethod,
                    s.Key == EntityKey.Unit_DynamiteHammer ? 4 : s.AttackRange, s.CategoryKey,
                    s.Key == EntityKey.Unit_Cowboy ? ActorAttackPower ?? s.AttackPower :
                    s.Key == EntityKey.Unit_DynamiteHammer ? 20 : s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? _traits : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, killReward: s.KillReward)).ToArray();
            var profiles = source.AttackProfiles.Select(p =>
                new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray();
            return new BattleInitialization(source.Stage, source.MainAnchor, source.MainRotation,
                units, source.Structures, source.Categories, profiles);
        }
    }
}
