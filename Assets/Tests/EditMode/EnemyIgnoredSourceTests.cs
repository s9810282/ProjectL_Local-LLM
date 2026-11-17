using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// 적 AI가 모형화하지 않는 출처를 판단에서 무시하고, 장애물은 후보가 건드릴 때만 세는지 본다(적 AI 근간 개편 설계 6.2, 계획 R-1).
    /// 실제 전투 계산은 그 출처를 모두 적용하므로 실제 모드의 공백은 그대로여야 한다.
    /// </summary>
    public sealed class EnemyIgnoredSourceTests
    {
        private static readonly IMovementTileRule[] Rules = EnemyAiBenchmarkFixtures.Rules;
        private static readonly IMovementTileRule[] NoRules = Array.Empty<IMovementTileRule>();

        [Test]
        public void OneArtifactDoesNotChangeEnemyDecisions()
        {
            bool fullRegression = EnemyAiBenchmarkFixtures.FullRegressionEnabled;
            foreach (string fixture in EnemyAiBenchmarkFixtures.Ids)
            foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
            {
                if (!fullRegression && !EnemyAiBenchmarkFixtures.IsRepresentative(fixture, seed)) continue;
                var input = EnemyAiBenchmarkFixtures.Build(fixture, seed);
                var withArtifact = WithArtifact(input);
                Assert.That(FirstDecision(withArtifact), Is.EqualTo(FirstDecision(input)), $"{fixture}/{seed} 첫 결정");
                int acting = ActedPublications(input);
                if (acting != 0)
                    Assert.That(ActedPublications(withArtifact), Is.Not.Zero, $"{fixture}/{seed}: Artifact 하나가 진영 전체를 대기로 바꿈");
            }
        }

        [TestCase("Held")]
        [TestCase("Item")]
        [TestCase("TemporaryItem")]
        [TestCase("Artifact")]
        [TestCase("Undeclared")]
        [TestCase("HarmOnlyDeclared")]
        public void ItemsHeldEnvironmentAndUndeclaredRulesAreIgnored(string source)
        {
            var input = Source(EnemyFollowupEstimateTests.Input(regeneration: false), source);
            var intent = input.WithIntent(CombatCommandKind.Move, default, default, null, null);

            bool enemyHarm = false, enemyBlocked = false;
            var enemyGaps = CombatCommandEffects.AssessmentCoverageSteps(intent, () => enemyHarm = true, () => enemyBlocked = true,
                enemyAssessment: true).ToArray();
            Assert.That(enemyGaps, Is.All.EqualTo(CombatAssessmentGap.None), "적 평가 모드의 공백");
            Assert.That(enemyHarm, Is.False, "적 평가 모드의 아군 피해 상한");
            Assert.That(enemyBlocked, Is.False, "적 평가 모드의 이동 투영 막기");

            bool combatHarm = false, combatBlocked = false;
            var combatGaps = CombatCommandEffects.AssessmentCoverageSteps(intent, () => combatHarm = true, () => combatBlocked = true).ToArray();
            Assert.That(combatGaps, Does.Contain(source == "Artifact"
                ? CombatAssessmentGap.ActualOnlyReaction | CombatAssessmentGap.UnsupportedEffect
                : CombatAssessmentGap.UnsupportedEffect), "실제 모드의 공백");
            Assert.That(combatBlocked, Is.True, "실제 모드의 이동 투영 막기");
            Assert.That(combatHarm, Is.EqualTo(source != "HarmOnlyDeclared"), "실제 모드의 아군 피해 상한");

            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var move = Assess(input, MoveAway(input), context, allowPrecise: false);
            Assert.That(move.Facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment), "후보 평가의 공백");
            Assert.That(move.Facts.PossibleAlliedHpLossPermille, Is.Zero, "후보 평가의 아군 피해 상한");
        }

        [Test]
        public void ObstacleCountsOnlyWhenCandidateCanTriggerIt()
        {
            var input = BarrelWorld();
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);

            // 화약통 A를 치면 A의 폭발 칸 안 화약통 B가 이어서 터진다. 두 폭발 칸 안의 아군만 센다(바깥 아군·행위자는 뺀다).
            using (var hitsBarrel = Assess(input, EnemyDecision.Attack(input.ActorId, AttackAim.ForCell(BarrelA), default), context, false))
            {
                Assert.That(hitsBarrel.Admitted, Is.True);
                Assert.That(hitsBarrel.Facts.PossibleAlliedHpLossPermille, Is.EqualTo(2000));
                Assert.That(hitsBarrel.Facts.Gaps.HasFlag(CombatAssessmentGap.UnsupportedEffect), Is.True);
            }

            using (var hitsPlayer = Assess(input, EnemyDecision.Attack(input.ActorId, AttackAim.ForCell(PlayerCell), default), context, false))
            {
                Assert.That(hitsPlayer.Admitted, Is.True);
                Assert.That(hitsPlayer.Facts.PossibleAlliedHpLossPermille, Is.Zero);
                Assert.That(hitsPlayer.Facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment));
            }

            using (var move = Assess(input, MoveAway(input), context, false))
            {
                Assert.That(move.Admitted, Is.True);
                Assert.That(move.Facts.PossibleAlliedHpLossPermille, Is.Zero);
                Assert.That(move.Facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment));
            }
        }

        [Test]
        public void DeclaredRulesAndRouteCoverageUnchanged()
        {
            // 공백을 선언한 규칙(사냥꾼 = 무작위 결과)은 두 모드에서 같은 공백·피해·막기를 낸다.
            var input = EnemyFollowupEstimateTests.AddTrait(EnemyFollowupEstimateTests.Input(regeneration: false), EntityKey.Unit_MainCharacter,
                new HunterTraitSpec(2, 3), new HunterTraitStatusSnapshot());
            var intent = input.WithIntent(CombatCommandKind.Move, default, default, null, null);
            bool combatHarm = false, combatBlocked = false, enemyHarm = false, enemyBlocked = false;
            var combat = CombatCommandEffects.AssessmentCoverageSteps(intent, () => combatHarm = true, () => combatBlocked = true).ToArray();
            var enemy = CombatCommandEffects.AssessmentCoverageSteps(intent, () => enemyHarm = true, () => enemyBlocked = true,
                enemyAssessment: true).ToArray();
            Assert.That(enemy, Is.EqualTo(combat));
            Assert.That(enemy, Does.Contain(CombatAssessmentGap.RandomOutcome));
            Assert.That((enemyHarm, enemyBlocked), Is.EqualTo((combatHarm, combatBlocked)));

            // 경로 위 지뢰는 경계 A–D의 경로 공백으로 남아 후속 세계를 만들지 않는다.
            var mined = CliffFallTests.WithEnvironment(EnemyFollowupEstimateTests.Input(regeneration: false),
                new MineSourceFact(71, new CellCoord(1, 2), 1, 10, true, new[] { Owner.Enemy }));
            var throughMine = EnemyDecision.Move(mined.ActorId,
                new MovementPath(new[] { new CellCoord(0, 2), new CellCoord(1, 2), new CellCoord(2, 2) }), default);
            using var route = Assess(mined, throughMine, new EnemyDecisionContext(mined, EnemyDecisionSettings.Default), true);
            Assert.That(route.Admitted, Is.True);
            Assert.That(route.Facts.IsPrecise, Is.False);
            Assert.That(route.MovedFacts, Is.Null);
        }

        [Test]
        public void KnockbackLeftToPrecisePreviewInEnemyMode()
        {
            // 적 평가: 넉백 계열은 행위자 공격이어도 공백·피해 상한을 내지 않는다. 강제 이동은 CombatActionAssessment가 닿는 칸만 검사하고
            // 정밀 미리보기가 센다(사용자 결정 2026-11-10; 이전 기대값: 행위자면 공백 + 상한). 실제 모드는 기존 보수 정책 그대로다.
            var input = ImpactBoltTests.Input();
            bool actingHarm = false;
            var acting = CombatCommandEffects.AssessmentCoverageSteps(input, () => actingHarm = true, enemyAssessment: true).ToArray();
            Assert.That(acting, Has.No.Member(CombatAssessmentGap.UnsupportedEffect));
            Assert.That(actingHarm, Is.False);
            bool combatHarm = false;
            var combat = CombatCommandEffects.AssessmentCoverageSteps(input, () => combatHarm = true).ToArray();
            Assert.That(combat, Does.Contain(CombatAssessmentGap.UnsupportedEffect));
            Assert.That(combatHarm, Is.True);

            var remote = input.WithActor(input.Characters.Single(c => c.CharacterKey == EntityKey.Unit_MainCharacter).EntityId);
            bool remoteHarm = false;
            var remoteGaps = CombatCommandEffects.AssessmentCoverageSteps(remote, () => remoteHarm = true, enemyAssessment: true).ToArray();
            Assert.That(remoteHarm, Is.False);
            Assert.That(remoteGaps, Has.No.Member(CombatAssessmentGap.UnsupportedEffect));
        }

        private static readonly CellCoord BarrelA = new CellCoord(2, 4);
        private static readonly CellCoord BarrelB = new CellCoord(3, 4);
        private static readonly CellCoord PlayerCell = new CellCoord(2, 1);

        // 행위자(0,2)·사거리 2, 플레이어(2,1), 화약통 A(2,4)·B(3,4), A 폭발 칸 안 아군(3,3), B 폭발 칸에만 든 아군(4,3), 바깥 아군(7,0).
        private static CombatCommandInput BarrelWorld()
        {
            var input = EnemyFollowupEstimateTests.World(actorCell: new CellCoord(0, 2), targetCell: PlayerCell, range: 2);
            var characters = input.Characters
                .Append(CombatCommandTestData.Character(12, EntityKey.Unit_Demon, new CellCoord(3, 3), Owner.Enemy))
                .Append(CombatCommandTestData.Character(14, EntityKey.Unit_Demon, new CellCoord(4, 3), Owner.Enemy))
                .Append(CombatCommandTestData.Character(13, EntityKey.Unit_Demon, new CellCoord(7, 0), Owner.Enemy)).ToArray();
            var barrels = new[] { Barrel(90, BarrelA), Barrel(91, BarrelB) };
            return WithBarrels(CombatCommandTestData.With(input, characters: characters), barrels);
        }

        internal static StructureCombatFacts Barrel(int id, CellCoord cell) =>
            new StructureCombatFacts(new EntityId(id), new StructureSpec(EntityKey.Structure_PowderBarrel, 1, 1,
                new[] { CellCoord.Zero }, 1, false, 0), new ObstacleDurabilitySnapshot(3, 3), cell, Rotation4.R0, new[] { cell }, Owner.Neutral);

        /// <summary>화약통 구조물·장애물 분류·내용 프로필을 서로 맞게 넣는다(실제 입력 검사가 셋의 일치를 요구한다).</summary>
        internal static CombatCommandInput WithBarrels(CombatCommandInput input, params StructureCombatFacts[] barrels)
        {
            var ids = barrels.Select(b => b.EntityId).ToArray();
            var placed = CombatCommandTestData.With(input, state: input.State.With(structures: barrels));
            return Rebuild(placed, new EnvironmentSemanticBindings(Array.Empty<long>(), Array.Empty<long>(), ids),
                ids.Select(id => new ObstacleContentProfile(id, ObstacleContentKind.PowderBarrel, 10)).ToArray());
        }

        private static CombatCommandInput Rebuild(CombatCommandInput input, EnvironmentSemanticBindings bindings,
            IReadOnlyList<ObstacleContentProfile> contents) => new CombatCommandInput(input.Number, input.Kind,
                input.ActorId, input.Aim, input.State, input.Characters.ToArray(), input.Profiles.ToArray(), input.Main, input.Owned,
                input.SoulCounts, input.Souls, input.Artifacts, input.TargetCell, input.Rotation, input.Battle, input.ProcessingKind,
                input.Path, input.MovementContext, input.BattleInstanceId, input.ArtifactItems, bindings, contents, input.ItemKey,
                input.ItemInventory, input.ItemDefinitions, input.GoldBalance, input.FameBalance, input.Membership,
                input.ExpectedBattleInstanceId, input.ExpectedControllerTurnSequence, input.ExpectedMoveCommandNumber);

        private static CombatCommandInput Source(CombatCommandInput input, string source) => source switch
        {
            "Held" => EnemyFollowupEstimateTests.ReplaceActor(input,
                actor => actor.With(heldEnvironment: new CharacterHeldEnvironmentState(suppliesAttackBonus: 1))),
            "Item" => EnemyFollowupEstimateTests.ReplaceActor(input,
                actor => actor.With(items: new CharacterItemState(1, Array.Empty<ItemTemporaryEffect>()))),
            "TemporaryItem" => EnemyFollowupEstimateTests.ReplaceActor(input, actor => actor.With(items: new CharacterItemState(null,
                new[] { new ItemTemporaryEffect(2, ItemStatKind.PhysicalDefense, 3) }))),
            "Artifact" => WithArtifact(input),
            "Undeclared" => EnemyFollowupEstimateTests.AddTrait(input, EntityKey.Unit_Demon,
                new IntimidationTraitSpec(20), new IntimidationTraitStatusSnapshot(20)),
            "HarmOnlyDeclared" => TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new LeapTraitSpec()),
            _ => throw new ArgumentOutOfRangeException(nameof(source)),
        };

        private static CombatCommandInput WithArtifact(CombatCommandInput input) => CombatCommandTestData.With(input,
            artifacts: new ArtifactLoadoutSnapshot(0, new[] { new ArtifactPlacedEffectSnapshot(11, 4, ArtifactSoulSchemaTests.SoulEffect()) }));

        // 행위자를 오른쪽으로 한 칸 옮기는 이동이다. 어떤 출처도 건드리지 않는다.
        private static EnemyDecision MoveAway(CombatCommandInput input)
        {
            var origin = input.Characters.Single(c => c.EntityId == input.ActorId).Anchor.Value;
            return EnemyDecision.Move(input.ActorId, new MovementPath(new[] { origin, origin + CellCoord.Right }), default);
        }

        private static CombatActionAssessment.Work Assess(CombatCommandInput input, EnemyDecision candidate,
            EnemyDecisionContext context, bool allowPrecise)
        {
            string before = EnemyActionAssessmentTests.Encode(input);
            var work = CombatActionAssessment.Begin(input, candidate, Rules, context, allowPrecise);
            for (int i = 0; i < 100000 && !work.IsComplete; i++) work.Step();
            Assert.That(work.IsComplete, Is.True);
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
            return work;
        }

        private static EnemyDecision FirstDecision(CombatCommandInput input)
        {
            using var session = new EnemyDecisionSession(input, Rules, EnemyDecisionSettings.Default);
            EnemyUtilityDecisionTests.Drain(session);
            return session.Decision;
        }

        // 한 적 진영을 끝까지 게시하며 이동·공격 게시 수를 센다.
        private static int ActedPublications(CombatCommandInput input)
        {
            int acted = 0;
            var current = input;
            for (int step = 0; step < 128; step++)
            {
                var transition = CombatCommandCalculator.AdvanceUtility(current, Rules);
                Assert.That(transition.Accepted, Is.True, transition.Reason);
                if (transition.Attack.HasValue || transition.PathMovement != null) acted++;
                current = CombatCommandComposition.Project(transition);
                if (transition.Outcome.IsStageEnded || current.State.Turn.ControllerPhase != CombatControllerPhase.Enemy) break;
            }
            return acted;
        }
    }
}
