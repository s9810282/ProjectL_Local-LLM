using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Experience;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 경험치·레벨업 설계 §3.2·§3.3: 처치 경험치는 가해 아군의 이번 전투 획득량에 쌓이고, 승리 단계에서만 보유 기록에 반영된다.
    /// </summary>
    public sealed class CombatExperienceSettlementTests
    {
        private static readonly LevelProgressionSpec Spec = new(150, 100, 10, 3, 1, 1);
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();
        private static readonly EntityId Actor = new(1);
        private static readonly EntityId Victim = new(2);

        [Test]
        public void AttackKill_AccruesFixedExperienceToAttacker_WithoutChangingRecord()
        {
            var input = Battle(KillInput(enemyLevel: 3));

            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, Actor).Experience.Earned, Is.EqualTo(10));
            Assert.That(result.Characters.Owned, Is.Empty, "승리 전에는 보유 기록이 바뀌지 않는다");
            var earned = result.Events.OfType<CombatExperienceEvent>().Single();
            Assert.That((earned.Kind, earned.VictimId, earned.RecipientId, earned.Amount),
                Is.EqualTo((CombatEventKind.ExperienceEarned, (EntityId?)Victim, Actor, 10)));
        }

        [Test]
        public void DisabledOrMissingBattleDefinition_ChangesNothing()
        {
            var withoutBattle = CombatCommandCalculator.Calculate(KillInput(enemyLevel: 3), NoRules);
            var disabled = CombatCommandCalculator.Calculate(Battle(KillInput(enemyLevel: 3), default(LevelProgressionSpec)), NoRules);

            foreach (var result in new[] { withoutBattle, disabled })
            {
                Assert.That(Final(result, Actor).Experience.Earned, Is.Zero);
                Assert.That(result.Events.OfType<CombatExperienceEvent>(), Is.Empty);
            }
        }

        [Test]
        public void VictoryKill_AppliesAccruedExperienceToOwnedRecord()
        {
            // 이미 100을 번 서브가 고정 경험치 10인 적을 처치해 승리한다. 110 = 100(1→2) + 10.
            var source = KillInput(enemyLevel: 4, earnedBefore: 100);
            var input = Battle(source, objective: BattleObjective.DesignatedBoss(Victim));

            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            var change = result.Characters.Owned.Single();
            Assert.That(change.Expected, Is.SameAs(input.Owned.Single(o => o.Id == ActorOwnedId(input))));
            Assert.That((change.Final.Level, change.Final.Experience), Is.EqualTo((2, 10)));
            var applied = result.Events.OfType<CombatExperienceEvent>().Single(e => e.Kind == CombatEventKind.ExperienceApplied);
            Assert.That((applied.RecipientId, applied.Amount, applied.LevelsGained), Is.EqualTo((Actor, 110, 1)));
            Assert.That(Final(result, Actor).Experience.Earned, Is.EqualTo(110), "번 양은 전투 기록으로 남는다");
        }

        [Test]
        public void EnemyTurnReflectionKill_GoesToStruckAlly()
        {
            var input = Battle(ThornHeartTestData.Input(hp: 2));

            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(ThornHeartTestData.Final(result, 1).Health.IsDead, Is.True);
            Assert.That(ThornHeartTestData.Final(result, 2).Experience.Earned, Is.EqualTo(10));
            Assert.That(result.Events.OfType<CombatExperienceEvent>().Single().RecipientId, Is.EqualTo(new EntityId(2)));
        }

        [Test]
        public void Preview_RunsSettlementWithoutChangingPreviewValues()
        {
            var off = CombatCommandCalculator.Preview(KillInput(enemyLevel: 3), NoRules);
            var on = CombatCommandCalculator.Preview(Battle(KillInput(enemyLevel: 3)), NoRules, out var projection);

            // 미리보기 투영도 같은 정산을 거친다(설계 §3.3). 결과는 설치하지 않는다.
            Assert.That(Final(projection, Actor).Experience.Earned, Is.EqualTo(10));
            Assert.That(on.CanAttack, Is.True);
            Assert.That(on.CanAttack, Is.EqualTo(off.CanAttack));
            Assert.That(on.Targets.ToArray().Select(t => (t.TargetId, t.ExpectedDamage, t.TargetHealthAfter, t.TargetKilled)),
                Is.EqualTo(off.Targets.ToArray().Select(t => (t.TargetId, t.ExpectedDamage, t.TargetHealthAfter, t.TargetKilled))));
        }

        // 가해자 규칙(설계 §3.2): 기록된 가해자 → 반사의 맞은 아군 → 플레이어 명령의 행동자.
        [TestCase(true, CombatProcessingKind.Command, 1, 10, TestName = "Credit_RecordedAllyAttacker")]
        [TestCase(false, CombatProcessingKind.Command, 1, 10, TestName = "Credit_NoRecord_PlayerCommandActor")]
        [TestCase(false, CombatProcessingKind.Advance, 0, 0, TestName = "Credit_NoRecord_AutomaticProgress_Nobody")]
        [TestCase(false, CombatProcessingKind.Command, 0, 0, TestName = "Credit_NoRecord_EmptyActor_Nobody")]
        public void Settle_CreditRules(bool credited, CombatProcessingKind kind, int actorId, int expectedActorEarned)
        {
            var (input, projected, events) = ManualKill(credited ? Actor : (EntityId?)null, kind,
                actorId == 0 ? EntityId.Empty : Actor);

            var owned = Settle(input, projected, events, InProgress()).Owned;

            Assert.That(owned, Is.Empty);
            Assert.That(projected[Actor].Experience.Earned, Is.EqualTo(expectedActorEarned));
        }

        [Test]
        public void Settle_RecordedEnemyAttacker_GivesNobodyEvenInPlayerCommand()
        {
            var enemyAttacker = new EntityId(3);
            var (input, projected, events) = ManualKill(enemyAttacker, CombatProcessingKind.Command, Actor);

            Settle(input, projected, events, InProgress());

            Assert.That(projected.Values.Sum(c => c.Experience.Earned), Is.Zero);
            Assert.That(events.OfType<CombatExperienceEvent>(), Is.Empty);
        }

        [Test]
        public void Settle_KilledEnemyWithoutDeathEvent_Throws()
        {
            var (input, projected, events) = ManualKill(Actor, CombatProcessingKind.Command, Actor);
            events.RemoveAll(e => e.Kind == CombatEventKind.Died);

            Assert.Throws<InvalidOperationException>(() =>
                Settle(input, projected, events, InProgress()));
        }

        [Test]
        public void Settle_DuplicateDeathEvent_ThrowsWithVictimId()
        {
            var (input, projected, events) = ManualKill(Actor, CombatProcessingKind.Command, Actor);
            events.Add(events.Single());

            var error = Assert.Throws<InvalidOperationException>(() =>
                Settle(input, projected, events, InProgress()));
            Assert.That(error.Message, Does.Contain("2개"));
        }

        [Test]
        public void Settle_VictoryGrowsMainOncePerReachedLevel_KeepingCurrentHealth()
        {
            // 메인이 160을 번 상태에서 직접 처치(10)해 승리한다. 170 = 150(1→2) + 20, 성장 하나.
            var main = new EntityId(10);
            var (input, projected, events) = ManualKill(main, CombatProcessingKind.Command, Actor);
            projected[main] = CombatExperienceSettlement.WithExperience(projected[main], CharacterExperienceFacts.ForAlly(160));
            var mainOwned = input.Owned.Single(o => o.Role == CharacterOwnershipRole.Main);

            var settled = Settle(input, projected, events,
                new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.DesignatedBossDefeated));

            var kind = MainGrowthRules.Choose(37, mainOwned.Id, 2);
            var grown = settled.GrownMain;
            Assert.That((settled.Owned.Single().Final.Level, settled.Owned.Single().Final.Experience), Is.EqualTo((2, 20)));
            Assert.That((grown.PersistentMaxHealth, grown.PersistentAttack, grown.PersistentDefense), Is.EqualTo((
                kind == MainGrowthKind.MaxHealth ? 103 : 100, kind == MainGrowthKind.Attack ? 2 : 1,
                kind == MainGrowthKind.Defense ? 1 : 0)));
            Assert.That(grown.CurrentHealth, Is.EqualTo(100), "현재 체력 유지(X4)");
            var facts = projected[main];
            Assert.That((facts.Stats.AttackPower, facts.Stats.PhysicalDefense, facts.Health.MaxHealth, facts.Health.CurrentHealth),
                Is.EqualTo((grown.PersistentAttack, grown.PersistentDefense, grown.PersistentMaxHealth, 100)),
                "메인 전투 사실은 성장한 지속 상태와 같다");
            var applied = events.OfType<CombatExperienceEvent>().Single(e => e.Kind == CombatEventKind.ExperienceApplied);
            Assert.That(applied.MainGrowth, Is.EqualTo(new[] { kind }));
        }

        [Test]
        public void Settle_DamagedMainInVictoryStep_GrowsFromItsCurrentHealth()
        {
            // 승리 단계에서 메인이 피해를 입어 체력이 지속 상태(100)와 다르다(70). 성장은 그 체력을 유지한 채 더해진다.
            var main = new EntityId(10);
            var (input, projected, events) = ManualKill(main, CombatProcessingKind.Command, Actor);
            var hurt = CombatCommandCalculator.Hurt(projected[main], 30);
            projected[main] = CombatExperienceSettlement.WithExperience(hurt, CharacterExperienceFacts.ForAlly(160));
            var mainOwned = input.Owned.Single(o => o.Role == CharacterOwnershipRole.Main);

            var settled = Settle(input, projected, events,
                new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.DesignatedBossDefeated));

            var kind = MainGrowthRules.Choose(37, mainOwned.Id, 2);
            Assert.That(settled.GrownMain.CurrentHealth, Is.EqualTo(70));
            Assert.That(projected[main].Health.CurrentHealth, Is.EqualTo(70));
            Assert.That(projected[main].Health.MaxHealth, Is.EqualTo(kind == MainGrowthKind.MaxHealth ? 103 : 100));
            Assert.That(events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.HealthChanged &&
                e.Final.EntityId == main), Is.EqualTo(kind == MainGrowthKind.MaxHealth), "최대 체력이 바뀔 때만 체력 사건");
        }

        [TestCase(CombatCommandKind.Attack)]
        [TestCase(CombatCommandKind.EndPlayerTurn)]
        public void AfterVictory_FurtherCommandsAreRejected_SoApplicationHappensOnce(CombatCommandKind kind)
        {
            var input = Battle(KillInput(enemyLevel: 4, earnedBefore: 100), objective: BattleObjective.DesignatedBoss(Victim));
            var victory = CombatCommandCalculator.Calculate(input, NoRules);
            Assert.That(victory.Characters.Owned.Count, Is.EqualTo(1));

            var next = CombatCommandCalculator.Calculate(CombatCommandComposition.Intent(CombatCommandComposition.Project(victory),
                kind, AttackAim.ForCell(new CellCoord(4, 4))), NoRules);

            Assert.That(next.Accepted, Is.False, "종료 국면의 명령은 거절된다");
            Assert.That(next.Characters.Owned, Is.Empty);
        }

        [Test]
        public void VictoryCommand_PublishesGrownMainPersistentState()
        {
            // 서브가 처치해 승리하고, 이미 150을 번 메인도 같은 단계에서 레벨업·성장한다. 종료 정산은 성장한 상태에서 시작한다.
            var source = KillInput(enemyLevel: 1);
            var mainId = new EntityId(10);
            var characters = source.Characters.Select(c => c.EntityId == mainId
                ? CharacterExperienceFactsTests.With(c, CharacterExperienceFacts.ForAlly(150)) : c).ToArray();
            var input = Battle(CombatCommandTestData.With(source, characters: characters),
                objective: BattleObjective.DesignatedBoss(Victim));

            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Victory));
            var mainOwned = input.Owned.Single(o => o.Role == CharacterOwnershipRole.Main);
            var kind = MainGrowthRules.Choose(37, mainOwned.Id, 2);
            var persistent = result.Characters.Main.Final;
            Assert.That((persistent.PersistentMaxHealth, persistent.PersistentAttack, persistent.PersistentDefense), Is.EqualTo((
                kind == MainGrowthKind.MaxHealth ? 103 : 100, kind == MainGrowthKind.Attack ? 2 : 1,
                kind == MainGrowthKind.Defense ? 1 : 0)));
            Assert.That(result.Characters.Owned.Select(o => o.Final.Role), Is.EquivalentTo(new[]
                { CharacterOwnershipRole.Sub, CharacterOwnershipRole.Main }));
        }

        [Test]
        public void Settle_AlreadyDeadEnemy_IsNotKilledAgain()
        {
            var (input, projected, events) = ManualKill(Actor, CombatProcessingKind.Command, Actor);
            var deadAtInput = CombatCommandCalculator.Hurt(input.Characters.Single(c => c.EntityId == Victim), 1000);
            input = CombatCommandTestData.With(input, characters: input.Characters
                .Select(c => c.EntityId == Victim ? deadAtInput : c).ToArray());

            Settle(input, projected, events, InProgress());

            Assert.That(projected[Actor].Experience.Earned, Is.Zero);
        }

        [Test]
        public void Settle_VictoryAppliesToDeadAllyCard_AndMultipleLevels()
        {
            // 처치한 아군이 같은 단계에서 죽어도 번 경험치는 보유 카드로 간다. 350 = 100 + 200 + 50.
            var (input, projected, events) = ManualKill(Actor, CombatProcessingKind.Command, Actor);
            projected[Actor] = CombatExperienceSettlement.WithExperience(
                CombatCommandCalculator.Hurt(projected[Actor], 1000), CharacterExperienceFacts.ForAlly(340));

            var settled = Settle(input, projected, events,
                new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.DesignatedBossDefeated));

            var change = settled.Owned.Single();
            Assert.That((change.Final.Level, change.Final.Experience), Is.EqualTo((3, 50)));
            Assert.That(events.OfType<CombatExperienceEvent>().Single(e => e.Kind == CombatEventKind.ExperienceApplied)
                .LevelsGained, Is.EqualTo(2));
        }

        [Test]
        public void Settle_DefeatOrInProgress_DoesNotApply()
        {
            var (input, projected, events) = ManualKill(Actor, CombatProcessingKind.Command, Actor);

            var settled = Settle(input, projected, events,
                new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.PlayerMainCharacterDied));

            Assert.That(settled.Owned, Is.Empty);
            Assert.That(settled.GrownMain, Is.Null);
            Assert.That(projected[Actor].Experience.Earned, Is.EqualTo(10));
        }

        /// <summary>계산기 없이 적 하나(레벨 3)가 죽은 투영과 사망 사건을 만든다. 가해자 기록 유무와 행동자를 정한다.</summary>
        private static (CombatCommandInput Input, Dictionary<EntityId, CharacterCombatFacts> Projected, List<CombatEvent> Events)
            ManualKill(EntityId? creditedAttacker, CombatProcessingKind kind, EntityId actor)
        {
            var source = KillInput(enemyLevel: 3);
            var enemyAttacker = CombatCommandTestData.Character(3, EntityKey.Unit_Demon, new CellCoord(6, 6), Owner.Enemy);
            var characters = source.Characters.Where(c => c.EntityId != new EntityId(3)).Append(enemyAttacker).ToArray();
            var input = new CombatCommandInput(kind == CombatProcessingKind.Command ? 1 : 0, source.Kind, actor, source.Aim,
                source.State, characters, source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls,
                source.Artifacts, battle: BattleFacts(Spec), processingKind: kind,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (source.State).Turn.ControllerTurnSequence);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var victim = projected[Victim];
            var dead = CombatCommandCalculator.Hurt(victim, 1000);
            projected[Victim] = dead;
            var origin = new CombatDamageOrigin(victim.Anchor, creditedAttacker, 1);
            var events = new List<CombatEvent>
            {
                new CombatCharacterEvent(CombatEventKind.Died, victim, dead, creditedAttacker.HasValue ? new[] { origin } : null)
            };
            return (input, projected, events);
        }

        /// <summary>행동자(서브, 공격 20)가 체력 1인 적(Victim)을 공격하는 입력이다.</summary>
        private static CombatCommandInput KillInput(int enemyLevel, int earnedBefore = 0)
        {
            var source = CombatCommandTestData.Input();
            var characters = source.Characters.Select(c =>
                c.EntityId == Victim
                    ? CharacterExperienceFactsTests.With(CombatCommandTestData.With(c, hp: 1), CharacterExperienceFacts.ForEnemy(enemyLevel))
                    : c.EntityId == Actor && earnedBefore != 0
                        ? CharacterExperienceFactsTests.With(c, CharacterExperienceFacts.ForAlly(earnedBefore))
                        : c).ToArray();
            return CombatCommandTestData.With(source, characters: characters);
        }

        private static CombatCommandInput Battle(CombatCommandInput i, LevelProgressionSpec? spec = null,
            BattleObjective objective = null)
        {
            var s = i.State;
            var state = objective == null ? s : new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,                s.IsNight,s.Structures,s.ThornFields,s.Turn,s.Environment,s.EnvironmentProgress,                s.WallBlocking,objective,s.Allocation,s.ItemRandomState);
            return new CombatCommandInput(i.Number, i.Kind, i.ActorId, i.Aim, state, i.Characters, i.Profiles, i.Main,
                i.Owned, i.SoulCounts, i.Souls, i.Artifacts, i.TargetCell, i.Rotation, BattleFacts(spec ?? Spec),
                i.ProcessingKind, i.Path, i.MovementContext, i.BattleInstanceId, i.ArtifactItems, i.EnvironmentBindings,
                i.ObstacleContents, i.ItemKey, i.ItemInventory, i.ItemDefinitions,
                expectedBattleInstanceId: i.ExpectedBattleInstanceId, expectedControllerTurnSequence: i.ExpectedControllerTurnSequence);
        }

        private static CombatBattleFacts BattleFacts(LevelProgressionSpec spec) =>
            new(10, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0, progression: spec);

        private static CharacterInstanceId ActorOwnedId(CombatCommandInput input) =>
            input.Characters.Single(c => c.EntityId == Actor).OwnedId.Value;

        private static CharacterCombatFacts Final(CombatCommandTransition result, EntityId id) =>
            CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == id);

        private static ExperienceSettlement Settle(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> projected, List<CombatEvent> events, StageOutcomeSnapshot outcome) =>
            CombatExperienceSettlement.Settle(input, projected, events, outcome,
                ConfirmedKillCalculator.Collect(input, projected, events), input.State.Rewards,
                outcome.Outcome == StageOutcome.Victory ? input.Battle?.ClearRewards : null);

        private static StageOutcomeSnapshot InProgress() => new(StageOutcome.InProgress, StageOutcomeReason.None);
    }
}
