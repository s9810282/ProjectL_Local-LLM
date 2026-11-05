using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>보류된 사령술과 유지되는 즉시 포효의 경계를 확인한다.</summary>
    public sealed class BossTurnTests
    {
        private static readonly EntityId Boss = CombatSummonTests.Boss;

        [Test]
        public void IneligibleEnemy_DoesNotInventAnActionOrSummon()
        {
            var input = BossInput(new CellCoord(3, 3));
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Characters.Created, Is.Empty);
            Assert.That(result.Attack.HasValue, Is.False);
            Assert.That(result.PathMovement, Is.Null);
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingPlayer));
            Assert.That(result.FinalState.Turn.CompletedPlayerTurnCount, Is.EqualTo(3));
        }

        [Test]
        public void PendingRoar_ResolvesBeforeEnemyCompletion()
        {
            var input = WithRoar(BossInput(new CellCoord(3, 3)), 5);
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.EnemyPhaseEnded), Is.False);
            Assert.That(result.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.Enemy));
            Assert.That(result.Characters.Created, Is.Empty);
            Assert.That(RoarHitCounter.HitCount(BossRoarTests.Final(result, Boss)), Is.Zero);
            var next = CombatCommandCalculator.Advance(CombatCommandComposition.Project(result), Array.Empty<IMovementTileRule>());
            Assert.That(next.Accepted, Is.True, next.Reason);
            Assert.That(next.FinalState.Turn.ControllerPhase, Is.EqualTo(CombatControllerPhase.StartingPlayer));
        }

        [Test]
        public void TerminalRoarAtEighthResponse_SuppressesDayAndControllerStart()
        {
            var seed = WithRoar(BossInput(new CellCoord(3, 3), completed: 8), 5);
            var input = CombatCommandTestData.With(seed, characters: seed.Characters.Select(c =>
                c.OwnershipRole == CharacterOwnershipRole.Main
                    ? CombatCommandTestData.With(c, hp: 1, position: new CellCoord(3, 5)) : c).ToArray());
            var result = CombatCommandCalculator.Advance(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Outcome.Outcome, Is.EqualTo(StageOutcome.Defeat));
            Assert.That(result.FinalState.Phase, Is.EqualTo(CombatFlowPhase.Finished));
            Assert.That(result.Events.OfType<CombatRoarEvent>().Count(), Is.EqualTo(1));
            Assert.That(result.Events.Any(e => e.Kind == CombatEventKind.NightEnded ||
                e.Kind == CombatEventKind.EnemyPhaseEnded || e.Kind == CombatEventKind.ControllerTurnStarted), Is.False);
            Assert.That(result.Characters.Created, Is.Empty);
        }

        internal static CombatCommandInput WithRoar(CombatCommandInput input, int hitCount, int threshold = 5)
        {
            var characters = input.Characters.Select(c => c.EntityId != Boss ? c : CombatCommandTestData.With(c,
                traits: c.Stats.TraitStatuses.ToArray().Append(new RoarTraitStatusSnapshot(hitCount, threshold)).ToArray())).ToArray();
            var profiles = input.Profiles.Select(p =>
            {
                if (p.Definition.Key != EntityKey.Unit_Demon) return p;
                var d = p.Definition;
                var spec = new UnitSpec(d.Key, d.Role, d.FootprintWidth, d.FootprintHeight, d.FootprintLocalCells.ToArray(),
                    d.MaxHealth, d.MoveMethod, d.AttackRange, d.CategoryKey, d.AttackPower, d.PhysicalDefense, d.Priority,
                    d.Traits.ToArray().Append(new RoarTraitSpec(threshold)).ToArray(), d.OwnerEligibility, d.Occlusion, d.Behavior,
                    killReward: d.KillReward);
                return new CombatAttackProfile(spec, new TargetingMethodSpec(TargetingShape.Fan,
                    TargetingChoiceMode.All, TargetingSplash.None, 0));
            }).ToArray();
            return CombatCommandTestData.With(input, characters, profiles: profiles);
        }

        internal static CombatCommandInput BossInput(CellCoord bossCell, long completed = 3,
            CombatSummonRecord[] records = null, params CharacterCombatFacts[] extra)
        {
            var input = CombatSummonTests.Input(bossCell, records ?? Array.Empty<CombatSummonRecord>(), 11, extra);
            var ability = CombatSummonTests.Ability;
            var status = new NecromancyTraitStatusSnapshot(ability.SummonedUnitKey, ability.MaxLivingSummons, ability.FirstActionDelay);
            var characters = input.Characters.Select(c => c.EntityId == Boss
                ? CombatCommandTestData.With(c, traits: new TraitStatusSnapshot[] { status }) : c).ToArray();
            var profiles = input.Profiles.Select(p =>
            {
                if (p.Definition.Key != EntityKey.Unit_Demon) return p;
                var d = p.Definition;
                return new CombatAttackProfile(new UnitSpec(d.Key, d.Role, d.FootprintWidth, d.FootprintHeight,
                    d.FootprintLocalCells.ToArray(), d.MaxHealth, d.MoveMethod, d.AttackRange, d.CategoryKey,
                    d.AttackPower, d.PhysicalDefense, d.Priority, new TraitSpec[] { ability }, d.OwnerEligibility,
                    d.Occlusion, new UnitBehaviorSpec(false, false, true), killReward: d.KillReward), p.Method);
            }).ToArray();
            var s = input.State;
            var turn = new CombatTurnFacts(StagePhase.Combat, controllerPhase: CombatControllerPhase.Enemy,
                controllerTurnSequence: 2, completedPlayerTurnCount: completed,
                enemyOrigins: new[] { new CombatEnemyOrigin(Boss, bossCell, Rotation4.R90) }, summons: records);
            var state = new CombatCommandState(s.BoardWidth,s.BoardHeight,CombatFlowPhase.Automatic,                true,s.Structures,s.ThornFields,turn,s.Environment,s.EnvironmentProgress,s.WallBlocking,                BattleObjective.DesignatedBoss(Boss),new CombatAllocationFacts(Math.Max(11, characters.Max(c => c.EntityId.Value) + 1),
                    s.Allocation.SummonRandomState));
            return new CombatCommandInput(0, CombatCommandKind.CompleteEnemyTurn, Boss, default, state, characters, profiles,
                input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, processingKind: CombatProcessingKind.Advance,
                battle: new CombatBattleFacts(10, 37, 0, 8, Array.Empty<CharacterInstanceId>(), 0,
                    categories: UnitSpecTestFactory.CreateDefaultCategoryCatalog()), environmentBindings: input.EnvironmentBindings);
        }
    }
}
