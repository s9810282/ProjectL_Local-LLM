using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class HeldEnvironmentEffectTests
    {
        [Test]
        public void SoulCollection_PreservesHeldStateAndItsIndependentAttackContribution()
        {
            var i = CombatCommandTestData.Input();
            var c = HeldEnvironmentEffects.WithHeldState(i.Characters[0], new CharacterHeldEnvironmentState(41, false, 7));
            var field = new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1,
                new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "fixture-soul"), c.Anchor.Value, 3, 0) });
            var result = new SoulEffectCalculator(i.Artifacts).Collect(new SoulCollectorInput(c,
                i.Owned.Single(o => o.Id == c.OwnedId.Value), 0, 100), field, true);
            var final = result.Characters.Characters.Single().Final;
            Assert.That(final.HeldEnvironment.SameValues(c.HeldEnvironment), Is.True);
            Assert.That(final.Stats.SoulAttackBonus, Is.EqualTo(1));
            Assert.That(new SoulEffectCalculator(i.Artifacts).EvaluateAttackPower(final, 1, new[] { final }), Is.EqualTo(28));
        }

        [Test]
        public void MovementContext_RejectsChangedHeldState_IntentRetainsIt()
        {
            var i = MovementTestData.SafeRoute(5, 2, 1, CellCoord.Zero, CellCoord.Right);
            i = ObstacleReactionTests.Copy(i);
            var rules = Array.Empty<IMovementTileRule>();
            var captured = MovementContextCalculator.Capture(i, Owner.Player, rules);
            var chars = i.Characters.Select(c => c.EntityId == i.ActorId ? HeldEnvironmentEffects.AcquireBadge(c, 41) : c).ToArray();
            var changed = CombatCommandTestData.With(i, chars);
            Assert.That(MovementContextCalculator.Matches(captured, changed, Owner.Player, rules), Is.False);
            var intent = CombatCommandComposition.Intent(changed, CombatCommandKind.Move);
            Assert.That(intent.Characters.Single(c => c.EntityId == i.ActorId).HeldEnvironment.BadgeAvailable, Is.True);
        }

        [Test]
        public void Badge_ActualZeroDamageAttackStillConsumesProtection()
        {
            var i = CombatCommandTestData.Input();
            var chars = i.Characters.ToArray();
            chars[0] = CombatCommandTestData.Character(1, chars[0].CharacterKey, chars[0].Anchor.Value, Owner.Player, attack: 0);
            chars[2] = HeldEnvironmentEffects.AcquireBadge(chars[2], 41);
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(i, chars), Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var target = result.Characters.Characters.Single(c => c.Final.EntityId == chars[2].EntityId).Final;
            Assert.That(target.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(target.HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(result.Events.OfType<CombatHeldEnvironmentEvent>().Any(e => e.CharacterId == target.EntityId), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BarrelTriggeredSupplies_UsesLivingOriginalCreditAfterExplosionDamage(bool killsCredit)
        {
            var barrel = ObstacleReactionTests.Barrel(51, new CellCoord(2, 3), 1, killsCredit ? 100 : 7);
            var supply = new StructureCombatFacts(new EntityId(52), new StructureSpec(EntityKey.Structure_Supplies,
                1, 1, new[] { CellCoord.Zero }, 10, false, 0), new HealthStateSnapshot(10, 10),
                new CellCoord(3, 3), Rotation4.R0, new[] { new CellCoord(3, 3) }, Owner.Neutral);
            var i = ObstacleReactionTests.Barrels(barrel, (supply, new ObstacleContentProfile(supply.EntityId,
                ObstacleContentKind.Supplies, 0, 2, 5)));
            i = CombatAttackQueries.WithAim(CombatCommandTestData.With(i, i.Characters.Where(c => c.EntityId == i.ActorId ||
                c.EntityId == new EntityId(10)).ToArray()), AttackAim.ForCell(barrel.Structure.Anchor));
            var result = CombatCommandCalculator.Calculate(i, Array.Empty<IMovementTileRule>());
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == i.ActorId).Final.HeldEnvironment.SuppliesAttackBonus,
                Is.EqualTo(killsCredit ? 0 : 2));
            if (!killsCredit)
            {
                var reward = result.Events.OfType<CombatHeldEnvironmentEvent>().Single();
                Assert.That(reward.Origin.CreditedCharacterId, Is.EqualTo(i.ActorId));
                Assert.That(reward.Origin.PhysicalCell, Is.EqualTo(supply.Anchor));
            }
        }

        [Test]
        public void Supplies_ActualAttackRewardsPersistIntoNextAttack_AndRetainBadge()
        {
            var supply = new StructureCombatFacts(new EntityId(51), new StructureSpec(EntityKey.Structure_Supplies,
                1, 1, new[] { CellCoord.Zero }, 30, false, 0), new HealthStateSnapshot(30, 30),
                new CellCoord(4, 3), Rotation4.R0, new[] { new CellCoord(4, 3) }, Owner.Neutral);
            var i = ObstacleReactionTests.Barrels((supply, new ObstacleContentProfile(supply.EntityId,
                ObstacleContentKind.Supplies, 0, 2, 5)));
            i = CombatCommandTestData.With(i, i.Characters.Select(c => c.EntityId == i.ActorId ? HeldEnvironmentEffects.AcquireBadge(c, 41) : c).ToArray());
            var first = CombatCommandCalculator.Calculate(i, Array.Empty<IMovementTileRule>());
            var firstActor = first.Characters.Characters.Single(c => c.Final.EntityId == i.ActorId).Final;
            Assert.That(firstActor.HeldEnvironment.SuppliesAttackBonus, Is.EqualTo(2));
            Assert.That(firstActor.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(first.FinalState.Structures.Single().Health.CurrentHealth, Is.EqualTo(10));
            var nextInput = CombatCommandComposition.Project(first);
            Assert.That(CombatCommandCalculator.Preview(nextInput, Array.Empty<IMovementTileRule>()).Targets.ToArray()
                .Single(t => t.TargetId == supply.EntityId).ExpectedDamage, Is.EqualTo(22));
            var second = CombatCommandCalculator.Calculate(nextInput, Array.Empty<IMovementTileRule>());
            Assert.That(second.Characters.Characters.Single(c => c.Final.EntityId == i.ActorId).Final.HeldEnvironment.SuppliesAttackBonus, Is.EqualTo(7));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Supplies_PerHitRewardsAccumulate_DestroyingHitIsExclusive(bool deadCredit)
        {
            var supply = new StructureCombatFacts(new EntityId(51), new StructureSpec(EntityKey.Structure_Supplies,
                1, 1, new[] { CellCoord.Zero }, 30, false, 0), new HealthStateSnapshot(30, 30),
                new CellCoord(4, 3), Rotation4.R0, new[] { new CellCoord(4, 3) }, Owner.Neutral);
            var input = ObstacleReactionTests.Barrels((supply, new ObstacleContentProfile(supply.EntityId,
                ObstacleContentKind.Supplies, 0, 2, 5)));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            if (deadCredit) characters[input.ActorId] = CombatCommandCalculator.Hurt(characters[input.ActorId], 100);
            var structures = input.State.Structures.ToDictionary(s => s.EntityId);
            var events = new System.Collections.Generic.List<CombatEvent>();
            var calc = new ObstacleReactionCalculator(input, input.State.Environment, characters, structures, events,
                new System.Collections.Generic.Dictionary<EntityId, CellCoord>());
            var origin = new CombatDamageOrigin(new CellCoord(1, 3), input.ActorId, 1);
            calc.DamageStructure(supply.EntityId, 0, origin);
            calc.DamageStructure(supply.EntityId, 10, origin);
            calc.DamageStructure(supply.EntityId, 10, origin);
            calc.DamageStructure(supply.EntityId, 10, origin);
            calc.DamageStructure(supply.EntityId, 10, origin);
            Assert.That(characters[input.ActorId].HeldEnvironment.SuppliesAttackBonus, Is.EqualTo(deadCredit ? 0 : 9));
            Assert.That(events.OfType<CombatObstacleReactionEvent>().Select(e => e.Reaction),
                Is.EqualTo(new[] { ObstacleReactionKind.Hit, ObstacleReactionKind.Hit, ObstacleReactionKind.Destroyed }));
            Assert.That(characters[input.ActorId].Stats.AttackPower, Is.EqualTo(20));
        }

        [Test]
        public void Badge_EachBarrelExplosionIsIndependent()
        {
            var input = ObstacleReactionTests.Barrels(ObstacleReactionTests.Barrel(51, new CellCoord(4, 3), 1, 7),
                ObstacleReactionTests.Barrel(52, new CellCoord(5, 3), 1, 7));
            var chars = input.Characters.Select(c => c.EntityId == new EntityId(3) ? HeldEnvironmentEffects.AcquireBadge(c, 41) : c).ToArray();
            var result = CombatCommandCalculator.Calculate(CombatCommandTestData.With(input, chars), Array.Empty<IMovementTileRule>());
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == new EntityId(3)).Final.Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(result.Events.OfType<CombatHeldEnvironmentEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void TerminalOutcome_ClearsHeldStateForAllCharacters()
        {
            var i = CombatCommandTestData.Input();
            var projected = i.Characters.Select(c => HeldEnvironmentEffects.WithHeldState(c,
                new CharacterHeldEnvironmentState(41, true, 7))).ToDictionary(c => c.EntityId);
            projected[new EntityId(10)] = CombatCommandCalculator.Hurt(projected[new EntityId(10)], 100);
            var result = CombatCommandCalculator.Finish(i, projected, i.SoulCounts.ToDictionary(p => p.Key, p => p.Value),
                i.Souls, new System.Collections.Generic.Dictionary<CellCoord, ThornFieldCellSnapshot>(),
                i.State.Structures.ToArray(), new System.Collections.Generic.List<long>(),
                new System.Collections.Generic.Dictionary<EntityId, CellCoord>(),
                new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact.SoulEffectCalculator(i.Artifacts), null);
            Assert.That(result.Outcome.IsStageEnded, Is.True);
            Assert.That(result.Characters.Characters.All(c => c.Final.HeldEnvironment.SameValues(CharacterHeldEnvironmentState.Empty)), Is.True);
        }

        [Test]
        public void Badge_ZeroDamageConsumesUse_HealthAssignmentBypassesShield()
        {
            var i = CombatCommandTestData.Input();
            var c = HeldEnvironmentEffects.AcquireBadge(i.Characters[0], 41);
            var held = new CommandAttackGroups();
            Assert.That(HeldEnvironmentEffects.Protect(held, c, 0, 1, out var spent), Is.Zero);
            Assert.That(spent.HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(HeldEnvironmentEffects.Protect(held, spent, 5, 1, out _), Is.Zero);
            var killed = CharacterSettlementCalculator.SetHealth(c, i.Main, 0).Characters.Single().Final;
            Assert.That(killed.Health.IsDead, Is.True);
            Assert.That(killed.HeldEnvironment.BadgeAvailable, Is.True);
        }

        [Test]
        public void Badge_ThornsInOneRouteAreIndependentAttacks_AndPreventReturn()
        {
            var i = MovementTestData.SafeRoute(8, 2, 2, CellCoord.Zero, CellCoord.Right, new CellCoord(2, 0));
            var chars = i.Characters.Select(c => c.EntityId == i.ActorId ? HeldEnvironmentEffects.AcquireBadge(c, 41) : c).ToArray();
            var s = i.State;
            var input = CombatCommandTestData.With(i, chars, new CombatCommandState(s.BoardWidth, s.BoardHeight,
                s.Phase, s.ActiveActorId, s.IsNight, s.Structures,
                new[] { new ThornFieldCellSnapshot(CellCoord.Right, 2, 7), new ThornFieldCellSnapshot(new CellCoord(2, 0), 2, 7) },
                s.Timeline, s.Turn, s.Environment));
            var rules = new IMovementTileRule[] { new ThornMovementTileRule() };
            var preview = CombatMovementCalculator.Preview(input, rules);
            var result = CombatCommandCalculator.Calculate(input, rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(MovementTestData.Actor(result).Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(preview.ProjectedActor.Health.CurrentHealth, Is.EqualTo(93));
            Assert.That(MovementTestData.Actor(result).HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(result.Events.OfType<CombatHeldEnvironmentEvent>().Count(), Is.EqualTo(1));
            Assert.That(MovementTestData.Actor(result).MovementCheckpointAnchor, Is.EqualTo(new CellCoord(2, 0)));
            Assert.That(result.FinalState.ThornFields, Is.Empty);
        }

        [Test]
        public void Badge_BlocksEveryHitOfOneAttack_ThenNextIndependentAttackDealsDamage()
        {
            var victim = HeldEnvironmentEffects.AcquireBadge(CombatCommandTestData.Input().Characters[2], 41);
            var projected = new System.Collections.Generic.Dictionary<EntityId, CharacterCombatFacts> { [victim.EntityId] = victim };
            var events = new System.Collections.Generic.List<CombatEvent>();
            var held = new CommandAttackGroups();
            var origin = new CombatDamageOrigin(new CellCoord(1, 3), new EntityId(1), CommandAttackGroups.Direct);
            for (int hit = 0; hit < 3; hit++)
                Assert.That(CombatDamageCalculator.ApplyHeldDefense(held, projected, victim.EntityId, 20, origin, events), Is.Zero);
            Assert.That(projected[victim.EntityId].HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(events.OfType<CombatHeldEnvironmentEvent>().Count(), Is.EqualTo(1));
            Assert.That(CombatDamageCalculator.ApplyHeldDefense(new CommandAttackGroups(), projected,
                victim.EntityId, 20, origin, events), Is.EqualTo(20));
        }

        [Test]
        public void Badge_ActualAttackAndPreviewAgree_ConsumptionSurvivesProjectAndMerge()
        {
            var i = CombatCommandTestData.Input();
            var chars = i.Characters.ToArray();
            var victim = chars[2] = HeldEnvironmentEffects.AcquireBadge(chars[2], 41);
            var input = CombatCommandTestData.With(i, chars);
            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var target = preview.Targets.ToArray().Single(t => t.TargetId == victim.EntityId);
            Assert.That(target.ExpectedDamage, Is.Zero);
            var spent = result.Characters.Characters.Single(c => c.Final.EntityId == victim.EntityId).Final;
            Assert.That(spent.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(spent.HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(victim.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(result.Events.OfType<CombatHeldEnvironmentEvent>().Count(), Is.EqualTo(1));
            var next = CombatCommandCalculator.Calculate(CombatCommandComposition.Project(result), Array.Empty<IMovementTileRule>());
            Assert.That(next.Characters.Characters.Single(c => c.Final.EntityId == victim.EntityId).Final.Health.CurrentHealth,
                Is.EqualTo(80));
            var merged = CombatCommandComposition.Merge(input, new[] { result, next });
            Assert.That(merged.Characters.Characters.Single(c => c.Final.EntityId == victim.EntityId).Final.HeldEnvironment.BadgeAvailable, Is.False);
        }

        [Test]
        public void SuppliesContribution_ChangesActualAttackWithoutChangingBaseOrSoulPower()
        {
            var i = CombatCommandTestData.Input();
            var chars = i.Characters.ToArray();
            chars[0] = HeldEnvironmentEffects.WithHeldState(chars[0], new CharacterHeldEnvironmentState(suppliesAttackBonus: 7));
            var preview = CombatCommandCalculator.Preview(CombatCommandTestData.With(i, chars), Array.Empty<IMovementTileRule>());
            Assert.That(preview.Targets.ToArray().Single(t => t.TargetId == new EntityId(2)).ExpectedDamage, Is.EqualTo(27));
            Assert.That(chars[0].Stats.AttackPower, Is.EqualTo(20));
            Assert.That(chars[0].Stats.SoulAttackBonus, Is.Zero);
        }

        [Test]
        public void Badge_DuplicateDoesNotAddProtection()
        {
            var c = CombatCommandTestData.Input().Characters[0];
            var first = HeldEnvironmentEffects.AcquireBadge(c, 41);
            var duplicate = HeldEnvironmentEffects.AcquireBadge(first, 42);
            Assert.That(duplicate.HeldEnvironment.SameValues(first.HeldEnvironment), Is.True);
            var defense = new CommandAttackGroups();
            Assert.That(HeldEnvironmentEffects.Protect(defense, duplicate, 5, 1, out var spent), Is.Zero);
            Assert.That(spent.HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(HeldEnvironmentEffects.Protect(defense, spent, 5, 1, out _), Is.Zero);
            Assert.That(HeldEnvironmentEffects.Protect(defense, spent, 5, 2, out _), Is.EqualTo(5));
        }

        [Test]
        public void CopyAndRecall_DoNotRechargeBadge()
        {
            var c = HeldEnvironmentEffects.AcquireBadge(CombatCommandTestData.Input().Characters[0], 41);
            HeldEnvironmentEffects.Protect(new CommandAttackGroups(), c, 5, 1, out c);
            c = CharacterSettlementCalculator.Copy(c, c.Health, c.Stats);
            c = CombatMovementCalculator.CopyMovement(c, c.Stats, c.Anchor, 0, 0, 0, 0, c.MovementCheckpointAnchor);
            c = CombatPlacementCalculator.Location(c, CharacterRuntimeArea.Hand, 0, null, null);
            c = CombatPlacementCalculator.Location(c, CharacterRuntimeArea.Field, null, new CellCoord(2, 3), c.Rotation ?? Rotation4.R0);
            Assert.That(c.HeldEnvironment.BadgeAvailable, Is.False);
            Assert.That(c.HeldEnvironment.BadgeSourceId, Is.EqualTo(41));
        }

        [Test]
        public void Supplies_ExpiryPreservesBadgeAndOtherContributions()
        {
            var input = CombatCommandTestData.Input();
            var c = HeldEnvironmentEffects.WithHeldState(input.Characters[0], new CharacterHeldEnvironmentState(41, true, 7));
            c = PositionTileModifierCalculator.Replace(c, new PositionTileModifier(c.Anchor.Value, new[] { 51L }, 3, 0));
            var expired = HeldEnvironmentEffects.ExpireSupplies(c);
            Assert.That(expired.HeldEnvironment.SuppliesAttackBonus, Is.Zero);
            Assert.That(expired.HeldEnvironment.BadgeAvailable, Is.True);
            Assert.That(expired.PositionModifier.SameValues(c.PositionModifier), Is.True);
            Assert.That(expired.Stats.EffectiveAttackPower, Is.EqualTo(c.Stats.EffectiveAttackPower));
        }

        [TestCase(10, true)]
        [TestCase(100, false)]
        public void NightEnd_ExpiresSuppliesBeforeRecallIncludingMain(long nightLength, bool expires)
        {
            var i = CombatCommandTestData.Input(empty: true);
            var characters = i.Characters.Select(c => HeldEnvironmentEffects.WithHeldState(c,
                new CharacterHeldEnvironmentState(41, true, 7))).ToArray();
            var turn = new CombatTurnFacts(StagePhase.Combat, participants: characters.Select(c =>
                new CombatTurnParticipant(c.EntityId, c.Stats.Priority, c.Stats.Delay)).ToArray());
            var state = new CombatCommandState(8, 8, CombatFlowPhase.PlayerInput, i.ActorId, true,
                i.State.Structures, i.State.ThornFields, i.State.Timeline, turn);
            var battle = new CombatBattleFacts(4, nightLength, 1, 8, 8,
                Array.Empty<CharacterInstanceId>(), 0, 100, 0, null);
            var input = new CombatCommandInput(i.Number, CombatCommandKind.EndUnitTurn, i.ActorId, i.Aim,
                state, characters, i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, battle: battle);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            var main = result.Characters.Characters.SingleOrDefault(c => c.Final.EntityId == new EntityId(10))?.Final ?? characters.Single(c => c.EntityId == new EntityId(10));
            var actor = result.Characters.Characters.Single(c => c.Final.EntityId == i.ActorId).Final;
            Assert.That(main.Area, Is.EqualTo(CharacterRuntimeArea.Field));
            Assert.That(main.HeldEnvironment.SuppliesAttackBonus, Is.EqualTo(expires ? 0 : 7));
            Assert.That(actor.HeldEnvironment.SuppliesAttackBonus, Is.EqualTo(expires ? 0 : 7));
            Assert.That(main.HeldEnvironment.BadgeAvailable && actor.HeldEnvironment.BadgeAvailable, Is.True);
            if (expires)
            {
                var returned = result.Events.OfType<CombatCharacterEvent>().Single(e => e.Kind == CombatEventKind.Returned);
                Assert.That(returned.Expected.HeldEnvironment.SuppliesAttackBonus, Is.Zero);
                Assert.That(CombatPlacementCalculator.Location(actor, CharacterRuntimeArea.Field, null,
                    new CellCoord(2, 3), Rotation4.R0).HeldEnvironment.SuppliesAttackBonus, Is.Zero);
            }
        }
    }
}
