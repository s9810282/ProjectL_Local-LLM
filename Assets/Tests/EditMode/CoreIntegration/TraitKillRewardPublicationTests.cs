using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitKillRewardPublicationTests
    {
        [Test]
        public void PreviewAndDuplicatePublication_DoNotHealOrGrantTwice()
        {
            using var battle = Battle();
            var before = battle.CaptureGameplay();
            var target = battle.Character(EntityKey.Unit_Demon).Anchor.Value;
            for (int i = 0; i < 2; i++)
            {
                battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(target)));
                Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
                Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
            }
            var transition = CombatCommandCalculator.Calculate(battle.Publication().Attack(battle.Actor, target), Array.Empty<IMovementTileRule>());
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(battle.Actor.Health.CurrentHealth, Is.EqualTo(battle.Actor.Health.MaxHealth));
            Assert.That(battle.Actor.Stats.TraitAttackBonus, Is.EqualTo(3));
            Assert.That(battle.Main.Stats.TraitAttackBonus, Is.EqualTo(3));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            battle.Buffers.BattleWriteSink.Apply(transition);
            once.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void StalePublication_CommitsNeitherRecoveryNorGrants()
        {
            using var battle = Battle();
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            input = input.WithWorld(input.State.With(traitTurn: new TraitTurnFacts(9, 0)), input.Characters, input.Main,
                input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(battle.Actor.Health.CurrentHealth, Is.EqualTo(1));
            Assert.That(battle.Main.TraitEffects.AttackGrants, Is.Empty);
        }

        private static ConfiguredMovementBattle Battle()
        {
            var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new KillSource());
            battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(battle.Actor, null, 1));
            battle.Buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(battle.Character(EntityKey.Unit_Demon), null, 1));
            return battle;
        }

        internal sealed class KillSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.Key == EntityKey.Unit_Demon ? 100 : s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.Key == EntityKey.Unit_Demon ? 13 : s.AttackPower,
                    s.PhysicalDefense, s.Priority, s.Key == EntityKey.Unit_Cowboy
                        ? new TraitSpec[] { new CallOfBloodTraitSpec(10), new MoraleBoostTraitSpec(20, 1) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
