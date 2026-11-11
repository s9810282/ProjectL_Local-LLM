using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class RampagePublicationTests
    {
        [TestCase("Damage", 10)]
        [TestCase("DoT", 10)]
        [TestCase("ItemHealing", 0)]
        [TestCase("Regeneration", 0)]
        [TestCase("SoulMaxHealth", 10)]
        [TestCase("Terrain", 0)]
        [TestCase("Reserve", 0)]
        [TestCase("Death", 0)]
        [TestCase("BattleEnd", 10)]
        public void HealthMaxHealthAndCleanupPublishConsistentStats(string change, int expectedBonus)
        {
            bool beforeStart = change is "DoT" or "ItemHealing" or "Death";
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RampageSource(change),
                startPlayerTurn: !beforeStart, enterCombat: change != "Reserve");
            var b = battle.Buffers;
            int hp = change == "Death" ? 1 : change is "Damage" or "DoT" ? 41 : change == "SoulMaxHealth" ? 40 : 39;
            b.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(battle.Actor, null, hp));
            bool seededHistory = change is "ItemHealing" or "Regeneration" or "Terrain" or "Reserve" or "Death" or "BattleEnd";
            if (seededHistory)
                InstallHistory(battle, 8);
            if (change == "ItemHealing")
            {
                var current = battle.Actor;
                b.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(current,
                    ItemCharacterChanges.WithItems(current, new CharacterItemState(1, current.Items.Effects))) }, Array.Empty<SoulCountChange>()));
            }
            var original = b.CharacterReadSink.Characters.Single(c => c.EntityId == battle.Actor.EntityId);
            var owned = b.CharacterReadSink.Owned.Single(o => o.Id == battle.Actor.OwnedId);
            if (beforeStart)
            {
                PeriodicStatusPublicationTests.Install(battle, battle.Actor, new StatusEffectSet(new AppliedStatus[]
                    { new FireStatus(PeriodicStatusPublicationTests.Origin(battle.Character(EntityKey.Unit_Demon)), 2) }));
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
            }
            else if (change == "Damage")
            {
                battle.Submit(battle.EndPlayerTurn());
                Assert.That(battle.Core.CombatAdvance.AdvanceCombat(), Is.True);
                var parts = battle.Publication();
                var input = parts.Assembler.Assemble(new CombatActionRequest(0, CombatCommandKind.Attack,
                    battle.Character(EntityKey.Unit_Demon).EntityId, AttackAim.ForCell(battle.Actor.Anchor.Value),
                    processingKind: CombatProcessingKind.Advance, expectedBattleInstanceId: b.CombatStateReadSink.BattleInstanceId,
                    expectedControllerTurnSequence: b.CombatStateReadSink.Current.CommandState.Turn.ControllerTurnSequence));
                var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
                Assert.That(transition.Accepted, Is.True, transition.Reason);
                Assert.That(parts.Publisher.Publish(transition), Is.True);
                Assert.That(battle.Actor.Health.CurrentHealth, Is.LessThan(40));
            }
            else if (change == "Regeneration") battle.Submit(battle.EndPlayerTurn());
            else if (change == "Reserve") battle.Submit(CombatViewCommandData.ReturnUnit(battle.Actor.EntityId));
            else if (change == "BattleEnd") battle.Submit(battle.Attack(battle.Actor, new CellCoord(1, 3)));
            else
            {
                var destination = new CellCoord(1, 1);
                if (change == "SoulMaxHealth")
                {
                    var field = new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1, PeriodicStatusPublicationTests.Origin(battle.Actor), destination, 2, 0) });
                    b.BattleWriteSink.Apply(new SoulActionChanges(new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                        new SoulFieldChange(b.CombatStateReadSink.SoulField, field), Array.Empty<long>()));
                }
                battle.Move(battle.Actor, destination);
            }
            var published = b.CharacterReadSink.Characters.Single(c => c.EntityId == original.EntityId);
            var final = published.ToFacts();
            Assert.That(final.Stats.TraitAttackBonus, Is.EqualTo(expectedBonus), change);
            Assert.That(final.Stats.TraitStatuses.ToArray().OfType<RampageTraitStatusSnapshot>().Single().KillAttackBonus, Is.Zero, change);
            Assert.That(published.EffectiveAttackPower, Is.EqualTo(CombatEffectiveStatsCalculator.AttackPower(final)));
            Assert.That(published.EffectiveAttackPower, Is.EqualTo(final.Stats.AttackPower + final.Stats.ArtifactAttackBonus +
                final.Stats.PositionAttackBonus + expectedBonus));
            Assert.That(published.StatusEffectSnapshots.OfType<RampageEffectSnapshot>().SingleOrDefault()?.AttackBonus ?? 0, Is.EqualTo(expectedBonus));
            Assert.That(b.CharacterReadSink.Owned.Single(o => o.Id == owned.Id).SameValues(owned), Is.True);
            Assert.That(original.Health.CurrentHealth, Is.EqualTo(hp));
            if (change == "SoulMaxHealth") Assert.That(final.Health.MaxHealth, Is.EqualTo(101));
            if (change == "Terrain") Assert.That(final.Health.MaxHealth, Is.EqualTo(50));
            if (change == "Reserve") Assert.That(final.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            if (change == "Death") Assert.That(final.Health.IsDead, Is.True);
            if (change == "BattleEnd") Assert.That(b.CombatStateReadSink.Current.CommandState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(original.Stats.TraitStatuses.ToArray().OfType<RampageTraitStatusSnapshot>().Single().KillAttackBonus,
                Is.EqualTo(seededHistory ? 8 : 0));
        }

        [Test]
        public void StaleSlowPreviewRejectsWithoutMovement()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new RampageSource("Movement"));
            var command = battle.PreviewMove(battle.Actor, battle.Actor.Anchor.Value, new CellCoord(1, 1));
            PeriodicStatusPublicationTests.Install(battle, battle.Actor, new StatusEffectSet(new AppliedStatus[]
                { new MovementRangeDecreaseStatus(PeriodicStatusPublicationTests.Origin(battle.Character(EntityKey.Unit_Demon)), 2) }));
            var before = battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).ToArray();
            battle.Buffers.CombatViewWriteSink.Submit(command);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Reason, Is.EqualTo("StaleContext"));
            Assert.That(battle.Buffers.CharacterReadSink.Characters.Select(c => c.ToFacts()).Zip(before, (after, prior) => after.SameValues(prior)), Is.All.True);
        }

        private static void InstallHistory(ConfiguredMovementBattle battle, int bonus)
        {
            var before = battle.Actor;
            var status = before.Stats.TraitStatuses.ToArray().OfType<RampageTraitStatusSnapshot>().Single();
            var after = before.With(stats: before.Stats.With(traitStatuses: before.Stats.TraitStatuses.ToArray().Select(t =>
                t is RampageTraitStatusSnapshot ? new RampageTraitStatusSnapshot(status.BaseAttackBonus, status.RankAttackBonus,
                    status.MovementBonus, bonus) : t).ToArray()));
            after = CombatEffectiveStatsCalculator.ProjectTraitAttack(after);
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(before, after) }, Array.Empty<SoulCountChange>()));
        }

        private sealed class RampageSource : IBattleInitializationSource
        {
            private readonly string _change;
            internal RampageSource(string change) => _change = change;

            public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new ConfiguredMovementBattle.EncounterSource(new CellCoord(0, 3),
                    terminalHeadquarters: _change == "BattleEnd").Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => s.Key == EntityKey.Unit_Cowboy || s.Key == EntityKey.Unit_Demon
                    ? new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(), 100,
                        s.MoveMethod, 3, s.CategoryKey, s.Key == EntityKey.Unit_Cowboy ? 20 : 8, 0, s.Priority,
                        s.Key != EntityKey.Unit_Cowboy ? s.Traits.ToArray() : _change == "Regeneration"
                            ? new TraitSpec[] { new RampageTraitSpec(10, 0, 1), new RegenerationTraitSpec(2) }
                            : new TraitSpec[] { new RampageTraitSpec(10, 0, 1) }, s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)
                    : s).ToArray();
                var environment = _change == "Terrain" ? new MovementEnvironmentFacts(new MovementTileFact[]
                    { new DestinationTileModifierFact(1, new CellCoord(1, 1), true, new[] { Owner.Player, Owner.Enemy }, 0, -50) }) : basis.Environment;
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray(),
                    environment: environment, itemDefinitions: new[] { new ItemDefinition(1, new WhiskeyItemSpec(50, 10)) });
            }
        }
    }
}
