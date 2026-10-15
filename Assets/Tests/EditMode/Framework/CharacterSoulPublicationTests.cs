using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Systems.Factory.Runtime.Buffer;
using ProjectL.Systems.ServiceKeys;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Framework
{
    public sealed class CharacterSoulPublicationTests
    {
        [Test]
        public void SoulAndHealth_ValidateAgainstProposedFinalCount()
        {
            var buffers = Prepare();
            var main = buffers.CharacterReadSink.Characters.Single().ToFacts();
            var field = Field();
            InstallField(buffers, field);
            buffers.CharacterReadSink.TryGetMainState(out var source);
            buffers.CharacterReadSink.TryGetOwned(main.OwnedId.Value, out var owned);
            var calculator = Calculator();
            var result = calculator.Collect(new SoulCollectorInput(main, owned, 0, 10, source),
                buffers.CombatStateReadSink.SoulField, true);
            Assert.That(buffers.CharacterReadSink.GetSoulCount(main.OwnedId.Value), Is.Zero);
            Assert.That(buffers.CombatStateReadSink.SoulField, Is.SameAs(field), "Preview is pure.");
            buffers.BattleWriteSink.Apply(result);
            Assert.That(buffers.CharacterReadSink.GetSoulCount(main.OwnedId.Value), Is.EqualTo(1));
            Assert.That(buffers.CharacterReadSink.TryGetMainState(out var persistent), Is.True);
            Assert.That(persistent.CurrentHealth, Is.EqualTo(11));
            Assert.That(persistent.PersistentMaxHealth, Is.EqualTo(10));
            Assert.That(buffers.CombatStateReadSink.SoulField.Entries, Is.Empty);
            buffers.BattleWriteSink.EndBattle();
            Assert.That(buffers.CharacterReadSink.GetSoulCount(main.OwnedId.Value), Is.EqualTo(1));
            Assert.That(buffers.CombatStateReadSink.SoulField.Entries, Is.Empty);
        }

        // 영혼 수에 따른 최대 체력 공식은 Core가 소유하며 ArtifactSoulBufferCalculationTests가 검증한다. Buffer는 원본 일치만 거절한다.
        [TestCase("count")]
        [TestCase("field")]
        public void RejectedCollection_ChangesNoBuffer(string invalid)
        {
            var buffers = Prepare();
            var main = buffers.CharacterReadSink.Characters.Single().ToFacts();
            var field = Field();
            InstallField(buffers, field);
            var result = Collection(main, field, invalid);
            Assert.Throws<InvalidOperationException>(() => buffers.BattleWriteSink.Apply(result));
            Assert.That(buffers.CharacterReadSink.GetSoulCount(main.OwnedId.Value), Is.Zero);
            Assert.That(buffers.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.EqualTo(10));
            Assert.That(buffers.CharacterReadSink.Characters.Single().Stats.CurrentActionCost,
                Is.EqualTo(main.Stats.CurrentActionCost));
            Assert.That(buffers.CharacterReadSink.Characters.Single().PendingMoveActionCost,
                Is.EqualTo(main.PendingMoveActionCost));
            Assert.That(buffers.CombatStateReadSink.SoulField, Is.SameAs(field));
        }

        [Test]
        public void GrowthOverflow_LeavesRealBuffersUntouched()
        {
            var state = CharacterBufferTestData.MainState(maxHealth: int.MaxValue);
            var buffers = Prepare(state: state);
            var main = buffers.CharacterReadSink.Characters.Single().ToFacts();
            buffers.CharacterReadSink.TryGetOwned(main.OwnedId.Value, out var owned);
            var field = Field();
            InstallField(buffers, field);
            Assert.Throws<OverflowException>(() => buffers.BattleWriteSink.Apply(Calculator().Collect(
                new SoulCollectorInput(main, owned, 0, int.MaxValue, state), field, true)));
            var after = buffers.CharacterReadSink.Characters.Single();
            Assert.That(after.Health.CurrentHealth, Is.EqualTo(main.Health.CurrentHealth));
            Assert.That(after.Health.MaxHealth, Is.EqualTo(main.Health.MaxHealth));
            Assert.That(after.Stats.EffectiveAttackPower, Is.EqualTo(main.Stats.EffectiveAttackPower));
            Assert.That(after.Stats.CurrentActionCost, Is.EqualTo(main.Stats.CurrentActionCost));
            Assert.That(buffers.CharacterReadSink.GetSoulCount(main.OwnedId.Value), Is.Zero);
            Assert.That(buffers.CombatStateReadSink.SoulField, Is.SameAs(field));
        }

        [Test]
        public void Collection_RejectsGrowthThatDoesNotMatchConsumedSoulCount()
        {
            var buffers = Prepare();
            var main = buffers.CharacterReadSink.Characters.Single().ToFacts();
            var field = Field();
            InstallField(buffers, field);
            var finalState = CharacterBufferTestData.MainState(12);
            var characters = new CharacterChanges(new[] { new CharacterStateChange(main,
                CharacterBufferTestData.MainFacts(main.OwnedId.Value, finalState, 2)) },
                new[] { new SoulCountChange(main.OwnedId.Value, 0, 2) },
                new MainCharacterPersistentChange(CharacterBufferTestData.MainState(), finalState));
            Assert.Throws<ArgumentException>(() => buffers.BattleWriteSink.Apply(new SoulActionChanges(
                characters, new SoulFieldChange(field, new SoulFieldSnapshot(2, Array.Empty<SoulFieldFacts>())),
                new long[] { 1 })));
            Assert.That(buffers.CharacterReadSink.GetSoulCount(main.OwnedId.Value), Is.Zero);
            Assert.That(buffers.CharacterReadSink.Characters.Single().Health.CurrentHealth, Is.EqualTo(10));
            Assert.That(buffers.CombatStateReadSink.SoulField, Is.SameAs(field));
        }

        [Test]
        public void Collection_WritesCountForLinkedCollectorOnly()
        {
            var buffers = Prepare(false);
            var acquired = buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub),
                new RosterAcquireRequest(EntityKey.Unit_Akimbo, CharacterOwnershipRole.Sub)
            }).Acquired;
            var main = buffers.CharacterReadSink.Owned.Single(x => x.Role == CharacterOwnershipRole.Main);
            var a = CharacterBufferTestData.SubFacts(acquired[0].Id, new EntityId(2));
            var b = CharacterBufferTestData.SubFacts(acquired[1].Id, new EntityId(3));
            buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(new[]
            {
                CharacterBufferTestData.MainFacts(main.Id, CharacterBufferTestData.MainState()), a, b
            }, CharacterBufferTestData.Combat(acquired[0].Id, acquired[1].Id)));
            var origin = new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "soul");
            var field = new SoulFieldSnapshot(3, new[]
            {
                new SoulFieldFacts(1, origin, a.Anchor.Value, 5, 1),
                new SoulFieldFacts(2, origin, a.Anchor.Value, 5, 1)
            });
            InstallField(buffers, field);
            buffers.BattleWriteSink.Apply(Calculator().Collect(new SoulCollectorInput(
                a, acquired[0], 0, 6), field, true));
            Assert.That(buffers.CharacterReadSink.GetSoulCount(acquired[0].Id), Is.EqualTo(2));
            Assert.That(buffers.CharacterReadSink.GetSoulCount(acquired[1].Id), Is.Zero);
        }

        private static SoulActionChanges Collection(CharacterCombatFacts main,
            SoulFieldSnapshot field, string invalid = null)
        {
            var state = CharacterBufferTestData.MainState(11);
            var final = CharacterBufferTestData.MainFacts(main.OwnedId.Value, state, 1);
            var expectedField = invalid == "field" ? new SoulFieldSnapshot(2, new[]
            {
                new SoulFieldFacts(1, field.Entries[0].Origin, new CellCoord(2, 1), 5, 1)
            }) : field;
            return new SoulActionChanges(new CharacterChanges(
                new[] { new CharacterStateChange(main, final) },
                new[] { new SoulCountChange(main.OwnedId.Value, invalid == "count" ? 1 : 0,
                    invalid == "count" ? 2 : 1) },
                new MainCharacterPersistentChange(CharacterBufferTestData.MainState(), state)),
                new SoulFieldChange(expectedField, new SoulFieldSnapshot(2, Array.Empty<SoulFieldFacts>())),
                new long[] { 1 });
        }

        private static void InstallField(IBufferQuery buffers, SoulFieldSnapshot field) =>
            buffers.BattleWriteSink.Apply(new SoulActionChanges(new CharacterChanges(
                Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(SoulFieldSnapshot.Empty, field), Array.Empty<long>()));

        private static SoulFieldSnapshot Field() => new SoulFieldSnapshot(2, new[]
        {
            new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "soul"),
                new CellCoord(1, 1), 5, 1)
        });

        private static SoulEffectCalculator Calculator() => new SoulEffectCalculator(
            new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()));

        private static IBufferQuery Prepare(bool battle = true, MainCharacterPersistentState state = null)
        {
            state ??= CharacterBufferTestData.MainState();
            var builder = new SystemBuilderHelper();
            BufferSystemFactory.Create().ExportServices(builder);
            var buffers = builder.BuildSystemReader().ServiceStoreReader.GetService(BufferSystemKey.Buffer);
            var main = buffers.CharacterWriteSink.Acquire(new[]
            {
                new RosterAcquireRequest(EntityKey.Unit_MainCharacter, CharacterOwnershipRole.Main)
            }, state).Acquired.Single();
            if (battle) buffers.BattleWriteSink.PrepareBattle(new BattlePreparation(new[]
            {
                CharacterBufferTestData.MainFacts(main.Id, state)
            }, CharacterBufferTestData.Combat()));
            return buffers;
        }
    }
}
