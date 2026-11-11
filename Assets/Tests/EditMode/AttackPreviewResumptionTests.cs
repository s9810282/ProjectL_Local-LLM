using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Tests.Core
{
    public sealed class AttackPreviewResumptionTests
    {
        private sealed class ThrowingRules : IReadOnlyList<IMovementTileRule>
        {
            internal readonly Exception Failure = new InvalidOperationException("probe");
            public int Count => throw Failure;
            public IMovementTileRule this[int index] => throw Failure;
            public IEnumerator<IMovementTileRule> GetEnumerator() => throw Failure;
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private static string Facts(object value) => AttackPreviewParityTests.Serialize(value, new HashSet<string>());

        private sealed class PresenceCounter : ICoreProfiler
        {
            internal int Calls;
            public void Begin(CoreProfileSection section)
            {
                if (section == CoreProfileSection.TraitPresenceReconcile) Calls++;
            }
            public void End(CoreProfileSection section) { }
        }

        private sealed class VisibilityCounter
        {
            internal int Copies;
        }

        private sealed class VisibilityProbeTile : MovementTileFact
        {
            private readonly VisibilityCounter _counter;

            internal VisibilityProbeTile(int id, VisibilityCounter counter, IReadOnlyList<Owner> knownTo)
                : base(id, CellCoord.Zero, "preview-probe", 0, true, knownTo) => _counter = counter;

            public override bool TriggersArrivalRule => false;
            public override bool SameValues(MovementTileFact other) => SameSourceValues(other);

            public override MovementTileFact WithKnownTo(IReadOnlyList<Owner> knownTo)
            {
                _counter.Copies++;
                return new VisibilityProbeTile(checked((int)Id), _counter, knownTo);
            }
        }

        [Test]
        public void EnvironmentVisibilityCopiesOneSourcePerStep()
        {
            const int count = 24;
            var source = CombatCommandTestData.Input(empty: true);
            var counter = new VisibilityCounter();
            var environment = new MovementEnvironmentFacts(Enumerable.Range(1, count)
                .Select(id => new VisibilityProbeTile(id, counter, new[] { Owner.Player, Owner.Enemy })).ToArray());
            var input = CombatCommandTestData.With(source, state: source.State.With(environment: environment,
                environmentProgress: EnvironmentProgressFacts.Initial(environment)));
            using var work = CombatCommandCalculator.BeginPreview(input, EnemyAiBenchmarkFixtures.Rules);
            while (!work.IsComplete)
            {
                int before = counter.Copies;
                work.Step();
                Assert.That(counter.Copies - before, Is.LessThanOrEqualTo(1), work.Phase);
            }
            Assert.That(counter.Copies, Is.EqualTo(count));
        }

        [TestCase(TargetingChoiceMode.All)]
        [TestCase(TargetingChoiceMode.Direction)]
        public void AreaSelectionRetainsGeometryAndTargetCursors(TargetingChoiceMode choice)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, choice, TargetingSplash.None, 0);
            var aim = choice == TargetingChoiceMode.All ? AttackAim.ForAll() : AttackAim.ForDirection(AttackDirection.Right);
            var input = CombatCommandTestData.Input(method, aim);
            using var work = CombatCommandCalculator.BeginPreview(input, EnemyAiBenchmarkFixtures.Rules);
            var phases = new List<string>();
            while (!work.IsComplete)
            {
                work.Step();
                phases.Add(work.Phase);
            }
            Assert.That(work.Preview.CanAttack, Is.True);
            Assert.That(phases.Count(p => p == "SelectionMaskItem"), Is.GreaterThan(1));
            Assert.That(phases.Count(p => p == "SelectionOcclusionCell"), Is.GreaterThan(1));
            Assert.That(phases.Count(p => p == "SelectionTargetItem"), Is.GreaterThan(1));
        }

        [TestCase("W8-1", "WallGeometryCell")]
        [TestCase("reflection-launched-pierce", "DeliveryPierceCell")]
        public void LongGeometryRetainsCellProgress(string id, string phase)
        {
            var vector = AttackPreviewParityTests.Vectors().Single(v => v.Id == id);
            using var work = CombatCommandCalculator.BeginPreview(vector.Input, vector.Rules);
            int visits = 0;
            while (!work.IsComplete)
            {
                work.Step();
                if (work.Phase == phase) visits++;
            }
            Assert.That(visits, Is.GreaterThan(1));
        }

        [Test]
        public void BentWallClippingYieldsEachPartBeforePublishingTheContact()
        {
            var wall = ConnectedWallGeometryTests.Build(new[]
            {
                ConnectedWallGeometryTests.Wall(71, new CellCoord(3, 2)),
                ConnectedWallGeometryTests.Wall(72, new CellCoord(3, 3)),
                ConnectedWallGeometryTests.Wall(73, new CellCoord(4, 2))
            });
            var origin = new CellCoord(1, 3);
            var target = new CellCoord(5, 3);
            var selection = new AttackCellSelection(target, new[] { target }, Array.Empty<CellCoord>());
            var elevation = new AttackElevation(Array.Empty<CellCoord>());
            IReadOnlyList<AttackBlockedContact> result = null;
            using var cursor = AttackOcclusionResolver.ResolveCellSteps(origin, selection, wall, elevation,
                AttackOcclusionSpec.Default, (_, blocked) => result = blocked).GetEnumerator();

            int parts = 0;
            while (cursor.MoveNext())
            {
                if (cursor.Current != "SelectionWallPart") continue;
                parts++;
                Assert.That(result, Is.Null);
            }

            Assert.That(parts, Is.EqualTo(wall.Runs.Single().Parts.Count));
            Assert.That(result.Single().WallSourceIds, Is.EqualTo(new long[] { 71, 72, 73 }));
            Assert.That(result.Single().Contact, Is.EqualTo(target));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DirectDamageDoesNotDrainPresenceWork(bool sacrifice)
        {
            var counter = new PresenceCounter();
            var input = sacrifice ? SacrificeTraitTests.Input(splash: true) : CombatCommandTestData.Input();
            using var work = CombatCommandCalculator.BeginPreview(input, EnemyAiBenchmarkFixtures.Rules);
            CoreProfiling.Install(counter);
            try
            {
                while (!work.IsComplete)
                {
                    counter.Calls = 0;
                    work.Step();
                    Assert.That(counter.Calls, Is.LessThanOrEqualTo(1), work.Phase);
                }
            }
            finally
            {
                CoreProfiling.Install(null);
            }
        }

        [TestCase("weak-knockback")]
        [TestCase("weak-two")]
        [TestCase("weak-main")]
        [TestCase("weak-pull")]
        [TestCase("pull-tornado")]
        [TestCase("barrel-cascade")]
        public void NestedEffectsDoNotDrainPresenceWork(string id)
        {
            var vector = AttackPreviewParityTests.MovementVectors().Concat(AttackPreviewParityTests.Vectors())
                .Single(v => v.Id == id);
            var counter = new PresenceCounter();
            using var work = CombatCommandCalculator.BeginPreview(vector.Input, vector.Rules);
            CoreProfiling.Install(counter);
            try
            {
                while (!work.IsComplete)
                {
                    counter.Calls = 0;
                    work.Step();
                    Assert.That(counter.Calls, Is.LessThanOrEqualTo(1), work.Phase);
                }
            }
            finally
            {
                CoreProfiling.Install(null);
            }
        }

        [Test]
        public void BeginStoresReferencesWithoutExecuting()
        {
            using var work = CombatCommandCalculator.BeginPreview(CombatCommandTestData.Input(), new ThrowingRules());
            Assert.That(work.IsComplete, Is.False);
            Assert.That(work.WorkVersion, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => _ = work.Preview);
            Assert.Throws<InvalidOperationException>(() => _ = work.Transition);
        }

        private sealed class FailingSacrificePresence : ICoreProfiler
        {
            internal readonly Exception Failure = new InvalidOperationException("child probe");
            private bool _planning;
            public void Begin(CoreProfileSection section)
            {
                if (section == CoreProfileSection.AttackSacrificePlanning) _planning = true;
                if (_planning && section == CoreProfileSection.TraitPresenceReconcile) throw Failure;
            }
            public void End(CoreProfileSection section)
            {
                if (section == CoreProfileSection.AttackSacrificePlanning) _planning = false;
            }
        }

        [Test]
        public void SacrificeChildFaultCannotResumeOrChangeSource()
        {
            var input = SacrificeTraitTests.Input(splash: true);
            string before = Facts(input);
            var profiler = new FailingSacrificePresence();
            using var work = CombatCommandCalculator.BeginPreview(input, EnemyAiBenchmarkFixtures.Rules);
            CoreProfiling.Install(profiler);
            try
            {
                var error = Assert.Throws<InvalidOperationException>(() =>
                {
                    while (!work.IsComplete) work.Step();
                });
                Assert.That(error, Is.SameAs(profiler.Failure));
                Assert.Throws<InvalidOperationException>(() => work.Step());
                Assert.Throws<InvalidOperationException>(() => _ = work.Transition);
                Assert.That(Facts(input), Is.EqualTo(before));
            }
            finally
            {
                CoreProfiling.Install(null);
            }
        }

        [Test]
        public void SacrificeRevalidationRetainsOrderAndCanBeCancelled()
        {
            var input = SacrificeTraitTests.Input(splash: true);
            var cell = new CellCoord(5, 4);
            input = TraitCombatFixtures.Characters(input, input.Characters.Where(c => c.EntityId.Value != 3)
                .Select(c => c.EntityId.Value == 10
                    ? c.WithPlacement(CharacterRuntimeArea.Field, null, cell, Rotation4.R0, new[] { cell })
                        .WithHealth(new HealthStateSnapshot(10, 100))
                    : c).ToArray());
            input = CombatCommandTestData.With(input, main: input.Main.WithCurrentHealth(10));
            var guard = CombatCommandTestData.Character(6, EntityKey.Unit_Cowboy, new CellCoord(6, 4), Owner.Player);
            guard = guard.With(stats: guard.Stats.With(category: CategorySnapshot.CreateStateless(CategoryKey.Heavy)));
            input = TraitCombatFixtures.Characters(input, input.Characters.Concat(new[] { guard }).ToArray());
            input = CombatCommandTestData.With(input, profiles: input.Profiles.Concat(new[]
            {
                new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Cowboy, 1, CategoryKey.Heavy),
                    new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0))
            }).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Cowboy, new SacrificeTraitSpec(2, 1));
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new EntrenchmentTraitSpec(1, 50));
            string before = Facts(input);
            var boundaries = new Dictionary<string, int>();
            using (var work = CombatCommandCalculator.BeginPreview(input, EnemyAiBenchmarkFixtures.Rules))
            {
                while (!work.IsComplete)
                {
                    work.Step();
                    if (work.Phase is "SacrificeBaselineComplete" or "SacrificeCandidateComplete" or
                        "SacrificeRemoveReservation" or "SacrificeRevalidationComplete")
                        boundaries.TryAdd(work.Phase, checked((int)work.WorkVersion));
                }
                Assert.That(TraitCombatFixtures.Final(work.Transition, 2).Health.CurrentHealth, Is.EqualTo(5));
                Assert.That(TraitCombatFixtures.Final(work.Transition, 10).Health.CurrentHealth, Is.EqualTo(3));
                Assert.That(TraitCombatFixtures.Final(work.Transition, 6).Health.CurrentHealth, Is.EqualTo(100));
            }
            Assert.That(boundaries.Keys, Does.Contain("SacrificeRemoveReservation"));
            Assert.That(boundaries.Keys, Does.Contain("SacrificeRevalidationComplete"));
            foreach (var boundary in boundaries)
            {
                using (var cancelled = CombatCommandCalculator.BeginPreview(input, EnemyAiBenchmarkFixtures.Rules))
                    for (int i = 0; i < boundary.Value; i++) cancelled.Step();
                Assert.That(Facts(input), Is.EqualTo(before), boundary.Key);
            }
        }

        [Test]
        public void EmptyReactionPreservesOriginalRuleAdmission()
        {
            var input = CombatCommandTestData.Input(empty: true);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            Assert.That(CombatAttackCalculator.Reaction(input, new ThrowingRules(), actor, input.Aim), Is.Null);
            Assert.That(CombatAttackCalculator.Reaction(input, null, actor, input.Aim), Is.Null);
        }

        [Test]
        public void FaultPropagatesWithoutRetry()
        {
            var rules = new ThrowingRules();
            using var work = CombatCommandCalculator.BeginPreview(CombatCommandTestData.Input(), rules);
            Assert.That(Assert.Throws<InvalidOperationException>(() => work.Step()), Is.SameAs(rules.Failure));
            Assert.Throws<InvalidOperationException>(() => work.Step());
            Assert.Throws<InvalidOperationException>(() => _ = work.IsComplete);
            Assert.Throws<InvalidOperationException>(() => _ = work.WorkVersion);
            Assert.Throws<InvalidOperationException>(() => _ = work.Preview);
            Assert.Throws<InvalidOperationException>(() => _ = work.Transition);
        }

        [Test]
        public void DisposedWorkerCannotResume()
        {
            var work = CombatCommandCalculator.BeginPreview(CombatCommandTestData.Input(), EnemyAiBenchmarkFixtures.Rules);
            work.Dispose();
            work.Dispose();
            Assert.Throws<ObjectDisposedException>(() => work.Step());
            Assert.Throws<ObjectDisposedException>(() => _ = work.IsComplete);
            Assert.Throws<ObjectDisposedException>(() => _ = work.WorkVersion);
            Assert.Throws<ObjectDisposedException>(() => _ = work.Preview);
            Assert.Throws<ObjectDisposedException>(() => _ = work.Transition);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void AllPartitionsMatchFrozenPreview(int partition)
        {
            var reference = AttackPreviewParityTests.ReadReference();
            foreach (var entry in AttackPreviewParityTests.ReadReference(Path.Combine(Directory.GetCurrentDirectory(),
                AttackPreviewParityTests.MovementReferencePath)))
                reference.Add(entry.Key, entry.Value);
            foreach (var vector in AttackPreviewParityTests.Vectors().Concat(AttackPreviewParityTests.MovementVectors()))
            {
                using var work = CombatCommandCalculator.BeginPreview(vector.Input, vector.Rules);
                string before = Facts(vector.Input);
                while (!work.IsComplete)
                {
                    for (int i = 0; i < partition && !work.IsComplete; i++)
                    {
                        long version = work.WorkVersion;
                        Assert.Throws<InvalidOperationException>(() => _ = work.Transition);
                        work.Step();
                        Assert.That(work.WorkVersion, Is.GreaterThan(version));
                        Assert.That(work.WorkVersion, Is.LessThan(1000000));
                    }
                    Assert.That(Facts(vector.Input), Is.EqualTo(before), vector.Id);
                }
                long completeVersion = work.WorkVersion;
                var transition = work.Transition;
                work.Step();
                Assert.That(work.WorkVersion, Is.EqualTo(completeVersion));
                Assert.That(work.Transition, Is.SameAs(transition));
                var actual = CombatCommandCalculator.Calculate(vector.Input, vector.Rules);
                string semantic = Facts(new object[] { before, work.Preview, work.Transition, actual });
                Assert.That(semantic, Is.EqualTo(AttackPreviewParityTests.Decode(reference[vector.Id].Split('|')[0])), vector.Id);
            }
        }

        [Test]
        public void CancelAtEveryObservedBoundaryLeavesSourceUntouched()
        {
            foreach (var vector in AttackPreviewParityTests.Vectors().Where(v =>
                v.Id == "single" || v.Id == "pull" || v.Id == "terminal-main" || v.Id == "sacrifice-ordered")
                .Concat(AttackPreviewParityTests.MovementVectors()))
            {
                int count;
                using (var whole = CombatCommandCalculator.BeginPreview(vector.Input, vector.Rules))
                {
                    while (!whole.IsComplete) whole.Step();
                    count = checked((int)whole.WorkVersion);
                }
                string before = Facts(vector.Input);
                for (int boundary = 0; boundary <= count; boundary++)
                {
                    var work = CombatCommandCalculator.BeginPreview(vector.Input, vector.Rules);
                    for (int i = 0; i < boundary; i++) work.Step();
                    work.Dispose();
                    Assert.That(Facts(vector.Input), Is.EqualTo(before), vector.Id + ":" + boundary);
                }
            }
        }

        [Test]
        public void TwoSuspendedPreviewsDoNotShareMutableState()
        {
            var input = PullTestData.Input();
            string before = Facts(input);
            using var first = CombatCommandCalculator.BeginPreview(input, EnemyAiBenchmarkFixtures.Rules);
            using var second = CombatCommandCalculator.BeginPreview(input, EnemyAiBenchmarkFixtures.Rules);
            while (!first.IsComplete || !second.IsComplete)
            {
                if (!first.IsComplete) first.Step();
                for (int i = 0; i < 2 && !second.IsComplete; i++) second.Step();
            }
            Assert.That(Facts(first.Preview), Is.EqualTo(Facts(second.Preview)));
            Assert.That(Facts(first.Transition), Is.EqualTo(Facts(second.Transition)));
            Assert.That(Facts(input), Is.EqualTo(before));
        }
    }
}

