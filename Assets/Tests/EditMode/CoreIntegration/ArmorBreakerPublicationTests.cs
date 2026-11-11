using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class ArmorBreakerPublicationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void PreviewAndPublication_KeepArmorAtomic(bool stale)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new ArmorSource());
            var victim = battle.Character(EntityKey.Unit_Demon);
            var input = battle.Publication().Attack(battle.Actor, victim.Anchor.Value);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            before.AssertUnchanged(battle.Buffers);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            if (stale)
            {
                battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(victim,
                    victim.With(stats: victim.Stats.With(physicalDefense: 21))) }, Array.Empty<SoulCountChange>()));
                before = new CoreIntegrationSnapshot(battle.Buffers);
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                before.AssertUnchanged(battle.Buffers);
                return;
            }
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(CombatEffectiveStatsCalculator.PhysicalDefense(battle.Character(EntityKey.Unit_Demon)), Is.EqualTo(10));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            battle.Buffers.BattleWriteSink.Apply(transition);
            once.AssertUnchanged(battle.Buffers);
        }

        internal sealed class ArmorSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.Key == EntityKey.Unit_Demon ? 1000 : s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey,
                    s.Key == EntityKey.Unit_Demon ? 0 : s.Key == EntityKey.Unit_Cowboy ? 50 : s.AttackPower,
                    s.Key == EntityKey.Unit_Demon ? 20 : s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? new TraitSpec[] { new ArmorBreakerTraitSpec(50) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
