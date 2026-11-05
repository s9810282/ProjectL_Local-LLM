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
    public sealed class TraitDamageReductionPublicationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void AuraDamage_PreviewAndStalePublicationAreAtomic(bool stale)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new AuraSource());
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var input = battle.Publication().EnemyStep().WithActor(battle.Character(EntityKey.Unit_Demon).EntityId)
                .WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(battle.Main.Anchor.Value), default, null, null);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            before.AssertUnchanged(battle.Buffers);
            if (stale) input = input.WithWorld(input.State.With(traitTurn: new TraitTurnFacts(9, 0)), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            if (stale)
            {
                Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
                before.AssertUnchanged(battle.Buffers);
            }
            else
            {
                battle.Buffers.BattleWriteSink.Apply(transition);
                Assert.That(battle.Main.Health.CurrentHealth, Is.EqualTo(battle.Main.Health.MaxHealth - 2));
            }
        }

        private sealed class AuraSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.MaxHealth, s.MoveMethod, s.Key == EntityKey.Unit_Demon ? 8 : s.AttackRange, s.CategoryKey,
                    s.Key == EntityKey.Unit_Demon ? 4 : s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? new TraitSpec[] { new EntrenchmentTraitSpec(1, 50) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
