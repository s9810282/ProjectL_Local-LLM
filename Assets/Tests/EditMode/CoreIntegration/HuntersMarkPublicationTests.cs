using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class HuntersMarkPublicationTests
    {
        [Test]
        public void RepeatedPreview_PublishesPersistentSourcesAndDamageExactlyOnce()
        {
            using var battle = Create();
            SetExposure(battle, Exposure(battle.Main.EntityId, 25, 2));
            var b = battle.Buffers;
            var target = battle.Character(EntityKey.Unit_Demon);
            var original = battle.CaptureGameplay();
            int sequence = b.CombatStateReadSink.LatestResultSequence;
            int handled = b.CombatStateReadSink.HandledCommandNumber;
            for (int i = 0; i < 3; i++)
            {
                b.CombatViewWriteSink.SetPreview(CombatViewPreviewData.Attack(battle.Actor.EntityId, AttackAim.ForCell(target.Anchor.Value)));
                Assert.That(battle.Core.CombatPreview.ProcessLatestPreview(), Is.True);
                Assert.That(b.CombatStateReadSink.TryGetLatestPreview(out var preview), Is.True);
                Assert.That(preview.Attack.Value.CanAttack, Is.True);
                Assert.That(preview.Attack.Value.Targets.ToArray().Single(t => t.TargetId == target.EntityId).TargetHealthAfter, Is.EqualTo(94));
                Assert.That(battle.CaptureGameplay(), Is.EqualTo(original));
                Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence));
                Assert.That(b.CombatStateReadSink.HandledCommandNumber, Is.EqualTo(handled));
            }
            battle.Submit(battle.Attack(battle.Actor, target.Anchor.Value));
            var final = battle.Character(EntityKey.Unit_Demon);
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(94));
            Assert.That(final.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Select(s => s.IncreasePercent), Is.EqualTo(new[] { 25, 10 }));
            Assert.That(final.Debuffs.HuntersMark, Is.Null);
            Assert.That(b.CombatStateReadSink.LatestResultSequence, Is.EqualTo(sequence + 1));
            Assert.That(b.CombatResultReadSink.TryGetLatest(out var result), Is.True);
            Assert.That(result.Events.OfType<CombatHuntersMarkEvent>(), Is.Empty);
            Assert.That(result.Events.OfType<CombatTraitStateEvent>().Any(e => e.Final.EntityId == target.EntityId), Is.True);
            Assert.That(battle.Actor.ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            var once = new CoreIntegrationSnapshot(b);
            Assert.That(battle.Core.CombatCommands.ProcessNextCommand(), Is.False);
            once.AssertUnchanged(b);
        }

        [Test]
        public void StaleExpectedExposure_RejectsAllWrites()
        {
            using var battle = Create();
            var original = Exposure(battle.Main.EntityId, 10, 2);
            SetExposure(battle, original);
            var input = battle.Publication().Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value);
            var calculated = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(calculated.Accepted, Is.True, calculated.Reason);
            SetExposure(battle, Exposure(battle.Main.EntityId, 25, 3));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<InvalidOperationException>(() => battle.Buffers.BattleWriteSink.Apply(calculated));
            before.AssertUnchanged(battle.Buffers);
            SetExposure(battle, original);
            battle.Buffers.BattleWriteSink.Apply(calculated);
            Assert.That(battle.Character(EntityKey.Unit_Demon).StatusEffects.Entries.OfType<WeaknessExposureStatus>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void SourceAbsent_StillAmplifiesWithoutDereferencingOrOwningTrait()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config, new HuntersMarkBattleSource());
            SetExposure(battle, Exposure(new EntityId(900000), 25, 2));
            battle.Submit(battle.Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value));
            var final = battle.Character(EntityKey.Unit_Demon);
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(94));
            Assert.That(final.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Single().Origin.Value.OwnerInstanceId, Is.EqualTo(900000));
        }

        [Test]
        public void CalculationOverflow_LeavesEveryStoreAndExposureUnpublished()
        {
            using var battle = new ConfiguredMovementBattle(ConfiguredMovementBattle.Config,
                new HuntersMarkBattleSource(new HuntersMarkTraitSpec(10, 1)) { ActorAttackPower = int.MaxValue });
            SetExposure(battle, Exposure(battle.Main.EntityId, int.MaxValue, 1));
            battle.Buffers.CombatViewWriteSink.Submit(battle.Attack(battle.Actor, battle.Character(EntityKey.Unit_Demon).Anchor.Value));
            var before = new CoreIntegrationSnapshot(battle.Buffers);
            Assert.Throws<OverflowException>(() => battle.Core.CombatCommands.ProcessNextCommand());
            before.AssertUnchanged(battle.Buffers);
        }

        [Test]
        public void BattleEnd_DiscardsExposureWithoutChangingOwnedRecords()
        {
            using var battle = Create();
            SetExposure(battle, Exposure(battle.Actor.EntityId, 25, 2));
            var captured = battle.Character(EntityKey.Unit_Demon);
            var owned = battle.Buffers.CharacterReadSink.Owned.ToArray();
            battle.Buffers.BattleWriteSink.EndBattle();
            Assert.That(battle.Buffers.CharacterReadSink.Characters, Is.Empty);
            Assert.That(battle.Buffers.CharacterReadSink.Owned, Is.EqualTo(owned));
            Assert.That(captured.StatusEffects.Entries.OfType<WeaknessExposureStatus>().Single().IncreasePercent, Is.EqualTo(25));
        }

        internal static ConfiguredMovementBattle Create(int percent = 10, int duration = 1) => new(ConfiguredMovementBattle.Config,
            new HuntersMarkBattleSource(new HuntersMarkTraitSpec(percent, duration)));

        internal static WeaknessExposureStatus Exposure(EntityId source, int percent, int duration) =>
            new(new EffectOriginKey(EffectOwnerKind.Trait, source.Value, (int)TraitKey.HuntersMark, "main"), percent, duration);

        internal static void SetExposure(ConfiguredMovementBattle battle, params WeaknessExposureStatus[] statuses)
        {
            var before = battle.Character(EntityKey.Unit_Demon);
            var final = before.WithStatusEffects(new StatusEffectSet(before.StatusEffects.Entries.Where(s => s is not WeaknessExposureStatus).Concat(statuses).ToArray()));
            battle.Buffers.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(before, final) }, Array.Empty<SoulCountChange>()));
        }
    }
}