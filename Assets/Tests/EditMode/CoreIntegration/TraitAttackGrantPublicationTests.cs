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
    public sealed class TraitAttackGrantPublicationTests
    {
        [Test]
        public void Preview_LeavesGrantsAndCountsUnchanged_CommandPublishesOnce()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new GrantSource());
            var before = battle.CaptureGameplay();
            var target = battle.Character(EntityKey.Unit_Demon).Anchor.Value;
            battle.Buffers.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(target)));
            Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
            Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
            var transition = CombatCommandCalculator.Calculate(battle.Publication().Attack(battle.Actor, target), Array.Empty<IMovementTileRule>());
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.TraitTurn.PlayerAttackActionCount, Is.EqualTo(1));
            Assert.That(battle.Main.Stats.TraitAttackBonus, Is.EqualTo(6));
            Assert.That(battle.Main.TraitEffects.AttackGrants.Select(g => g.Origin.DefinitionKey),
                Is.EquivalentTo(new[] { (int)TraitKey.Vanguard, (int)TraitKey.CleanSweep }));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            battle.Buffers.BattleWriteSink.Apply(transition);
            once.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void StaleTurnFacts_RejectAllStoresBeforeGrantInstallation()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new GrantSource());
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            var stale = input.WithWorld(input.State.With(traitTurn: new TraitTurnFacts(7, 0)), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(stale, Array.Empty<IMovementTileRule>());
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(battle.Main.TraitEffects.AttackGrants, Is.Empty);
        }

        internal sealed class GrantSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s =>
                {
                    TraitSpec[] traits = s.Key == EntityKey.Unit_Cowboy ? new TraitSpec[] { new VanguardTraitSpec(3, 1) }
                        : s.Key == EntityKey.Unit_Akimbo ? new TraitSpec[] { new CleanSweepTraitSpec(3, 1) } : s.Traits.ToArray();
                    return new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                        s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority,
                        traits, s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward);
                }).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures,
                    basis.Categories, basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
