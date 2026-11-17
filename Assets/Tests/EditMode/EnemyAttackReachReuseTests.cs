using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyAttackReachReuseTests
    {
        private static readonly EntityId Main = new EntityId(1);
        private static readonly EntityId Second = new EntityId(20);
        private static readonly CellCoord Start = new CellCoord(2, 2);

        [Test]
        public void RepeatedPositionTargetsMatchUncachedReach()
        {
            var input = EnemyFollowupEstimateTests.World(actorCell: Start, targetCell: new CellCoord(4, 2), range: 3);
            var second = CombatCommandTestData.Character(20, EntityKey.Unit_Akimbo, new CellCoord(3, 3), Owner.Player);
            input = TraitCombatFixtures.Characters(input, input.Characters.Append(second).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Append(
                new CombatAttackProfile(CombatCommandTestData.Spec(second.CharacterKey, 10), input.Profiles[0].Method)).ToArray());
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);

            var first = Count(() => Reach(input, actor, Main, geometry, scope).From(Start));
            var repeat = Count(() => Reach(input, actor, Second, geometry, scope).From(Start));
            var uncachedFirst = Count(() => Reach(input, actor, Main, geometry).From(Start));
            var uncachedSecond = Count(() => Reach(input, actor, Second, geometry).From(Start));

            Assert.That(first.hit && repeat.hit, Is.True);
            Assert.That((first.hit, repeat.hit), Is.EqualTo((uncachedFirst.hit, uncachedSecond.hit)));
            Assert.That((first.damage, repeat.damage, uncachedSecond.damage), Is.EqualTo((1, 0, 1)));
            Assert.That((first.bindings, repeat.bindings, uncachedSecond.bindings), Is.EqualTo((2, 0, 2)));
            Assert.That(scope.Prerequisites.Count, Is.EqualTo(1));
        }

        [Test]
        public void DifferentPositionKeepsDamageAndRangeIndependent()
        {
            var input = EnemyFollowupEstimateTests.World(actorCell: Start, targetCell: new CellCoord(6, 2), range: 4);
            var restricted = Actor(input).With(debuffs: new CharacterDebuffState(disarm: new DisarmState(
                new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.Disarm, "main"), 2)));
            input = CombatCommandTestData.With(input, characters: new[] { input.Characters[0], restricted });
            var modified = new CellCoord(4, 2);
            input = CliffFallTests.WithEnvironment(input,
                new StaticTerrainFact(71, modified, StaticTerrainKind.Sandstorm, true, new[] { Owner.Enemy, Owner.Player }),
                new DestinationTileModifierFact(72, modified, true, new[] { Owner.Enemy, Owner.Player }, 9, 0));
            var plain = new CellCoord(3, 2);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, restricted, geometry);
            var cached = Reach(input, restricted, Main, geometry, scope);
            var uncached = Reach(input, restricted, Main, geometry);
            foreach (var cell in new[] { modified, plain, modified })
            {
                Assert.That(uncached.From(cell), Is.True);
                Assert.That(cached.From(cell), Is.True, cell.ToString());
            }

            Assert.That(scope.Moved[modified].Debuffs.Disarm, Is.Null);
            Assert.That(restricted.Debuffs.Disarm, Is.Not.Null);
            var normal = scope.Prerequisites[plain];
            var special = scope.Prerequisites[modified];
            Assert.That((normal.damage, special.damage), Is.EqualTo((40, 49)));
            Assert.That(normal.selection.Range, Is.EqualTo(4));
            Assert.That(special.selection.Range, Is.LessThan(normal.selection.Range));
        }

        [Test]
        public void SameGeometryDifferentEnvironmentIsRejected()
        {
            var input = EnemyFollowupEstimateTests.World(actorCell: Start);
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            Reach(input, actor, Main, geometry, scope).From(Start);
            var sameValues = new MovementEnvironmentFacts(geometry.Environment.Tiles);
            Assert.That(sameValues.SameValues(geometry.Environment), Is.True);
            Assert.Throws<InvalidOperationException>(() => new EnemyAttackReach(input, actor, Main, sameValues, geometry, scope));
        }

        [TestCase("world")]
        [TestCase("actor")]
        [TestCase("geometry")]
        public void NewWorldCannotJoinExistingScope(string changed)
        {
            var input = EnemyFollowupEstimateTests.World(actorCell: Start, targetCell: new CellCoord(3, 2));
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            Assert.That(Reach(input, actor, Main, geometry, scope).From(Start), Is.True);
            var otherActor = CombatCommandTestData.With(actor, appointRange: 2);
            var otherInput = CombatCommandTestData.With(input, characters: new[] { input.Characters[0], otherActor });
            var otherGeometry = new EnemyDecisionContext.ObservedGeometry(geometry.Environment, geometry.Walls, geometry.Elevation);
            Assert.That(otherActor.EntityId, Is.EqualTo(actor.EntityId));
            Assert.Throws<InvalidOperationException>(() => new EnemyAttackReach(
                changed == "world" ? otherInput : input, changed == "actor" ? otherActor : actor, Main,
                geometry.Environment, changed == "geometry" ? otherGeometry : geometry, scope));
            Assert.That(scope.Prerequisites.Count, Is.EqualTo(1));
        }

        [Test]
        public void NoAimDoesNotBuildDamageOrSelection()
        {
            var input = EnemyFollowupEstimateTests.World(actorCell: Start, targetCell: new CellCoord(7, 2), range: 1);
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            var near = new CellCoord(6, 2);
            var cached = Reach(input, actor, Main, geometry, scope);
            var miss = Count(() => cached.From(Start));
            var hit = Count(() => cached.From(near));
            Assert.That(miss, Is.EqualTo((false, 0, 0)));
            Assert.That(miss.hit, Is.EqualTo(Reach(input, actor, Main, geometry).From(Start)));
            Assert.That(hit, Is.EqualTo((true, 1, 2)));
            Assert.That(scope.Prerequisites.Keys, Is.EqualTo(new[] { near }));
        }

        private static CharacterCombatFacts Actor(CombatCommandInput input) =>
            input.Characters.Single(unit => unit.EntityId == input.ActorId);

        [TestCase(1)]
        [TestCase(3)]
        public void CachedDeliveryMatchesEveryRequestedTarget(int piercingCap)
        {
            var input = PiercingShotTestData.Input(cap: piercingCap);
            var actor = Actor(input);
            var origin = actor.Anchor.Value;
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            var targets = new[] { new EntityId(9), new EntityId(2), new EntityId(7) };
            foreach (var target in targets)
            {
                var expected = Reach(input, actor, target, geometry).From(origin);
                Assert.That(Reach(input, actor, target, geometry, scope).From(origin), Is.EqualTo(expected));
            }
            foreach (var target in targets)
            {
                var repeat = TraceReach(Reach(input, actor, target, geometry, scope), origin);
                Assert.That(repeat.hit, Is.EqualTo(Reach(input, actor, target, geometry).From(origin)));
                Assert.That(repeat.phases.Count(phase => phase == "SelectionPolicy"), Is.Zero,
                    "완료된 같은 위치·조준은 대상이 달라도 전달 계산을 다시 시작하지 않습니다.");
            }
        }

        private static (bool hit, List<string> phases) TraceReach(EnemyAttackReach reach, CellCoord cell)
        {
            bool hit = false;
            var phases = new List<string>();
            foreach (var phase in reach.FromSteps(cell, value => hit = value)) phases.Add(phase);
            return (hit, phases);
        }

        [Test]
        public void NullDeliveryIsACompletedCachedFailure()
        {
            var input = PiercingShotTestData.Input(cap: 1);
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            var origin = actor.Anchor.Value;
            Assert.That(Reach(input, actor, new EntityId(2), geometry, scope).From(origin), Is.False);
            Assert.That(scope.Deliveries.Values.Any(value => value == null), Is.True);
            Assert.That(scope.Deliveries.Values.Any(value => value != null), Is.True);
            var repeat = TraceReach(Reach(input, actor, new EntityId(2), geometry, scope), origin);
            Assert.That(repeat.hit, Is.False);
            Assert.That(repeat.phases, Does.Not.Contain("SelectionPolicy"));
            Assert.That(Reach(input, actor, new EntityId(9), geometry, scope).From(origin), Is.True,
                "한 대상의 빗나감은 같은 전달에 포함된 다른 대상의 실패로 저장하지 않습니다.");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(256)]
        public void CapacityZeroAndFullPreserveReachAndDelivery(int capacity)
        {
            var input = PiercingShotTestData.Input(cap: 3);
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry, capacity);
            foreach (var target in new[] { new EntityId(9), new EntityId(2), new EntityId(7), new EntityId(2) })
            {
                Assert.That(Reach(input, actor, target, geometry, scope).From(actor.Anchor.Value),
                    Is.EqualTo(Reach(input, actor, target, geometry).From(actor.Anchor.Value)));
                Assert.That(scope.Deliveries.Count, Is.LessThanOrEqualTo(capacity));
            }
            Assert.That(scope.Deliveries.Count == 0, Is.EqualTo(capacity == 0));
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            foreach (var entry in scope.Deliveries)
            {
                var cell = entry.Key.cell;
                ResolvedAttackDelivery expected = null;
                foreach (var _ in CombatAttackQueries.DeliverySteps(scope.Projected[cell], scope.Moved[cell], profile,
                    entry.Key.aim, scope.Prerequisites[cell].damage, geometry.Environment, geometry.Walls, geometry.Elevation,
                    value => expected = value, scope.Prerequisites[cell].selection)) { }
                Assert.That(DeliveryFacts(entry.Value), Is.EqualTo(DeliveryFacts(expected)),
                    "관통 전달의 순서·피해·차단 정보까지 같아야 합니다.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SharedAimPreservesAllAndSplashDelivery(bool all)
        {
            var input = all
                ? CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare,
                    TargetingChoiceMode.All, TargetingSplash.None, 0), AttackAim.ForAll())
                : CombatCommandTestData.Input();
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            foreach (var target in input.Characters.Where(unit => unit.Owner != actor.Owner))
            {
                bool expected = Reach(input, actor, target.EntityId, geometry).From(actor.Anchor.Value);
                Assert.That(Reach(input, actor, target.EntityId, geometry, scope).From(actor.Anchor.Value), Is.EqualTo(expected));
                Assert.That(TraceReach(Reach(input, actor, target.EntityId, geometry, scope), actor.Anchor.Value).phases,
                    Does.Not.Contain("SelectionPolicy"));
            }
            Assert.That(scope.Deliveries.Values.Any(value => value != null), Is.True);
        }

        [TestCase("SelectionPolicy")]
        [TestCase("DeliveryDirectComplete")]
        public void CancelledMissDoesNotPublishEntry(string stopAt)
        {
            var input = EnemyFollowupEstimateTests.World(actorCell: Start, targetCell: new CellCoord(3, 2));
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            bool completed = false;
            using (var cursor = Reach(input, actor, Main, geometry, scope).FromSteps(Start, _ => completed = true).GetEnumerator())
            {
                bool found = false;
                while (cursor.MoveNext())
                    if (cursor.Current == stopAt) { found = true; break; }
                Assert.That(found, Is.True, stopAt);
                Assert.That(scope.Deliveries, Is.Empty,
                    "전달 콜백 뒤라도 반복자가 끝나기 전에는 완료된 항목이 아닙니다.");
            }
            Assert.That(completed, Is.False);
            var retry = TraceReach(Reach(input, actor, Main, geometry, scope), Start);
            Assert.That(retry.hit, Is.True);
            Assert.That(retry.phases, Does.Contain("SelectionPolicy"));
            Assert.That(TraceReach(Reach(input, actor, Main, geometry, scope), Start).phases,
                Does.Not.Contain("SelectionPolicy"));
        }

        [Test]
        public void FaultedMissDoesNotPublishEntry()
        {
            var input = PiercingShotTestData.Input(cap: 1);
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            var origin = actor.Anchor.Value;
            using (var prepare = Reach(input, actor, new EntityId(9), geometry, scope).FromSteps(origin, _ => { }).GetEnumerator())
            {
                while (prepare.MoveNext() && prepare.Current != "PursuitSelectionPreparation") { }
            }
            Assert.That(scope.Prerequisites.Count, Is.EqualTo(1));
            CoreProfiling.Install(new ThrowOnBindingCapture());
            try
            {
                Assert.Throws<InvalidOperationException>(() => Reach(input, actor, new EntityId(9), geometry, scope).From(origin));
            }
            finally { CoreProfiling.Install(null); }
            Assert.That(scope.Deliveries.ContainsKey((origin, AttackAim.ForCell(new CellCoord(3, 3)))), Is.False);
            Assert.That(Reach(input, actor, new EntityId(9), geometry, scope).From(origin), Is.True);
        }

        [Test]
        public void DifferentEnvironmentCannotHitCachedDelivery()
        {
            var input = EnemyFollowupEstimateTests.World(actorCell: Start, targetCell: new CellCoord(3, 2));
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            Assert.That(Reach(input, actor, Main, geometry, scope).From(Start), Is.True);
            Assert.That(scope.Deliveries.Count, Is.Positive);
            Assert.Throws<InvalidOperationException>(() => new EnemyAttackReach(input, actor, Main,
                new MovementEnvironmentFacts(geometry.Environment.Tiles), geometry, scope));
        }

        [Test]
        public void DefaultDeliveryCapacityStopsInsertingWithoutDroppingQueries()
        {
            var input = EnemyAiBenchmarkFixtures.Build("A8", 1).WithActor(new EntityId(11));
            var actor = Actor(input);
            var geometry = Observe(input);
            var scope = new EnemyAttackReach.Relocations(input, actor, geometry);
            int queriesAfterFull = 0;
            foreach (var target in input.Characters.Where(unit => unit.Owner != actor.Owner))
            {
                var cached = Reach(input, actor, target.EntityId, geometry, scope);
                var uncached = Reach(input, actor, target.EntityId, geometry);
                for (int y = 0; y < input.State.BoardHeight; y++)
                for (int x = 0; x < input.State.BoardWidth; x++)
                {
                    var cell = new CellCoord(x, y);
                    if (input.Characters.Any(unit => unit.EntityId != actor.EntityId && unit.OccupiedCells.Contains(cell))) continue;
                    bool full = scope.Deliveries.Count == 256;
                    Assert.That(cached.From(cell), Is.EqualTo(uncached.From(cell)), cell.ToString());
                    Assert.That(scope.Deliveries.Count, Is.LessThanOrEqualTo(256));
                    if (full) queriesAfterFull++;
                }
            }
            Assert.That(scope.Deliveries.Count, Is.EqualTo(256));
            Assert.That(queriesAfterFull, Is.Positive);
            Assert.Throws<ArgumentOutOfRangeException>(() => new EnemyAttackReach.Relocations(input, actor, geometry, -1));
        }

        private static string DeliveryFacts(ResolvedAttackDelivery value) => value == null ? "null" :
            EnemyActionAssessmentTests.Encode(new object[] { value.AffectedCells, value.PiercingShot, value.BlockedContacts,
                value.Hits.Select(hit => new object[] { hit.TargetId, hit.Role, hit.BaseDamage, hit.AppliedPercent,
                    hit.ImpactOrigin, hit.Cause }).ToArray() });

        private sealed class ThrowOnBindingCapture : ICoreProfiler
        {
            public void Begin(CoreProfileSection section)
            {
                if (section == CoreProfileSection.AttackEffectBinding) throw new InvalidOperationException("시험용 전달 중단");
            }

            public void End(CoreProfileSection section) { }
        }

        private static EnemyDecisionContext.ObservedGeometry Observe(CombatCommandInput input)
        {
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            EnemyDecisionContext.ObservedGeometry geometry = null;
            foreach (var _ in context.GeometrySteps(value => geometry = value)) { }
            return geometry;
        }

        private static EnemyAttackReach Reach(CombatCommandInput input, CharacterCombatFacts actor, EntityId target,
            EnemyDecisionContext.ObservedGeometry geometry, EnemyAttackReach.Relocations scope = null)
        {
            var reach = new EnemyAttackReach(input, actor, target, geometry.Environment, geometry, scope);
            foreach (var _ in reach.TargetSteps()) { }
            return reach;
        }

        private static (bool hit, int damage, int bindings) Count(Func<bool> run)
        {
            var counter = new AttackCounter();
            CoreProfiling.Install(counter);
            try
            {
                bool hit = run();
                return (hit, counter.Damage, counter.Bindings);
            }
            finally
            {
                CoreProfiling.Install(null);
            }
        }

        private sealed class AttackCounter : ICoreProfiler
        {
            internal int Damage { get; private set; }
            internal int Bindings { get; private set; }

            public void Begin(CoreProfileSection section)
            {
                if (section == CoreProfileSection.AttackCommonDamage) Damage++;
                if (section == CoreProfileSection.AttackEffectBinding) Bindings++;
            }

            public void End(CoreProfileSection section) { }
        }
    }
}
