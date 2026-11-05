using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class TraitShieldPublicationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void ControllerStart_PublishesIsolationAgainstBothSides(bool enemy)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new SoloSource(enemy), startPlayerTurn: false);
            var main = battle.Main;
            var moved = main.WithPlacement(main.Area, null, new CellCoord(7, 0), main.Rotation, new[] { new CellCoord(7, 0) });
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(main, moved) },
                Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.SoulCountChange>()));
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            if (enemy)
            {
                battle.Submit(battle.EndPlayerTurn());
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            }
            var owner = enemy ? battle.Character(EntityKey.Unit_Demon) : battle.Actor;
            Assert.That(Shield(owner), Is.Zero);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void BothSideEnds_PublishSoloResetsOnlyForEndingSideAtConfiguredInterval(int interval)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new SoloResetSource(interval), enterCombat: false);
            battle.Submit(CombatViewCommandData.PlaceUnit(
                battle.Actor.EntityId, new CellCoord(7, 1), Rotation4.R0));
            battle.Submit(CombatViewCommandData.EnterCombat());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            var originals = new[] { battle.Actor, battle.Character(EntityKey.Unit_Demon) };
            Assert.That(originals.All(c => Shield(c) == 10), Is.True);
            var spent = originals.Select(c => new CharacterStateChange(c, c.WithStatusEffects(new StatusEffectSet(
                c.StatusEffects.Entries.Select(s => s is ShieldStatus shield
                    ? new ShieldStatus(shield.Origin.Value, 6, shield.Eligible, shield.LastResetControllerTurnCount) : s).ToArray())))).ToArray();
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(spent,
                Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.SoulCountChange>()));
            for (int turn = 1; turn <= interval + 1; turn++)
            {
                var beforePlayerEnd = new[] { battle.Actor, battle.Character(EntityKey.Unit_Demon) };
                battle.Submit(battle.EndPlayerTurn());
                AssertSoloResetBoundary(battle, beforePlayerEnd, Owner.Player, turn, interval);
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
                var beforeEnemyEnd = new[] { battle.Actor, battle.Character(EntityKey.Unit_Demon) };
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
                Assert.That(battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.ControllerPhase,
                    Is.EqualTo(CombatControllerPhase.StartingPlayer));
                AssertSoloResetBoundary(battle, beforeEnemyEnd, Owner.Enemy, turn, interval);
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            }
            Assert.That(originals.All(c => Shield(c) == 10), Is.True);
        }

        [Test]
        public void ShieldSpend_CommitsOnceAndPreviewKeepsBudget()
        {
            using var battle = Battle();
            var capturedShield = battle.Main.StatusEffects.Entries.OfType<ShieldStatus>().Single(s => s.Eligible);
            Assert.That(Shield(battle.Main), Is.EqualTo(10));
            var input = AttackMain(battle);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            before.AssertUnchanged(battle.Buffers);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            battle.Buffers.BattleWriteSink.Apply(transition);
            Assert.That(battle.Main.Health.CurrentHealth, Is.EqualTo(battle.Main.Health.MaxHealth));
            Assert.That(Shield(battle.Main), Is.InRange(1, 9));
            Assert.That(capturedShield.RemainingBudget, Is.EqualTo(10));
            Assert.That(battle.Main.StatusEffects.Entries.OfType<ShieldStatus>().Single(s => s.Origin == capturedShield.Origin).RemainingBudget,
                Is.EqualTo(Shield(battle.Main)));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            once.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void StaleShieldSpend_InstallsNothing()
        {
            using var battle = Battle();
            var input = AttackMain(battle);
            input = input.WithWorld(input.State.With(traitTurn: new TraitTurnFacts(8, 0)), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(Shield(battle.Main), Is.EqualTo(10));
        }

        [Test]
        public void StaleCanonicalShieldBudget_RejectsAllStoresWithoutChangingCapturedSnapshot()
        {
            using var battle = Battle();
            var main = battle.Main;
            var transition = CombatCommandCalculator.Calculate(AttackMain(battle), Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            var changed = main.WithStatusEffects(new StatusEffectSet(main.StatusEffects.Entries.Select(s => s is ShieldStatus shield && shield.Eligible
                ? new ShieldStatus(shield.Origin.Value, shield.RemainingBudget - 1, shield.Eligible, shield.LastResetControllerTurnCount) : s).ToArray()));
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(main, changed) },
                Array.Empty<ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster.SoulCountChange>()));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(transition));
            before.AssertUnchanged(battle.Buffers);
            Assert.That(main.TraitEffects.ActiveShieldTotal, Is.EqualTo(10));
            Assert.That(Shield(battle.Main), Is.EqualTo(9));
        }

        private static ConfiguredMovementBattle Battle()
        {
            var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new ShieldSource());
            battle.Submit(battle.EndPlayerTurn());
            Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            return battle;
        }

        private static CombatCommandInput AttackMain(ConfiguredMovementBattle battle) => battle.Publication().EnemyStep()
            .WithActor(battle.Character(EntityKey.Unit_Demon).EntityId)
            .WithIntent(CombatCommandKind.Attack, AttackAim.ForCell(battle.Main.Anchor.Value), default, null, null);

        private static int Shield(CharacterCombatFacts character)
        {
            var property = typeof(CharacterTraitEffects).GetProperty("ActiveShieldTotal");
            Assert.That(property, Is.Not.Null);
            return (int)property.GetValue(character.TraitEffects);
        }

        private static void AssertSoloResetBoundary(ConfiguredMovementBattle battle, CharacterCombatFacts[] before,
            Owner endedSide, int ownEnds, int interval)
        {
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.Any(e => e.Kind == (endedSide == Owner.Player
                ? CombatEventKind.PlayerTurnEnded : CombatEventKind.EnemyPhaseEnded)), Is.True);
            var resets = result.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(t => t.TraitKey == TraitKey.SoloAction && t.TraitActivationKind == TraitActivationKind.ShieldReset).ToArray();
            Assert.That(resets.Select(t => t.SourceUnitId), Is.EquivalentTo(before
                .Where(c => c.Owner == endedSide && ownEnds % interval == 0).Select(c => c.EntityId)));
            foreach (var character in before)
            {
                bool resetDue = character.Owner == endedSide && ownEnds % interval == 0;
                var oldShield = character.TraitEffects.Shields.Single();
                var shield = battle.Character(character.CharacterKey).TraitEffects.Shields.Single();
                Assert.That(shield.RemainingBudget, Is.EqualTo(resetDue ? 10 : oldShield.RemainingBudget));
                Assert.That(shield.LastResetControllerTurnCount, Is.EqualTo(resetDue
                    ? battle.Buffers.CombatStateReadSink.Current.CommandState.Turn.CompletedControllerTurnCount
                    : oldShield.LastResetControllerTurnCount));
            }
        }

        private sealed class SoloResetSource : IBattleInitializationSource
        {
            private readonly int _interval;
            internal SoloResetSource(int interval) => _interval = interval;

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(7, 4)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.MaxHealth, s.MoveMethod,
                    s.Key == EntityKey.Unit_Demon ? 1 : s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy || s.Key == EntityKey.Unit_Demon
                        ? new TraitSpec[] { new SoloActionTraitSpec(10, 1, 0, _interval) } : Array.Empty<TraitSpec>(),
                    s.OwnerEligibility, s.Occlusion,
                    s.Key == EntityKey.Unit_Demon ? new UnitBehaviorSpec(false, true, false) : s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }

        private sealed class SoloSource : IBattleInitializationSource
        {
            private readonly bool _enemy;
            internal SoloSource(bool enemy) => _enemy = enemy;
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 2)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == (_enemy ? EntityKey.Unit_Demon : EntityKey.Unit_Cowboy) ? new TraitSpec[] { new SoloActionTraitSpec(10, 1, 0, 1) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }

        private sealed class ShieldSource : IBattleInitializationSource
        {
            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3)).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(),
                    s.MaxHealth, s.MoveMethod, s.Key == EntityKey.Unit_Demon ? 8 : s.AttackRange, s.CategoryKey,
                    s.Key == EntityKey.Unit_Demon ? 4 : s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Cowboy ? new TraitSpec[] { new CohesionTraitSpec(0, 10, 1, 1) } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }
    }
}
