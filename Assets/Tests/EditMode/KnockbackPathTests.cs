using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    /// <summary>거리 N 공격 넉백·타일 강제 이동·강제 이동 면역을 확인한다(계획 T4.1, G8·G9).</summary>
    public sealed class KnockbackPathTests
    {
        private static readonly EntityId Target = new EntityId(2);
        private static readonly UnitBehaviorSpec Immune = new UnitBehaviorSpec(true, true, true);

        [Test]
        public void HeavyTarget_TakesDamageAndKnockback()
        {
            var input = WithHeavy(Input(3), EntityKey.Unit_Demon);

            var result = Attack(input);

            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(
                e => e.Kind == CombatEventKind.Moved && e.Final.EntityId == Target), Is.True);
            // 헤비 카테고리 면역이 꺼져 있으므로 면역이 아니라 이동 기록이 남는다.
            Assert.That(Knockback(result).Stop, Is.EqualTo(AttackKnockbackStop.Completed));
        }

        [Test]
        public void HeavyTarget_ShortKnockbackMovesWithoutChangingDirectDamage()
        {
            var input = WithHeavy(Input(1), EntityKey.Unit_Demon);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var control = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == input.ActorId
                    ? CombatCommandTestData.With(c, traits: Array.Empty<TraitStatusSnapshot>()) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == actor.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(actor.CharacterKey, 20), p.Method) : p).ToArray());

            var result = Attack(input);
            var noKnockback = Attack(control);

            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(Final(noKnockback).Health.CurrentHealth));
            Assert.That(Final(result).Health.CurrentHealth, Is.LessThan(100));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(
                e => e.Kind == CombatEventKind.Moved && e.Final.EntityId == Target), Is.EqualTo(1));
            Assert.That(noKnockback.Attack.Value.Knockbacks, Is.Empty);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void Knockback_UsesTargetCategoryAndExplicitImmunity(bool heavyAttacker, bool immuneAttacker)
        {
            var input = Input(3);
            if (heavyAttacker) input = WithHeavy(input, EntityKey.Unit_Akimbo);
            if (immuneAttacker) input = WithBehavior(input, EntityKey.Unit_Akimbo, Immune);

            var result = Attack(input);

            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(80));
        }

        [Test]
        public void HeavyTarget_TraversesHazards()
        {
            var input = WithHeavy(Input(3), EntityKey.Unit_Demon);
            input = CombatCommandTestData.With(input, state: EnvironmentDamageTests.WithDamageObjects(input.State, (new CellCoord(3, 3), 6)));

            var result = CombatCommandCalculator.Calculate(input, EnvironmentDamageTests.DamageRules);

            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(74));
            Assert.That(EnvironmentDamageTests.ActiveDamageTiles(result.FinalState), Is.Empty);
        }

        [Test]
        public void HeavyTarget_TriggersDestinationMine()
        {
            var input = CliffFallTests.WithEnvironment(WithHeavy(Input(3), EntityKey.Unit_Demon),
                MineEnvironmentTests.Mine(id: 60, damage: 7, cell: new CellCoord(5, 3)));

            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(73));
            Assert.That(result.FinalState.Environment.Tiles.OfType<MineSourceFact>().Single().Active, Is.False);
            Assert.That(result.Events.OfType<CombatObjectActivationEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void HeavyTarget_PreviewMatchesCommittedKnockbackAndHazards()
        {
            var input = WithHeavy(Input(3), EntityKey.Unit_Demon);
            input = CombatCommandTestData.With(input, state: EnvironmentDamageTests.WithDamageObjects(input.State, (new CellCoord(3, 3), 6)));

            var preview = CombatCommandCalculator.Preview(input, EnvironmentDamageTests.DamageRules, out var projection);
            var result = CombatCommandCalculator.Calculate(input, EnvironmentDamageTests.DamageRules);

            Assert.That(preview.CanAttack, Is.True);
            Assert.That(projection.Accepted, Is.True, projection.Reason);
            Assert.That(Final(projection).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(Final(result).Anchor, Is.EqualTo(Final(projection).Anchor));
            var target = preview.Targets.ToArray().Single(t => t.TargetId == Target);
            Assert.That(target.ExpectedDamage, Is.EqualTo(20));
            Assert.That(target.TargetHealthAfter, Is.EqualTo(74));
            Assert.That(Final(projection).Health.CurrentHealth, Is.EqualTo(74));
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(74));
            var original = input.Characters.Single(c => c.EntityId == Target);
            Assert.That(original.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(original.Anchor, Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(EnvironmentDamageTests.ActiveDamageTiles(input.State).Single().Cell, Is.EqualTo(new CellCoord(3, 3)));
            // 미리보기와 실제가 같은 넉백 요약을 낸다.
            var previewed = preview.Knockbacks.Single(k => k.TargetId == Target);
            var committed = Knockback(result);
            Assert.That(previewed.Stop, Is.EqualTo(committed.Stop));
            Assert.That(previewed.Step, Is.EqualTo(committed.Step));
            Assert.That(previewed.Path, Is.EqualTo(committed.Path));
            Assert.That(previewed.FinalAnchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(preview.Knockbacks.Count, Is.EqualTo(result.Attack.Value.Knockbacks.Count));
        }

        [Test]
        public void HeavyTarget_LethalAttackStillKills()
        {
            var input = WithHeavy(Input(3), EntityKey.Unit_Demon);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == Target
                ? CombatCommandTestData.With(c, hp: 10) : c).ToArray());

            var result = Attack(input);

            Assert.That(Final(result).Health.IsDead, Is.True);
            Assert.That(Final(result).Health.CurrentHealth, Is.Zero);
            AssertNoTargetMovement(result);
        }

        [Test]
        public void Distance3_OpenPath_MovesThreeCells_WithMovedPerStep()
        {
            var result = Attack(Input(3));

            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            var moves = result.Events.OfType<CombatCharacterEvent>()
                .Where(e => e.Kind == CombatEventKind.Moved && e.Final.EntityId == Target).Select(e => e.Final.Anchor.Value).ToArray();
            Assert.That(moves, Is.EqualTo(new[] { new CellCoord(3, 3), new CellCoord(4, 3), new CellCoord(5, 3) }));
            // 요약 경로는 그 대상 Moved 사건의 위치 순서와 같다(설계 2026-10-27 §5.3).
            var record = Knockback(result);
            Assert.That(record.Stop, Is.EqualTo(AttackKnockbackStop.Completed));
            Assert.That(record.TargetKind, Is.EqualTo(AttackKnockbackTargetKind.Character));
            Assert.That(record.StartAnchor, Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(record.Step, Is.EqualTo(CellCoord.Right));
            Assert.That(record.RequestedDistance, Is.EqualTo(3));
            Assert.That(record.Path, Is.EqualTo(moves));
            Assert.That(record.FinalAnchor, Is.EqualTo(Final(result).Anchor.Value));
        }

        [Test]
        public void BlockedMidway_StopsBeforeFirstBlockedCell()
        {
            var result = Attack(Input(3, extra: CombatCommandTestData.Character(7, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy)));

            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(3, 3)));
            Assert.That(Knockback(result).Stop, Is.EqualTo(AttackKnockbackStop.Blocked));
            Assert.That(Knockback(result).Path, Is.EqualTo(new[] { new CellCoord(3, 3) }));
        }

        [Test]
        public void BlockedAtFirstCell_RecordsEmptyPathAtStart()
        {
            var result = Attack(Input(3, extra: CombatCommandTestData.Character(7, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy)));

            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(2, 3)));
            AssertNoTargetMovement(result);
            var record = Knockback(result);
            Assert.That(record.Stop, Is.EqualTo(AttackKnockbackStop.Blocked));
            Assert.That(record.Path, Is.Empty);
            Assert.That(record.FinalAnchor, Is.EqualTo(new CellCoord(2, 3)));
        }

        [Test]
        public void BoardEdge_StopsAtLastInsideCell()
        {
            var result = Attack(Input(3, attacker: new CellCoord(5, 3), target: new CellCoord(6, 3)));

            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(7, 3)));
            Assert.That(Knockback(result).Stop, Is.EqualTo(AttackKnockbackStop.Blocked));
            Assert.That(Knockback(result).Path, Is.EqualTo(new[] { new CellCoord(7, 3) }));
        }

        [Test]
        public void EarlierTargetIsProcessedFirst_SoItCanBeBlockedByLaterTarget()
        {
            // 대상 ID 순서로 민다. ID가 작은 앞 대상은 아직 밀리지 않은 뒤 대상에 막힌다.
            var behind = new EntityId(7);
            var input = AimAll(Input(3, extra: CombatCommandTestData.Character(behind.Value, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy)));

            var result = Attack(input);
            var records = result.Attack.Value.Knockbacks;

            Assert.That(records.Select(k => k.TargetId), Is.EqualTo(new[] { Target, behind }));
            Assert.That(records[0].Stop, Is.EqualTo(AttackKnockbackStop.Blocked));
            Assert.That(records[0].Path, Is.Empty);
            Assert.That(records[1].Stop, Is.EqualTo(AttackKnockbackStop.Completed));
            Assert.That(records[1].Path, Is.EqualTo(new[] { new CellCoord(4, 3), new CellCoord(5, 3), new CellCoord(6, 3) }));
        }

        [Test]
        public void CliffMidway_FallsThere_AndCancelsRemainingCells()
        {
            var input = CliffFallTests.WithEnvironment(Input(3), CliffFallTests.Ground(StaticTerrainKind.Cliff, cell: new CellCoord(4, 3)));

            var result = Attack(input);

            var final = Final(result);
            Assert.That(final.Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatFallEvent>().Single().EntityId, Is.EqualTo(Target));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Moved && e.Final.EntityId == Target),
                Is.EqualTo(2));
            Assert.That(Knockback(result).Stop, Is.EqualTo(AttackKnockbackStop.Interrupted));
            Assert.That(Knockback(result).Path, Is.EqualTo(new[] { new CellCoord(3, 3), new CellCoord(4, 3) }));
        }

        [Test]
        public void LethalHazardMidway_InterruptsAtThatCell()
        {
            var input = Input(3, extra: Array.Empty<CharacterCombatFacts>());
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == Target
                ? CombatCommandTestData.With(c, hp: 25) : c).ToArray());
            input = CombatCommandTestData.With(input, state: EnvironmentDamageTests.WithDamageObjects(input.State, (new CellCoord(3, 3), 6)));

            var result = CombatCommandCalculator.Calculate(input, EnvironmentDamageTests.DamageRules);

            Assert.That(Final(result).Health.IsDead, Is.True);
            Assert.That(Knockback(result).Stop, Is.EqualTo(AttackKnockbackStop.Interrupted));
            Assert.That(Knockback(result).Path, Is.EqualTo(new[] { new CellCoord(3, 3) }));
        }

        [Test]
        public void HazardMidway_DamagesWhilePassing_AndContinues()
        {
            var input = Input(3);
            input = CombatCommandTestData.With(input, state: EnvironmentDamageTests.WithDamageObjects(input.State, (new CellCoord(3, 3), 6)));
            var withoutHazard = Final(Attack(Input(3))).Health.CurrentHealth;

            var result = CombatCommandCalculator.Calculate(input, EnvironmentDamageTests.DamageRules);

            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(withoutHazard - 6));
            Assert.That(EnvironmentDamageTests.ActiveDamageTiles(result.FinalState).Any(f => f.Cell == new CellCoord(3, 3)), Is.False);
        }

        [Test]
        public void MineOnPassedCell_ExplodesWhilePassing_BeforeAttackResultIsFixed()
        {
            var input = CliffFallTests.WithEnvironment(Input(3), MineEnvironmentTests.Mine(id: 60, damage: 7, cell: new CellCoord(4, 3)));
            var withoutMine = Attack(Input(3));

            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(5, 3)));
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(Final(withoutMine).Health.CurrentHealth - 7));
            Assert.That(result.FinalState.Environment.Tiles.OfType<MineSourceFact>().Single().Active, Is.False);
            // G8-2a A: 지나가는 칸의 효과는 공격 결과 고정(S10) 전에 일어나 피격 체력 기록에 포함된다.
            var hit = result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == Target);
            Assert.That(hit.TargetHealthAfter, Is.EqualTo(Final(result).Health.CurrentHealth));
        }

        [Test]
        public void MineOnFinalCell_KeepsExistingArrivalTiming_AfterAttackResult()
        {
            var input = CliffFallTests.WithEnvironment(Input(3), MineEnvironmentTests.Mine(id: 60, damage: 7, cell: new CellCoord(5, 3)));

            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);

            Assert.That(result.Accepted, Is.True, result.Reason);
            var hit = result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == Target);
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(hit.TargetHealthAfter - 7));
        }

        [TestCase(1, 5)]
        [TestCase(100, 3)]
        public void MineBeforeBlockedCell_ResolvesBeforeCheckingRemainingPath(int blockerHealth, int expectedX)
        {
            var blockerId = new EntityId(8);
            var blocker = CombatCommandTestData.With(CombatCommandTestData.Character(blockerId.Value,
                EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy), hp: blockerHealth);
            var input = CliffFallTests.WithEnvironment(Input(3, extra: blocker),
                MineEnvironmentTests.Mine(id: 60, damage: 7, cell: new CellCoord(3, 3)));

            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            var finalBlocker = CombatCommandComposition.Project(result).Characters.Single(c => c.EntityId == blockerId);

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(finalBlocker.Health.CurrentHealth, Is.EqualTo(Math.Max(0, blockerHealth - 7)));
            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(expectedX, 3)));
            Assert.That(Final(result).Health.CurrentHealth, Is.EqualTo(73));
            Assert.That(result.FinalState.Environment.Tiles.OfType<MineSourceFact>().Single().Active, Is.False);
            // 앞의 유닛이 살아남아 멈춰도, 남은 거리가 있는 칸의 지뢰는 공격 결과 고정 전에 처리한다.
            Assert.That(result.Attack.Value.Hits.ToArray().Single(h => h.TargetId == Target).TargetHealthAfter, Is.EqualTo(73));
            Assert.That(result.Events.OfType<CombatObjectActivationEvent>().Count(), Is.EqualTo(1));
        }

        [Test]
        public void MineDuringKnockback_KillsBoss_StopsRemainingTargetsBeforeHazards()
        {
            var bossId = new EntityId(8);
            var laterId = new EntityId(9);
            var boss = CombatCommandTestData.With(CombatCommandTestData.Character(bossId.Value,
                EntityKey.Unit_Gravekeeper, new CellCoord(3, 4), Owner.Enemy), hp: 21);
            var later = CombatCommandTestData.Character(laterId.Value, EntityKey.Unit_Demon, new CellCoord(1, 4), Owner.Enemy);
            var input = CliffFallTests.WithEnvironment(Input(3, extra: new[] { boss, later }),
                MineEnvironmentTests.Mine(id: 60, damage: 7, cell: new CellCoord(3, 3)));
            var all = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0);
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(p.Definition, all) : p)
                .Append(new CombatAttackProfile(With(CombatCommandTestData.Spec(EntityKey.Unit_Gravekeeper, 5), Immune), all)).ToArray();
            var s = input.State;
            var state = EnvironmentDamageTests.WithDamageObjects(new CombatCommandState(s.BoardWidth,s.BoardHeight,s.Phase,                s.Structures,s.Turn,s.Environment,s.EnvironmentProgress,s.WallBlocking,BattleObjective.DesignatedBoss(bossId),s.Allocation), (new CellCoord(1, 5), 100));
            input = new CombatCommandInput(input.Number, input.Kind, input.ActorId, AttackAim.ForAll(), state,
                input.Characters, profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                environmentBindings: EnvironmentDamageTests.WithDamageObjectBindings(input.EnvironmentBindings, state),
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence,
                battleInstanceId: input.BattleInstanceId);

            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules.Concat(EnvironmentDamageTests.DamageRules).ToArray());
            var characters = CombatCommandComposition.Project(result).Characters;

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(characters.Single(c => c.EntityId == bossId).Health.IsDead, Is.True);
            Assert.That(result.Outcome.Reason, Is.EqualTo(StageOutcomeReason.DesignatedBossDefeated));
            // 보스가 지뢰로 죽기 전 직접 피해 20은 유지하되, 이후 대상의 이동·위험 오브젝트 피해는 취소한다.
            Assert.That(characters.Single(c => c.EntityId == laterId).Anchor, Is.EqualTo(new CellCoord(1, 4)));
            Assert.That(characters.Single(c => c.EntityId == laterId).Health.CurrentHealth, Is.EqualTo(80));
            Assert.That(EnvironmentDamageTests.ActiveDamageTiles(result.FinalState).Single().Cell, Is.EqualTo(new CellCoord(1, 5)));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved &&
                e.Final.EntityId == laterId), Is.False);
            Assert.That(result.Events.OfType<CombatOutcomeEvent>().Count(), Is.EqualTo(1));
            // 지뢰가 보스를 죽여 넉백이 중단돼도 이동하던 대상은 살아 있다. Interrupted는 사망 표시가 아니다.
            var moved = characters.Single(c => c.EntityId == Target);
            Assert.That(moved.Health.IsDead, Is.False);
            var record = result.Attack.Value.Knockbacks.Single(k => k.TargetId == Target);
            Assert.That(record.Stop, Is.EqualTo(AttackKnockbackStop.Interrupted));
            Assert.That(record.Path, Is.EqualTo(new[] { new CellCoord(3, 3) }));
            Assert.That(record.FinalAnchor, Is.EqualTo(moved.Anchor.Value));
            Assert.That(result.Attack.Value.Knockbacks.Any(k => k.TargetId == laterId || k.TargetId == bossId), Is.False);
        }

        [Test]
        public void ImmuneTarget_TakesDamageButDoesNotMove()
        {
            var input = Input(3);
            input = WithBehavior(input, EntityKey.Unit_Demon, Immune);

            var result = Attack(input);

            Assert.That(Final(result).Anchor, Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(Final(result).Health.CurrentHealth, Is.LessThan(100));
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved && e.Final.EntityId == Target), Is.False);
            var record = Knockback(result);
            Assert.That(record.Stop, Is.EqualTo(AttackKnockbackStop.Immune));
            Assert.That(record.Path, Is.Empty);
            Assert.That(record.Step, Is.EqualTo(CellCoord.Right));
            Assert.That(record.RequestedDistance, Is.EqualTo(3));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TileForcedMovement_PushesNCells_UnlessImmune(bool immune)
        {
            var seed = CombatCommandTestData.Input(empty: true);
            var tile = new PushTile(80, new CellCoord(2, 3), Rotation4.R90, 3);
            var input = CliffFallTests.WithEnvironment(seed, tile);
            if (immune) input = WithBehavior(input, EntityKey.Unit_Akimbo, Immune);
            var move = new CombatCommandInput(1, CombatCommandKind.Move, input.ActorId, default, input.State, input.Characters,
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                path: new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3) }),
                environmentBindings: input.EnvironmentBindings,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (input.State).Turn.ControllerTurnSequence);

            var result = CombatCommandCalculator.Calculate(move, new IMovementTileRule[] { new PushRule(), new EnvironmentDamageTests.DamageRule() });

            Assert.That(result.Accepted, Is.True, result.Reason);
            var step = ImpactPatternCalculator.GetForwardStep(Rotation4.R90);
            var expected = immune ? new CellCoord(2, 3) : new CellCoord(2 + step.X * 3, 3 + step.Y * 3);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Anchor, Is.EqualTo(expected));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void Heavy_TileForcedMovementUsesOnlyExplicitImmunity(bool immune)
        {
            var seed = WithHeavy(CombatCommandTestData.Input(empty: true), EntityKey.Unit_Akimbo);
            var input = CliffFallTests.WithEnvironment(seed, new PushTile(80, new CellCoord(2, 3), Rotation4.R90, 3));
            if (immune) input = WithBehavior(input, EntityKey.Unit_Akimbo, Immune);
            var move = new CombatCommandInput(1, CombatCommandKind.Move, input.ActorId, default, input.State, input.Characters,
                input.Profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                path: new MovementPath(new[] { new CellCoord(1, 3), new CellCoord(2, 3) }),
                environmentBindings: input.EnvironmentBindings,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (input.State).Turn.ControllerTurnSequence);

            var result = CombatCommandCalculator.Calculate(move, new IMovementTileRule[] { new PushRule(), new EnvironmentDamageTests.DamageRule() });

            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Characters.Characters.Single(c => c.Final.EntityId == input.ActorId).Final.Anchor,
                Is.EqualTo(immune ? new CellCoord(2, 3) : new CellCoord(5, 3)));
        }
        private static void AssertNoTargetMovement(CombatCommandTransition result)
        {
            Assert.That(result.Events.OfType<CombatCharacterEvent>().Any(
                e => e.Kind == CombatEventKind.Moved && e.Final.EntityId == Target), Is.False);
        }

        private static CombatCommandTransition Attack(CombatCommandInput input)
        {
            var result = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(result.Accepted, Is.True, result.Reason);
            return result;
        }

        private static CharacterCombatFacts Final(CombatCommandTransition result) =>
            result.Characters.Characters.Single(c => c.Final.EntityId == Target).Final;

        private static AttackKnockbackSnapshot Knockback(CombatCommandTransition result) =>
            result.Attack.Value.Knockbacks.Single(k => k.TargetId == Target);

        /// <summary>공격자 둘레 전체를 지정하도록 바꾼다. 여러 대상의 처리 순서를 볼 때 쓴다.</summary>
        private static CombatCommandInput AimAll(CombatCommandInput input)
        {
            var all = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.All, TargetingSplash.None, 0);
            var profiles = input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(p.Definition, all) : p).ToArray();
            return new CombatCommandInput(input.Number, input.Kind, input.ActorId, AttackAim.ForAll(), input.State,
                input.Characters, profiles, input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts,
                environmentBindings: input.EnvironmentBindings,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence,
                battleInstanceId: input.BattleInstanceId);
        }

        /// <summary>넉백 거리 N 특성을 가진 공격자가 바로 오른쪽 적을 단일 지정(스플래시 없음)으로 때리는 입력이다.</summary>
        private static CombatCommandInput Input(int distance, CellCoord? attacker = null, CellCoord? target = null,
            params CharacterCombatFacts[] extra)
        {
            var seed = CombatCommandTestData.Input(empty: true);
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            var actor = CombatCommandTestData.With(seed.Characters[0], traits: new TraitStatusSnapshot[] { new KnockbackTraitStatusSnapshot(distance) },
                position: attacker ?? new CellCoord(1, 3));
            var victim = CombatCommandTestData.Character(Target.Value, EntityKey.Unit_Demon, target ?? new CellCoord(2, 3), Owner.Enemy);
            var profiles = seed.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 20, CategoryKey.Main, new KnockbackTraitSpec(distance)), method)
                : p).ToArray();
            var characters = new[] { actor, seed.Characters[1], victim }.Concat(extra).ToArray();
            return new CombatCommandInput(1, CombatCommandKind.Attack, actor.EntityId, AttackAim.ForCell(victim.Anchor.Value), seed.State,
                characters, profiles, seed.Main, seed.Owned, seed.SoulCounts, seed.Souls, seed.Artifacts,
                expectedBattleInstanceId: 0, expectedControllerTurnSequence: (seed.State).Turn.ControllerTurnSequence);
        }

        private static CombatCommandInput WithBehavior(CombatCommandInput input, EntityKey key, UnitBehaviorSpec behavior) =>
            new CombatCommandInput(input.Number, input.Kind, input.ActorId, input.Aim, input.State, input.Characters,
                input.Profiles.Select(p => p.Definition.Key != key ? p : new CombatAttackProfile(With(p.Definition, behavior), p.Method)).ToArray(),
                input.Main, input.Owned, input.SoulCounts, input.Souls, input.Artifacts, input.TargetCell, input.Rotation, input.Battle,
                input.ProcessingKind, input.Path, input.MovementContext, input.BattleInstanceId, input.ArtifactItems,
                input.EnvironmentBindings, input.ObstacleContents,
                expectedBattleInstanceId: input.ExpectedBattleInstanceId, expectedControllerTurnSequence: input.ExpectedControllerTurnSequence);

        private static CombatCommandInput WithHeavy(CombatCommandInput input, EntityKey key) => CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.CharacterKey == key
                    ? CombatCommandTestData.With(c, category: CategorySnapshot.CreateStateless(CategoryKey.Heavy)) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == key
                    ? new CombatAttackProfile(With(p.Definition, p.Definition.Behavior, CategoryKey.Heavy), p.Method) : p).ToArray());

        private static UnitSpec With(UnitSpec s, UnitBehaviorSpec behavior, CategoryKey? category = null) => new UnitSpec(s.Key, s.Role, s.FootprintWidth,
            s.FootprintHeight, s.FootprintLocalCells.ToArray(), s.MaxHealth, s.MoveMethod, s.AttackRange,
            category ?? s.CategoryKey, s.AttackPower, s.PhysicalDefense, s.Priority, s.Traits.ToArray(), s.OwnerEligibility, s.Occlusion, behavior,
            killReward: s.KillReward);

        /// <summary>밟으면 진입한 유닛을 지정 방향으로 N칸 미는 시험용 타일이다(원인은 일반 강제 이동).</summary>
        private sealed class PushTile : MovementTileFact
        {
            internal Rotation4 Direction { get; }
            internal int Distance { get; }
            internal PushTile(long id, CellCoord cell, Rotation4 direction, int distance)
                : base(id, cell, "fixture-push", 0, true, new[] { Owner.Player, Owner.Enemy })
            { Direction = direction; Distance = distance; }
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other) &&
                other is PushTile p && p.Direction == Direction && p.Distance == Distance;
            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo) => this;
        }

        private sealed class PushRule : IMovementTileRule
        {
            public string Key => "fixture-push";
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current) => new[] { mover };
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
            {
                var push = (PushTile)tile;
                return input.Phase == MovementEffectPhase.Hazard
                    ? new MovementStepEffects(forcedMovement: new[] { new MovementForcedMoveRequest(input.MoverId, push.Direction, push.Distance) })
                    : new MovementStepEffects();
            }
        }
    }
}
