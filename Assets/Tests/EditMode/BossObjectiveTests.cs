using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    /// <summary>지정 보스 목표가 모든 판정 경로에서 같은 대상으로 승패를 정하는지 확인한다(계획 T1.1).</summary>
    public sealed class BossObjectiveTests
    {
        private static readonly EntityId Boss = new EntityId(2);

        [Test]
        public void DesignatedBossDeath_WithLivingSummons_Wins()
        {
            var input = CombatCommandTestData.Input();
            var characters = input.Characters.Select(c => c.EntityId == Boss ? CombatCommandCalculator.Hurt(c, 1000) : c).ToArray();

            var result = BattleOutcomeCalculator.Resolve(characters, Array.Empty<StructureCombatFacts>(), BattleObjective.DesignatedBoss(Boss));

            Assert.That(result.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(result.Reason, Is.EqualTo(StageOutcomeReason.DesignatedBossDefeated));
            Assert.That(characters.Count(c => c.Owner == Owner.Enemy && !c.Health.IsDead), Is.EqualTo(2));
        }

        [Test]
        public void SameKeyNonTargetDeath_StaysInProgress()
        {
            var input = CombatCommandTestData.Input();
            var other = new EntityId(3);
            Assert.That(input.Characters.Single(c => c.EntityId == other).CharacterKey,
                Is.EqualTo(input.Characters.Single(c => c.EntityId == Boss).CharacterKey));
            var characters = input.Characters.Select(c => c.EntityId == other ? CombatCommandCalculator.Hurt(c, 1000) : c).ToArray();

            var result = BattleOutcomeCalculator.Resolve(characters, Array.Empty<StructureCombatFacts>(), BattleObjective.DesignatedBoss(Boss));

            Assert.That(result.Outcome, Is.EqualTo(StageOutcome.InProgress));
        }

        [Test]
        public void HeadquartersDeath_DoesNotCompleteBossObjective()
        {
            var input = CombatCommandTestData.Input();
            var result = BattleOutcomeCalculator.Resolve(input.Characters, new[] { Headquarters(dead: true) },
                BattleObjective.DesignatedBoss(Boss));

            Assert.That(result.Outcome, Is.EqualTo(StageOutcome.InProgress));
        }

        [Test]
        public void HeadquartersObjective_Unchanged()
        {
            var input = CombatCommandTestData.Input();
            var characters = input.Characters.Select(c => c.EntityId == Boss ? CombatCommandCalculator.Hurt(c, 1000) : c).ToArray();

            Assert.That(BattleOutcomeCalculator.Resolve(characters, Array.Empty<StructureCombatFacts>(),
                BattleObjective.EnemyHeadquarters).Outcome, Is.EqualTo(StageOutcome.InProgress));
            var won = BattleOutcomeCalculator.Resolve(input.Characters, new[] { Headquarters(dead: true) },
                BattleObjective.EnemyHeadquarters);
            Assert.That(won.Outcome, Is.EqualTo(StageOutcome.Victory));
            Assert.That(won.Reason, Is.EqualTo(StageOutcomeReason.EnemyHeadquartersDestroyed));
        }

        [Test]
        public void MainAndBossDeath_DefeatHasPriority()
        {
            var input = CombatCommandTestData.Input();
            var main = input.Characters.Single(c => c.EntityId.Value == 10);
            var characters = input.Characters.Select(c => c.EntityId == Boss || c.EntityId == main.EntityId
                ? CombatCommandCalculator.Hurt(c, 1000) : c).ToArray();

            var result = BattleOutcomeCalculator.Resolve(characters, Array.Empty<StructureCombatFacts>(), BattleObjective.DesignatedBoss(Boss));

            Assert.That(result.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(result.Reason, Is.EqualTo(StageOutcomeReason.PlayerMainCharacterDied));
        }

        [Test]
        public void MissingTargetFact_ThrowsInsteadOfGuessing()
        {
            var input = CombatCommandTestData.Input(empty: true);
            Assert.Throws<InvalidOperationException>(() => BattleOutcomeCalculator.Resolve(input.Characters,
                Array.Empty<StructureCombatFacts>(), BattleObjective.DesignatedBoss(Boss)));
        }

        [Test]
        public void ObjectiveContract_RejectsInvalidCombinations()
        {
            Assert.Throws<ArgumentException>(() => BattleObjective.DesignatedBoss(EntityId.Empty));
            Assert.That(BattleObjective.EnemyHeadquarters.TargetEntityId.IsEmpty, Is.True);
            Assert.That(BattleObjective.DesignatedBoss(Boss).SameValues(BattleObjective.DesignatedBoss(Boss)), Is.True);
            Assert.That(BattleObjective.DesignatedBoss(Boss).SameValues(BattleObjective.EnemyHeadquarters), Is.False);
        }

        [Test]
        public void CommandState_ComparesObjective()
        {
            var state = CombatCommandTestData.Input().State;
            var boss = WithObjective(state, BattleObjective.DesignatedBoss(Boss));

            Assert.That(state.Objective.Kind, Is.EqualTo(BattleObjectiveKind.EnemyHeadquartersDestroyed));
            Assert.That(boss.SameValues(state), Is.False);
            Assert.That(boss.SameValues(WithObjective(state, BattleObjective.DesignatedBoss(Boss))), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void AttackPath_UsesObjective(bool killTarget)
        {
            var source = CombatCommandTestData.Input();
            var victim = new EntityId(2);
            var target = killTarget ? victim : new EntityId(3);
            var characters = source.Characters.Select(c => c.EntityId == victim ? CombatCommandTestData.With(c, hp: 1) : c).ToArray();
            var input = CombatCommandTestData.With(source, characters,
                WithObjective(source.State, BattleObjective.DesignatedBoss(target)));

            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == victim).Final.Health.IsDead, Is.True);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(killTarget ? StageOutcome.Victory : StageOutcome.InProgress));
            Assert.That(result.FinalState.Phase == CombatFlowPhase.Finished, Is.EqualTo(killTarget));
            Assert.That(result.FinalState.Objective.SameValues(input.State.Objective), Is.True);
            if (killTarget)
                Assert.That(result.Events.OfType<CombatOutcomeEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void WithoutRailwayDefinition_DoesNotResolveBossObjective()
        {
            var source = CombatCommandTestData.Input();
            var input = CombatCommandTestData.With(source, state: WithObjective(source.State, BattleObjective.DesignatedBoss(Boss)));
            Assert.That(CombatGimmickCalculator.TryAdvance(input), Is.Null);
            Assert.That(input.Characters.Single(c => c.EntityId == Boss).Health.IsDead, Is.False);
            Assert.That(input.State.Objective.TargetEntityId, Is.EqualTo(Boss));
        }

        [Test]
        public void BossObjective_EntersWithoutHeadquarters()
        {
            var accepted = CombatCommandCalculator.Calculate(EnterCombat(bossAlive: true), Array.Empty<IMovementTileRule>());
            var rejected = CombatCommandCalculator.Calculate(EnterCombat(bossAlive: false), Array.Empty<IMovementTileRule>());

            Assert.That(accepted.Accepted, Is.True, accepted.Reason);
            Assert.That(accepted.FinalState.Objective.TargetEntityId, Is.EqualTo(Boss));
            Assert.That(CombatCommandComposition.Project(accepted).Characters.Single(c => c.EntityId == Boss).ActionUse, Is.EqualTo(UnitActionUse.Deferred));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Reason, Is.EqualTo("InvalidBattleObjective"));
        }

        private static CombatCommandInput EnterCombat(bool bossAlive)
        {
            var source = CombatCommandTestData.Input();
            var characters = source.Characters.Select(c => c.EntityId == Boss && !bossAlive ? CombatCommandCalculator.Hurt(c, 1000) : c).ToArray();
            var state = new CombatCommandState(8,8,CombatFlowPhase.PlayerInput,false,                Array.Empty<StructureCombatFacts>(),source.State.ThornFields,                new CombatTurnFacts(StagePhase.Deployment),objective: BattleObjective.DesignatedBoss(Boss));
            return new CombatCommandInput(1, CombatCommandKind.EnterCombat, source.ActorId, default, state, characters,
                source.Profiles, source.Main, source.Owned, source.SoulCounts, source.Souls, source.Artifacts,
                battle: new CombatBattleFacts(10, 37, 0, 0, Array.Empty<CharacterInstanceId>(), 0),
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (state).Turn.ControllerTurnSequence);
        }

        private static CombatCommandState WithObjective(CombatCommandState s, BattleObjective objective) =>
            new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,s.IsNight,s.Structures,                s.ThornFields,s.Turn,s.Environment,s.EnvironmentProgress,s.WallBlocking,objective);

        private static StructureCombatFacts Headquarters(bool dead) => new StructureCombatFacts(new EntityId(20),
            new StructureSpec(EntityKey.Structure_Headquarters, 1, 1, new[] { CellCoord.Zero }, 100, false, 0),
            new HealthStateSnapshot(dead ? 0 : 100, 100), new CellCoord(6, 6), Rotation4.R0, new[] { new CellCoord(6, 6) });
    }
}
