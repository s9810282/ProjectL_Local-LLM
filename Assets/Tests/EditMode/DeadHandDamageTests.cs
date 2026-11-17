using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;

namespace ProjectL.Tests.Core
{
    public sealed class DeadHandDamageTests
    {
        [TestCase(2, 9)]
        [TestCase(3, 12)]
        public void SweepDistanceUsesTheSettledPositionAndExtendsTheLastConfiguredCoefficient(int entries, int expected)
        {
            var input = DeadHandTestData.Input(active: true,
                method: new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0),
                aim: AttackAim.ForCell(new CellCoord(3, 2)));
            input = WithDistanceScale(input, new[] { 50, 150, 200 }.Take(entries).ToArray());

            var preview = CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Sweep.Targets.Single().FinalAnchor, Is.EqualTo(new CellCoord(6, 2)));
            Assert.That(result.Attack.Value.Hits.Span[0].DamageDealt, Is.EqualTo(expected));
            Assert.That(preview.Targets.Span[0].ExpectedDamage, Is.EqualTo(expected));
        }

        [Test]
        public void SweepDoesNotHideMissingCoefficientForTheOriginalTargetDistance()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(3, 3) }, active: true);
            input = WithDistanceScale(input, new[] { 50 });
            Assert.Throws<InvalidOperationException>(() => TraitCombatFixtures.Attack(input));
        }

        internal static CombatCommandInput WithDistanceScale(CombatCommandInput input, int[] percents)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var basis = profile.Definition;
            var spec = UnitSpecTestFactory.Create(basis.Key, 1, 1, new[] { CellCoord.Zero }, basis.MaxHealth, basis.MoveMethod,
                basis.AttackRange, basis.AttackPower, basis.Priority, new TraitSpec[]
                { new DeadHandTraitSpec(100), new DistanceDamageScaleTraitSpec(percents) }, categoryKey: basis.CategoryKey);
            return CombatCommandTestData.With(input,
                input.Characters.Select(c => c.EntityId == actor.EntityId
                    ? CombatCommandTestData.With(c, traits: new TraitStatusSnapshot[]
                    { new DeadHandTraitStatusSnapshot(), new DistanceDamageScaleTraitStatusSnapshot(percents) }) : c).ToArray(),
                profiles: input.Profiles.Select(p => p == profile ? new CombatAttackProfile(spec, p.Method, p.OwnedId) : p).ToArray());
        }

        [TestCase(0, 0)]
        [TestCase(1, 5)]
        [TestCase(2, 10)]
        public void LastStandEvidenceAndDamageUseTheSameRosterAfterSweepEntryDeath(int remoteEnemies, int expectedBonus)
        {
            var targets = new[] { new CellCoord(3, 2), new CellCoord(3, 3), new CellCoord(4, 2) }
                .Concat(Enumerable.Range(0, remoteEnemies).Select(i => new CellCoord(7, 7 - i))).ToArray();
            var input = DeadHandTestData.Input(targets, active: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 4
                ? CombatCommandTestData.With(c, hp: 1) : c).ToArray());
            var loadout = new ArtifactLoadoutSnapshot(0, new[] { new ArtifactPlacedEffectSnapshot(12, 4,
                ArtifactLastStandRuleTests.LastStandEffect()) });
            input = CombatCommandTestData.With(input, artifacts: loadout);
            input = TraitTrailTestData.Environment(input, MineEnvironmentTests.Mine(60, damage: 7, cell: new CellCoord(5, 2)));

            var preview = CombatCommandCalculator.Preview(input, MineEnvironmentTests.Rules);
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 4).Health.IsDead, Is.True);
            Assert.That(result.Events.OfType<CombatArtifactEvent>().SelectMany(e => e.Activations).Select(a => a.AppliedAmount),
                Is.EqualTo(expectedBonus == 0 ? Array.Empty<int>() : new[] { expectedBonus }));
            var hits = result.Attack.Value.Hits.ToArray();
            Assert.That(hits.Select(h => h.TargetId), Is.EquivalentTo(new[] { new EntityId(2), new EntityId(3) }));
            Assert.That(hits.Select(h => h.DamageDealt), Is.All.EqualTo(6 + expectedBonus));
            Assert.That(preview.Targets.ToArray().Where(t => t.IsDirectHit).Select(t => t.ExpectedDamage),
                Is.EquivalentTo(new[] { 6 + expectedBonus, 6 + expectedBonus }));
        }

        [Test]
        public void RangeQueryBindsSelectionEffectsOnceForAllAimCandidates()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(3, 2), new CellCoord(4, 1) }, active: true);
            var profiler = new RangeProfiler();
            AttackRangeResult range;
            CoreProfiling.Install(profiler);
            try
            {
                range = CombatCommandCalculator.GetAttackRange(input);
            }
            finally
            {
                CoreProfiling.Install(null);
            }
            Assert.That(range.AttackAims.ToArray(), Is.EquivalentTo(new[]
                { AttackAim.ForDirection(AttackDirection.Up), AttackAim.ForDirection(AttackDirection.Right) }));
            Assert.That(profiler.BindingCount, Is.EqualTo(1));
            foreach (var aim in range.AttackAims.ToArray())
                Assert.That(CombatCommandCalculator.Preview(CombatAttackQueries.WithAim(input, aim),
                    Array.Empty<IMovementTileRule>()).CanAttack, Is.True);
        }

        private sealed class RangeProfiler : ICoreProfiler
        {
            internal int BindingCount { get; private set; }

            public void Begin(CoreProfileSection section)
            {
                if (section == CoreProfileSection.AttackEffectBinding) BindingCount++;
            }

            public void End(CoreProfileSection section) { }
        }

        [Test]
        public void KillChainFallbackRecomputesExtraDamageWithoutParentPlan()
        {
            var input = TraitCombatFixtures.Equip(DeadHandTestData.Input(new[] { new CellCoord(3, 2), new CellCoord(7, 7) }, active: true),
                EntityKey.Unit_Akimbo, new DeadHandTraitSpec(200), new KillChainAttackTraitSpec());
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var characters = input.Characters.ToDictionary(c => c.EntityId);
            characters[new EntityId(2)] = CombatCommandCalculator.Hurt(characters[new EntityId(2)], 100);
            var events = new List<CombatEvent>();
            var arrival = MovementProjection.ForArrival(input, characters, events);
            KillChainAttackCalculator.Resolve(input, actor.EntityId, new CombatEffectEngine(),
                new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),
                CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor)), 12,
                arrival, characters, new CommandAttackGroups(), new Dictionary<EntityId, CellCoord>(), new CombatHitCause(), new HashSet<EntityId>());
            Assert.That(characters[new EntityId(3)].Health.CurrentHealth, Is.EqualTo(94));
            Assert.That(events.OfType<CombatSweepEvent>(), Is.Empty);
        }

        [Test]
        public void TerminalMovementStopsAllAttackHits()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(2, 2), new CellCoord(4, 2) }, active: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 10
                ? CombatCommandTestData.With(c, hp: 1, position: new CellCoord(5, 3)) : c).ToArray());
            input = CliffFallTests.WithEnvironment(input, MineEnvironmentTests.Mine(60, damage: 7, cell: new CellCoord(5, 2)));
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Sweep.Interrupted, Is.True);
            Assert.That(result.Attack.Value.Hits.Length, Is.Zero);
            Assert.That(result.Attack.Value.Sweep.Targets.Last().Stop, Is.EqualTo(AttackSweepStop.NotProcessed));
            Assert.That(TraitCombatFixtures.Final(result, 2).Anchor, Is.EqualTo(new CellCoord(2, 2)));
        }

        [Test]
        public void DamageDoesNotRepackAfterKillingBlocker()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(2, 2), new CellCoord(4, 2), new CellCoord(7, 7) }, active: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 3
                ? CombatCommandTestData.With(c, hp: 1) : c).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.IsDead, Is.True);
            Assert.That(TraitCombatFixtures.Final(result, 2).Anchor, Is.EqualTo(new CellCoord(4, 2)));
            Assert.That(result.Attack.Value.Sweep.Targets.Single(t => t.TargetId.Value == 2).Stop, Is.EqualTo(AttackSweepStop.Blocked));
        }

        [Test]
        public void OtherDamageEffectRemainsActiveOnExtraAttack()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(3, 2), new CellCoord(7, 7) }, active: true, percent: 200);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 2
                ? CombatCommandTestData.With(c, hp: 24) : c).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new DeadHandTraitSpec(200),
                new KillChainAttackTraitSpec(), new StationaryFireTraitSpec(100));
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(24));
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(88));
        }

        [TestCase(1)]
        [TestCase(7)]
        [TestCase(31)]
        public void ResumedPreviewMatchesExecutionAndKeepsInputUnchanged(int partition)
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(2, 2), new CellCoord(4, 2) }, active: true, percent: 150);
            using var work = new CombatAttackCalculation(input, Array.Empty<IMovementTileRule>());
            while (!work.IsComplete)
                for (int i = 0; i < partition && !work.IsComplete; i++) work.Step();
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(work.Preview.Sweep.Targets.Select(t => (t.TargetId, t.FinalAnchor, t.Stop)),
                Is.EqualTo(result.Attack.Value.Sweep.Targets.Select(t => (t.TargetId, t.FinalAnchor, t.Stop))));
            Assert.That(work.Preview.Targets.ToArray().Select(t => t.ExpectedDamage), Is.EqualTo(new[] { 9, 9 }));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 2).Anchor, Is.EqualTo(new CellCoord(2, 2)));
            Assert.That(input.Characters.Single(c => c.EntityId.Value == 3).Health.CurrentHealth, Is.EqualTo(100));
        }

        [TestCase(6, 100, 6)]
        [TestCase(15, 50, 8)]
        [TestCase(15, 150, 23)]
        [TestCase(6, 200, 12)]
        [TestCase(6, 0, 0)]
        public void RankCoefficientAppliesOnceAfterAllMovement(int power, int percent, int expected)
        {
            var input = DeadHandTestData.Input(active: true, power: power, percent: percent);
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(expected));
            Assert.That(TraitCombatFixtures.Final(result, 2).Anchor, Is.EqualTo(new CellCoord(5, 2)));
            Assert.That(result.Attack.Value.Sweep, Is.Not.Null);
            Assert.That(result.Attack.Value.Knockbacks, Is.Empty);
            Assert.That(TraitCombatFixtures.Final(result, 1).ActionUse, Is.EqualTo(UnitActionUse.Attacked));
            var events = result.Events.ToArray();
            int attackIndex = Array.FindIndex(events, e => e is CombatAttackEvent);
            Assert.That(events.Take(attackIndex).OfType<CombatCharacterEvent>().Count(e => e.Kind == CombatEventKind.Moved), Is.EqualTo(2));
            Assert.That(events.Skip(attackIndex).OfType<CombatCharacterEvent>().Any(e => e.Kind == CombatEventKind.Moved), Is.False);
            Assert.That(events.OfType<CombatSweepEvent>().Single().Sweep, Is.SameAs(result.Attack.Value.Sweep));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BlockedOrImmuneTargetsStillTakeOneHit(bool immune)
        {
            var input = DeadHandTestData.Input(active: true);
            if (immune) input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Demon, new SteadfastWillTraitSpec());
            else input = TraitCombatFixtures.Characters(input, input.Characters.Append(
                CombatCommandTestData.Character(7, EntityKey.Unit_Demon, new CellCoord(4, 2), Owner.Player)).ToArray());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(6));
            Assert.That(result.Attack.Value.Sweep.Targets.Single().EnteredCells, Is.Empty);
        }

        [Test]
        public void MovementDeathIsNotHitAgainAndSurvivorsUseVacatedSpace()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(2, 2), new CellCoord(4, 2), new CellCoord(7, 7) }, active: true);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 3
                ? CombatCommandTestData.With(c, hp: 1) : c).ToArray());
            input = CliffFallTests.WithEnvironment(input, MineEnvironmentTests.Mine(60, damage: 7, cell: new CellCoord(5, 2)));
            var result = CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Select(h => h.TargetId), Is.EqualTo(new[] { new EntityId(2) }));
            Assert.That(TraitCombatFixtures.Final(result, 2).Anchor, Is.EqualTo(new CellCoord(5, 2)));
            Assert.That(TraitCombatFixtures.Final(result, 2).Health.CurrentHealth, Is.EqualTo(94));
        }

        [Test]
        public void KillChainUsesUnscaledCommonDamageAndDoesNotSweep()
        {
            var input = DeadHandTestData.Input(new[] { new CellCoord(3, 2), new CellCoord(7, 7) }, active: true, percent: 200);
            input = TraitCombatFixtures.Characters(input, input.Characters.Select(c => c.EntityId.Value == 2
                ? CombatCommandTestData.With(c, hp: 12) : c).ToArray());
            input = TraitCombatFixtures.Equip(input, EntityKey.Unit_Akimbo, new DeadHandTraitSpec(200), new KillChainAttackTraitSpec());
            var result = TraitCombatFixtures.Attack(input);
            Assert.That(result.Accepted, Is.True, result.Reason);
            Assert.That(result.Attack.Value.Hits.ToArray().Single().DamageDealt, Is.EqualTo(12));
            Assert.That(TraitCombatFixtures.Final(result, 3).Health.CurrentHealth, Is.EqualTo(94));
            Assert.That(TraitCombatFixtures.Final(result, 3).Anchor, Is.EqualTo(new CellCoord(7, 7)));
            Assert.That(result.Events.OfType<CombatSweepEvent>().Count(), Is.EqualTo(1));
        }
    }
}
