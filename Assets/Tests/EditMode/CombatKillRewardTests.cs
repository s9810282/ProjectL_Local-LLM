using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class CombatKillRewardTests
    {
        [TestCase(EntityKey.Unit_Demon, 10, 10, 1)]
        [TestCase(EntityKey.Unit_Demon, 10, 10, 5)]
        [TestCase(EntityKey.Unit_Gravekeeper, 20, 20, 1)]
        [TestCase(EntityKey.Unit_Gravekeeper, 20, 20, 5)]
        [TestCase(EntityKey.Unit_UndeadHeavy, 30, 30, 1)]
        [TestCase(EntityKey.Unit_UndeadHeavy, 30, 30, 5)]
        [TestCase(EntityKey.Unit_UndeadCannonCart, 25, 25, 1)]
        [TestCase(EntityKey.Unit_UndeadCannonCart, 25, 25, 5)]
        [TestCase(EntityKey.Unit_Stage1Boss, 300, 500, 1)]
        [TestCase(EntityKey.Unit_Stage1Boss, 300, 500, 5)]
        public void Attack_UsesDefinitionAmountsRegardlessOfLevel(EntityKey key, int gold, int xp, int level)
        {
            var input = Input(new UnitKillRewardSpec(gold, xp), level, key);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.RewardChange.ExpectedGold, Is.EqualTo(7));
            Assert.That(result.RewardChange.FinalGold, Is.EqualTo(7 + gold));
            Assert.That(result.FinalState.Rewards.KillGoldEarned, Is.EqualTo(gold));
            Assert.That(CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == input.ActorId).Experience.Earned, Is.EqualTo(xp));
            Assert.That(result.Characters.Owned, Is.Empty);
            Assert.That(input.Owned.All(o => o.Level == 1), Is.True);
        }

        [TestCase(0, 10)]
        [TestCase(10, 0)]
        public void ZeroGoldOrZeroExperience_DoesNotDisableOtherComponent(int gold, int xp)
        {
            var result = CombatCommandCalculator.Calculate(Input(new UnitKillRewardSpec(gold, xp)), Array.Empty<IMovementTileRule>());
            Assert.That(result.FinalState.Rewards.KillGoldEarned, Is.EqualTo(gold));
            Assert.That(result.RewardChange?.FinalGold ?? 7, Is.EqualTo(7 + gold));
            Assert.That(result.Events.OfType<CombatExperienceEvent>().Sum(e => e.Amount), Is.EqualTo(xp));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void ExcludedBallistaAndConfiguredZero_DoNotRequireBalance(bool excluded)
        {
            var input = Input(excluded ? UnitKillRewardSpec.Excluded : new UnitKillRewardSpec(0, 0),
                key: EntityKey.Unit_Ballista, goldBalance: null);
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.RewardChange, Is.Null);
            Assert.That(result.Events.OfType<CombatExperienceEvent>(), Is.Empty);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MissingRewardOrMissingProfile_Fails(bool profilePresent)
        {
            var input = Input(null);
            if (!profilePresent) input = CombatCommandTestData.With(input, profiles: input.Profiles.Where(p => p.Definition.Key != EntityKey.Unit_Demon).ToArray());
            Assert.Throws<InvalidOperationException>(() => CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()));
        }

        [Test]
        public void GoldNeedsActualBalance_AndCheckedArithmetic()
        {
            Assert.Throws<InvalidOperationException>(() => CombatCommandCalculator.Calculate(Input(new UnitKillRewardSpec(10, 10), goldBalance: null), Array.Empty<IMovementTileRule>()));
            Assert.Throws<OverflowException>(() => CombatCommandCalculator.Calculate(Input(new UnitKillRewardSpec(10, 10), goldBalance: int.MaxValue), Array.Empty<IMovementTileRule>()));
            var input = Input(new UnitKillRewardSpec(10, 10));
            var s = input.State;
            input = CombatCommandTestData.With(input, state: new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.Structures,s.Turn,s.Environment,                s.EnvironmentProgress,s.WallBlocking,s.Objective,s.Allocation,s.ItemRandomState,                new CombatRewardFacts(false, Array.Empty<CharacterInstanceId>(), int.MaxValue)));
            Assert.Throws<OverflowException>(() => CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>()));
        }

        [Test]
        public void Composition_TwoInstancesAndNoEffectStep_KeepOriginalBalanceAndCountEachDeathOnce()
        {
            var original = Input(new UnitKillRewardSpec(10, 10));
            var first = CombatCommandCalculator.Calculate(original, Array.Empty<IMovementTileRule>());
            var projected = CombatCommandComposition.Project(first);
            Assert.That(projected.GoldBalance, Is.EqualTo(17));
            var secondInput = CombatCommandComposition.Intent(projected, CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(4, 4)));
            Assert.That(secondInput.GoldBalance, Is.EqualTo(17));
            var second = CombatCommandTestData.Reaction(secondInput, Array.Empty<IMovementTileRule>());
            var merged = CombatCommandComposition.Merge(original, new[] { first, second });
            Assert.That((merged.RewardChange.ExpectedGold, merged.RewardChange.FinalGold), Is.EqualTo((7, 27)));
            Assert.That(merged.FinalState.Rewards.KillGoldEarned, Is.EqualTo(20));
            Assert.That(merged.Events.OfType<CombatExperienceEvent>().Sum(e => e.Amount), Is.EqualTo(20));
            var after = CombatCommandComposition.Project(merged);
            var noEffect = CombatCommandComposition.Intent(after, CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(4, 3)));
            var corpse = CombatCommandCalculator.Calculate(noEffect, Array.Empty<IMovementTileRule>());
            Assert.That(corpse.RewardChange, Is.Null);
            Assert.That(corpse.FinalState.Rewards.KillGoldEarned, Is.EqualTo(20));
        }

        [Test]
        public void ComposedFinalKill_PreservesCanonicalSplitAndPaysClearOnce()
        {
            var source = Input(new UnitKillRewardSpec(10, 10));
            var s = source.State;
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,                s.Structures,s.Turn,s.Environment,s.EnvironmentProgress,s.WallBlocking,                BattleObjective.DesignatedBoss(new EntityId(3)),s.Allocation,s.ItemRandomState,                new CombatRewardFacts(true, source.Owned.Select(o => o.Id).ToArray(), 0));
            var chars = source.Characters.Select(c => c.EntityId.Value != 3 ? c : new CharacterCombatFacts(c.EntityId,
                c.OwnedId, EntityKey.Unit_Gravekeeper, c.Owner, c.OwnershipRole, c.Area, c.Health, c.Stats,
                c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.ActionUse,
                experience: c.Experience)).ToArray();
            var demon = source.Profiles.Single(p => p.Definition.Key == EntityKey.Unit_Demon);
            var profiles = source.Profiles.Append(new CombatAttackProfile(WithReward(demon.Definition,
                EntityKey.Unit_Gravekeeper, new UnitKillRewardSpec(20, 20)), demon.Method)).ToArray();
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, state, chars,
                profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                battle: new CombatBattleFacts(4, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0,
                    progression: source.Battle.Progression, clearRewards: new StageClearRewardSpec(100, 50, 100)),
                battleInstanceId: 1, goldBalance: 7, fameBalance: 0,
                expectedBattleInstanceId: 1, expectedControllerTurnSequence: source.ExpectedControllerTurnSequence);
            var first = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var second = CombatCommandTestData.Reaction(CombatCommandComposition.Intent(CombatCommandComposition.Project(first),
                CombatCommandKind.Attack, AttackAim.ForCell(new CellCoord(4, 4))), Array.Empty<IMovementTileRule>());

            var merged = CombatCommandComposition.Merge(input, new[] { first, second });
            var receipt = BattleRewardReceiptCalculator.Create(merged, 2, 1);

            Assert.That(first.RewardSettlement.KillGold, Is.EqualTo(10));
            Assert.That(second.RewardSettlement.KillGold, Is.EqualTo(20));
            Assert.That((merged.RewardSettlement.KillGold, merged.RewardSettlement.ClearGold, merged.RewardSettlement.ClearFame),
                Is.EqualTo((30, 100, 50)));
            Assert.That((merged.RewardChange.ExpectedGold, merged.RewardChange.FinalGold), Is.EqualTo((7, 137)));
            Assert.That(receipt.TotalGoldEarned, Is.EqualTo(130));
            Assert.That(receipt.Characters.Select(c => (c.KillExperience, c.ClearExperience)), Is.EqualTo(new[] { (0, 100), (30, 100) }));
            Assert.That(receipt.Characters[1], Is.SameAs(second.RewardSettlement.Characters[1]));
            Assert.That(merged.Events.OfType<CombatExperienceEvent>().Where(e => e.Kind == CombatEventKind.ExperienceApplied)
                .Select(e => e.Amount), Is.EquivalentTo(new[] { 100, 130 }));
            Assert.Throws<InvalidOperationException>(() => CombatCommandComposition.Merge(input, new[] { first, second, second }));
        }

        [Test]
        public void MissingSettlement_CannotHideRewardInComposition_AndRejectedTransitionCannotCarryIt()
        {
            var input = Input(new UnitKillRewardSpec(10, 10));
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            var omitted = new CombatCommandTransition(input, true, "", result.Characters, result.Field, result.FinalState,
                result.Outcome, events: result.Events, rewardChange: result.RewardChange);
            Assert.Throws<InvalidOperationException>(() => CombatCommandComposition.Merge(input, new[] { omitted }));
            Assert.Throws<ArgumentException>(() => new CombatCommandTransition(input, false, "Rejected",
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()),
                new SoulFieldChange(input.Souls, input.Souls), input.State,
                new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None), rewardSettlement: result.RewardSettlement));
        }

        [Test]
        public void CreatedEnemyDyingImmediately_IsCreditedOnce_AndDuplicateDeathIsInvalid()
        {
            var input = Input(new UnitKillRewardSpec(20, 20));
            var before = CombatCommandTestData.Character(30, EntityKey.Unit_Demon, new CellCoord(6, 6), Owner.Enemy);
            var dead = CombatCommandCalculator.Hurt(before, 1000);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected.Add(dead.EntityId, dead);
            var events = new List<CombatEvent> { new CombatCharacterEvent(CombatEventKind.Died, before, dead) };
            var kill = ConfirmedKillCalculator.Collect(input, projected, events, input.State).Single();
            Assert.That((kill.VictimId, kill.RecipientId, kill.Gold, kill.Experience), Is.EqualTo((dead.EntityId, input.ActorId, 20, 20)));
            events.Add(events[0]);
            Assert.Throws<InvalidOperationException>(() => ConfirmedKillCalculator.Collect(input, projected, events, input.State));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ExplicitEnemyCreditAndActorlessAutomaticDeath_PayNeitherComponent(bool automatic)
        {
            var seed = Input(new UnitKillRewardSpec(10, 10));
            var input = automatic ? Copy(seed, processing: CombatProcessingKind.Advance) : seed;
            var victim = input.Characters.Single(c => c.EntityId.Value == 2);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            projected[victim.EntityId] = CombatCommandCalculator.Hurt(victim, 1000);
            var origins = automatic ? null : new[] { new CombatDamageOrigin(victim.Anchor, new EntityId(3), 1) };
            var events = new[] { new CombatCharacterEvent(CombatEventKind.Died, victim, projected[victim.EntityId], origins) };
            Assert.That(ConfirmedKillCalculator.Collect(input, projected, events, input.State), Is.Empty);
        }

        [Test]
        public void PreviewAndDisabledProgression_KeepGoldIndependentOfLeveling()
        {
            var input = Input(new UnitKillRewardSpec(10, 10));
            CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>(), out var preview);
            Assert.That(preview.RewardChange.FinalGold, Is.EqualTo(17));
            Assert.That(input.GoldBalance, Is.EqualTo(7));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Health.IsDead, Is.False);
            Assert.That(input.State.Rewards.KillGoldEarned, Is.Zero);
            var disabled = Copy(input, disableProgression: true);
            var result = CombatCommandCalculator.Calculate(disabled, Array.Empty<IMovementTileRule>());
            Assert.That(result.RewardChange.FinalGold, Is.EqualTo(17));
            Assert.That(result.Events.OfType<CombatExperienceEvent>(), Is.Empty);
        }

        [Test]
        public void FinalKillDuringRoarMineReaction_MergesGrantAndVictoryExperienceOnce()
        {
            var seed = BossRoarTests.Input(4);
            var target = BossRoarTests.Ally;
            seed = CombatCommandTestData.With(seed, characters: seed.Characters.Select(c => c.EntityId == target
                ? CombatCommandTestData.With(c, hp: 1, position: new CellCoord(4, 4)) : c).ToArray(),
                profiles: seed.Profiles.Select(p => new CombatAttackProfile(WithReward(p.Definition, p.Definition.Key,
                    new UnitKillRewardSpec(20, 20)), p.Method)).ToArray());
            seed = CliffFallTests.WithEnvironment(seed, MineEnvironmentTests.Mine(cell: new CellCoord(5, 3)));
            var s = seed.State;
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,                s.Structures,s.Turn,s.Environment,s.EnvironmentProgress,                s.WallBlocking,BattleObjective.DesignatedBoss(target),s.Allocation,s.ItemRandomState,s.Rewards);
            var input = new CombatCommandInput(seed.Number, seed.Kind, seed.ActorId, seed.Aim, state, seed.Characters,
                seed.Profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                battle: new CombatBattleFacts(10, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0,
                    progression: new LevelProgressionSpec(150, 100, 3, 1, 1)),
                environmentBindings: seed.EnvironmentBindings, goldBalance: 7, fameBalance: 0,
                expectedBattleInstanceId: seed.ExpectedBattleInstanceId, expectedControllerTurnSequence: seed.ExpectedControllerTurnSequence,
                battleInstanceId: seed.BattleInstanceId);
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(result.Events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That((result.RewardChange.ExpectedGold, result.RewardChange.FinalGold), Is.EqualTo((7, 27)));
            Assert.That(result.FinalState.Rewards.KillGoldEarned, Is.EqualTo(20));
            Assert.That(result.Characters.Owned.Single().Final.Experience, Is.EqualTo(20));
            // 기록만 있는 아군(포효에 맞은 아군 등)도 영수증 행을 받으므로, 경험치를 받은 행이 하나뿐인지 본다(스테이지 기록 설계 Q-S6).
            Assert.That(result.RewardSettlement.Characters.Single(c => c.TotalExperience > 0).TotalExperience, Is.EqualTo(20));
            Assert.That(result.RewardSettlement.HasClearReward, Is.False);
            Assert.Throws<InvalidOperationException>(() => BattleRewardReceiptCalculator.Create(result, 1, 0));
            Assert.That(result.Events.OfType<CombatExperienceEvent>().Count(e => e.Kind == CombatEventKind.ExperienceApplied), Is.EqualTo(1));
        }

        [Test]
        public void QueryAimCopy_PreservesActualBalances()
        {
            var input = Input(new UnitKillRewardSpec(10, 10));
            var aimed = CombatAttackQueries.WithAim(input, AttackAim.ForCell(new CellCoord(4, 4)));
            Assert.That(aimed.GoldBalance, Is.EqualTo(7));
            Assert.That(aimed.FameBalance, Is.Zero);
            Assert.That(CombatCommandCalculator.Calculate(aimed, Array.Empty<IMovementTileRule>()).RewardChange.FinalGold, Is.EqualTo(17));
        }

        internal static CombatCommandInput Input(UnitKillRewardSpec reward, int level = 1,
            EntityKey key = EntityKey.Unit_Demon, int? goldBalance = 7)
        {
            var seed = CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var characters = seed.Characters.Select(c => c.Owner != Owner.Enemy ? c : new CharacterCombatFacts(
                c.EntityId, null, key, c.Owner, null, c.Area, new HealthStateSnapshot(1, 100), c.Stats,
                c.ReserveSlotIndex, c.Anchor, c.Rotation, c.OccupiedCells, c.ActionUse,
                experience: CharacterExperienceFacts.ForEnemy(level))).ToArray();
            var profiles = seed.Profiles.Select(p => p.Definition.Key != EntityKey.Unit_Demon ? p : new CombatAttackProfile(
                WithReward(p.Definition, key, reward), p.Method)).ToArray();
            return new CombatCommandInput(seed.Number, seed.Kind, seed.ActorId, seed.Aim, seed.State, characters, profiles,
                seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                battle: new CombatBattleFacts(10, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0,
                    progression: new LevelProgressionSpec(150, 100, 3, 1, 1)), goldBalance: goldBalance, fameBalance: 0,
                expectedBattleInstanceId: seed.ExpectedBattleInstanceId, expectedControllerTurnSequence: seed.ExpectedControllerTurnSequence,
                battleInstanceId: seed.BattleInstanceId);
        }

        private static UnitSpec WithReward(UnitSpec s, EntityKey key, UnitKillRewardSpec reward) =>
            new(key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth,
                s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority, s.Traits.ToArray(),
                s.OwnerEligibility, s.Occlusion, s.Behavior, killReward: reward);

        private static CombatCommandInput Copy(CombatCommandInput s, CombatProcessingKind processing = CombatProcessingKind.Command,
            bool disableProgression = false) => new(processing == CombatProcessingKind.Command ? s.Number : 0,
                s.Kind, processing == CombatProcessingKind.Command ? s.ActorId : EntityId.Empty, s.Aim,
                s.State, s.Characters, s.Profiles, s.Main, s.Owned, s.SoulCounts, s.Souls, s.Artifacts,
                battle: disableProgression ? null : s.Battle, processingKind: processing,
                goldBalance: s.GoldBalance, fameBalance: s.FameBalance,
                expectedBattleInstanceId: s.ExpectedBattleInstanceId, expectedControllerTurnSequence: s.ExpectedControllerTurnSequence,
                battleInstanceId: s.BattleInstanceId);
    }
}
