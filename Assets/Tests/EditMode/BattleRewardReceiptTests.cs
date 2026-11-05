using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Experience;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.Core
{
    public sealed class BattleRewardReceiptTests
    {
        [TestCase(1)]
        [TestCase(3)]
        public void CapturedCohort_ReceivesClearOnce_UnionWithPendingKillExperience(int subs)
        {
            var input = Input(subs);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var cohort = projected.Values.Where(c => c.Area == CharacterRuntimeArea.Field).Select(c => c.OwnedId.Value).ToArray();
            var killer = projected.Values.First(c => c.OwnershipRole == CharacterOwnershipRole.Sub);
            projected[killer.EntityId] = CombatExperienceSettlement.WithExperience(killer, CharacterExperienceFacts.ForAlly(30));
            var events = new List<CombatEvent>();
            var result = CombatExperienceSettlement.Settle(input, projected, events, Victory(), Array.Empty<CreditedKill>(),
                new CombatRewardFacts(true, cohort, 30), input.Battle.ClearRewards);
            Assert.That(result.Owned.Count, Is.EqualTo(subs + 1));
            Assert.That(events.OfType<CombatExperienceEvent>().Select(e => e.Amount).OrderBy(x => x),
                Is.EqualTo(Enumerable.Repeat(100, subs).Append(130).OrderBy(x => x)));
            var grownSub = result.Owned.Single(c => c.Final.Id == killer.OwnedId.Value).Final;
            Assert.That((grownSub.Level, grownSub.Experience), Is.EqualTo((2, 30)));
            Assert.That(projected[killer.EntityId].Experience.Earned, Is.EqualTo(30));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DeadOrReturnedParticipant_StillGetsClear_OffCohortKillerGetsOnlyKill(bool dead)
        {
            var input = Input(1);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var participant = projected.Values.First(c => c.OwnershipRole == CharacterOwnershipRole.Sub);
            projected[participant.EntityId] = dead ? CombatCommandTestData.With(participant, hp: 0) :
                new CharacterCombatFacts(participant.EntityId, participant.OwnedId, participant.CharacterKey, participant.Owner, participant.OwnershipRole, CharacterRuntimeArea.Hand, participant.Health, participant.Stats, 0, null, null, Array.Empty<CellCoord>(), participant.ActionUse);
            var killer = projected.Values.First(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            projected[killer.EntityId] = CombatExperienceSettlement.WithExperience(killer, CharacterExperienceFacts.ForAlly(30));
            var events = new List<CombatEvent>();
            var result = CombatExperienceSettlement.Settle(input, projected, events, Victory(), Array.Empty<CreditedKill>(),
                new CombatRewardFacts(true, new[] { participant.OwnedId.Value }, 30), input.Battle.ClearRewards);
            Assert.That(result.Owned.Single(c => c.Final.Id == participant.OwnedId.Value).Final.Level, Is.EqualTo(2));
            Assert.That(events.OfType<CombatExperienceEvent>().Single(e => e.RecipientId == killer.EntityId).Amount, Is.EqualTo(30));
            Assert.That(projected[participant.EntityId].Experience.Earned, Is.Zero);
        }

        [Test]
        public void EmptyCohortAndDefeat_DoNotApplyClearExperience()
        {
            var input = Input(1);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            foreach (var outcome in new[] { Victory(), new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.PlayerMainCharacterDied) })
            {
                var result = CombatExperienceSettlement.Settle(input, projected, new List<CombatEvent>(), outcome,
                    Array.Empty<CreditedKill>(), outcome.Outcome == StageOutcome.Victory ? CombatRewardFacts.Empty :
                    new CombatRewardFacts(true, new[] { input.Owned[0].Id }, 0),
                    outcome.Outcome == StageOutcome.Victory ? input.Battle.ClearRewards : null);
                Assert.That(result.Owned, Is.Empty);
            }
        }

        [Test]
        public void ClearExperienceOverflowAndInvalidParticipant_FailBeforeReturningProposal()
        {
            var input = Input(0);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var main = projected.Values.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            projected[main.EntityId] = CombatExperienceSettlement.WithExperience(main, CharacterExperienceFacts.ForAlly(int.MaxValue));
            Assert.Throws<OverflowException>(() => CombatExperienceSettlement.Settle(input, projected, new List<CombatEvent>(), Victory(),
                Array.Empty<CreditedKill>(), new CombatRewardFacts(true, new[] { main.OwnedId.Value }, 0), input.Battle.ClearRewards));
            Assert.Throws<InvalidOperationException>(() => CombatExperienceSettlement.Settle(input, projected, new List<CombatEvent>(), Victory(),
                Array.Empty<CreditedKill>(), new CombatRewardFacts(true, new[] { new CharacterInstanceId(99999) }, 0), input.Battle.ClearRewards));
        }

        [Test]
        public void FinalizedRows_ShareTheAppliedSplit_AndKeepNonparticipantKillExperience()
        {
            var input = Input(2);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var subs = input.Characters.Where(c => c.OwnershipRole == CharacterOwnershipRole.Sub).Take(2).ToArray();
            foreach (var sub in subs)
                projected[sub.EntityId] = CombatExperienceSettlement.WithExperience(sub, CharacterExperienceFacts.ForAlly(10));
            var main = input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var participants = new[] { main.OwnedId.Value, subs[0].OwnedId.Value };
            var events = new List<CombatEvent>();

            var result = CombatExperienceSettlement.Settle(input, projected, events, Victory(), Array.Empty<CreditedKill>(),
                new CombatRewardFacts(true, participants, 20), input.Battle.ClearRewards);

            Assert.That(result.Characters[0].CharacterId, Is.EqualTo(main.OwnedId.Value));
            Assert.That(result.Characters.Select(c => (c.CharacterId, c.KillExperience, c.ClearExperience)), Is.EquivalentTo(new[] {
                (main.OwnedId.Value, 0, 100), (subs[0].OwnedId.Value, 10, 100), (subs[1].OwnedId.Value, 10, 0) }));
            foreach (var row in result.Characters)
            {
                var applied = events.OfType<CombatExperienceEvent>().Single(e => e.Owned.Final.Id == row.CharacterId);
                Assert.That(applied.Amount, Is.EqualTo(row.TotalExperience));
                Assert.That((row.LevelAfter, row.ExperienceAfter), Is.EqualTo((applied.Owned.Final.Level, applied.Owned.Final.Experience)));
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ZeroOrDisabledProgression_PreservesPortraitRowsWithoutApplyingExperience(bool disabled)
        {
            var source = Input(1);
            var input = new CombatCommandInput(source.Number, source.Kind, source.ActorId, source.Aim, source.State,
                source.Characters, source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                battle: new CombatBattleFacts(4, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0,
                    progression: disabled ? default : source.Battle.Progression, clearRewards: source.Battle.ClearRewards),
                expectedBattleInstanceId: source.ExpectedBattleInstanceId, expectedControllerTurnSequence: source.ExpectedControllerTurnSequence,
                battleInstanceId: source.BattleInstanceId);
            var events = new List<CombatEvent>();
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var result = CombatExperienceSettlement.Settle(input, projected, events, Victory(), Array.Empty<CreditedKill>(),
                new CombatRewardFacts(true, input.Characters.Where(c => c.Area == CharacterRuntimeArea.Field)
                    .Select(c => c.OwnedId.Value).ToArray(), 0),
                disabled ? input.Battle.ClearRewards : new StageClearRewardSpec(0, 0, 0));

            Assert.That(result.Characters.Count, Is.EqualTo(2));
            Assert.That(result.Characters.All(c => c.TotalExperience == 0 && c.LevelBefore == c.LevelAfter &&
                c.ExperienceBefore == c.ExperienceAfter && c.RequiredExperienceBefore == (disabled ? 0 :
                    c.CharacterKey == EntityKey.Unit_Gravekeeper ? 150 : 100)), Is.True);
            Assert.That(result.Owned, Is.Empty);
            Assert.That(result.GrownMain, Is.Null);
            Assert.That(events, Is.Empty);
        }

        [Test]
        public void ReceiptCopiesNestedCollections_AndChecksTotalsAndIdentities()
        {
            var ids = new[] { new CharacterInstanceId(1) };
            var entries = new[] { new CharacterRewardReceipt(ids[0], EntityKey.Unit_Gravekeeper, 500, 100, 1, 0, 150, 3, 150, 450) };
            var receipt = new BattleRewardReceipt(1, 2, 0, 300, 100, 50, ids, entries);
            ids[0] = new CharacterInstanceId(2); entries[0] = null;
            Assert.That(receipt.TotalGoldEarned, Is.EqualTo(400));
            Assert.That(receipt.Characters[0].TotalExperience, Is.EqualTo(600));
            Assert.That($"{receipt.StageDepth}-{receipt.StageWidthIndex + 1}", Is.EqualTo("2-1"));
            Assert.That(receipt.ClearParticipantIds[0].Value, Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(() => ((IList<CharacterRewardReceipt>)receipt.Characters).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<CharacterInstanceId>)receipt.ClearParticipantIds).Clear());
            Assert.Throws<OverflowException>(() => new BattleRewardReceipt(1, 1, 0, int.MaxValue, 1, 0,
                Array.Empty<CharacterInstanceId>(), Array.Empty<CharacterRewardReceipt>()));
            Assert.Throws<ArgumentException>(() => new BattleRewardReceipt(1, 1, 0, 0, 0, 0, ids, Array.Empty<CharacterRewardReceipt>()));
        }

        private static CombatCommandInput Input(int subs)
        {
            var s = CombatRewardParticipationTests.Deployed(subs);
            return new CombatCommandInput(s.Number, s.Kind, s.ActorId, s.Aim, s.State, s.Characters, s.Profiles, s.Main,
                s.Owned, s.SoulCounts, s.Souls, s.Artifacts, battle: new CombatBattleFacts(4, 37, 0, 0,
                    Array.Empty<CharacterInstanceId>(), 0, progression: new LevelProgressionSpec(150, 100, 3, 1, 1),
                    clearRewards: new StageClearRewardSpec(100, 50, 100)),
                expectedBattleInstanceId: s.ExpectedBattleInstanceId, expectedControllerTurnSequence: s.ExpectedControllerTurnSequence,
                battleInstanceId: s.BattleInstanceId);
        }
        private static StageOutcomeSnapshot Victory() => new(StageOutcome.Victory, StageOutcomeReason.DesignatedBossDefeated);
    }
}
