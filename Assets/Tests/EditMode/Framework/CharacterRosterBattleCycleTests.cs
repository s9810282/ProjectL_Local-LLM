using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed class CharacterRosterBattleCycleTests
    {
        [Test]
        public void WoundedMain_KeepsCurrentHealth_MaxIsPersistentPlusSouls()
        {
            var b = Create();
            Enter(b);
            var main = Main(b);
            Assert.That(main.Health.CurrentHealth, Is.EqualTo(7));
            Assert.That(main.Health.MaxHealth, Is.EqualTo(17));
        }

        [Test]
        public void RepeatedEntryWithSouls_AppliesCountOnce()
        {
            var b = Create();
            for (int i = 0; i < 3; i++)
            {
                Enter(b);
                Assert.That(Main(b).Health.MaxHealth, Is.EqualTo(17));
                Assert.That(Main(b).Stats.SoulAttackBonus, Is.EqualTo(2));
                b.BattleWriteSink.EndBattle();
                b.CharacterReadSink.TryGetMainState(out var stored);
                Assert.That(stored.PersistentMaxHealth, Is.EqualTo(15));
                Assert.That(stored.CurrentHealth, Is.EqualTo(7));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SubEntry_FillsSoulAdjustedMaxAfterDamageOrDeath(bool dead)
        {
            var b = Create();
            var sub = b.CharacterReadSink.Owned.First(x => x.Role == CharacterOwnershipRole.Sub);
            b.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(sub.Id, 0, 2) }));
            Enter(b);
            var active = Find(b, sub.Id);
            var s = active.Stats;
            var temporary = new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.MoveCost,
                s.AppointRange, s.AttackPower, s.PhysicalDefense,
                s.MaxActionCost, s.AttackCost, s.RemainingMoveRange, s.CurrentActionCost,
                s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category, 5,
                s.AttackPower + 5, 2, 2);
            var final = new CharacterCombatFacts(active.EntityId, sub.Id, active.CharacterKey, active.Owner,
                active.OwnershipRole, dead ? CharacterRuntimeArea.Excluded : active.Area,
                new HealthStateSnapshot(dead ? 0 : 3, 16), temporary,
                dead ? null : active.ReserveSlotIndex, null, null, Array.Empty<CellCoord>(),
                active.MovementCheckpointAnchor, 0, 0, 3, 4);
            b.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(active, final) },
                Array.Empty<SoulCountChange>()));
            b.BattleWriteSink.EndBattle();
            Assert.That(b.CharacterReadSink.Characters, Is.Empty);
            Assert.That(b.CharacterReadSink.TryGetOwnedInstance(active.EntityId, out _), Is.False);
            Assert.That(b.CharacterReadSink.GetSoulCount(sub.Id), Is.EqualTo(2));
            Enter(b, reverse: true);
            var next = Find(b, sub.Id);
            Assert.That(next.Area, Is.EqualTo(CharacterRuntimeArea.Hand));
            Assert.That(next.Health.CurrentHealth, Is.EqualTo(12));
            Assert.That(next.Health.MaxHealth, Is.EqualTo(12));
            Assert.That(next.TemporaryAttackBonus, Is.Zero);
            Assert.That(next.TemporaryMaxHealthBonus, Is.Zero);
            Assert.That(Main(b).Health.CurrentHealth, Is.EqualTo(7));
        }

        [Test]
        public void SameKindCards_OnlyCollectorGrows_AcrossReassignedEntityIds()
        {
            var b = Create();
            var subs = b.CharacterReadSink.Owned.Where(x => x.Role == CharacterOwnershipRole.Sub).ToArray();
            Enter(b);
            var a = Find(b, subs[0].Id);
            var cell = new CellCoord(2, 1);
            var deployed = new CharacterCombatFacts(a.EntityId, a.OwnedId, a.CharacterKey, a.Owner,
                a.OwnershipRole, CharacterRuntimeArea.Field, a.Health, a.Stats, null, cell, Rotation4.R0,
                new[] { cell }, cell, 0, 0);
            b.BattleWriteSink.Apply(new CharacterChanges(new[] { new CharacterStateChange(a, deployed) },
                Array.Empty<SoulCountChange>()));
            var field = new SoulFieldSnapshot(2, new[]
            {
                new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "soul"), cell, 5, 1)
            });
            b.BattleWriteSink.Apply(new SoulActionChanges(new CharacterChanges(
                Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(b.CombatStateReadSink.SoulField, field), Array.Empty<long>()));
            var collected = new SoulEffectCalculator(new ArtifactLoadoutSnapshot(0,
                Array.Empty<ArtifactPlacedEffectSnapshot>())).Collect(
                new SoulCollectorInput(deployed, subs[0], 0, 10), field, true);
            b.BattleWriteSink.Apply(collected);
            b.BattleWriteSink.EndBattle();
            Enter(b, reverse: true);
            Assert.That(Find(b, subs[0].Id).EntityId, Is.Not.EqualTo(a.EntityId));
            Assert.That(Find(b, subs[0].Id).Stats.SoulAttackBonus, Is.EqualTo(1));
            Assert.That(Find(b, subs[1].Id).Stats.SoulAttackBonus, Is.Zero);
            Assert.That(b.CharacterReadSink.Characters.Single(x => x.Owner == Owner.Enemy).Stats.SoulAttackBonus, Is.Zero);
        }

        private static IBufferQuery Create()
        {
            var b = CharacterBattleSettlementTests.Create();
            b.BattleWriteSink.EndBattle();
            b.CharacterReadSink.TryGetMainState(out var original);
            b.CharacterWriteSink.Apply(new CharacterChanges(Array.Empty<CharacterStateChange>(),
                new[] { new SoulCountChange(new CharacterInstanceId(1), 0, 2) },
                new MainCharacterPersistentChange(original, CharacterBufferTestData.MainState(7, 15))));
            b.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub),
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
            });
            return b;
        }

        private static void Enter(IBufferQuery b, bool reverse = false)
        {
            b.CharacterReadSink.TryGetMainState(out var main);
            var souls = new Dictionary<CharacterInstanceId, int>();
            foreach (var owned in b.CharacterReadSink.Owned) souls.Add(owned.Id, b.CharacterReadSink.GetSoulCount(owned.Id));
            var order = b.CharacterReadSink.Owned.Where(x => x.Role == CharacterOwnershipRole.Sub).Select(x => x.Id).ToArray();
            if (reverse) Array.Reverse(order);
            b.BattleWriteSink.PrepareBattle(BattlePreparationCalculator.Prepare(
                CharacterBattlePreparationTests.Initialization(withEnemies: true),
                b.CharacterReadSink.Owned, main, souls, new InitialDrawPlan(order),
                b.CharacterReadSink.Characters.Select(x => x.ToFacts()).ToArray()));
        }

        private static CharacterCombatFacts Main(IBufferQuery b) =>
            b.CharacterReadSink.Characters.Single(x => x.OwnershipRole == CharacterOwnershipRole.Main).ToFacts();
        private static CharacterCombatFacts Find(IBufferQuery b, CharacterInstanceId id) =>
            b.CharacterReadSink.Characters.Single(x => x.OwnedId == id).ToFacts();
    }
}
