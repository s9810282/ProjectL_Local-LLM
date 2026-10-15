using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class ArtifactSoulBufferCalculationTests
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(5)]
        public void Collection_AddsOneCountPerSoul_AndDerivesPlusOneAttackAndMaxHealth(int heal)
        {
            var field = Field(heal);
            var collector = Facts(count: 3, temporaryAttack: 2);
            var input = Input(collector, 3);
            var calculator = EmptyCalculator();

            SoulActionChanges result = calculator.Collect(input, field, true);
            var final = result.Characters.Characters.Single().Final;

            Assert.That(result.Characters.Souls.Single().ExpectedCount, Is.EqualTo(3));
            Assert.That(result.Characters.Souls.Single().FinalCount, Is.EqualTo(5));
            Assert.That(final.Health.MaxHealth, Is.EqualTo(15));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(Math.Min(15, 9 + heal * 2)));
            Assert.That(final.Stats.AttackPower, Is.EqualTo(5));
            Assert.That(final.Stats.SoulAttackBonus, Is.EqualTo(5));
            Assert.That(final.Stats.EffectiveAttackPower, Is.EqualTo(12));
            Assert.That(result.Field.Final.Entries, Is.Empty);
            Assert.That(result.ConsumedSoulIds, Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(field.Entries.Count, Is.EqualTo(2), "Calculation/preview must not mutate inputs.");
            Assert.That(collector.Health.CurrentHealth, Is.EqualTo(9));
        }

        [Test]
        public void RepeatedCollection_RecomputesFromCountAndKeepsTemporaryBonusSeparate()
        {
            var calculator = EmptyCalculator();
            var first = calculator.Collect(Input(Facts(0, 2), 0), Field(1), true);
            var second = calculator.Collect(Input(first.Characters.Characters.Single().Final, 2),
                Field(1), true);
            Assert.That(second.Characters.Souls.Single().FinalCount, Is.EqualTo(4));
            Assert.That(second.Characters.Characters.Single().Final.Stats.EffectiveAttackPower,
                Is.EqualTo(11));
        }

        [TestCase(4, 5)]
        [TestCase(-2, -3)]
        public void Collection_PreservesIndependentPositionContribution(int positionAttack, int positionHealth)
        {
            var original = Facts(2, temporaryAttack: 3);
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] {
                new DestinationTileModifierFact(7, original.Anchor.Value, true, new[] { Owner.Player },
                    positionAttack, positionHealth) });
            var modifier = PositionTileModifierCalculator.ResolveAt(original.Anchor.Value, environment);
            var actor = PositionTileModifierCalculator.Replace(original, modifier);
            var calculator = EmptyCalculator();
            var collection = calculator.Collect(Input(actor, 2), Field(1), true);
            var final = collection.Characters.Characters.Single().Final;
            Assert.That(final.Stats.SoulAttackBonus, Is.EqualTo(4));
            Assert.That(final.Stats.SoulMaxHealthBonus, Is.EqualTo(4));
            Assert.That(final.Stats.ArtifactAttackBonus, Is.EqualTo(7));
            Assert.That(final.TemporaryAttackBonus, Is.EqualTo(3));
            Assert.That(final.TemporaryMaxHealthBonus, Is.Zero);
            Assert.That(final.Stats.EffectiveAttackPower, Is.EqualTo(12 + positionAttack));
            Assert.That(final.Health.MaxHealth, Is.EqualTo(14 + positionHealth));
            Assert.That(final.PositionModifier.SameValues(modifier), Is.True);
            Assert.That(calculator.EvaluateAttackPower(final, 4, new[] { final }), Is.EqualTo(12 + positionAttack));
            var removed = PositionTileModifierCalculator.Replace(final, PositionTileModifier.None);
            Assert.That(removed.Stats.EffectiveAttackPower, Is.EqualTo(12));
            Assert.That(removed.Health.MaxHealth, Is.EqualTo(14));
            Assert.That(removed.Stats.SoulAttackBonus, Is.EqualTo(4));
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void FailedArrivalOrDeadCollector_ConsumesNothing(bool moved, bool dead)
        {
            var result = EmptyCalculator().Collect(Input(Facts(0, dead: dead), 0), Field(1), moved);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.Characters.Souls, Is.Empty);
            Assert.That(result.ConsumedSoulIds, Is.Empty);
            Assert.That(result.Field.Final.Entries.Count, Is.EqualTo(2));
        }

        [Test]
        public void SoulCollection_HappensOnCommittedMovement()
        {
            var calculator = EmptyCalculator();
            Assert.Throws<InvalidOperationException>(() =>
                calculator.Collect(Input(Facts(0, pending: 1), 0), Field(1), true));
            var facts = Facts(0);
            var result = calculator.Collect(Input(facts, 0), Field(1), true);
            var final = result.Characters.Characters.Single().Final;
            Assert.That(final.PendingMoveActionCost, Is.Zero);
            Assert.That(final.MovementCheckpointAnchor, Is.EqualTo(facts.Anchor.Value));
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(facts.Stats.CurrentActionCost));
        }

        [Test]
        public void ForcedArrival_PreservesExistingMovementAccounting()
        {
            var facts = Facts(0, pending: 1);
            var input = new SoulCollectorInput(facts,
                new OwnedCharacterRecord(facts.OwnedId.Value, facts.CharacterKey, CharacterOwnershipRole.Sub),
                0, 10, arrivalKind: SoulArrivalKind.Forced);
            var final = EmptyCalculator().Collect(input, Field(1), true)
                .Characters.Characters.Single().Final;
            Assert.That(final.PendingMoveActionCost, Is.EqualTo(1));
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(facts.Stats.CurrentActionCost));
        }

        [Test]
        public void InvalidOwnedLinkAndGrowthOverflow_RejectBeforeAnyPublication()
        {
            var facts = Facts(0);
            Assert.Throws<ArgumentException>(() => new SoulCollectorInput(facts,
                new OwnedCharacterRecord(new CharacterInstanceId(2), facts.CharacterKey,
                    CharacterOwnershipRole.Sub), 0, 10));
            Assert.Throws<OverflowException>(() => EmptyCalculator().Collect(
                new SoulCollectorInput(Facts(0, baseHealth: int.MaxValue),
                    new OwnedCharacterRecord(facts.OwnedId.Value, facts.CharacterKey,
                        CharacterOwnershipRole.Sub), 0, int.MaxValue), Field(1), true));
        }

        [TestCase(true, Owner.Enemy, 1)]
        [TestCase(false, Owner.Enemy, 0)]
        [TestCase(true, Owner.Player, 0)]
        public void SpawnAndExpiry_UseDeathFactsAndKeepFiveTurnLifetime(bool night, Owner owner, int count)
        {
            var effect = ArtifactSoulSchemaTests.SoulEffect();
            var calculator = new SoulEffectCalculator(new ArtifactLoadoutSnapshot(1,
                new[] { new ArtifactPlacedEffectSnapshot(11, 4, effect) }));
            var alive = Facts(0, owner: owner);
            var dead = Facts(0, dead: true, owner: owner);
            var result = calculator.PlanDeaths(SoulFieldSnapshot.Empty,
                new[] { new SoulDeathFacts(new CharacterStateChange(alive, dead), dead.Anchor.Value) }, night, false);
            Assert.That(result.Final.Entries.Count, Is.EqualTo(count));
            if (count == 0) return;
            Assert.That(result.Final.Entries.Single().RemainingUnitTurns, Is.EqualTo(5));
            SoulFieldSnapshot field = result.Final;
            for (int i = 0; i < 5; i++) field = calculator.PlanCountdown(field).Final;
            Assert.That(field.Entries, Is.Empty);
            Assert.That(field.NextSoulId, Is.EqualTo(2));
        }

        [Test]
        public void LastStandAndTemporaryAttackBuff_DoNotChangePersistentBaseOrCount()
        {
            var effect = ArtifactLastStandRuleTests.LastStandEffect();
            var calculator = new SoulEffectCalculator(new ArtifactLoadoutSnapshot(1,
                new[] { new ArtifactPlacedEffectSnapshot(12, 4, effect) }));
            var attacker = Facts(2, 3);
            var power = calculator.EvaluateAttackPower(attacker, 2,
                new[] { attacker, Facts(0, owner: Owner.Enemy), Facts(0, owner: Owner.Enemy, entityId: 3) });
            Assert.That(power, Is.EqualTo(15)); // base 5 + souls 2 + temporary 3 + Last Stand 5
            Assert.That(attacker.Stats.AttackPower, Is.EqualTo(5));
            Assert.That(attacker.Stats.SoulAttackBonus, Is.EqualTo(2));
        }

        [Test]
        public void EnemyAttack_NeverUsesPlayerSoulCount()
        {
            var enemy = Facts(0, owner: Owner.Enemy);
            Assert.That(EmptyCalculator().EvaluateAttackPower(enemy, int.MaxValue, new[] { enemy }),
                Is.EqualTo(5));
        }

        [Test]
        public void TurnEnd_SpawnsBeforeCountdown_AndDisplacedDeathKeepsItsActualAnchor()
        {
            var calculator = new SoulEffectCalculator(new ArtifactLoadoutSnapshot(1,
                new[] { new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()) }));
            var before = Facts(0, owner: Owner.Enemy);
            var dead = new CharacterCombatFacts(before.EntityId, null, before.CharacterKey, Owner.Enemy,
                null, CharacterRuntimeArea.Excluded, new HealthStateSnapshot(0, 10), before.Stats,
                null, null, null, Array.Empty<CellCoord>(), before.MovementCheckpointAnchor, 0, 0);
            var result = calculator.PlanDeaths(SoulFieldSnapshot.Empty,
                new[] { new SoulDeathFacts(new CharacterStateChange(before, dead), new CellCoord(3, 1)) }, true, true);
            Assert.That(result.Final.Entries.Single().Cell, Is.EqualTo(new CellCoord(3, 1)));
            Assert.That(result.Final.Entries.Single().RemainingUnitTurns, Is.EqualTo(4));
        }

        [Test]
        public void LargeAttackPower_DoesNotOverflowWhenFinalPowerFits()
        {
            var attacker = Facts(0, attack: 1_500_000_000);
            Assert.That(EmptyCalculator().EvaluateAttackPower(attacker, 0, new[] { attacker }),
                Is.EqualTo(1_500_000_000));
        }

        [Test]
        public void Collection_OrdersDistinctSameCellSouls_AndLeavesOtherCellsAndInputIntact()
        {
            var origin = new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "soul");
            var entries = new[]
            {
                new SoulFieldFacts(2, origin, new CellCoord(2, 1), 5, 0),
                new SoulFieldFacts(3, origin, new CellCoord(3, 1), 5, 1),
                new SoulFieldFacts(1, origin, new CellCoord(2, 1), 5, 10)
            };
            var field = new SoulFieldSnapshot(4, entries);
            entries[0] = null;
            var result = EmptyCalculator().Collect(Input(Facts(0), 0), field, true);
            var final = result.Characters.Characters.Single().Final;
            Assert.That(result.ConsumedSoulIds, Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(final.Health, Is.EqualTo(new HealthStateSnapshot(11, 12)));
            Assert.That(final.Stats.EffectiveAttackPower, Is.EqualTo(7));
            Assert.That(result.Field.Final.Entries.Select(s => s.Id), Is.EqualTo(new long[] { 3 }));
            Assert.That(result.Field.Final.NextSoulId, Is.EqualTo(4));
            Assert.That(field.Entries.Select(s => s.Id), Is.EqualTo(new long[] { 1, 2, 3 }));
            Assert.Throws<ArgumentException>(() => new SoulFieldSnapshot(4,
                new[] { field.Entries[0], field.Entries[0] }));
            Assert.Throws<ArgumentException>(() => new SoulFieldSnapshot(1, new[] { field.Entries[0] }));
            Assert.Throws<NotSupportedException>(() =>
                ((System.Collections.Generic.IList<long>)result.ConsumedSoulIds).Clear());
        }

        [Test]
        public void Collection_SecondSoulOverflowLeavesWholeInputUnchanged()
        {
            var facts = Facts(0, baseHealth: int.MaxValue - 1);
            var field = Field(0);
            Assert.Throws<OverflowException>(() => EmptyCalculator().Collect(
                new SoulCollectorInput(facts, new OwnedCharacterRecord(facts.OwnedId.Value,
                    facts.CharacterKey, CharacterOwnershipRole.Sub), 0, int.MaxValue - 1), field, true));
            Assert.That(facts.Health, Is.EqualTo(new HealthStateSnapshot(9, int.MaxValue - 1)));
            Assert.That(facts.Stats.SoulAttackBonus, Is.Zero);
            Assert.That(facts.Stats.CurrentActionCost, Is.EqualTo(2));
            Assert.That(field.Entries.Select(s => s.Id), Is.EqualTo(new long[] { 1, 2 }));
            Assert.That(field.NextSoulId, Is.EqualTo(3));
        }

        [Test]
        public void Deaths_OrderEntityThenOrigin_AndRepeatedCalculationDoesNotAllocateLiveIds()
        {
            var calculator = TwoAltars();
            var first = DeathAt(2, new CellCoord(3, 1));
            var second = DeathAt(3, new CellCoord(4, 1));
            var source = new SoulFieldSnapshot(7, Array.Empty<SoulFieldFacts>());
            var result = calculator.PlanDeaths(source, new[] { second, first }, true, false);
            var repeated = calculator.PlanDeaths(source, new[] { first, second }, true, false);
            Assert.That(result.Final.Entries.Select(s => s.Id), Is.EqualTo(new long[] { 7, 8, 9, 10 }));
            Assert.That(result.Final.Entries.Select(s => s.Cell), Is.EqualTo(new[]
                { new CellCoord(3, 1), new CellCoord(3, 1), new CellCoord(4, 1), new CellCoord(4, 1) }));
            Assert.That(result.Final.Entries.Select(s => s.Origin.OwnerInstanceId),
                Is.EqualTo(new[] { 4, 5, 4, 5 }));
            Assert.That(result.Final.Entries.Select(s => s.RemainingUnitTurns), Is.All.EqualTo(5));
            Assert.That(result.Final.NextSoulId, Is.EqualTo(11));
            Assert.That(repeated.Final.SameValues(result.Final), Is.True);
            Assert.That(source.Entries, Is.Empty);
            Assert.That(source.NextSoulId, Is.EqualTo(7));
        }

        [Test]
        public void Deaths_DuplicateFactOrLateIdOverflowCannotPublishPartialSpawns()
        {
            var first = DeathAt(2, new CellCoord(3, 1));
            var second = DeathAt(3, new CellCoord(4, 1));
            var source = new SoulFieldSnapshot(long.MaxValue - 2, Array.Empty<SoulFieldFacts>());
            Assert.Throws<ArgumentException>(() => TwoAltars().PlanDeaths(source,
                new[] { first, first }, true, false));
            Assert.Throws<OverflowException>(() => TwoAltars().PlanDeaths(source,
                new[] { first, second }, true, false));
            Assert.That(source.Entries, Is.Empty);
            Assert.That(source.NextSoulId, Is.EqualTo(long.MaxValue - 2));
        }

        [Test]
        public void Countdown_RemovesExpiringSoulOnce_AndPreservesIdsOriginHealAndInput()
        {
            var origin = new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "soul");
            var source = new SoulFieldSnapshot(3, new[]
            {
                new SoulFieldFacts(1, origin, CellCoord.Zero, 1, 3),
                new SoulFieldFacts(2, origin, CellCoord.Right, 5, 7)
            });
            var result = EmptyCalculator().PlanCountdown(source);
            var living = result.Final.Entries.Single();
            Assert.That(living.Id, Is.EqualTo(2));
            Assert.That(living.RemainingUnitTurns, Is.EqualTo(4));
            Assert.That(living.Origin, Is.EqualTo(origin));
            Assert.That(living.Cell, Is.EqualTo(CellCoord.Right));
            Assert.That(living.HealAmount, Is.EqualTo(7));
            Assert.That(result.Final.NextSoulId, Is.EqualTo(3));
            Assert.That(source.Entries.Select(s => s.RemainingUnitTurns), Is.EqualTo(new[] { 1, 5 }));
            Assert.Throws<NotSupportedException>(() =>
                ((System.Collections.Generic.IList<SoulFieldFacts>)result.Final.Entries).Clear());
        }

        [Test]
        public void LastStand_CountsOnlyLivingFieldFacts_AndKeepsEnemyAndTemporaryBonusesSeparate()
        {
            var actor = Facts(2, temporaryAttack: 3);
            var reserveSource = Facts(0, owner: Owner.Enemy, entityId: 5);
            var reserve = new CharacterCombatFacts(reserveSource.EntityId, null, reserveSource.CharacterKey,
                Owner.Enemy, null, CharacterRuntimeArea.Excluded, reserveSource.Health, reserveSource.Stats,
                null, null, null, Array.Empty<CellCoord>(), reserveSource.MovementCheckpointAnchor, 0, 0);
            var characters = new[] { Facts(0, owner: Owner.Enemy, dead: true, entityId: 4), reserve,
                Facts(0, owner: Owner.Enemy, entityId: 3), actor, Facts(0, owner: Owner.Enemy) };
            var calculator = new SoulEffectCalculator(new ArtifactLoadoutSnapshot(0, new[]
                { new ArtifactPlacedEffectSnapshot(12, 4, ArtifactLastStandRuleTests.LastStandEffect()) }));
            var captured = ArtifactCombatFacts.Capture(characters);
            Assert.That(captured.Units.ToArray().Select(u => u.Id.Value), Is.EqualTo(new long[] { 1, 2, 3 }));
            Assert.That(captured.PlayerUnitCount, Is.EqualTo(1));
            Assert.That(captured.EnemyUnitCount, Is.EqualTo(2));
            Assert.That(calculator.EvaluateAttackPower(actor, 2, characters), Is.EqualTo(15));
            Assert.That(calculator.EvaluateAttackPower(characters[2], int.MaxValue, characters), Is.EqualTo(5));
            Assert.That(actor.Stats.AttackPower, Is.EqualTo(5));
            Assert.That(actor.TemporaryAttackBonus, Is.EqualTo(3));
            Assert.That(actor.Stats.SoulAttackBonus, Is.EqualTo(2));
            var maximum = Facts(0, attack: int.MaxValue);
            Assert.Throws<OverflowException>(() => calculator.EvaluateAttackPower(maximum, 0,
                new[] { maximum, Facts(0, owner: Owner.Enemy), Facts(0, owner: Owner.Enemy, entityId: 3) }));
            Assert.That(maximum.Health, Is.EqualTo(new HealthStateSnapshot(9, 10)));
            Assert.That(maximum.Stats.AttackPower, Is.EqualTo(int.MaxValue));
            Assert.That(maximum.Stats.CurrentActionCost, Is.EqualTo(2));
        }

        private static SoulDeathFacts DeathAt(int entityId, CellCoord anchor)
        {
            var before = Facts(0, owner: Owner.Enemy, entityId: entityId);
            var dead = new CharacterCombatFacts(before.EntityId, null, before.CharacterKey, Owner.Enemy,
                null, CharacterRuntimeArea.Excluded, new HealthStateSnapshot(0, 10), before.Stats,
                null, null, null, Array.Empty<CellCoord>(), before.MovementCheckpointAnchor, 0, 0);
            return new SoulDeathFacts(new CharacterStateChange(before, dead), anchor);
        }

        private static SoulEffectCalculator TwoAltars() => new SoulEffectCalculator(
            new ArtifactLoadoutSnapshot(1, new[]
            {
                new ArtifactPlacedEffectSnapshot(11, 5, ArtifactSoulSchemaTests.SoulEffect()),
                new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect())
            }));

        private static SoulEffectCalculator EmptyCalculator() => new SoulEffectCalculator(
            new ArtifactLoadoutSnapshot(0, Array.Empty<ArtifactPlacedEffectSnapshot>()));

        private static SoulCollectorInput Input(CharacterCombatFacts facts, int count) =>
            new SoulCollectorInput(facts, new OwnedCharacterRecord(facts.OwnedId.Value,
                facts.CharacterKey, CharacterOwnershipRole.Sub), count, 10);

        private static SoulFieldSnapshot Field(int heal) => new SoulFieldSnapshot(3, new[]
        {
            new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "soul"),
                new CellCoord(2, 1), 5, heal),
            new SoulFieldFacts(2, new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "soul"),
                new CellCoord(2, 1), 5, heal)
        });

        private static CharacterCombatFacts Facts(int count, int temporaryAttack = 0,
            bool dead = false, int pending = 0, Owner owner = Owner.Player, int entityId = 1,
            int baseHealth = 10, int attack = 5)
        {
            var cell = new CellCoord(2, 1);
            var stats = new UnitStatsSnapshot(MoveMethod.LiftAndPlace, 3, 1, 3, attack, 0, 4, 1, 2, 2, 1, 1, Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Base), count + temporaryAttack,
                attack + count + temporaryAttack, count, count);
            return new CharacterCombatFacts(new EntityId(owner == Owner.Enemy && entityId == 1 ? 2 : entityId),
                owner == Owner.Player ? new CharacterInstanceId(1) : (CharacterInstanceId?)null,
                EntityKey.Unit_Akimbo, owner, owner == Owner.Player ? CharacterOwnershipRole.Sub : (CharacterOwnershipRole?)null,
                CharacterRuntimeArea.Field, new HealthStateSnapshot(dead ? 0 : 9, baseHealth + count), stats,
                null, cell, Rotation4.R0, new[] { cell }, cell, 1, pending, temporaryAttack);
        }
    }
}
