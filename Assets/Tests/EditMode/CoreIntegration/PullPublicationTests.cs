using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PullPublicationTests
    {
        [Test]
        public void RepeatedPreview_PublishesMovementAndDamageOncePreservingExposure()
        {
            using var battle = Create();
            var target = battle.Character(EntityKey.Unit_Demon);
            var exposure = new StatusEffectSet(new[] { HuntersMarkPublicationTests.Exposure(battle.Main.EntityId, 25, 2) });
            Change(battle, target, target.WithStatusEffects(exposure));
            var b = battle.Buffers;
            var before = battle.CaptureGameplay();
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            for (int i = 0; i < 3; i++)
            {
                b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(target.Anchor.Value)));
                Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
                Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
                Assert.That(preview.Attack.Value.CanAttack, Is.True);
                Assert.That(Movement(preview.Attack.Value).FinalAnchor, Is.EqualTo(new CellCoord(0, 2)));
                Assert.That(preview.Attack.Value.Targets.ToArray().Single().TargetHealthAfter, Is.EqualTo(94));
                Assert.That(battle.CaptureGameplay(), Is.EqualTo(before));
            }
            battle.Submit(battle.Attack(battle.Actor, target.Anchor.Value));
            var final = battle.Character(EntityKey.Unit_Demon);
            Assert.That(final.Anchor, Is.EqualTo(new CellCoord(0, 2)));
            Assert.That(final.OccupiedCells, Is.EqualTo(new[] { new CellCoord(0, 2) }));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(94));
            Assert.That(final.StatusEffects.SameValues(exposure), Is.True);
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatPullEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.OfType<CombatHuntersMarkEvent>(), Is.Empty);
            var pull = result.Events.OfType<CombatPullEvent>().Single().Movement;
            Assert.That(result.Attack.Value.Pull, Is.SameAs(pull));
            Assert.That(pull.Steps.Select(s => s.To), Is.EqualTo(pull.Path));
            Assert.That(pull.Steps.All(s => s.Cause.Agency == CombatMovementAgency.Forced && s.Cause.TraitKey == TraitKey.Pull), Is.True);
            Assert.That(pull.Steps.First().From, Is.EqualTo(pull.StartAnchor));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved && e.Final.EntityId == target.EntityId), Is.False);
            var once = new CoreIntegrationSnapshot(b);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            once.AssertUnchanged(b);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StaleTargetPositionOrExposure_RejectsAllStores(bool position)
        {
            using var battle = Create();
            var target = battle.Character(EntityKey.Unit_Demon);
            var input = battle.Publication().Attack(battle.Actor, target.Anchor.Value);
            var calculated = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(calculated.Accepted, Is.True, calculated.Reason);
            var altered = position
                ? target.WithPlacement(target.Area, null, new CellCoord(1, 3), target.Rotation, new[] { new CellCoord(1, 3) })
                : target.WithStatusEffects(new StatusEffectSet(new[] { HuntersMarkPublicationTests.Exposure(battle.Main.EntityId, 25, 2) }));
            Change(battle, target, altered);
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(calculated));
            before.AssertUnchanged(battle.Buffers);
            Change(battle, altered, target);
            battle.Buffers.BattleWriteSink.Apply(calculated);
            Assert.That(battle.Character(EntityKey.Unit_Demon).Anchor, Is.EqualTo(new CellCoord(0, 2)));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.DoesNotThrow(() => battle.Buffers.BattleWriteSink.Apply(calculated));
            once.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void FriendlyMove_PublishesWithoutFakeDamageHit()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new PullBattleSource(new PullTraitSpec(1), target: new CellCoord(7, 5)));
            var ally = battle.Character(EntityKey.Unit_DynamiteHammer);
            battle.Submit(battle.PreviewMove(ally, new CellCoord(2, 0), new CellCoord(2, 1), new CellCoord(2, 2),
                new CellCoord(1, 2), new CellCoord(0, 2), new CellCoord(0, 3)));
            ally = battle.Character(EntityKey.Unit_DynamiteHammer);
            battle.Submit(battle.Attack(battle.Actor, ally.Anchor.Value));
            var final = battle.Character(EntityKey.Unit_DynamiteHammer);
            Assert.That(final.Anchor, Is.EqualTo(new CellCoord(0, 2)));
            Assert.That(final.Health, Is.EqualTo(ally.Health));
            Assert.That(final.ActionUse, Is.EqualTo(UnitActionUse.Moved));
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
            Assert.That(Movement(result.Attack.Value).TargetId, Is.EqualTo(ally.EntityId));
            Assert.That(result.Events.OfType<CombatPullEvent>().Count(), Is.EqualTo(1));
        }


        [TestCase(1)]
        [TestCase(2)]
        public void ObstacleOrdinaryDamage_LeavesOccupancyUnchanged(int distance)
        {
            var cell = new CellCoord(0, 1 + distance);
            var definition = new StructureSpec(EntityKey.Structure_PowderBarrel, 2, 1,
                new[] { CellCoord.Zero, CellCoord.Right }, 100, false, 0);
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new PullBattleSource(new PullTraitSpec(1), target: new CellCoord(7, 5))
                { Obstacle = new InitialObstaclePlacementSpec(definition, ObstacleContentKind.PowderBarrel, cell, Rotation4.R0, 3, 7) });
            var before = battle.Buffers.CombatStateReadSink.Current.CommandState.Structures.Single(t => t.Definition.Key == definition.Key);
            int sequence = battle.Buffers.CombatStateReadSink.LatestResultSequence;
            battle.Buffers.CombatViewWriteSink.Submit(battle.Attack(battle.Actor, cell));
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.True);
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Accepted, Is.EqualTo(distance == 1));
            var after = battle.Buffers.CombatStateReadSink.Current.CommandState.Structures.Single(t => t.EntityId == before.EntityId);
            Assert.That(after.OccupiedCells, Is.EqualTo(before.OccupiedCells));
            Assert.That(after.ObstacleDurability.RemainingHits, Is.EqualTo(distance == 1 ? 2 : 3));
            Assert.That(result.Events.OfType<CombatPullEvent>(), Is.Empty);
            Assert.That(battle.Buffers.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            once.AssertUnchanged(battle.Buffers);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SteadfastTarget_BlocksKnockbackAndStillPublishesPull(bool pull)
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new SteadfastSource(pull));
            var before = battle.Character(EntityKey.Unit_Demon);
            battle.Submit(battle.Attack(battle.Actor, before.Anchor.Value));
            var after = battle.Character(EntityKey.Unit_Demon);
            Assert.That(after.Anchor, Is.EqualTo(pull ? new CellCoord(0, 2) : before.Anchor.Value));
            Assert.That(after.Health.CurrentHealth, Is.LessThan(before.Health.CurrentHealth));
            Assert.That(battle.Buffers.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            if (pull) Assert.That(Movement(result.Attack.Value).FinalAnchor, Is.EqualTo(new CellCoord(0, 2)));
            else Assert.That(result.Attack.Value.Knockbacks.Single().Stop, Is.EqualTo(AttackKnockbackStop.Immune));
            var once = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            once.AssertUnchanged(battle.Buffers);
        }

        private sealed class SteadfastSource : ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem.IBattleInitializationSource
        {
            private readonly bool _pull;
            internal SteadfastSource(bool pull) => _pull = pull;
            public BattleInitialization Read(ProjectL.Systems.DataBaseSystem.IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
            {
                var basis = new PullBattleSource(_pull ? (TraitSpec)new PullTraitSpec(1) : new KnockbackTraitSpec(1),
                    range: _pull ? 1 : 3).Read(database, chapterKey, stageKey);
                var units = basis.Units.Select(s => new UnitSpec(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight,
                    s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod, s.AttackRange, s.CategoryKey,
                    s.AttackPower, s.PhysicalDefense, s.Priority,
                    s.Key == EntityKey.Unit_Demon ? new TraitSpec[] { new SteadfastWillTraitSpec() } : s.Traits.ToArray(),
                    s.OwnerEligibility, s.Occlusion, s.Behavior, s.KillReward)).ToArray();
                return new BattleInitialization(basis.Stage, basis.MainAnchor, basis.MainRotation, units, basis.Structures, basis.Categories,
                    basis.AttackProfiles.Select(p => new CombatAttackProfile(units.Single(u => u.Key == p.Definition.Key), p.Method, p.OwnedId)).ToArray());
            }
        }

        private static ConfiguredMovementBattle Create() => new(ConfiguredMovementBattle.Config, new PullBattleSource(new PullTraitSpec(1)));

        private static void Change(ConfiguredMovementBattle battle, CharacterCombatFacts before, CharacterCombatFacts after) =>
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(before, after) }, Array.Empty<SoulCountChange>()));

        private static AttackPullSnapshot Movement<T>(T result)
        {
            var property = typeof(T).GetProperty("Pull");
            Assert.That(property, Is.Not.Null, "Pull contract is missing.");
            var movement = (AttackPullSnapshot)property.GetValue(result);
            Assert.That(movement, Is.Not.Null);
            return movement;
        }
    }
}
