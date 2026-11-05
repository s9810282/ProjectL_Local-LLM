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
    public sealed class TraitTurnLifecyclePublicationTests
    {
        [Test]
        public void TurnEnd_ExpiresGrantsThenAffectedSideRestrictionsAndResetsCount()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RestrictionSource());
            var enemy = battle.Character(EntityKey.Unit_Demon);
            battle.Submit(battle.Attack(battle.Actor, enemy.Anchor.Value));
            Assert.That(battle.Main.Stats.TraitAttackBonus, Is.EqualTo(6));
            Assert.That(Status(battle.Character(EntityKey.Unit_Demon), "Disarm"), Is.Not.Null);
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Main.Stats.TraitAttackBonus, Is.Zero);
            Assert.That(Status(battle.Character(EntityKey.Unit_Demon), "Disarm"), Is.Not.Null);
            battle.AdvanceToPlayerOrDay();
            Assert.That(Status(battle.Character(EntityKey.Unit_Demon), "Disarm"), Is.Null);
            Assert.That(Status(battle.Character(EntityKey.Unit_Demon), "AnkleBreaker"), Is.Null);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.TraitTurn.PlayerAttackActionCount, Is.Zero);
        }

        [Test]
        public void StaleExpiry_CommitsNeitherStatusNorTurnChanges()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RestrictionSource());
            battle.Submit(battle.Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value));
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var input = battle.Publication().EnemyStep();
            var transition = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            var enemy = battle.Character(EntityKey.Unit_Demon);
            var refreshed = Refresh(enemy, "Disarm", 2);
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(enemy, refreshed) },
                Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.SoulCountChange>()));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
        }

        private static object Status(CharacterCombatFacts character, string name)
        {
            var property = typeof(CharacterDebuffState).GetProperty(name);
            Assert.That(property, Is.Not.Null, name);
            return property.GetValue(character.Debuffs);
        }

        private static CharacterCombatFacts Refresh(CharacterCombatFacts character, string name, int duration)
        {
            var original = Status(character, name);
            var updated = Activator.CreateInstance(original.GetType(), original.GetType().GetProperty("Origin").GetValue(original), duration);
            var method = typeof(CharacterDebuffState).GetMethod("WithRestrictions");
            return character.With(debuffs: (CharacterDebuffState)method.Invoke(character.Debuffs,
                new[] { updated, Status(character, "AnkleBreaker") }));
        }

        private sealed class RestrictionSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.Key == EntityKey.Unit_Demon ? 100 : s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower,
                    s.PhysicalDefense, s.Priority, s.Key == EntityKey.Unit_Cowboy
                        ? new TraitSpec[] { new VanguardTraitSpec(3, 1), new DisarmTraitSpec(1), new AnkleBreakerTraitSpec(1) }
                        : s.Key == EntityKey.Unit_Akimbo ? new TraitSpec[] { new CleanSweepTraitSpec(3, 1) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
