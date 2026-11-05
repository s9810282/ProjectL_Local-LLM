using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class IntimidationStatePreservationTests
    {
        private static CharacterDebuffState Active => new CharacterDebuffState(new IntimidationDebuffState(20));

        [Test]
        public void WithDebuffs_ReplacesOnlyDebuffs_AndRejectsNull()
        {
            var source = CombatCommandTestData.Input().Characters[0];
            source = HeldEnvironmentEffects.WithHeldState(source, new CharacterHeldEnvironmentState(41, false, 7));
            source = PositionTileModifierCalculator.Replace(source, new PositionTileModifier(source.Anchor.Value, new[] { 51L }, 3, 2));
            Assert.That(source.Debuffs.SameValues(CharacterDebuffState.Empty), Is.True);

            var final = CombatDebuffApplication.WithDebuffs(source, Active);

            Assert.That(final.StatusEffects.Entries.Where(s => s.Key != StatusEffectKey.AttackDecrease),
                Is.EqualTo(source.StatusEffects.Entries.Where(s => s.Key != StatusEffectKey.AttackDecrease)));
            Assert.That(final.StatusEffects.Entries.OfType<AttackDecreaseStatus>().Single().ReductionPercent, Is.EqualTo(20));
            foreach (var property in typeof(CharacterCombatFacts).GetProperties().Where(p => p.Name != nameof(CharacterCombatFacts.Debuffs) && p.Name != nameof(CharacterCombatFacts.StatusEffects)))
                Assert.That(property.GetValue(final), Is.EqualTo(property.GetValue(source)), property.Name);
            Assert.That(final.Debuffs.SameValues(Active), Is.True);
            Assert.That(source.Debuffs.Intimidation, Is.Null);
            Assert.Throws<ArgumentNullException>(() => CombatDebuffApplication.WithDebuffs(null, Active));
            Assert.Throws<ArgumentNullException>(() => CombatDebuffApplication.WithDebuffs(source, null));
        }

        [TestCase("damage")]
        [TestCase("health-assignment")]
        [TestCase("movement-record")]
        [TestCase("turn-reset")]
        [TestCase("position-bonus")]
        [TestCase("held-item")]
        public void ExistingCopyOwners_PreserveDebuffAndOriginal(string operation)
        {
            var input = CombatCommandTestData.Input();
            var source = Mark(input.Characters[0]);
            CharacterCombatFacts final = operation switch
            {
                "damage" => CombatCommandCalculator.Hurt(source, 5),
                "health-assignment" => CharacterSettlementCalculator.SetHealth(source, input.Main, 80).Characters.Single().Final,
                "movement-record" => source.With(actionUse: UnitActionUse.Moved),
                "turn-reset" => source.With(actionUse: UnitActionUse.Available),
                "position-bonus" => PositionTileModifierCalculator.Replace(source, new PositionTileModifier(source.Anchor.Value, new[] { 51L }, 3, 2)),
                "held-item" => HeldEnvironmentEffects.AcquireBadge(source, 41),
                _ => throw new ArgumentOutOfRangeException(nameof(operation))
            };
            AssertPreserved(source, final);
            Assert.That(source.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(source.ActionUse, Is.EqualTo(UnitActionUse.Available));
            Assert.That(source.Area, Is.EqualTo(CharacterRuntimeArea.Field));
        }

        [Test]
        public void SoulCollection_GrowsStatsAndPreservesDebuff()
        {
            var input = CombatCommandTestData.Input();
            var source = Mark(input.Characters[0]);
            var field = new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1,
                new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "fixture-soul"), source.Anchor.Value, 3, 0) });
            var result = new SoulEffectCalculator(input.Artifacts).Collect(new SoulCollectorInput(source,
                input.Owned.Single(o => o.Id == source.OwnedId.Value), 0, 100), field, true);
            var final = result.Characters.Characters.Single().Final;
            Assert.That(final.Stats.SoulAttackBonus, Is.EqualTo(1));
            AssertPreserved(source, final);
        }

        [Test]
        public void MoveBackOnLaterPlayerTurn_PreservesDebuffAcrossRealCommands()
        {
            var input = MarkAll(MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right));
            var moved = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(moved.Accepted, Is.True, moved.Reason);
            Assert.That(MovementTestData.Actor(moved).Anchor, Is.EqualTo(CellCoord.Right));
            AssertPreserved(input.Characters[0], MovementTestData.Actor(moved));
            var started = CombatTurnCalculator.BeginControllerTurn(EnemyControllerPhaseTests.Input(MovementTestData.After(moved), CombatControllerPhase.StartingPlayer));
            var nextMove = CombatCommandTestData.FreshCommand(CombatCommandComposition.Project(started), CombatCommandKind.Move, input.ActorId);
            var returned = CombatCommandCalculator.Calculate(MovementTestData.Route(nextMove, CellCoord.Right, CellCoord.Zero), Array.Empty<IMovementTileRule>());
            Assert.That(returned.Accepted, Is.True, returned.Reason);
            Assert.That(MovementTestData.Actor(returned).Anchor, Is.EqualTo(CellCoord.Zero));
            AssertPreserved(input.Characters[0], MovementTestData.Actor(returned));
        }

        [Test]
        public void AttackKnockback_PreservesVictimDebuffAfterDamageAndRelocation()
        {
            var input = MarkAll(CliffFallTests.Knockback());
            var source = input.Characters.Single(c => c.EntityId.Value == 2);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var final = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == source.EntityId);
            Assert.That(final.Anchor, Is.Not.EqualTo(source.Anchor));
            Assert.That(final.Health.CurrentHealth, Is.LessThan(source.Health.CurrentHealth));
            AssertPreserved(source, final);
        }

        [TestCase(1, false)]
        [TestCase(8, true)]
        public void EndTurnPreservesDebuff_NightRecallClearsOnlyReturnedRecipients(long completedPlayerTurns, bool nightEnds)
        {
            var seed = MarkAll(CombatCommandTestData.Input());
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy, controllerTurnSequence: 1, completedPlayerTurnCount: completedPlayerTurns);
            var state = new CombatCommandState(8,8,CombatFlowPhase.Automatic,true,                seed.State.Structures,seed.State.ThornFields,turn);
            var battle = new CombatBattleFacts(4, 1, 8, 8, Array.Empty<CharacterInstanceId>(), 0);
            var input = new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, seed.ActorId, seed.Aim,
                state, seed.Characters, seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts, battle: battle, processingKind: CombatProcessingKind.Advance,
                expectedBattleInstanceId: seed.ExpectedBattleInstanceId, expectedControllerTurnSequence: seed.ExpectedControllerTurnSequence,
                battleInstanceId: seed.BattleInstanceId);
            var result = CombatTurnCalculator.EndEnemyPhase(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var projected = CombatCommandComposition.Project(result);
            foreach (var source in input.Characters)
            {
                var final = projected.Characters.Single(c => c.EntityId == source.EntityId);
                if (nightEnds && final.Area != CharacterRuntimeArea.Field)
                    Assert.That(final.Debuffs.Intimidation, Is.Null);
                else AssertPreserved(source, final);
            }
            Assert.That(projected.Characters.Single(c => c.EntityId == input.ActorId).Area,
                Is.EqualTo(nightEnds ? CharacterRuntimeArea.Hand : CharacterRuntimeArea.Field));
            Assert.That(projected.Characters.Single(c => c.EntityId.Value == 2).Area,
                Is.EqualTo(nightEnds ? CharacterRuntimeArea.EnemyWaiting : CharacterRuntimeArea.Field));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Finalization_DeathClearsOnlyVictim_AndTerminalClearsEveryone(bool mainDies)
        {
            var seed = MarkAll(CombatCommandTestData.Input());
            var projected = seed.Characters.ToDictionary(c => c.EntityId);
            var dying = seed.Characters.Single(c => c.EntityId.Value == (mainDies ? 10 : 2));
            projected[dying.EntityId] = CombatCommandCalculator.Hurt(dying, 100);
            var result = CombatCommandCalculator.Finish(seed, projected, seed.SoulCounts.ToDictionary(p => p.Key, p => p.Value),
                seed.Souls, seed.State.ThornFields.ToDictionary(f => f.Cell), seed.State.Structures.ToArray(),
                new System.Collections.Generic.List<long>(), new System.Collections.Generic.Dictionary<EntityId, CellCoord>(),
                new SoulEffectCalculator(seed.Artifacts), null, events: new System.Collections.Generic.List<CombatEvent>
                { new CombatCharacterEvent(CombatEventKind.Died, dying, projected[dying.EntityId]) });
            var final = CombatCommandComposition.Project(result);
            foreach (var source in seed.Characters)
            {
                var after = final.Characters.Single(c => c.EntityId == source.EntityId);
                if (mainDies || source.EntityId == dying.EntityId)
                    Assert.That(after.Debuffs.Intimidation, Is.Null, source.EntityId.ToString());
                else
                    AssertPreserved(source, after);
                Assert.That(source.Debuffs.SameValues(Active), Is.True);
            }
            Assert.That(result.Outcome.IsStageEnded, Is.EqualTo(mainDies));
        }

        [TestCase(7, false)]
        [TestCase(100, true)]
        public void MineDamage_PreservesLivingVictimStateAndClearsDeadVictim(int damage, bool dies)
        {
            var seed = MarkAll(CliffFallTests.WithEnvironment(
                MovementTestData.SafeRoute(CellCoord.Zero,CellCoord.Right),
                MineEnvironmentTests.Mine(cell: CellCoord.Right, damage: damage)));
            var result = CombatCommandCalculator.Calculate(seed, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            var after = MovementTestData.Actor(result);
            Assert.That(after.Health.CurrentHealth, Is.EqualTo(100 - damage));
            if (dies)
                Assert.That(after.Debuffs.Intimidation, Is.Null);
            else
                AssertPreserved(seed.Characters[0], after);
        }

        [Test]
        public void IntimidationHolderDeath_DoesNotClearOtherVictims()
        {
            var seed = MarkAll(CombatCommandEffectTests.ActorEffect(
                new IntimidationTraitSpec(20), new IntimidationTraitStatusSnapshot(20)));
            var projected = seed.Characters.ToDictionary(c => c.EntityId);
            projected[seed.ActorId] = CombatCommandCalculator.Hurt(projected[seed.ActorId], 100);
            var result = CombatCommandCalculator.Finish(seed, projected, seed.SoulCounts.ToDictionary(p => p.Key, p => p.Value),
                seed.Souls, seed.State.ThornFields.ToDictionary(f => f.Cell), seed.State.Structures.ToArray(),
                new System.Collections.Generic.List<long>(), new System.Collections.Generic.Dictionary<EntityId, CellCoord>(),
                new SoulEffectCalculator(seed.Artifacts), null);
            var after = CombatCommandComposition.Project(result);
            Assert.That(after.Characters.Single(c => c.EntityId == seed.ActorId).Debuffs.Intimidation, Is.Null);
            foreach (var victim in seed.Characters.Where(c => c.EntityId != seed.ActorId))
                AssertPreserved(victim, after.Characters.Single(c => c.EntityId == victim.EntityId));
        }

        internal static CharacterCombatFacts Mark(CharacterCombatFacts source) =>
            CombatDebuffApplication.WithDebuffs(source, Active);

        private static CombatCommandInput MarkAll(CombatCommandInput source) =>
            CombatCommandTestData.With(source, characters: source.Characters.Select(Mark).ToArray());

        private static void AssertPreserved(CharacterCombatFacts source, CharacterCombatFacts final)
        {
            Assert.That(final.Debuffs.SameValues(Active), Is.True);
            Assert.That(source.Debuffs.SameValues(Active), Is.True);
            Assert.That(final.Stats.TraitStatuses.ToArray().Select(t => t.Key),
                Is.EqualTo(source.Stats.TraitStatuses.ToArray().Select(t => t.Key)));
        }
    }
}
