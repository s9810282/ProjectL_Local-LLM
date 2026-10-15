using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public class CombatCommandEffectTests
    {
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(3, 3)]
        [TestCase(99, 4)]
        public void PiercingCap_PreservesTargetOrderAndDamage(int cap, int expectedCount)
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction,
                TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], appointRange: 5,
                category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) });
            var characters = new[] { actor, input.Characters[1] }.Concat(Enumerable.Range(2, 4).Select(id =>
                CombatCommandTestData.Character(id, EntityKey.Unit_Demon, new CellCoord(id + 1, 3), Owner.Enemy))).ToArray();
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20, CategoryKey.Cannon,
                new PiercingFalloffTraitSpec(30, cap)), method);
            input = CombatCommandTestData.With(input, characters: characters, profiles: profiles);

            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Attack.HasValue, Is.True);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(hit => hit.TargetId.Value),
                Is.EqualTo(new[] { 2, 3, 4, 5 }.Take(expectedCount)));
            Assert.That(result.Attack.Value.Hits.ToArray().Select(hit => hit.DamageDealt),
                Is.EqualTo(new[] { 20, 14, 8, 2 }.Take(expectedCount)));
        }

        [Test]
        public void Victory_HealsProjectedMainOnceAndSpawnsNightDeathsAfterArrivalSettlement()
        {
            var input = CombatCommandTestData.Input(empty: true);
            var chars = new[] { input.Characters[0], CombatCommandTestData.With(input.Characters[1], hp: 70),
                CombatCommandTestData.With(CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Enemy), hp: 1) };
            var persistent = new MainCharacterPersistentState(70, 100, 1, 5, 4, 1);
            var hq = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 1, new[] { CellCoord.Zero }, 5, false, 0), new HealthStateSnapshot(5, 5),
                new CellCoord(4, 3), Rotation4.R0, new[] { new CellCoord(4, 3) });
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, true,
                new[] { hq }, input.State.ThornFields, input.State.Timeline);
            var effect = new ArtifactEffectData
            {
                entryId = "chalice", trigger = ArtifactTriggerType.StageCleared,
                source = new ArtifactSourceData { faction = ArtifactFactionFilter.Any },
                target = new ArtifactTargetData { type = ArtifactTargetType.MainCharacter, faction = ArtifactFactionFilter.Ally },
                effect = new ArtifactEffectPayloadData { type = ArtifactEffectType.Heal },
                value = new ArtifactValueData { amount = 3 }
            };
            var loadout = new ArtifactLoadoutSnapshot(0, new[] { new ArtifactPlacedEffectSnapshot(1, 1, effect),
                new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()) });
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, state, artifacts: loadout, main: persistent), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(result.Characters.Main.Final.CurrentHealth, Is.EqualTo(73));
            Assert.That(result.Field.Final.Entries.Single().Cell, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == chars[1].EntityId).Final.Health.CurrentHealth, Is.EqualTo(73));
            var completed = result.WithCompletion(new BattleCompletionChanges(new SoulActionChanges(result.Characters,
                result.Field, result.ConsumedSoulIds), result.Outcome,
                ChapterProgressionResult.FinishStage(1, new StageNodeId(2), new StageKey(10), true)));
            Assert.That(completed.Characters.Main.Final.CurrentHealth, Is.EqualTo(73));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MainArrivalCollection_PreservesGrownMaximumAndPersistentHealth(bool forced)
        {
            var input = forced ? ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1), CategoryKey.Cannon)
                : CombatCommandTestData.Input(pending: true, empty: true);
            var source = forced ? input.Characters[1] : input.Characters[0];
            var cell = forced ? new CellCoord(4, 3) : source.Anchor.Value;
            var main = new CharacterCombatFacts(source.EntityId, new CharacterInstanceId(1), EntityKey.Unit_MainCharacter,
                Owner.Player, CharacterOwnershipRole.Main, CharacterRuntimeArea.Field, source.Health, source.Stats,
                null, cell, Rotation4.R0, new[] { cell }, forced ? cell : source.MovementCheckpointAnchor,
                source.CommittedMoveDistance, source.PendingMoveActionCost);
            var chars = forced ? new[] { input.Characters[0], main } : new[] { main };
            var soul = new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1,
                new EffectOriginKey(EffectOwnerKind.Artifact, 0, 1, "soul"),
                forced ? new CellCoord(5, 3) : cell, 5, forced ? 30 : 1) });
            var prepared = new CombatCommandInput(1, CombatCommandKind.Attack, input.ActorId, input.Aim,
                input.State, chars, input.Profiles, new MainCharacterPersistentState(100, 100, main.Stats.AttackPower, 5, 4, 1),
                chars.Select(c => new OwnedCharacterRecord(c.OwnedId.Value, c.CharacterKey, c.OwnershipRole.Value)).ToArray(),
                chars.ToDictionary(c => c.OwnedId.Value, c => 0), soul, input.Artifacts);
            if (!forced) prepared = CombatCommandTestData.With(prepared,
                state: WithFields(prepared, new ThornFieldCellSnapshot(cell, 5, 2)));
            var result = forced ? CombatCommandCalculator.Calculate(prepared, System.Array.Empty<IMovementTileRule>()) : SouthArrival(prepared);
            var final = result.Characters.Characters.Single(c => c.Final.EntityId == main.EntityId).Final;
            Assert.That(final.Health.MaxHealth, Is.EqualTo(101));
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(forced ? 101 : 99));
            Assert.That(result.Characters.Main.Final.CurrentHealth, Is.EqualTo(forced ? 101 : 99));
            Assert.That(result.Characters.Main.Final.PersistentMaxHealth, Is.EqualTo(100));
            Assert.That(result.Characters.Souls.Single().FinalCount, Is.EqualTo(1));
            Assert.That(result.ConsumedSoulIds.Single(), Is.EqualTo(1));
        }

        [Test]
        public void Ballista_MultipleRaysAggregateStructureDamageAndClampHealthOnce()
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            var chars = new[] { CombatCommandTestData.With(input.Characters[0],
                category: CategorySnapshot.CreateStateless(CategoryKey.Cannon), traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) }),
                input.Characters[1], CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy) };
            var profiles = input.Profiles.ToArray(); profiles[0] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Cannon, new PiercingFalloffTraitSpec(30, 99)), method);
            var occupied = new[] { new CellCoord(4, 0), new CellCoord(4, 3), new CellCoord(4, 6) };
            var structure = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 7, new[] { new CellCoord(0, 0), new CellCoord(0, 3), new CellCoord(0, 6) }, 40, false, 0),
                new HealthStateSnapshot(40, 40), new CellCoord(4, 0), Rotation4.R0, occupied, Owner.Player);
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, false,
                new[] { structure }, input.State.ThornFields, input.State.Timeline);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, state, profiles), System.Array.Empty<IMovementTileRule>());
            var hit = result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == structure.EntityId);
            Assert.That(hit.DamageDealt, Is.EqualTo(40));
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.True);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.InProgress));
            Assert.That(result.Attack.Value.ActionCostSpent, Is.EqualTo(2));
            var destruction = result.Events.OfType<CombatStructureEvent>().Single();
            Assert.That(destruction.Expected.Anchor, Is.EqualTo(new CellCoord(4, 0)));
            Assert.That(destruction.DamageOrigins.Single().PhysicalCell, Is.EqualTo(new CellCoord(1, 3)));
            Assert.That(destruction.DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(input.ActorId));
        }

        [Test]
        public void Cannon_NewSplashDoesNotStackLegacySquare_AndIndirectNearVictimRemainsEligible()
        {
            var input = CombatCommandTestData.Input(aim: AttackAim.ForCell(new CellCoord(3, 3)), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[] { new ThornFieldTraitStatusSnapshot(5, 5) });
            var chars = new[] { actor, input.Characters[1], CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy) };
            var profiles = input.Profiles.ToArray(); profiles[0] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Cannon, new ThornFieldTraitSpec(5, 5)), profiles[0].Method);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, profiles: profiles), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.Value.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(19));
            Assert.That(result.FinalState.ThornFields.Count, Is.EqualTo(7));
        }

        [Test]
        public void Cannon_EmptyNearAimIsRejectedBeforeAnyEffects()
        {
            var input = CombatCommandTestData.Input(aim: AttackAim.ForCell(new CellCoord(2, 3)), empty: true);
            var chars = input.Characters.ToArray(); chars[0] = CombatCommandTestData.With(chars[0], category: CategorySnapshot.CreateStateless(CategoryKey.Cannon));
            var profiles = input.Profiles.ToArray(); profiles[0] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Cannon), profiles[0].Method);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, profiles: profiles), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.ThornFields, Is.Empty);
        }

        [Test]
        public void ThornApplication_ExcludesCasterAndClipsFieldsAtBoardEdge()
        {
            var input = ActorEffect(new ThornFieldTraitSpec(5, 5), new ThornFieldTraitStatusSnapshot(5, 5));
            var near = CombatAttackQueries.WithAim(CombatCommandTestData.With(input, characters: new[] {
                input.Characters[0], input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(2, 3), Owner.Enemy) }),
                AttackAim.ForCell(new CellCoord(2, 3)));
            var result = CombatCommandCalculator.Calculate(near, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Health.CurrentHealth, Is.EqualTo(100));
            var border = CombatAttackQueries.WithAim(CombatCommandTestData.With(input, characters: new[] {
                input.Characters[0], input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, CellCoord.Zero, Owner.Enemy) }),
                AttackAim.ForCell(CellCoord.Zero));
            Assert.That(CombatCommandCalculator.Calculate(border, System.Array.Empty<IMovementTileRule>()).FinalState.ThornFields.Count, Is.EqualTo(3));
        }

        [Test]
        public void UnresolvedReflectionLoadout_DoesNotActivateOnNewAttackRoute()
        {
            var input = CombatCommandTestData.Input(pending: true);
            var loadout = new ArtifactLoadoutSnapshot(0, new[] { new ArtifactPlacedEffectSnapshot(13, 1,
                ArtifactThornHeartRuleTests.HeartEffect()) });
            Assert.Throws<NotSupportedException>(() => CombatCommandCalculator.Calculate(
                CombatCommandTestData.With(input, artifacts: loadout), System.Array.Empty<IMovementTileRule>()));
            Assert.That(input.Characters[0].PendingMoveActionCost, Is.EqualTo(1));
        }

        [Test]
        public void Rider_RefundsPaidPendingMoveOnceAfterAttackCost()
        {
            var input = CombatCommandTestData.Input(pending: true);
            var chars = input.Characters.ToArray(); chars[0] = CombatCommandTestData.With(chars[0], category: CategorySnapshot.CreateRider(false));
            var profiles = input.Profiles.ToArray(); profiles[0] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Rider), profiles[0].Method);
            var arrived = SouthArrival(CombatCommandTestData.With(input, chars, profiles: profiles));
            var result = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(MovementTestData.After(arrived),
                CombatCommandKind.Attack, input.Aim), System.Array.Empty<IMovementTileRule>());
            var actor = result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final;
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(3));
            Assert.That(actor.Stats.Category.RefundUsedThisTurn, Is.True);
            Assert.That(result.Attack.Value.ActionCostSpent, Is.EqualTo(2));
            Assert.That(result.Attack.Value.ActionCostRefunded, Is.EqualTo(1));
            Assert.That(actor.CommittedMoveDistance, Is.EqualTo(1));
            Assert.That(actor.PendingMoveActionCost, Is.Zero);
        }

        [Test]
        public void HeavySplash_UsesSelectedCenterAndExistingMatchupThenReduction()
        {
            var input = CombatCommandTestData.Input();
            var chars = input.Characters.ToArray();
            chars[0] = CombatCommandTestData.With(chars[0], category: CategorySnapshot.CreateRider(false));
            chars[3] = CombatCommandTestData.With(chars[3], category: CategorySnapshot.CreateHeavy(Rotation4.R180, false, 50));
            var profiles = input.Profiles.ToArray(); profiles[1] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Demon, 1, CategoryKey.Heavy), profiles[1].Method);
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Rider), profiles[0].Method);
            // 같은 정의의 나머지 피해 대상도 같은 Category를 가진다.
            chars[2] = CombatCommandTestData.With(chars[2], category: CategorySnapshot.CreateHeavy(Rotation4.R0, false, 50));
            chars[4] = CombatCommandTestData.With(chars[4], category: CategorySnapshot.CreateHeavy(Rotation4.R0, false, 50));
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, profiles: profiles), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == new EntityId(3)).DamageDealt, Is.EqualTo(5));
        }

        [Test]
        public void LastStand_UsesOriginalRosterForEveryFixedDamageValue()
        {
            var input = CombatCommandTestData.Input();
            var chars = input.Characters.ToArray(); chars[2] = CombatCommandTestData.With(chars[2], hp: 1);
            var loadout = new ArtifactLoadoutSnapshot(0, new[] { new ArtifactPlacedEffectSnapshot(12, 4,
                ArtifactLastStandRuleTests.LastStandEffect()) });
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, artifacts: loadout), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(1));
            Assert.That(result.Attack.Value.Hits.Span[1].DamageDealt, Is.EqualTo(17));
            Assert.That(result.Attack.Value.Hits.Span[2].DamageDealt, Is.EqualTo(17));
            Assert.That(result.Events.OfType<CombatArtifactEvent>().Single().Activations.Single().AppliedAmount, Is.EqualTo(5));
        }

        [Test]
        public void NightForcedDeath_SpawnsAtDestinationAfterCollectionAndDoesNotCountDown()
        {
            var input = ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            var chars = input.Characters.ToArray(); chars[2] = CombatCommandTestData.With(chars[2], hp: 25);
            chars[4] = CombatCommandTestData.With(chars[4], hp: 1);
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, true,
                input.State.Structures, new[] { new ThornFieldCellSnapshot(new CellCoord(5, 3), 5, 7) }, input.State.Timeline);
            var loadout = new ArtifactLoadoutSnapshot(0, new[] { new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()) });
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, state, artifacts: loadout), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Field.Final.Entries.Count, Is.EqualTo(2));
            Assert.That(result.Field.Final.Entries.All(s => s.Cell == new CellCoord(5, 3)), Is.True);
            Assert.That(result.Field.Final.Entries.All(s => s.RemainingUnitTurns == 5), Is.True);
            Assert.That(result.FinalState.ThornFields, Is.Empty);
            var thornDeath = result.Events.OfType<CombatCharacterEvent>().Single(e =>
                e.Kind == CombatEventKind.Died && e.Final.EntityId == new EntityId(2));
            Assert.That(thornDeath.DamageOrigins.Single().PhysicalCell, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(thornDeath.DamageOrigins.Single().CreditedCharacterId, Is.Null);
        }

        [Test]
        public void LastStandSoulAndTemporaryBonus_ComposeOnce_AndNextAttackUsesRemainingRoster()
        {
            var source = CombatCommandTestData.Input();
            var chars = source.Characters.ToArray();
            var actor = chars[0]; var stats = actor.Stats;
            chars[0] = new CharacterCombatFacts(actor.EntityId, actor.OwnedId, actor.CharacterKey, actor.Owner,
                actor.OwnershipRole, actor.Area, new HealthStateSnapshot(100, 102),
                new UnitStatsSnapshot(stats.MoveMethod, stats.MoveRange, stats.MoveCost, stats.AppointRange, stats.AttackPower, stats.PhysicalDefense, stats.MaxActionCost,
                    stats.AttackCost, stats.RemainingMoveRange, stats.CurrentActionCost, stats.Priority,
                    stats.Delay, stats.TraitStatuses.ToArray(), stats.Category, 5, 25, 2, 2),
                null, actor.Anchor, actor.Rotation, actor.OccupiedCells, actor.MovementCheckpointAnchor,
                0, 0, temporaryAttackBonus: 3);
            chars[2] = CombatCommandTestData.With(chars[2], hp: 1);
            var counts = source.SoulCounts.ToDictionary(p => p.Key, p => p.Value);
            counts[actor.OwnedId.Value] = 2;
            var loadout = new ArtifactLoadoutSnapshot(0, new[]
                { new ArtifactPlacedEffectSnapshot(12, 4, ArtifactLastStandRuleTests.LastStandEffect()) });
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim,
                source.State, chars, source.Profiles, source.Main, source.Owned, counts, source.Souls, loadout);
            var preview = CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            var first = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(first.Accepted, Is.True, first.Reason);
            Assert.That(first.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt),
                Is.EqualTo(new[] { 1, 21, 21 }));
            Assert.That(preview.Targets.ToArray().Select(h => h.ExpectedDamage),
                Is.EqualTo(new[] { 30, 21, 21 }));
            Assert.That(first.Events.OfType<CombatArtifactEvent>().Single().Activations.Single().AppliedAmount, Is.EqualTo(5));
            var projected = CombatCommandComposition.Project(first);
            // 첫 주 표적은 죽었으므로 살아 있는 적을 재지정한다. 재지정한 적은 주 표적 피해를 받는다.
            var second = CombatCommandCalculator.Calculate(CombatAttackQueries.WithAim(projected,
                AttackAim.ForCell(new CellCoord(4, 4))), System.Array.Empty<IMovementTileRule>());
            Assert.That(second.Accepted, Is.True, second.Reason);
            Assert.That(second.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(new[] { 25, 17 }));
            Assert.That(second.Events.OfType<CombatArtifactEvent>(), Is.Empty);
            var final = second.Characters.Characters.Single(c => c.Final.EntityId == actor.EntityId).Final;
            Assert.That(final.Stats.AttackPower, Is.EqualTo(20));
            Assert.That(final.Stats.SoulAttackBonus, Is.EqualTo(2));
            Assert.That(final.TemporaryAttackBonus, Is.EqualTo(3));
            Assert.That(final.Stats.CurrentActionCost, Is.EqualTo(1));
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(input.SoulCounts[actor.OwnedId.Value], Is.EqualTo(2));
        }

        [Test]
        public void ForcedArrivalCollection_KeepsAttackDamageSeparateFromHealing()
        {
            var input = ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1), CategoryKey.Cannon);
            var friendly = CombatCommandTestData.Input(friendly: true, category: CategoryKey.Cannon);
            input = new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, input.State,
                friendly.Characters, input.Profiles, friendly.Main, friendly.Owned, friendly.SoulCounts,
                new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1, new EffectOriginKey(EffectOwnerKind.Artifact, 0, 1, "soul"),
                    new CellCoord(4, 3), 3, 70) }), input.Artifacts);
            var chars = input.Characters.ToArray();
            chars[0] = CombatCommandTestData.With(chars[0], traits: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(1) });
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars), System.Array.Empty<IMovementTileRule>());
            var victim = result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(2)).Final;
            Assert.That(victim.Health.CurrentHealth, Is.EqualTo(101));
            Assert.That(victim.Stats.SoulAttackBonus, Is.EqualTo(1));
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == victim.EntityId).DamageDealt, Is.EqualTo(14));
            Assert.That(result.ConsumedSoulIds.Single(), Is.EqualTo(1));
        }

        [TestCase(Owner.Enemy, StageOutcome.Victory)]
        [TestCase(Owner.Player, StageOutcome.InProgress)]
        public void FinalStructureDamage_ResolvesProjectedEnemyHeadquartersOnly(Owner owner, StageOutcome expected)
        {
            var input = CombatCommandTestData.Input(empty: true, category: owner == Owner.Player ? CategoryKey.Cannon : CategoryKey.Base);
            var hq = new StructureCombatFacts(new EntityId(20),
                new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 10, false, 0),
                new HealthStateSnapshot(10, 10), new CellCoord(4, 3), Rotation4.R0, new[] { new CellCoord(4, 3) }, owner);
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, false,
                new[] { hq }, input.State.ThornFields, input.State.Timeline);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, state: state), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.FinalState.Structures.Single().Health.IsDead, Is.True);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(expected));
        }

        [Test]
        public void MainKilledByFriendlyAttack_ProducesDefeatAndPersistentHealthZero()
        {
            var input = CombatCommandTestData.Input(empty: true, category: CategoryKey.Cannon);
            var chars = input.Characters.ToArray();
            chars[1] = CombatCommandTestData.With(chars[1], hp: 1, position: new CellCoord(4, 3));
            var main = new MainCharacterPersistentState(1, 100, 1, 5, 4, 1);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, main: main), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(result.Characters.Main.Final.CurrentHealth, Is.Zero);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(10)).Final.Area,
                Is.EqualTo(CharacterRuntimeArea.Excluded));
        }

        [Test]
        public void Focus_AddsToCommonDamageBeforeSplash()
        {
            var input = ActorEffect(new FocusTraitSpec(5, 3), new FocusTraitStatusSnapshot(2, false, false));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(30));
            Assert.That(result.Attack.Value.Hits.Span[1].DamageDealt, Is.EqualTo(21));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId)
                .Final.Stats.TraitStatuses.ToArray().OfType<FocusTraitStatusSnapshot>().Single().GainScheduled, Is.True);
        }

        [Test]
        public void Discount_ChargesOnceAndConsumesOnlyAfterAcceptedAttack()
        {
            var input = ActorEffect(new MoveAttackDiscountTraitSpec(1, 1), new MoveAttackDiscountTraitStatusSnapshot(true, true));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            var actor = result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final;
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(4));
            Assert.That(actor.Stats.TraitStatuses.ToArray().OfType<MoveAttackDiscountTraitStatusSnapshot>().Single().DiscountArmed, Is.False);
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(5));
        }

        [Test]
        public void LethalRoute_AcceptsMovementWithoutAttackCostOrCountdown()
        {
            var input = CombatCommandTestData.Input(pending: true);
            var state = WithFields(input, new ThornFieldCellSnapshot(new CellCoord(1, 3), 5, 200),
                new ThornFieldCellSnapshot(new CellCoord(0, 0), 5, 2));
            var result = SouthArrival(CombatCommandTestData.With(input, state: state));
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Attack, Is.Null);
            Assert.That(result.PathMovement.StopReason, Is.EqualTo(MovementStopReason.Dead));
            var actor = result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final;
            Assert.That(actor.Health.IsDead, Is.True);
            Assert.That(actor.Area, Is.EqualTo(CharacterRuntimeArea.Excluded));
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(4));
            Assert.That(actor.PendingMoveActionCost, Is.Zero);
            Assert.That(actor.CommittedMoveDistance, Is.EqualTo(1));
            Assert.That(result.FinalState.ActiveActorId.IsEmpty, Is.True);
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Automatic));
            Assert.That(result.FinalState.Timeline, Is.Empty);
            Assert.That(result.FinalState.ThornFields.Single().RemainingUnitTurns, Is.EqualTo(5));
            var rejected = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(MovementTestData.After(result),
                CombatCommandKind.Attack, input.Aim), System.Array.Empty<IMovementTileRule>());
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Characters.Characters, Is.Empty);
        }

        [Test]
        public void InsufficientAp_RejectsBeforeLethalTraversal()
        {
            var input = CombatCommandTestData.Input(pending: true);
            var chars = input.Characters.ToArray(); chars[0] = CombatCommandTestData.With(chars[0], ap: 1);
            var state = WithFields(input, new ThornFieldCellSnapshot(new CellCoord(1, 3), 5, 200));
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, state), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Characters.Characters, Is.Empty);
            Assert.That(result.FinalState.ThornFields.Count, Is.EqualTo(1));
        }

        [Test]
        public void SurvivingArrivalSoul_ChangesDamageAndOwnedGrowthWithoutChargingMoveAgain()
        {
            var input = CombatCommandTestData.Input(pending: true);
            var souls = new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1,
                new EffectOriginKey(EffectOwnerKind.Artifact, 0, 1, "soul"), new CellCoord(1, 3), 3, 0) });
            var moved = SouthArrival(CombatCommandTestData.With(input, souls: souls));
            var result = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(MovementTestData.After(moved),
                CombatCommandKind.Attack, input.Aim), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(21));
            Assert.That(result.Attack.Value.Hits.Span[1].DamageDealt, Is.EqualTo(14));
            Assert.That(moved.Characters.Souls.Single().FinalCount, Is.EqualTo(1));
            Assert.That(moved.ConsumedSoulIds.Single(), Is.EqualTo(1));
            Assert.That(result.ConsumedSoulIds, Is.Empty);
            var actor = result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final;
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(2));
            Assert.That(actor.Health.MaxHealth, Is.EqualTo(101));
            Assert.That(actor.Stats.SoulAttackBonus, Is.EqualTo(1));
        }

        [Test]
        public void ThornApplication_UsesCapturedOccupancyAndDoesNotBackfillCorpses()
        {
            var input = ActorEffect(new ThornFieldTraitSpec(5, 5), new ThornFieldTraitStatusSnapshot(5, 5));
            var chars = input.Characters.ToArray();
            chars[3] = CombatCommandTestData.With(chars[3], hp: 1);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(2)).Final.Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(3)).Final.Health.IsDead, Is.True);
            Assert.That(result.FinalState.ThornFields.Count, Is.EqualTo(6));
            Assert.That(result.FinalState.ThornFields.Any(f => f.Cell == new CellCoord(4, 4)), Is.False);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(4)).Final.Health.CurrentHealth, Is.EqualTo(81));
            var thornDamage = result.Events.OfType<CombatCharacterEvent>().Single(e =>
                e.Final.EntityId == new EntityId(4) && e.Expected.Health.CurrentHealth - e.Final.Health.CurrentHealth == 5);
            Assert.That(thornDamage.DamageOrigins.Single().PhysicalCell, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(thornDamage.DamageOrigins.Single().CreditedCharacterId, Is.EqualTo(input.ActorId));
        }

        [Test]
        public void Knockback_ReleasesAllOrdinaryCasualtiesBeforeDestinationsAndConsumesForcedFields()
        {
            var input = ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            var chars = input.Characters.ToArray(); chars[4] = CombatCommandTestData.With(chars[4], hp: 1);
            var state = WithFields(input, new ThornFieldCellSnapshot(new CellCoord(5, 3), 5, 7));
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, state), System.Array.Empty<IMovementTileRule>());
            var victim = result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(2)).Final;
            Assert.That(victim.Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(victim.Health.CurrentHealth, Is.EqualTo(73));
            Assert.That(result.FinalState.ThornFields, Is.Empty);
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(27));
        }

        [Test]
        public void Ballista_PerRayFalloffPreservesEncounterOrder()
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine, TargetingChoiceMode.Direction, TargetingSplash.None, 0);
            var input = CombatCommandTestData.Input(method, AttackAim.ForDirection(AttackDirection.Right), empty: true);
            var actor = CombatCommandTestData.With(input.Characters[0], category: CategorySnapshot.CreateStateless(CategoryKey.Cannon),
                traits: new TraitStatusSnapshot[] { new PiercingFalloffTraitStatusSnapshot(30) });
            var chars = new[] { actor, input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy),
                CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy),
                CombatCommandTestData.Character(4, EntityKey.Unit_Demon, new CellCoord(5, 3), Owner.Enemy) };
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20,
                CategoryKey.Cannon, new PiercingFalloffTraitSpec(30, 99)), method);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars, profiles: profiles), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt), Is.EqualTo(new[] { 20, 14, 8 }));
        }

        [Test]
        public void FocusAndDiscount_FollowTurnMoveAttackLifecycle_WithoutPreviewConsumption()
        {
            var input = CombatCommonCommandTests.Command(CombatCommandKind.Attack, ActorEffect(
                new FocusTraitSpec(5, 3), new FocusTraitStatusSnapshot(0, false, false)));
            var actor = CombatCommandTestData.With(input.Characters[0], traits: new TraitStatusSnapshot[] {
                new FocusTraitStatusSnapshot(0, false, false), new MoveAttackDiscountTraitStatusSnapshot(false, false) });
            var profiles = input.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20,
                CategoryKey.Base, new FocusTraitSpec(5, 3), new MoveAttackDiscountTraitSpec(1, 1)), profiles[0].Method);
            input = CombatCommandTestData.With(input, input.Characters.Select(c => c.EntityId == actor.EntityId ? actor : c).ToArray(), profiles: profiles);
            input = CombatCommonCommandTests.Command(CombatCommandKind.Attack, input);
            input = CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(4, 3)));
            var first = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            var second = CombatCommandCalculator.Calculate(CombatCommandComposition.Project(first), System.Array.Empty<IMovementTileRule>());
            Assert.That(first.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(20));
            Assert.That(second.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(20));
            Assert.That(first.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Count(t => t.TraitActivationKind == TraitActivationKind.FocusGainScheduled), Is.EqualTo(1));
            Assert.That(second.Events.OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Count(t => t.TraitActivationKind == TraitActivationKind.FocusGainScheduled), Is.Zero);
            var end = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(CombatCommandComposition.Project(second), CombatCommandKind.EndUnitTurn), System.Array.Empty<IMovementTileRule>());
            var next = BeginAfter(end);
            Assert.That(next.Characters[0].Stats.TraitStatuses.ToArray().OfType<FocusTraitStatusSnapshot>().Single().ActiveStacks, Is.EqualTo(1));
            var beforeMove = CombatCommandCalculator.Preview(CombatCommandComposition.Intent(next, CombatCommandKind.Attack, input.Aim), System.Array.Empty<IMovementTileRule>());
            Assert.That(beforeMove.Targets.Span[0].ExpectedDamage, Is.EqualTo(25));
            var move = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(next, CombatCommandKind.Move,
                path: new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3) })), System.Array.Empty<IMovementTileRule>());
            var afterMove = CombatCommandComposition.Project(move);
            var attack = CombatCommandComposition.Intent(afterMove, CombatCommandKind.Attack, input.Aim);
            var preview = CombatCommandCalculator.Preview(attack, System.Array.Empty<IMovementTileRule>());
            Assert.That(preview.AttackCost, Is.EqualTo(1));
            Assert.That(preview.Targets.Span[0].ExpectedDamage, Is.EqualTo(20));
            Assert.That(afterMove.Characters[0].Stats.TraitStatuses.ToArray().OfType<FocusTraitStatusSnapshot>().Single().ActiveStacks, Is.Zero);
            Assert.That(afterMove.Characters[0].Stats.TraitStatuses.ToArray().OfType<MoveAttackDiscountTraitStatusSnapshot>().Single().DiscountArmed, Is.True);
            var hit = CombatCommandCalculator.Calculate(attack, System.Array.Empty<IMovementTileRule>());
            var final = hit.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final;
            Assert.That(final.Stats.TraitStatuses.ToArray().OfType<MoveAttackDiscountTraitStatusSnapshot>().Single().DiscountArmed, Is.False);
            Assert.That(final.Stats.TraitStatuses.ToArray().OfType<FocusTraitStatusSnapshot>().Single().GainScheduled, Is.False);
            Assert.That(actor.Stats.TraitStatuses.ToArray().OfType<FocusTraitStatusSnapshot>().Single().ActiveStacks, Is.Zero);
        }

        [Test]
        public void Rider_SecondAttackDoesNotRefund_AndNextTurnResetsRefundState()
        {
            var input = CombatCommandTestData.Input(pending: true);
            var actor = CombatCommandTestData.With(input.Characters[0], category: CategorySnapshot.CreateRider(false));
            var s = actor.Stats;
            actor = new CharacterCombatFacts(actor.EntityId, actor.OwnedId, actor.CharacterKey, actor.Owner, actor.OwnershipRole,
                actor.Area, actor.Health, new UnitStatsSnapshot(s.MoveMethod, s.MoveRange, s.MoveCost, s.AppointRange, s.AttackPower, s.PhysicalDefense, s.MaxActionCost, s.AttackCost, 3, s.CurrentActionCost,
                    s.Priority, s.Delay, s.TraitStatuses.ToArray(), s.Category), null, actor.Anchor, actor.Rotation,
                actor.OccupiedCells, actor.MovementCheckpointAnchor, 0, 1, pendingMoveDistance: 1, attackRefundEligibleMoveCost: 1);
            var profiles = input.Profiles.ToArray(); profiles[0] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Rider), profiles[0].Method);
            input = CombatCommandTestData.With(input, new[] { actor, input.Characters[1], input.Characters[2] }, profiles: profiles);
            input = CombatCommonCommandTests.Command(CombatCommandKind.Attack, input);
            input = CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(4, 3)));
            input = CombatCommandComposition.Intent(MovementTestData.After(SouthArrival(input)), CombatCommandKind.Attack, input.Aim);
            var first = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            var move = CombatCommandCalculator.Calculate(MovementTestData.Route(MovementTestData.After(first),
                new CellCoord(1, 3), new CellCoord(2, 3)), System.Array.Empty<IMovementTileRule>());
            Assert.That(move.Accepted, Is.True, move.Reason);
            var attack = CombatCommandComposition.Intent(CombatCommandComposition.Project(move), CombatCommandKind.Attack, input.Aim);
            Assert.That(CombatCommandCalculator.Preview(attack, System.Array.Empty<IMovementTileRule>()).ActionCostRefunded, Is.Zero);
            var second = CombatCommandCalculator.Calculate(attack, System.Array.Empty<IMovementTileRule>());
            Assert.That(first.Attack.Value.ActionCostRefunded, Is.EqualTo(1));
            Assert.That(second.Attack.Value.ActionCostRefunded, Is.Zero);
            Assert.That(second.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Stats.CurrentActionCost, Is.Zero);
            var ended = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(CombatCommandComposition.Project(second), CombatCommandKind.EndUnitTurn), System.Array.Empty<IMovementTileRule>());
            Assert.That(BeginAfter(ended).Characters[0].Stats.Category.RefundUsedThisTurn, Is.False);
        }

        [Test]
        public void Heavy_MoveUsesActualDirection_WhilePreviewPreservesFacing()
        {
            var input = CombatCommonCommandTests.Move(new CellCoord(1, 3), new CellCoord(1, 4), new CellCoord(2, 4));
            var chars = input.Characters.ToArray(); chars[0] = CombatCommandTestData.With(chars[0], category: CategorySnapshot.CreateHeavy(Rotation4.R0, false, 50));
            var profiles = input.Profiles.ToArray(); profiles[0] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Heavy), profiles[0].Method);
            input = CombatCommandTestData.With(input, chars, profiles: profiles);
            var preview = CombatMovementCalculator.Preview(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(preview.ProjectedCategory.DefenseFacing, Is.EqualTo(Rotation4.R90));
            var move = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(move.Characters.Characters.Single().Final.Stats.Category.DefenseFacing, Is.EqualTo(Rotation4.R90));
            Assert.That(move.Characters.Characters.Single().Final.Stats.Category.MovedThisTurn, Is.True);
            Assert.That(chars[0].Stats.Category.DefenseFacing, Is.EqualTo(Rotation4.R0));
            Assert.That(chars[0].Stats.Category.MovedThisTurn, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Knockback_BlockedDestinationPreservesPositionResourcesAndStoredField(bool boardEdge)
        {
            var input = ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            var targetCell = boardEdge ? new CellCoord(7, 3) : new CellCoord(4, 3);
            var chars = new[] { input.Characters[0], input.Characters[1],
                CombatCommandTestData.With(input.Characters[2], position: targetCell) };
            chars[2] = new CharacterCombatFacts(chars[2].EntityId, chars[2].OwnedId, chars[2].CharacterKey, chars[2].Owner,
                chars[2].OwnershipRole, chars[2].Area, chars[2].Health, chars[2].Stats, null, targetCell, chars[2].Rotation,
                new[] { targetCell }, targetCell, 0, 0);
            if (boardEdge) chars[0] = CombatCommandTestData.With(chars[0], position: new CellCoord(4, 3));
            else chars = chars.Concat(new[] { CombatCommandTestData.Character(3, EntityKey.Unit_Demon,
                new CellCoord(5, 3), Owner.Enemy) }).ToArray();
            var state = WithFields(input, new ThornFieldCellSnapshot(new CellCoord(5, 3), 5, 7));
            input = CombatAttackQueries.WithAim(CombatCommandTestData.With(input, chars, state), AttackAim.ForCell(targetCell));
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            var target = result.Characters.Characters.Single(c => c.Final.EntityId.Value == 2).Final;
            Assert.That(target.Anchor, Is.EqualTo(targetCell));
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(target.Stats, Is.EqualTo(chars[2].Stats));
            Assert.That(result.FinalState.ThornFields.Single().ThornDamage, Is.EqualTo(7));
        }

        [Test]
        public void ExtremeThornDamage_StopsBeforeSuffixWithoutChangingSourceFacts()
        {
            var input = CombatCommandTestData.Input(empty: true);
            var actor = input.Characters[0];
            var state = WithFields(input, new ThornFieldCellSnapshot(new CellCoord(2, 3), 5, int.MaxValue),
                new ThornFieldCellSnapshot(new CellCoord(3, 3), 5, 1));
            input = CombatCommandTestData.With(input, new[] { actor, input.Characters[1] }, state);
            input = MovementTestData.Route(input, new CellCoord(1, 3), new CellCoord(2, 3), new CellCoord(3, 3));
            var rules = new IMovementTileRule[] { new ThornMovementTileRule() };
            Assert.That(CombatMovementCalculator.Preview(input, rules).PathMovement.StopReason, Is.EqualTo(MovementStopReason.Dead));
            var result = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(1));
            Assert.That(result.PathMovement.ActionCostSpent, Is.EqualTo(1));
            Assert.That(result.FinalState.ThornFields.Single().Cell, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(actor.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(actor.PendingMoveActionCost, Is.Zero);
            Assert.That(actor.Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(state.ThornFields.Count, Is.EqualTo(2));
        }

        [Test]
        public void ForcedOrdinaryPlusThornOverflow_ThrowsBeforeAnyInputChanges()
        {
            var input = ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            input = CombatCommandTestData.With(input, new[] { input.Characters[0], input.Characters[1], input.Characters[2] },
                WithFields(input, new ThornFieldCellSnapshot(new CellCoord(5, 3), 5, int.MaxValue)));
            Assert.Throws<OverflowException>(() => CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()));
            Assert.That(input.Characters[2].Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(input.Characters[2].Anchor, Is.EqualTo(new CellCoord(4, 3)));
            Assert.That(input.Characters[0].Stats.CurrentActionCost, Is.EqualTo(5));
            Assert.That(input.State.ThornFields.Single().ThornDamage, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void ThornLifetime_ExpiresOnSuccessfulTurnEnds_AndRejectionKeepsLifetime()
        {
            var input = CombatCommonCommandTests.Command(CombatCommandKind.EndUnitTurn);
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, true,
                input.State.Structures, new[] { new ThornFieldCellSnapshot(new CellCoord(0, 0), 2, 7) },
                input.State.Timeline, input.State.Turn);
            input = new CombatCommandInput(input.Number, input.Kind, input.ActorId, default, state, input.Characters,
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                battle: new CombatBattleFacts(10, 100, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0, 11, 10, 2));
            var first = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            Assert.That(first.FinalState.ThornFields.Single().RemainingUnitTurns, Is.EqualTo(1));
            var next = BeginAfter(first);
            var rejected = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(next, CombatCommandKind.Attack,
                AttackAim.ForCell(new CellCoord(99, 99))), System.Array.Empty<IMovementTileRule>());
            Assert.That(rejected.FinalState.ThornFields.Single().RemainingUnitTurns, Is.EqualTo(1));
            var end = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(next, CombatCommandKind.EndUnitTurn), System.Array.Empty<IMovementTileRule>());
            Assert.That(end.FinalState.ThornFields, Is.Empty);
            Assert.That(state.ThornFields.Single().RemainingUnitTurns, Is.EqualTo(2));
        }

        [Test]
        public void ThornRefresh_PreservesStoredDamageAndLongerLifetime()
        {
            var input = ActorEffect(new ThornFieldTraitSpec(5, 5), new ThornFieldTraitStatusSnapshot(5, 5));
            var state = WithFields(input, new ThornFieldCellSnapshot(new CellCoord(3, 3), 2, 9),
                new ThornFieldCellSnapshot(new CellCoord(4, 4), 7, 11));
            input = CombatCommandTestData.With(input, new[] { input.Characters[0], input.Characters[1], input.Characters[2] }, state);
            var result = CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>());
            var refreshed = result.FinalState.ThornFields.Single(f => f.Cell == new CellCoord(3, 3));
            var longer = result.FinalState.ThornFields.Single(f => f.Cell == new CellCoord(4, 4));
            Assert.That(refreshed.RemainingUnitTurns, Is.EqualTo(5));
            Assert.That(refreshed.ThornDamage, Is.EqualTo(9));
            Assert.That(longer.RemainingUnitTurns, Is.EqualTo(7));
            Assert.That(longer.ThornDamage, Is.EqualTo(11));
            Assert.That(state.ThornFields[0].RemainingUnitTurns, Is.EqualTo(2));
        }

        [Test]
        public void ThornApplication_StructuresReceiveOnlyOrdinaryDamage_AndBlockFieldCreation()
        {
            var input = ActorEffect(new ThornFieldTraitSpec(5, 5), new ThornFieldTraitStatusSnapshot(5, 5));
            var cells = new[] { new CellCoord(4, 3), new CellCoord(4, 4) };
            var structure = new StructureCombatFacts(new EntityId(20), new StructureSpec(EntityKey.Structure_Headquarters,
                1, 2, new[] { CellCoord.Zero, CellCoord.Up }, 50, false, 0), new HealthStateSnapshot(50, 50),
                cells[0], Rotation4.R0, cells, Owner.Enemy);
            var state = new CombatCommandState(8, 8, input.State.Phase, input.ActorId, false,
                new[] { structure }, input.State.ThornFields, input.State.Timeline);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input,
                new[] { input.Characters[0], input.Characters[1] }, state), System.Array.Empty<IMovementTileRule>());
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(20));
            Assert.That(result.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(30));
            Assert.That(result.FinalState.ThornFields.Count, Is.EqualTo(7));
            Assert.That(result.FinalState.ThornFields.Any(f => cells.Contains(f.Cell)), Is.False);
        }

        [Test]
        public void PendingDistanceOverflow_ThrowsWithoutChangingFacts()
        {
            var input = CombatCommandTestData.Input(empty: true, pending: true);
            var actor = input.Characters[0];
            actor = new CharacterCombatFacts(actor.EntityId, actor.OwnedId, actor.CharacterKey, actor.Owner, actor.OwnershipRole,
                actor.Area, actor.Health, actor.Stats, null, actor.Anchor, actor.Rotation, actor.OccupiedCells,
                actor.MovementCheckpointAnchor, int.MaxValue, actor.PendingMoveActionCost,
                pendingMoveDistance: 1, attackRefundEligibleMoveCost: 1);
            input = CombatCommandTestData.With(input, new[] { actor, input.Characters[1],
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy) });
            Assert.Throws<OverflowException>(() => CombatCommandCalculator.Preview(input, System.Array.Empty<IMovementTileRule>()));
            Assert.Throws<OverflowException>(() => CombatCommandCalculator.Calculate(input, System.Array.Empty<IMovementTileRule>()));
            Assert.That(actor.CommittedMoveDistance, Is.EqualTo(int.MaxValue));
            Assert.That(actor.PendingMoveActionCost, Is.EqualTo(1));
            Assert.That(actor.Health.CurrentHealth, Is.EqualTo(100));
        }

        [Test]
        public void SuccessiveRoutes_ConsumeOnlyEnteredThorns_BeforeAttack()
        {
            var input = CombatCommonCommandTests.Move(new CellCoord(1, 3), new CellCoord(1, 4), new CellCoord(2, 4));
            input = CombatCommandTestData.With(input, characters: input.Characters.Concat(new[] {
                CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 4), Owner.Enemy) }).ToArray(), state: WithFields(input,
                new ThornFieldCellSnapshot(new CellCoord(2, 3), 5, 9),
                new ThornFieldCellSnapshot(new CellCoord(2, 4), 5, 7),
                new ThornFieldCellSnapshot(new CellCoord(3, 4), 5, 5)));
            var rules = new IMovementTileRule[] { new ThornMovementTileRule() };
            var first = CombatCommandCalculator.Calculate(MovementTestData.Route(input,
                new CellCoord(1, 3), new CellCoord(1, 4), new CellCoord(2, 4)), rules);
            var retarget = CombatCommandCalculator.Calculate(MovementTestData.Route(MovementTestData.After(first),
                new CellCoord(2, 4), new CellCoord(3, 4)), rules);
            var attack = CombatCommandComposition.Intent(CombatCommandComposition.Project(retarget), CombatCommandKind.Attack,
                AttackAim.ForCell(new CellCoord(4, 4)));
            Assert.That(CombatCommandCalculator.Preview(attack, System.Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            Assert.That(attack.State.ThornFields.Count, Is.EqualTo(1));
            var result = CombatCommandCalculator.Calculate(attack, System.Array.Empty<IMovementTileRule>());
            var final = result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final;
            Assert.That(final.Health.CurrentHealth, Is.EqualTo(88));
            Assert.That(final.Stats.CurrentActionCost, Is.Zero);
            Assert.That(final.CommittedMoveDistance, Is.EqualTo(3));
            Assert.That(final.PendingMoveActionCost, Is.Zero);
            Assert.That(result.FinalState.ThornFields.Single().Cell, Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(result.FinalState.ThornFields.Single().ThornDamage, Is.EqualTo(9));
            Assert.That(input.Characters[0].Health.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(0, 5, 95, 25)]
        [TestCase(25, 5, 95, 25)]
        [TestCase(0, 120, 0, 120)]
        public void Knockback_ReconcilesPositionModifiers_WithoutCountingHpCapAsDamage(int destinationBonus,
            int thornDamage, int expectedHp, int expectedDamage)
        {
            var input = ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            var victim = CombatCommandTestData.Character(2, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy);
            var tiles = new MovementEnvironmentFacts(new MovementTileFact[] {
                new DestinationTileModifierFact(7, victim.Anchor.Value, true, new[] { Owner.Player, Owner.Enemy }, 5, 50),
                new DestinationTileModifierFact(8, new CellCoord(5, 3), destinationBonus != 0,
                    new[] { Owner.Player, Owner.Enemy }, 2, destinationBonus) });
            victim = PositionTileModifierCalculator.Replace(victim,
                PositionTileModifierCalculator.ResolveAt(victim.Anchor.Value, tiles));
            victim = CombatCommandTestData.With(victim, hp: 140);
            var s = input.State;
            input = CombatCommandTestData.With(input, characters: new[] { input.Characters[0], input.Characters[1], victim },
                state: new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                    s.Structures, new[] { new ThornFieldCellSnapshot(new CellCoord(5, 3), 5, thornDamage) }, s.Timeline, s.Turn, tiles));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var final = result.Characters.Characters.Single(c => c.Final.EntityId == victim.EntityId).Final;
            Assert.That(final.Anchor, Is.EqualTo(expectedHp == 0 ? (CellCoord?)null : new CellCoord(5, 3)));
            Assert.That(final.Health, Is.EqualTo(new HealthStateSnapshot(expectedHp, 100 + destinationBonus)));
            Assert.That(final.Stats.PositionAttackBonus, Is.EqualTo(destinationBonus == 0 ? 0 : 2));
            Assert.That(final.PositionModifier.SourceIds, Is.EqualTo(destinationBonus == 0 ? Array.Empty<long>() : new long[] { 8 }));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(expectedDamage));
            Assert.That(result.Attack.Value.Hits.Span[0].TargetKilled, Is.EqualTo(expectedHp == 0));
            var healthEvents = result.Events.OfType<CombatCharacterEvent>()
                .Where(e => e.Kind == CombatEventKind.HealthChanged || e.Kind == CombatEventKind.Died).ToArray();
            Assert.That(healthEvents.Sum(e => e.Expected.Health.CurrentHealth - e.Final.Health.CurrentHealth), Is.EqualTo(expectedDamage));
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.Span[0].ExpectedDamage, Is.EqualTo(20 + thornDamage));
            Assert.That(preview.Targets.Span[0].TargetHealthAfter, Is.EqualTo(expectedHp));
        }

        [Test]
        public void KnockbackPreview_DoesNotResolveHiddenDestinationSources()
        {
            var input = ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1));
            var victim = input.Characters.Single(c => c.EntityId == new EntityId(2));
            var s = input.State;
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] {
                new DestinationTileModifierFact(7, new CellCoord(5, 3), true, new[] { Owner.Enemy }, 1, 1),
                new DestinationTileModifierFact(8, new CellCoord(5, 3), true, new[] { Owner.Enemy }, 1, 1) });
            input = CombatCommandTestData.With(input, characters: new[] { input.Characters[0], input.Characters[1], victim },
                state: new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, s.ActiveActorId, s.IsNight,
                    s.Structures, s.ThornFields, s.Timeline, s.Turn, environment));
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
            Assert.Throws<NotSupportedException>(() => CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()));
        }

        private static CombatCommandTransition SouthArrival(CombatCommandInput input)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var stop = actor.Anchor.Value;
            var origin = new CellCoord(stop.X, stop.Y - 1);
            var departed = CombatMovementCalculator.CopyMovement(actor,
                CombatCommandEffects.With(actor.Stats, remainingMoveRange: actor.Stats.MoveRange), origin, 0, 0, 0, 0, origin);
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == actor.EntityId ? departed : c).ToArray());
            return CombatCommandCalculator.Calculate(MovementTestData.Route(input, origin, stop),
                new IMovementTileRule[] { new ThornMovementTileRule() });
        }

        private static CombatCommandInput BeginAfter(CombatCommandTransition ended)
        {
            var input = CombatCommandComposition.Project(ended);
            var automatic = new CombatCommandInput(0, CombatCommandKind.Attack, EntityId.Empty, default,
                input.State, input.Characters, input.Profiles, input.Main, input.Owned, input.SoulCounts,
                input.Souls, input.Artifacts, battle: input.Battle, processingKind: CombatProcessingKind.Advance);
            var begun = CombatCommandCalculator.Advance(automatic, System.Array.Empty<IMovementTileRule>());
            var current = CombatCommandComposition.Project(begun);
            return new CombatCommandInput(1, CombatCommandKind.Attack, begun.FinalState.ActiveActorId, default,
                current.State, current.Characters, current.Profiles, current.Main, current.Owned, current.SoulCounts,
                current.Souls, current.Artifacts, battle: current.Battle);
        }

        internal static CombatCommandInput ActorEffect(TraitSpec spec, TraitStatusSnapshot status, CategoryKey category = CategoryKey.Base)
        {
            var input = CombatCommandTestData.Input(category: category);
            var chars = input.Characters.ToArray(); chars[0] = CombatCommandTestData.With(chars[0], traits: new[] { status });
            var profiles = input.Profiles.ToArray(); profiles[0] = new CombatAttackProfile(
                CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, category, spec), profiles[0].Method);
            return CombatCommandTestData.With(input, chars, profiles: profiles);
        }

        internal static CombatCommandState WithFields(CombatCommandInput input, params ThornFieldCellSnapshot[] fields) =>
            new(input.State.BoardWidth, input.State.BoardHeight, input.State.Phase, input.State.ActiveActorId,
                input.State.IsNight, input.State.Structures, fields, input.State.Timeline);
    }
}
