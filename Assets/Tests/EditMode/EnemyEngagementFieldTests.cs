using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>여러 대상 교전 필드(적 AI 근간 개편 설계 6.3, 계획 R-2)의 대상·비용·목표 칸 규칙을 본다.</summary>
    public sealed class EnemyEngagementFieldTests
    {
        private static readonly EntityId Main = new EntityId(1);
        private static readonly EntityId Second = new EntityId(2);
        private static readonly CellCoord Origin = new CellCoord(1, 3);

        [Test]
        public void NearestByRouteNotStraightLine()
        {
            // 직선거리가 가까운 주인공(4,3)은 벽 뒤라 먼 길이고, 더 먼 두 번째 대상(0,0)은 길이 짧다.
            var input = WithSecond(EnemyFollowupEstimateTests.World(width: 9, height: 7, actorCell: Origin, targetCell: new CellCoord(4, 3)),
                new CellCoord(0, 0));
            input = CliffFallTests.WithEnvironment(input, Enumerable.Range(0, 6)
                .Select(y => (MovementTileFact)WallStateLifecycleTests.Wall(100 + y, new CellCoord(3, y))).ToArray());
            var field = Build(input);
            Assert.That(field.TryGetBestTarget(Origin, out var best), Is.True);
            Assert.That(best, Is.EqualTo(Second));
            Assert.That(field.TryGetCost(Origin, out int cost), Is.True);
            Assert.That(cost, Is.EqualTo(2));
            Assert.That(CandidateProgress(input, new CellCoord(1, 2)), Is.EqualTo(1));
        }

        [Test]
        public void HaboobUnattackableTargetIsSkipped()
        {
            // 구름 안의 주인공은 밖에서 지정할 수 없고 구름 안 공격 자리도 없다. 닿을 수 있는 바깥 대상을 고르고 판을 돌지 않는다.
            var input = WithSecond(EnemyFollowupEstimateTests.World(width: 9, height: 7, actorCell: Origin, targetCell: new CellCoord(4, 3)),
                new CellCoord(0, 0));
            input = HaboobTargetingTests.WithArea(input, new CellCoord(4, 3));
            var field = Build(input);
            Assert.That(field.TryGetBestTarget(Origin, out var best), Is.True);
            Assert.That(best, Is.EqualTo(Second));
            Assert.That(CandidateProgress(input, new CellCoord(1, 2)), Is.EqualTo(1));
        }

        [Test]
        public void AlliedUnitsAreSoftHostileAreHard()
        {
            // 한 칸 통로(y=1)의 가운데를 아군이 막으면 비용이 더해질 뿐 길은 남는다.
            var corridor = Corridor();
            var allied = WithUnit(corridor, CombatCommandTestData.Character(12, EntityKey.Unit_Demon, new CellCoord(3, 1), Owner.Enemy));
            var field = Build(allied);
            Assert.That(field.TryGetCost(new CellCoord(0, 1), out int cost), Is.True);
            Assert.That(cost, Is.EqualTo(5 + EnemyAiTuning.AllySoftCost));
            Assert.That(field.IsAttackPosition(new CellCoord(3, 1)), Is.False);
            Assert.That(CandidateProgress(allied, new CellCoord(2, 1)), Is.EqualTo(2));

            // 플레이어 유닛이 막으면 그 너머로는 길이 없고, 막은 유닛이 가장 가까운 대상이 된다.
            var hostile = WithSecond(corridor, new CellCoord(3, 1));
            field = Build(hostile);
            Assert.That(field.TryGetCost(new CellCoord(5, 1), out _), Is.False);
            Assert.That(field.TryGetBestTarget(new CellCoord(0, 1), out var best), Is.True);
            Assert.That(best, Is.EqualTo(Second));
        }

        // 끌어오기 추가 사거리와 관통(전체 탐색)까지 공용 도달 판정과 목표 칸이 같아야 한다.
        [TestCase("Square1")]
        [TestCase("Cardinal3")]
        [TestCase("FanDirection2")]
        [TestCase("RadialSplash2")]
        [TestCase("PullExtra")]
        [TestCase("Pierce")]
        public void GoalsMatchAttackReach(string shape)
        {
            static TargetingMethodSpec Method(TargetingShape s, TargetingChoiceMode m, TargetingSplash splash = TargetingSplash.None) =>
                new TargetingMethodSpec(s, m, splash, splash == TargetingSplash.None ? 0 : 50);
            var scene = shape switch
            {
                "Square1" => new EnemyPursuitEquivalenceTests.Scene(Method(TargetingShape.RadialSquare, TargetingChoiceMode.Single), 1),
                "Cardinal3" => new EnemyPursuitEquivalenceTests.Scene(Method(TargetingShape.CardinalLine, TargetingChoiceMode.Single), 3),
                "FanDirection2" => new EnemyPursuitEquivalenceTests.Scene(Method(TargetingShape.Fan, TargetingChoiceMode.Direction), 2),
                "RadialSplash2" => new EnemyPursuitEquivalenceTests.Scene(
                    Method(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial), 2),
                "PullExtra" => new EnemyPursuitEquivalenceTests.Scene(Method(TargetingShape.CardinalLine, TargetingChoiceMode.Single), 1,
                    pullExtra: 1),
                "Pierce" => new EnemyPursuitEquivalenceTests.Scene(Method(TargetingShape.EightDirectionLine, TargetingChoiceMode.Single), 3,
                    piercingCap: int.MaxValue),
                _ => throw new ArgumentOutOfRangeException(nameof(shape)),
            };
            AssertGoalsMatchReference(scene.Build(new CellCoord(1, 1)), scene.TargetId, 7, 7);
        }

        /// <summary>
        /// 같은 공격 프로필(같은 유닛 키)을 쓰는 두 적도 목표 칸은 각자의 사실로 정한다. 목표 칸을 행위자끼리 나누는 보관소가 생겨도
        /// 사거리·공격 제한·도착 보정·크기·관통 줄 위 유닛 같은 행위자별 값이 섞이면 안 된다(계획 R-2 검토 요청).
        /// 끌어오기는 적의 경우 프로필 정의와 공개 상태가 반드시 같아(PullAttackCalculator) 같은 프로필의 두 적이 다를 수 없다.
        /// </summary>
        [TestCase("Range")]
        [TestCase("Disarm")]
        [TestCase("Modifier")]
        public void SameProfileActorsKeepTheirOwnGoals(string variant)
        {
            // 두 적 모두 같은 유닛 키·같은 사거리 1에서 시작한다.
            var other = new EntityId(12);
            var input = WithUnit(EnemyFollowupEstimateTests.World(width: 7, height: 7, actorCell: new CellCoord(1, 1),
                targetCell: new CellCoord(4, 4)), CombatCommandTestData.With(
                    CombatCommandTestData.Character(12, EntityKey.Unit_Demon, new CellCoord(5, 1), Owner.Enemy), appointRange: 1));
            int baseline = GoalCount(input.WithActor(other), 7, 7);
            input = variant switch
            {
                // 유닛마다의 사거리 값이 다르면 같은 프로필이어도 공격 위치가 다르다.
                "Range" => CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == other
                    ? CombatCommandTestData.With(c, appointRange: 2) : c).ToArray()),
                // 여러 턴 기하에서는 지금의 공격 봉쇄를 풀어 본다(EnemyAttackReach.MovedTo).
                "Disarm" => CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == other
                    ? c.With(debuffs: new CharacterDebuffState(disarm: new DisarmState(
                        new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.Disarm, "main"), 2))) : c).ToArray()),
                "Modifier" => CliffFallTests.WithEnvironment(input,
                    new DestinationTileModifierFact(71, new CellCoord(5, 1), true, new[] { Owner.Enemy }, 10, 0)),
                _ => throw new ArgumentOutOfRangeException(nameof(variant)),
            };
            AssertGoalsMatchReference(input, Main, 7, 7);
            AssertGoalsMatchReference(input.WithActor(other), Main, 7, 7);
            int first = GoalCount(input, 7, 7), second = GoalCount(input.WithActor(other), 7, 7);
            if (variant == "Range") Assert.That(second, Is.GreaterThan(first), "사거리가 긴 쪽의 공격 위치가 더 많아야 한다.");
            if (variant == "Disarm") Assert.That(second, Is.EqualTo(baseline), "무장 해제는 여러 턴 목표 칸을 바꾸지 않는다.");
        }

        [Test]
        public void TargetValueAndHeadquartersOrder()
        {
            // 값 = 기본 공격 한 번의 손실 천분율, 처치면 처치 가중치를 더한다(행위자 공격력 40).
            var input = WithSecond(EnemyFollowupEstimateTests.World(width: 9, height: 7, actorCell: Origin, targetCell: new CellCoord(7, 3)),
                new CellCoord(7, 5), health: 30);
            var field = Build(input);
            Assert.That(field.TargetValue(Main), Is.EqualTo(400));
            Assert.That(field.TargetValue(Second), Is.EqualTo(300 + EnemyUtilityScorer.OpposingEliminationWeight));

            // 본부는 닿는 유닛 공격 위치가 없을 때만 대상이다. 본부(2,2)가 주인공(6,1)보다 가깝다.
            var open = WithHeadquarters(EnemyFollowupEstimateTests.World(width: 7, height: 3, actorCell: new CellCoord(0, 1),
                targetCell: new CellCoord(6, 1)), new CellCoord(2, 2));
            field = Build(open);
            Assert.That(field.TryGetBestTarget(new CellCoord(0, 1), out var best), Is.True);
            Assert.That(best, Is.EqualTo(Main));
            var walled = CliffFallTests.WithEnvironment(open, new[] { new CellCoord(5, 0), new CellCoord(5, 1), new CellCoord(5, 2),
                new CellCoord(6, 0), new CellCoord(6, 2) }.Select((cell, i) => (MovementTileFact)WallStateLifecycleTests.Wall(200 + i, cell)).ToArray());
            field = Build(walled);
            Assert.That(field.TryGetBestTarget(new CellCoord(0, 1), out best), Is.True);
            Assert.That(best, Is.EqualTo(HeadquartersId));
        }

        [Test]
        public void TauntRestrictsFieldToSource()
        {
            // 두 번째 대상이 더 가깝지만 도발 의무가 있으면 필드는 그 원본만 대상으로 한다.
            var input = WithSecond(EnemyFollowupEstimateTests.World(width: 9, height: 7, actorCell: Origin, targetCell: new CellCoord(7, 3)),
                new CellCoord(1, 5));
            Assert.That(Build(input).TryGetBestTarget(Origin, out var free), Is.True);
            Assert.That(free, Is.EqualTo(Second));
            var restricted = Build(input, new[] { Main });
            Assert.That(restricted.TryGetBestTarget(Origin, out var forced), Is.True);
            Assert.That(forced, Is.EqualTo(Main));
            Assert.That(restricted.TargetValue(Second), Is.Zero);
        }

        [Test]
        public void PartitionsProduceIdenticalFields()
        {
            var input = WithUnit(WithSecond(EnemyFollowupEstimateTests.World(width: 9, height: 7, actorCell: Origin,
                targetCell: new CellCoord(7, 3)), new CellCoord(1, 6)), CombatCommandTestData.Character(12, EntityKey.Unit_Demon,
                new CellCoord(2, 3), Owner.Enemy));
            string expected = null;
            foreach (int partition in new[] { 1, 2, 31 })
            {
                string actual = Describe(Build(input, partition: partition), 9, 7);
                expected ??= actual;
                Assert.That(actual, Is.EqualTo(expected), $"partition={partition}");
            }
        }

        private static readonly EntityId HeadquartersId = new EntityId(80);

        /// <summary>행위자(0,1)·주인공(6,1)이 있는 7×3 판의 가운데 줄만 열린 통로다.</summary>
        private static CombatCommandInput Corridor()
        {
            var input = EnemyFollowupEstimateTests.World(width: 7, height: 3, actorCell: new CellCoord(0, 1), targetCell: new CellCoord(6, 1));
            var walls = new List<MovementTileFact>();
            for (int x = 0; x < 7; x++)
            {
                walls.Add(WallStateLifecycleTests.Wall(100 + x, new CellCoord(x, 0)));
                walls.Add(WallStateLifecycleTests.Wall(120 + x, new CellCoord(x, 2)));
            }
            return CliffFallTests.WithEnvironment(input, walls.ToArray());
        }

        private static CombatCommandInput WithSecond(CombatCommandInput input, CellCoord cell, int health = 100)
        {
            var unit = CombatCommandTestData.Character(2, EntityKey.Unit_Cowboy, cell, Owner.Player);
            if (health != 100) unit = unit.WithHealth(new HealthStateSnapshot(health, 100));
            return WithUnit(input, unit);
        }

        internal static CombatCommandInput WithUnit(CombatCommandInput input, CharacterCombatFacts unit)
        {
            var profiles = input.Profiles.Any(p => p.Definition.Key == unit.CharacterKey) ? input.Profiles.ToArray()
                : input.Profiles.Append(new CombatAttackProfile(CombatCommandTestData.Spec(unit.CharacterKey, 1),
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))).ToArray();
            // 소유 기록·영혼 수를 새 유닛까지 맞춘다(위협 필드가 플레이어 유닛의 공격력을 읽는다). 하붑·벽은 이 뒤에 더한다.
            return MineEnvironmentTests.Copy(CombatCommandTestData.With(input, characters: input.Characters.Append(unit).ToArray(),
                profiles: profiles));
        }

        private static CombatCommandInput WithHeadquarters(CombatCommandInput input, CellCoord cell)
        {
            var headquarters = new StructureCombatFacts(HeadquartersId, new StructureSpec(EntityKey.Structure_Headquarters, 1, 1,
                new[] { CellCoord.Zero }, 100, false, 0), new HealthStateSnapshot(100, 100), cell, Rotation4.R0, new[] { cell }, Owner.Player);
            return CombatCommandTestData.With(input, state: input.State.With(structures: new[] { headquarters }));
        }

        internal static EnemyEngagementField Build(CombatCommandInput input, IReadOnlyCollection<EntityId> restrictTo = null, int partition = 1)
        {
            string before = EnemyActionAssessmentTests.Encode(input);
            var actor = input.Characters.Single(unit => unit.EntityId == input.ActorId);
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            EnemyEngagementField field = null;
            using var steps = EnemyEngagementField.BuildSteps(context, input.WithActor(input.ActorId), actor, restrictTo,
                value => field = value).GetEnumerator();
            int count = 0;
            bool more = true;
            while (more && count < 4000000)
                for (int i = 0; i < partition && more; i++, count++) more = steps.MoveNext();
            Assert.That(more, Is.False);
            Assert.That(field, Is.Not.Null);
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
            return field;
        }

        /// <summary>필드의 공격 위치가 공용 도달 판정과 같은지 본다. 유닛이 선 칸은 착지할 수 없어 비교하지 않는다.</summary>
        internal static void AssertGoalsMatchReference(CombatCommandInput input, EntityId target, int width, int height)
        {
            var field = Build(input, new[] { target });
            var actor = input.Characters.Single(unit => unit.EntityId == input.ActorId);
            var occupied = new HashSet<CellCoord>(input.Characters.Where(unit => unit.EntityId != actor.EntityId &&
                unit.Area == CharacterRuntimeArea.Field && !unit.Health.IsDead).SelectMany(unit => unit.OccupiedCells));
            int compared = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var cell = new CellCoord(x, y);
                    if (occupied.Contains(cell) || !field.TryGetCost(cell, out int cost)) continue;
                    bool expected = EnemyPursuitReference.CanAttackFrom(input, actor, cell, target);
                    Assert.That(cost == 0, Is.EqualTo(expected), cell.ToString());
                    compared++;
                }
            Assert.That(compared, Is.Positive);
        }

        private static int GoalCount(CombatCommandInput input, int width, int height)
        {
            var field = Build(input, new[] { Main });
            int count = 0;
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (field.IsAttackPosition(new CellCoord(x, y))) count++;
            return count;
        }

        private static long CandidateProgress(CombatCommandInput input, CellCoord endpoint)
        {
            var actor = input.Characters.Single(unit => unit.EntityId == input.ActorId);
            using var candidates = new EnemyActionCandidates(new EnemyDecisionContext(input, EnemyDecisionSettings.Default), actor,
                EnemyActionCandidateTests.MakeForecast(input));
            while (!candidates.IsComplete) candidates.Step();
            // 진척은 종점만 본다. 경로는 가로 다음 세로로 한 칸씩 잇는다.
            var cells = new List<CellCoord> { actor.Anchor.Value };
            while (cells[^1] != endpoint)
            {
                var last = cells[^1];
                cells.Add(last.X != endpoint.X ? new CellCoord(last.X + Math.Sign(endpoint.X - last.X), last.Y)
                    : new CellCoord(last.X, last.Y + Math.Sign(endpoint.Y - last.Y)));
            }
            return candidates.Progress(EnemyDecision.Move(actor.EntityId, new MovementPath(cells), default));
        }

        private static string Describe(EnemyEngagementField field, int width, int height)
        {
            var text = new StringBuilder();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var cell = new CellCoord(x, y);
                    text.Append(field.TryGetCost(cell, out int cost) ? cost.ToString() : "-").Append(':');
                    text.Append(field.TryGetBestTarget(cell, out var best) ? best.Value.ToString() : "-").Append(' ');
                }
            return text.Append(field.ExpandedNodes).ToString();
        }
    }
}
