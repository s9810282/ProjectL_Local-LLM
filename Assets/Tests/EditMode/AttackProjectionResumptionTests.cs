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
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class AttackProjectionResumptionTests
    {
        [Test]
        public void ProjectionConstructionDoesNotTraverseInConstructor()
        {
            var input = CombatCommandTestData.Input();
            int enumerated = 0;
            IEnumerable<AttackBlockedContact> Contacts()
            {
                enumerated++;
                yield break;
            }
            using var work = CommandAttackProjection.BeginCreate(input, input.State.Environment, Contacts(), false);
            Assert.That(enumerated, Is.Zero);
            Assert.That(work.WorkVersion, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => { _ = work.Result; });
            work.Step();
            Assert.That(work.IsComplete, Is.False);
            int units = 1;
            while (!work.IsComplete)
            {
                long previous = work.WorkVersion;
                work.Step();
                Assert.That(work.WorkVersion, Is.GreaterThan(previous));
                Assert.That(++units, Is.LessThan(10000));
            }
            Assert.That(units, Is.GreaterThan(input.Characters.Count));
            Assert.That(enumerated, Is.EqualTo(1));
            Assert.That(work.Result.Characters.Keys, Is.EquivalentTo(input.Characters.Select(c => c.EntityId)));
            Assert.That(work.Result.Events, Is.Empty);
        }

        [Test]
        public void DisposedConstructionDoesNotCompleteOrMutateInput()
        {
            var input = CombatCommandTestData.Input();
            string before = Facts(input);
            var work = CommandAttackProjection.BeginCreate(input, input.State.Environment, null, false);
            work.Step();
            work.Dispose();
            work.Dispose();
            Assert.Throws<ObjectDisposedException>(() => work.Step());
            Assert.That(Facts(input), Is.EqualTo(before));
        }

        [Test]
        public void CurrentInputDoesNotExposePartialReconciliation()
        {
            var input = EnemyAiBenchmarkFixtures.Build("T8", 1).WithActor(new EntityId(11));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var projection = MovementProjection.ForArrival(input, characters, new List<CombatEvent>());
            using var work = projection.BeginCurrentInput();
            Assert.That(work.WorkVersion, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => { _ = work.Result; });
            work.Step();
            Assert.That(work.IsComplete, Is.False);
            Assert.Throws<InvalidOperationException>(() => { _ = work.Result; });
            Drain(work);
            Assert.That(work.Result.Characters.Count, Is.EqualTo(input.Characters.Count));
            Assert.That(work.Result.Characters.Single(c => c.EntityId == input.ActorId).StatusEffects.Entries.Count,
                Is.GreaterThan(input.Characters.Single(c => c.EntityId == input.ActorId).StatusEffects.Entries.Count));
        }

        [Test]
        public void CancelledObservationDoesNotApplyProposals()
        {
            var input = EnemyAiBenchmarkFixtures.Build("T8", 1).WithActor(new EntityId(11));
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, characters, events);
            var work = projection.BeginCurrentInput();
            work.Step();
            work.Dispose();
            Assert.That(events, Is.Empty);
            foreach (var character in input.Characters)
                Assert.That(characters[character.EntityId], Is.SameAs(character));
            Assert.Throws<ObjectDisposedException>(() => work.Step());
        }

        [Test]
        public void MutationBetweenObservationsRebuildsCurrentFacts()
        {
            var input = CombatCommandTestData.Input();
            string source = Facts(input);
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            using var first = projection.BeginCurrentInput();
            Drain(first);
            var original = first.Result.Characters.Single(c => c.EntityId == input.ActorId);
            projection.ReplaceActor(projection.Actor.WithHealth(new HealthStateSnapshot(90, 100)));
            using var second = projection.BeginCurrentInput();
            Drain(second);
            Assert.That(second.Result.Characters.Single(c => c.EntityId == input.ActorId).Health.CurrentHealth, Is.EqualTo(90));
            Assert.That(original.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(Facts(input), Is.EqualTo(source));
        }

        [Test]
        public void FinalResultDoesNotAliasMutableBuilder()
        {
            var input = CombatCommandTestData.Input();
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            var work = projection.BeginCurrentInput();
            Drain(work);
            var result = work.Result;
            string captured = Facts(result);
            work.Dispose();
            projection.ReplaceActor(projection.Actor.WithHealth(new HealthStateSnapshot(80, 100)));
            Assert.That(Facts(result), Is.EqualTo(captured));
        }

        private static void Drain(MovementProjection.InputWork work)
        {
            int steps = 0;
            while (!work.IsComplete)
            {
                long version = work.WorkVersion;
                work.Step();
                Assert.That(work.WorkVersion, Is.GreaterThan(version));
                Assert.That(++steps, Is.LessThan(100000));
            }
        }

        [Test]
        public void CompletionIsNotExposedUntilAllSettlementEnds()
        {
            var input = CombatCommandTestData.Input();
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), new List<CombatEvent>());
            using var work = projection.BeginComplete();
            Assert.That(work.WorkVersion, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => { _ = work.Result; });
            int steps = 0;
            while (!work.IsComplete)
            {
                long before = work.WorkVersion;
                work.Step();
                Assert.That(work.WorkVersion, Is.GreaterThan(before));
                Assert.That(++steps, Is.LessThan(100000));
            }
            Assert.That(steps, Is.GreaterThan(input.Characters.Count));
            Assert.That(work.Result.Accepted, Is.True);
            long complete = work.WorkVersion;
            work.Step();
            Assert.That(work.WorkVersion, Is.EqualTo(complete));
        }

        [Test]
        public void DisposingCompletionNeverSettlesRemainingWork()
        {
            var input = EnemyAiBenchmarkFixtures.Build("T8", 1).WithActor(new EntityId(11));
            string source = Facts(input);
            var events = new List<CombatEvent>();
            var projection = MovementProjection.ForArrival(input, input.Characters.ToDictionary(c => c.EntityId), events);
            var work = projection.BeginComplete();
            work.Step();
            int before = events.Count;
            work.Dispose();
            Assert.That(events.Count, Is.EqualTo(before));
            Assert.That(Facts(input), Is.EqualTo(source));
            Assert.Throws<ObjectDisposedException>(() => work.Step());
        }

        private static string Facts(CombatCommandInput input) => AttackPreviewParityTests.Serialize(input, new HashSet<string>());

        [Test]
        public void PresenceRecipientsReadOneCapturedPopulation()
        {
            var input = TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Demon, new CohesionTraitSpec(2, 10, 2, 2));
            var expected = TraitPresenceReconciler.Calculate(input, input.Characters);
            var population = input.Characters.ToDictionary(c => c.EntityId);
            var events = new List<CombatEvent>();
            using var work = TraitPresenceReconciler.BeginReconcile(input, population, events);
            int steps = 0;
            while (!work.IsComplete && events.Count == 0)
            {
                foreach (var original in input.Characters)
                    Assert.That(population[original.EntityId], Is.SameAs(original));
                work.Step();
                Assert.That(++steps, Is.LessThan(100000));
            }
            Assert.That(events.Count, Is.GreaterThan(0));
            foreach (var original in input.Characters)
                Assert.That(population[original.EntityId], Is.SameAs(original), "모든 받는 쪽을 계산하기 전에 적용하면 안 됩니다.");
            while (!work.IsComplete)
            {
                work.Step();
                Assert.That(++steps, Is.LessThan(100000));
            }
            foreach (var change in expected.Characters)
                Assert.That(AttackPreviewParityTests.Serialize(population[change.Final.EntityId], new HashSet<string>()),
                    Is.EqualTo(AttackPreviewParityTests.Serialize(change.Final, new HashSet<string>())));
        }

        [Test]
        public void AttackResultAssemblyYieldsAndSealsBothResultsTogether()
        {
            var input = CombatCommandTestData.Input();
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var environment = input.State.Environment;
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight, environment, input.EnvironmentBindings, input.State.WallBlocking);
            var selection = CombatAttackQueries.Selection(input, actor, profile, environment: environment, walls: walls);
            var delivery = CombatAttackDelivery.Resolve(input, actor, profile, selection.Selection, 20, walls, AttackElevation.From(environment, input.EnvironmentBindings));
            var plan = new CommandAttackPlan(input, EnemyAiBenchmarkFixtures.Rules, profile, actor, actor, environment, walls,
                selection.Selection, delivery, new CombatEffectEngine(), CombatCommandEffects.Capture(actor, profile),
                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())), 20, Rotation4.R0, true);
            var world = new CommandAttackProjection(input, environment, selection.BlockedContacts, false);
            var ledger = new CommandAttackLedger();
            AttackDirectDamageCalculator.Apply(plan, world, ledger, new HashSet<EntityId>());
            ledger.CaptureHitHealth(world);
            var cost = CombatActionAdmission.Settlement(actor, UnitActionUse.Attacked);
            using var work = AttackResultFactory.BeginCreate(plan, world, ledger, cost, Array.Empty<TraitActivationSnapshot>(), Array.Empty<CategoryActivationSnapshot>());
            Assert.That(work.WorkVersion, Is.Zero);
            Assert.Throws<InvalidOperationException>(() => { _ = work.Result; });
            Assert.Throws<InvalidOperationException>(() => { _ = work.Preview; });
            int steps = 0;
            while (!work.IsComplete)
            {
                work.Step();
                Assert.That(++steps, Is.LessThan(10000));
            }
            Assert.That(steps, Is.GreaterThan(work.Result.Hits.Length));
            Assert.That(work.Preview.CanAttack, Is.True);
            Assert.That(work.Result.Hits.Length, Is.EqualTo(3));
            Assert.That(work.Preview.Targets.ToArray().Select(t => t.TargetId),
                Is.EqualTo(work.Result.Hits.ToArray().Select(t => t.TargetId)));
        }
    }
}
