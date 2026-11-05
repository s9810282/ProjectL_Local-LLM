using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
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

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TauntPublicationTests
    {
        [TestCase(false, 1, 1)]
        [TestCase(false, 2, 9)]
        [TestCase(true, 2, 9)]
        public void AutomaticAttack_HitsTaunterAndStalePublicationIsAtomic(bool stale, int rank, int expectedDamage)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new TauntSource(rank));
            var owner = battle.Actor;
            var main = battle.Main;
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var input = battle.Publication().EnemyStep();
            var transition = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(transition.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { owner.EntityId }));
            Assert.That(transition.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(expectedDamage));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            if (stale)
            {
                var moved = owner.WithPlacement(owner.Area, null, new CellCoord(1, 1), owner.Rotation, new[] { new CellCoord(1, 1) });
                battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(owner, moved) }, Array.Empty<SoulCountChange>()));
                var changed = new CoreIntegrationSnapshot(battle.Buffers);
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                changed.AssertUnchanged(battle.Buffers);
            }
            else
            {
                before.AssertUnchanged(battle.Buffers);
                battle.Buffers.BattleWriteSink.Apply(transition);
                Assert.That(battle.Actor.Health.CurrentHealth, Is.EqualTo(owner.Health.CurrentHealth - expectedDamage));
                Assert.That(battle.Main.Health, Is.EqualTo(main.Health));
                var once = new CoreIntegrationSnapshot(battle.Buffers);
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                once.AssertUnchanged(battle.Buffers);
            }
        }

        private sealed class TauntSource : IBattleInitializationSource
        {
            private readonly int _rank;

            internal TauntSource(int rank) => _rank = rank;

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.MaxHealth, s.MoveMethod, s.Key == EntityKey.Unit_Demon ? 8 : s.AttackRange, s.CategoryKey,
                    s.Key == EntityKey.Unit_Demon ? (_rank == 1 ? 1 : 10) : s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? new[] { RankedCombatTestData.Spec(TraitKey.Taunt, _rank) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
