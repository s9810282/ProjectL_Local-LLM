using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Experience;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards;
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
    /// 유닛별 스테이지 기록 설계 D3·D4·Q-S1~Q-S6: 명령 계산이 보유 아군의 전투 사실에 네 값을 실제 경로대로 더하는지 확인한다.
    /// 피해량은 계산 규칙에 기대지 않도록 입력과 결과의 체력 차이로 비교한다.
    /// </summary>
    public sealed class CombatStageRecordTests
    {
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [Test]
        public void Attack_AddsEnemyHealthLossToTheAttacker_OverkillExcluded_AndCountsTheKill()
        {
            // 중심 적(체력 5)은 넘친 피해로 죽고, 둘레의 두 적은 산다(설계 Q-S1·Q-S5).
            var seed = CombatCommandTestData.Input();
            var input = CombatCommandTestData.With(seed, seed.Characters
                .Select(c => c.EntityId.Value == 2 ? CombatCommandTestData.With(c, hp: 5) : c).ToArray());

            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, 2).Health.IsDead, Is.True);
            var actor = Final(result, 1).StageRecord;
            Assert.That(actor.DamageDealt, Is.EqualTo(EnemyHealthLoss(input, result)));
            Assert.That(actor.DamageDealt, Is.GreaterThan(5));
            Assert.That(actor.Kills, Is.EqualTo(1));
            Assert.That(actor.DamageTaken, Is.Zero);
            Assert.That(actor.DistanceMoved, Is.Zero);
            Assert.That(Final(result, 10).StageRecord.IsEmpty, Is.True);
            Assert.That(Finals(result).Where(c => c.Owner == Owner.Enemy).All(c => c.StageRecord.IsEmpty), Is.True);
        }

        [Test]
        public void KillOfAnExcludedRewardEnemy_KeepsTheDamage_ButIsNotAKill()
        {
            var seed = CombatCommandTestData.Input();
            var characters = seed.Characters.Select(c => c.EntityId.Value == 2 ? CombatCommandTestData.With(c, hp: 5) : c).ToArray();
            var profiles = seed.Profiles.Select(p => p.Definition.Key != EntityKey.Unit_Demon ? p :
                new CombatAttackProfile(WithReward(p.Definition, UnitKillRewardSpec.Excluded), p.Method)).ToArray();
            var input = CombatCommandTestData.With(seed, characters, profiles: profiles);

            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, 2).Health.IsDead, Is.True);
            var actor = Final(result, 1).StageRecord;
            Assert.That(actor.Kills, Is.Zero);
            Assert.That(actor.DamageDealt, Is.EqualTo(EnemyHealthLoss(input, result)));
        }

        [TestCase(100)]
        [TestCase(5)]
        public void EnemyAttack_AddsTheAlliesActualHealthLossAsTaken(int mainHealth)
        {
            var input = EnemyAttack(mainHealth);

            var result = CombatCommandCalculator.Advance(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            var before = input.Characters.Single(c => c.EntityId.Value == 10);
            var main = Final(result, 10);
            int loss = before.Health.CurrentHealth - main.Health.CurrentHealth;
            Assert.That(loss, Is.GreaterThan(0));
            Assert.That(main.StageRecord.DamageTaken, Is.EqualTo(loss));
            Assert.That(main.StageRecord.DamageDealt, Is.Zero);
            if (mainHealth == 5) Assert.That(main.StageRecord.DamageTaken, Is.EqualTo(5));
            Assert.That(Final(result, 1).StageRecord.IsEmpty, Is.True);
        }

        [Test]
        public void PlacementMineKill_CreditsTheKillAndTheDamageToTheSameAlly()
        {
            // 출처에 가해자가 없는 환경 피해도 처치와 같은 귀속 규칙(명령 행동자)으로 넣은 피해가 된다(설계 D4).
            // 지뢰가 놓인 아군 자신에게 준 피해는 받은 피해일 뿐 넣은 피해가 아니다(Q-S2).
            var i = CombatCommonCommandTests.Command(CombatCommandKind.PlaceUnit);
            var hand = CombatPlacementCalculator.Location(i.Characters[0], CharacterRuntimeArea.Hand, 0, null, null);
            var cell = new CellCoord(2, 0);
            var enemy = CombatCommandTestData.With(CombatCommandTestData.Character(30, EntityKey.Unit_Demon,
                new CellCoord(3, 0), Owner.Enemy), hp: 10);
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                i.State.Structures,i.State.ThornFields,new CombatTurnFacts(StagePhase.Deployment),                new MovementEnvironmentFacts(new[] { MineEnvironmentTests.Mine(cell: cell) }));
            var input = new CombatCommandInput(1, i.Kind, i.ActorId, default, state, new[] { hand, i.Characters[1], enemy },
                i.Profiles, i.Main, i.Owned, i.SoulCounts, i.Souls, i.Artifacts, targetCell: cell,
                battle: new CombatBattleFacts(10, 37, 2, 2, Array.Empty<CharacterInstanceId>(), 0),
                environmentBindings: ObjectArrivalOrderTests.Bindings(11),
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: state.Turn.ControllerTurnSequence);

            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result, enemy.EntityId.Value).Health.IsDead, Is.True);
            var placed = Final(result, input.ActorId.Value);
            Assert.That(placed.StageRecord.Kills, Is.EqualTo(1));
            Assert.That(placed.StageRecord.DamageDealt, Is.EqualTo(10));
            Assert.That(placed.StageRecord.DamageTaken, Is.EqualTo(hand.Health.CurrentHealth - placed.Health.CurrentHealth));
        }

        [Test]
        public void Move_AddsTheEnteredPathCellsToTheMover()
        {
            var input = CombatCommandComposition.Intent(CombatCommandTestData.Input(empty: true), CombatCommandKind.Move,
                path: new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(1, 4), new CellCoord(2, 4) }));

            var result = CombatCommandCalculator.Calculate(input, NoRules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.PathMovement.DistanceSpent, Is.EqualTo(2));
            Assert.That(Final(result, 1).StageRecord.DistanceMoved, Is.EqualTo(2));
            Assert.That(Finals(result).Where(c => c.EntityId.Value != 1).All(c => c.StageRecord.DistanceMoved == 0), Is.True);
        }

        [Test]
        public void FallDeath_IsNotTakenDamage()
        {
            // 추락은 피해 적용 경계를 지나지 않는다(설계 Q-S3).
            var seed = CombatCommandTestData.Input();
            var cell = new CellCoord(1, 3);
            var input = CliffFallTests.WithEnvironment(seed, CliffFallTests.Ground(cell: cell));
            var projection = CliffFallTests.Projection(input);

            Assert.That(CliffFallCalculator.ResolveAt(projection, new EntityId(1), cell, false), Is.True);
            projection.ResolveDeathBoundary();
            var result = projection.Complete();

            var fallen = Final(result, 1);
            Assert.That(fallen.Health.IsDead, Is.True);
            Assert.That(fallen.StageRecord.IsEmpty, Is.True);
        }

        [Test]
        public void VictoryReceipt_CopiesEachRecord_AndAddsRowsForNonparticipantsWithARecord()
        {
            var s = CombatRewardParticipationTests.Deployed(2);
            var input = new CombatCommandInput(s.Number, s.Kind, s.ActorId, s.Aim, s.State, s.Characters, s.Profiles, s.Main,
                s.Owned, s.SoulCounts, s.Souls, s.Artifacts, battle: new CombatBattleFacts(4, 37, 0, 0,
                    Array.Empty<CharacterInstanceId>(), 0, progression: new LevelProgressionSpec(150, 100, 3, 1, 1),
                    clearRewards: new StageClearRewardSpec(100, 50, 100)),
                expectedBattleInstanceId: s.ExpectedBattleInstanceId, expectedControllerTurnSequence: s.ExpectedControllerTurnSequence,
                battleInstanceId: s.BattleInstanceId);
            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var main = input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var subs = input.Characters.Where(c => c.OwnershipRole == CharacterOwnershipRole.Sub).Take(2).ToArray();
            var mainRecord = CharacterStageRecord.Empty.Add(damageTaken: 4, distanceMoved: 2);
            var outsiderRecord = CharacterStageRecord.Empty.Add(damageDealt: 7);
            projected[main.EntityId] = main.With(stageRecord: mainRecord);
            projected[subs[1].EntityId] = subs[1].With(stageRecord: outsiderRecord);

            var result = CombatExperienceSettlement.Settle(input, projected, new List<CombatEvent>(),
                new StageOutcomeSnapshot(StageOutcome.Victory, StageOutcomeReason.DesignatedBossDefeated), Array.Empty<CreditedKill>(),
                new CombatRewardFacts(true, new[] { main.OwnedId.Value, subs[0].OwnedId.Value }, 0), input.Battle.ClearRewards);

            Assert.That(Row(result, main).StageRecord.SameValues(mainRecord), Is.True);
            Assert.That(Row(result, subs[0]).StageRecord.IsEmpty, Is.True);
            var outsider = Row(result, subs[1]);
            Assert.That(outsider.StageRecord.SameValues(outsiderRecord), Is.True);
            Assert.That((outsider.KillExperience, outsider.ClearExperience), Is.EqualTo((0, 0)));
        }

        private static CharacterRewardReceipt Row(ExperienceSettlement result, CharacterCombatFacts facts) =>
            result.Characters.Single(r => r.CharacterId == facts.OwnedId.Value);

        private static IEnumerable<CharacterCombatFacts> Finals(CombatCommandTransition result) =>
            CombatCommandComposition.Project(result).Characters;

        private static CharacterCombatFacts Final(CombatCommandTransition result, int id) =>
            Finals(result).Single(c => c.EntityId == new EntityId(id));

        /// <summary>적이 이 명령에서 잃은 체력의 합이다. 죽은 적은 남은 체력까지만 잃는다.</summary>
        private static int EnemyHealthLoss(CombatCommandInput input, CombatCommandTransition result) =>
            input.Characters.Where(c => c.Owner == Owner.Enemy)
                .Sum(c => c.Health.CurrentHealth - Final(result, c.EntityId.Value).Health.CurrentHealth);

        /// <summary>적(공격력 20) 하나가 메인 하나를 치는 자동 진행 입력이다(CombatCommonCommandTests의 자동 진행 입력과 같은 구성).</summary>
        private static CombatCommandInput EnemyAttack(int mainHealth)
        {
            var source = CombatCommonCommandTests.Command(CombatCommandKind.Attack);
            var actor = CombatCommandTestData.Character(1, EntityKey.Unit_Akimbo, new CellCoord(1, 3), Owner.Enemy, 20);
            var main = CombatCommandTestData.With(source.Characters[1], hp: mainHealth, position: new CellCoord(2, 3));
            var profiles = source.Profiles.ToArray();
            profiles[0] = new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20),
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                controllerTurnSequence: 2, completedPlayerTurnCount: 1);
            var state = source.State.With(phase: CombatFlowPhase.Automatic,turn: turn);
            return new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, actor.EntityId, default,
                state, new[] { actor, main }, profiles, source.Main, source.Owned, source.SoulCounts, source.Souls,
                source.Artifacts, battle: source.Battle, processingKind: CombatProcessingKind.Advance,
                battleInstanceId: source.BattleInstanceId, expectedBattleInstanceId: source.BattleInstanceId,
                expectedControllerTurnSequence: state.Turn.ControllerTurnSequence);
        }

        private static UnitSpec WithReward(UnitSpec s, UnitKillRewardSpec reward) =>
            new(s.Key, s.Role, s.FootprintWidth, s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth,
                s.MoveMethod, s.AttackRange, s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority, s.Traits.ToArray(),
                s.OwnerEligibility, s.Occlusion, s.Behavior, killReward: reward);
    }
}
