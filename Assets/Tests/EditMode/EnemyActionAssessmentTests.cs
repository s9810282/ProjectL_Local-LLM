using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyActionAssessmentTests
    {
        internal const string RouteReference = "docs/superpowers/reports/2026-11-07-enemy-utility-ai/phase-4-route-reference.tsv";

        [Test]
        public void MoveAssessmentHidesUnknownMine()
        {
            var hidden = MineEnvironmentTests.Input(MineEnvironmentTests.Mine(hidden: true, damage: 100));
            var absent = MineEnvironmentTests.Input();
            string original = Encode(hidden);
            var a = Assess(hidden, MineEnvironmentTests.Rules, out var projected);
            var b = Assess(absent, MineEnvironmentTests.Rules, out _);
            Assert.That(FactsKey(a), Is.EqualTo(FactsKey(b)));
            Assert.That(a.IsPrecise, Is.True);
            Assert.That(projected.State.Environment.Tiles, Is.Empty);
            Assert.That(projected.Characters.Single(c => c.EntityId == hidden.ActorId).Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(a.GuaranteedTerminal, Is.False);
            Assert.That(Encode(hidden), Is.EqualTo(original));
            var actual = CombatCommandCalculator.Calculate(hidden, MineEnvironmentTests.Rules);
            Assert.That(MovementTestData.Actor(actual).Health.IsDead, Is.True, "The hidden effect genuinely changes actual execution.");
        }

        [Test]
        public void DifferentUnobservedSeedsGiveSameAssessment()
        {
            foreach (var source in new[] { CombatCommandTestData.Input(), HunterTraitTests.Input(splash: true) })
            {
                var changed = CombatCommandTestData.With(source, state: source.State.With(itemRandomState: 9751,
                    environmentProgress: new EnvironmentProgressFacts(source.State.EnvironmentProgress.NextObjectEntryOrdinal,
                        source.State.EnvironmentProgress.NextSourceId, 9187)));
                string original = Encode(changed);
                Assert.That(FactsKey(Assess(source, Array.Empty<IMovementTileRule>(), out _)),
                    Is.EqualTo(FactsKey(Assess(changed, Array.Empty<IMovementTileRule>(), out _))));
                Assert.That(Encode(changed), Is.EqualTo(original));
            }
        }

        [Test]
        public void AttackHealthLossIsActualCappedLoss()
        {
            var input = CombatCommandTestData.Input(new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.None, 0));
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 2
                ? c.WithHealth(new HealthStateSnapshot(3, 100)) : c).ToArray());
            var facts = Assess(input, Array.Empty<IMovementTileRule>(), out _);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.OpponentHpLossPermille, Is.EqualTo(30));
            Assert.That(facts.OpponentEliminations, Is.EqualTo(1));
        }

        [Test]
        public void HealingIsNotDamageCredit()
        {
            var input = RouteCases().First().Input;
            var environment = new MovementEnvironmentFacts(new[] { new DestinationTileModifierFact(71, input.Path.Endpoint, true,
                new[] { Owner.Player }, 0, 10) });
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.WithHealth(new HealthStateSnapshot(50, 100)) : c).ToArray(), state: input.State.With(environment: environment,
                    environmentProgress: EnvironmentProgressFacts.Initial(environment)),
                souls: new SoulFieldSnapshot(2, new[] { new SoulFieldFacts(1,
                    new EffectOriginKey(EffectOwnerKind.Artifact, 4, 11, "soul"), input.Path.Endpoint, 5, 2) }));
            var facts = Assess(input, Array.Empty<IMovementTileRule>(), out var projected);
            Assert.That(projected.Characters.Single(c => c.EntityId == input.ActorId).Health.CurrentHealth, Is.GreaterThan(50));
            Assert.That(facts.AlliedHpLossPermille, Is.Zero);
            Assert.That(facts.OpponentHpLossPermille, Is.Zero);
        }

        [Test]
        public void TraitActivationDoesNotDoubleCountDamage()
        {
            var input = ArmorBreakerTraitTests.Input();
            var facts = Assess(input, Array.Empty<IMovementTileRule>(), out _);
            var actual = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            long loss = input.Characters.Where(c => c.Owner == Owner.Enemy).Sum(c =>
                Math.Max(0, c.Health.CurrentHealth - TraitCombatFixtures.Final(actual, (int)c.EntityId.Value).Health.CurrentHealth)
                * 1000L / c.Health.MaxHealth);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.OpponentHpLossPermille, Is.EqualTo(loss));
        }

        [Test]
        public void NoDamageContactStillAdmitted()
        {
            var input = CombatCommandTestData.Input();
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId == input.ActorId
                ? c.With(stats: c.Stats.With(attackPower: 0)) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == EntityKey.Unit_Akimbo
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(EntityKey.Unit_Akimbo, 0), p.Method) : p).ToArray());
            var facts = Assess(input, Array.Empty<IMovementTileRule>(), out _);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.OpponentHpLossPermille, Is.Zero);
        }

        [Test]
        public void KnownUnsupportedArrivalDoesNotInvokeCallback()
        {
            var input = MineEnvironmentTests.Input(MineEnvironmentTests.Mine());
            var facts = Assess(input, new IMovementTileRule[] { new RejectInvocationRule() }, out var projected);
            Assert.That(facts.IsPrecise, Is.False);
            Assert.That((facts.Gaps & CombatAssessmentGap.UnsupportedEffect) != 0, Is.True);
            Assert.That(projected, Is.Null);
        }

        private sealed class RejectInvocationRule : IMovementTileRule
        {
            public string Key => new MineMovementTileRule().Key;
            public IReadOnlyList<EntityId> TargetIds(MovementTileFact tile, EntityId mover, CombatCommandInput current)
                => throw new InvalidOperationException("Unsupported callback was executed.");
            public MovementStepEffects Evaluate(MovementStepInput input, MovementTileFact tile)
                => throw new InvalidOperationException("Unsupported callback was executed.");
        }

        [Test]
        public void CancellationKeepsSpentBudgetAndReleasesResults()
        {
            var input = CombatCommandTestData.Input();
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var work = CombatActionAssessment.Begin(input, EnemyDecision.Attack(input.ActorId, input.Aim, default),
                Array.Empty<IMovementTileRule>(), context);
            while (context.PreviewCount == 0) work.Step();
            work.Dispose();
            Assert.That(context.PreviewCount, Is.EqualTo(1));
            Assert.Throws<ObjectDisposedException>(() => work.Step());
            Assert.Throws<ObjectDisposedException>(() => { _ = work.MovedFacts; });
        }

        [Test]
        public void DeadMoverHasNoFollowupWorld()
        {
            var vector = RouteCases().First(v => v.Id == "weak");
            var facts = Assess(vector.Input, vector.Rules, out var moved);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.AlliedEliminations, Is.EqualTo(1));
            Assert.That(moved, Is.Null);
        }

        internal static EnemyScoreFacts Assess(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            out CombatCommandInput moved)
        {
            var candidate = input.Kind == CombatCommandKind.Move ? EnemyDecision.Move(input.ActorId, input.Path, default)
                : EnemyDecision.Attack(input.ActorId, input.Aim, default);
            Assert.That(CombatActionAssessment.TryAssess(input, candidate, rules,
                new EnemyDecisionContext(input, EnemyDecisionSettings.Default), out var facts, out moved), Is.True);
            return facts;
        }

        internal static string FactsKey(EnemyScoreFacts facts) => Encode(new object[] { facts.Gaps, facts.IsPrecise,
            facts.BudgetExhausted, facts.OpponentHpLossPermille, facts.AlliedHpLossPermille, facts.OpponentEliminations,
            facts.AlliedEliminations, facts.RestrictionDeltaPermille, facts.PossibleAlliedHpLossPermille, facts.Endpoint,
            facts.ProjectedTerminal, facts.GuaranteedTerminal });

        [TestCase("ArmorBreaker")]
        [TestCase("DistanceDamageScale")]
        [TestCase("Taunt")]
        [TestCase("SteadfastWill")]
        [TestCase("SafeBombardment")]
        public void SupportedOwnerPreviewMatchesActualConsequences(string rule)
        {
            var input = SupportedCase(rule);
            using var preview = CombatCommandCalculator.BeginPreview(input, Array.Empty<IMovementTileRule>());
            while (!preview.IsComplete) preview.Step();
            var actual = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(preview.Transition.Accepted, Is.True);
            foreach (var before in input.Characters)
            {
                var observed = TraitCombatFixtures.Final(preview.Transition, (int)before.EntityId.Value);
                var after = TraitCombatFixtures.Final(actual, (int)before.EntityId.Value);
                Assert.That(Encode(new object[] { observed.Health, observed.Anchor, observed.StatusEffects, observed.Debuffs }),
                    Is.EqualTo(Encode(new object[] { after.Health, after.Anchor, after.StatusEffects, after.Debuffs })), rule);
            }
        }

        private static CombatCommandInput SupportedCase(string rule)
        {
            if (rule == "ArmorBreaker") return ArmorBreakerTraitTests.Input();
            if (rule == "DistanceDamageScale")
                return CombatCommandEffectTests.ActorEffect(new DistanceDamageScaleTraitSpec(new[] { 100, 120, 140, 160 }),
                    new DistanceDamageScaleTraitStatusSnapshot(new[] { 100, 120, 140, 160 }));
            if (rule == "SafeBombardment") return CombatCommandTestData.Input(category: CategoryKey.Cannon, friendly: true);
            if (rule == "SteadfastWill")
                return TraitCombatFixtures.Equip(CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(2),
                    new KnockbackTraitStatusSnapshot(2)), EntityKey.Unit_Demon, new SteadfastWillTraitSpec());
            return TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Demon,
                new TauntTraitSpec(4, 25));
        }

        [Test]
        public void UnmodelledForcedArrivalIsPartial()
        {
            // 넉백 자체는 정밀 미리보기에 맡기지만(사용자 결정 2026-11-10), 밀려날 수 있는 칸에 규칙을 모르는 도착 원본이 있으면
            // 결과를 한정할 수 없어 부분 평가로 남는다. 이 평가에는 지뢰 규칙을 넘기지 않아 지뢰가 모르는 도착 원본이다(이전: 넉백만으로 부분).
            var input = CliffFallTests.WithEnvironment(
                CombatCommandEffectTests.ActorEffect(new KnockbackTraitSpec(1), new KnockbackTraitStatusSnapshot(1)),
                new MineSourceFact(71, new CellCoord(6, 3), 1, 10, true, new[] { Owner.Player }));
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            Assert.That(CombatActionAssessment.TryAssess(input, EnemyDecision.Attack(input.ActorId, input.Aim, default),
                Array.Empty<IMovementTileRule>(), context, out var facts, out var moved), Is.True);
            Assert.That(facts.IsPrecise, Is.False);
            Assert.That((facts.Gaps & CombatAssessmentGap.UnsupportedEffect) != 0, Is.True);
            Assert.That(facts.OpponentHpLossPermille, Is.Zero);
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.GreaterThan(0));
            Assert.That(moved, Is.Null);
            Assert.That(context.PreviewCount, Is.Zero);
        }

        [TestCase("Hunter", CombatAssessmentGap.RandomOutcome)]
        [TestCase("KillChainAttack", CombatAssessmentGap.ActualOnlyReaction)]
        [TestCase("Roar", CombatAssessmentGap.ActualOnlyReaction)]
        public void RandomAndActualOnlyGapsPreventPreciseChildren(string rule, CombatAssessmentGap expected)
        {
            var input = rule == "Hunter" ? HunterTraitTests.Input(splash: true) :
                rule == "Roar" ? BossRoarTests.Input(4) :
                TraitCombatFixtures.Equip(CombatCommandTestData.Input(), EntityKey.Unit_Akimbo, new KillChainAttackTraitSpec());
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            string before = Encode(input);
            CombatActionAssessment.TryAssess(input, EnemyDecision.Attack(input.ActorId, input.Aim, default),
                Array.Empty<IMovementTileRule>(), context, out var facts, out var moved);
            Assert.That((facts.Gaps & expected) != 0, Is.True);
            Assert.That(facts.IsPrecise, Is.False);
            Assert.That(facts.GuaranteedTerminal, Is.False);
            Assert.That(moved, Is.Null);
            Assert.That(context.PreviewCount, Is.Zero);
            Assert.That(Encode(input), Is.EqualTo(before));
        }

        [Test]
        public void PublicAttackAssessmentUsesCappedConsequencesAndRetainsHiddenGap()
        {
            var input = CombatCommandTestData.Input();
            var candidate = EnemyDecision.Attack(input.ActorId, input.Aim, default);
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            string before = Encode(input);
            Assert.That(CombatActionAssessment.TryAssess(input, candidate, Array.Empty<IMovementTileRule>(), context,
                out var facts, out var moved), Is.True);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.OpponentHpLossPermille, Is.GreaterThan(0));
            Assert.That(facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment));
            Assert.That(facts.GuaranteedTerminal, Is.False);
            Assert.That(moved, Is.Null);
            Assert.That(context.PreviewCount, Is.EqualTo(1));
            Assert.That(Encode(input), Is.EqualTo(before));
        }

        [Test]
        public void LogicalBudgetChargedOnceAfterCoverage()
        {
            var input = CombatCommandTestData.Input();
            var context = new EnemyDecisionContext(input, new EnemyDecisionSettings(actorPreviewLimit: 1, decisionPreviewLimit: 1));
            var candidate = EnemyDecision.Attack(input.ActorId, input.Aim, default);
            using var work = CombatActionAssessment.Begin(input, candidate, Array.Empty<IMovementTileRule>(), context);
            Assert.Throws<InvalidOperationException>(() => { _ = work.Facts; });
            for (int i = 0; i < input.Characters.Count; i++) work.Step();
            Assert.That(context.PreviewCount, Is.Zero, "Coverage must yield before creating a precise child.");
            while (!work.IsComplete) work.Step();
            Assert.That(context.PreviewCount, Is.EqualTo(1));
            Assert.That(work.Facts.IsPrecise, Is.True);
            Assert.That(CombatActionAssessment.TryAssess(input, candidate, Array.Empty<IMovementTileRule>(), context,
                out var exhausted, out _), Is.True);
            Assert.That(exhausted.IsPrecise, Is.False);
            Assert.That(exhausted.BudgetExhausted, Is.True);
            Assert.That(context.PreviewCount, Is.EqualTo(1));
        }

        [Test]
        public void AssessmentStepNeverDrainsAttackOrMove()
        {
            var attack = CombatCommandTestData.Input();
            var route = RouteCases().First().Input;
            foreach (var input in new[] { attack, route })
            {
                var candidate = input.Kind == CombatCommandKind.Attack
                    ? EnemyDecision.Attack(input.ActorId, input.Aim, default)
                    : EnemyDecision.Move(input.ActorId, input.Path, default);
                var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
                using var work = CombatActionAssessment.Begin(input, candidate, Array.Empty<IMovementTileRule>(), context);
                var counter = new PresenceCounter();
                CoreProfiling.Install(counter);
                try
                {
                    int guard = 0;
                    while (!work.IsComplete)
                    {
                        int before = counter.Calls;
                        work.Step();
                        Assert.That(counter.Calls - before, Is.LessThanOrEqualTo(1));
                        Assert.That(++guard, Is.LessThan(100000));
                    }
                }
                finally { CoreProfiling.Install(null); }
                Assert.That(work.Admitted, Is.True);
                Assert.That(work.Facts.IsPrecise, Is.True);
                if (input.Kind == CombatCommandKind.Move)
                {
                    Assert.That(work.MovedFacts, Is.Not.Null);
                    Assert.That(work.Facts.Endpoint, Is.EqualTo(input.Path.Endpoint));
                }
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(31)]
        public void MoveRoutePartitionsMatchPreview(int partition)
        {
            foreach (var vector in RouteCases())
            {
                string before = Encode(vector.Input);
                var expected = CombatMovementCalculator.ProjectRoute(vector.Input, vector.Rules, true, out var failure, out int valid);
                using var work = CombatMovementCalculator.BeginProjectRoute(vector.Input, vector.Rules, true);
                Assert.Throws<InvalidOperationException>(() => { _ = work.Projection; });
                int steps = 0;
                while (!work.IsComplete)
                {
                    for (int i = 0; i < partition && !work.IsComplete; i++) work.Step();
                    Assert.That(++steps, Is.LessThan(100000), vector.Id);
                }
                Assert.That(work.Failure, Is.EqualTo(failure), vector.Id);
                Assert.That(work.ValidStepCount, Is.EqualTo(valid), vector.Id);
                Assert.That(Encode(work.Projection?.Transition()), Is.EqualTo(Encode(expected?.Transition())), vector.Id);
                Assert.That(Encode(vector.Input), Is.EqualTo(before));
            }
        }

        [Test]
        public void RouteStepDoesNotDrainPresenceAndCancellationDoesNotFinish()
        {
            foreach (var vector in RouteCases())
            {
                var counter = new PresenceCounter();
                using var work = CombatMovementCalculator.BeginProjectRoute(vector.Input, vector.Rules, true);
                CoreProfiling.Install(counter);
                try
                {
                    int guard = 0;
                    while (!work.IsComplete)
                    {
                        int before = counter.Calls;
                        work.Step();
                        Assert.That(counter.Calls - before, Is.LessThanOrEqualTo(1), vector.Id);
                        Assert.That(++guard, Is.LessThan(100000));
                    }
                }
                finally { CoreProfiling.Install(null); }
                using var cancelled = CombatMovementCalculator.BeginProjectRoute(vector.Input, vector.Rules, true);
                cancelled.Step();
                cancelled.Dispose();
                Assert.Throws<ObjectDisposedException>(() => cancelled.Step());
            }
        }

        internal static string Encode(object value) => AttackPreviewParityTests.Serialize(value, new HashSet<string>());

        private sealed class PresenceCounter : ICoreProfiler
        {
            internal int Calls;
            public void Begin(CoreProfileSection section) { if (section == CoreProfileSection.TraitPresenceReconcile) Calls++; }
            public void End(CoreProfileSection section) { }
        }

        [Test]
        public void FrozenRoutePreviewAndExecutionRemainEquivalent()
        {
            var rows = RouteCases().Select(v => v.Id + "\t" + Convert.ToBase64String(Encoding.UTF8.GetBytes(
                AttackPreviewParityTests.Serialize(new object[] { v.Input, CombatMovementCalculator.Preview(v.Input, v.Rules),
                    CombatCommandCalculator.Calculate(v.Input, v.Rules) }, new HashSet<string>())))).ToArray();
            if (Environment.GetEnvironmentVariable("PROJECTL_ENEMY_AI_CAPTURE") == "phase4-route-reference")
            {
                Assert.That(File.Exists(RouteReference), Is.False, "Never replace a frozen oracle.");
                File.WriteAllLines(RouteReference, rows, new UTF8Encoding(false));
                TestContext.WriteLine($"Captured route vectors={rows.Length}");
            }
            else Assert.That(rows, Is.EqualTo(File.ReadAllLines(RouteReference)));
        }

        internal static IEnumerable<AttackPreviewParityTests.Vector> RouteCases()
        {
            var route = MovementTestData.WithMovement(MovementTestData.SafeRoute(
                new CellCoord(1, 1), new CellCoord(2, 1), new CellCoord(3, 1)), 5);
            yield return new AttackPreviewParityTests.Vector("open", route);
            yield return new AttackPreviewParityTests.Vector("recon", ReconSquadMovementTests.Route());
            yield return new AttackPreviewParityTests.Vector("swap", PositionSwapMovementTests.Route());
            yield return new AttackPreviewParityTests.Vector("weak", CliffFallTests.WithEnvironment(route,
                new StaticTerrainFact(71, new CellCoord(3, 1), StaticTerrainKind.WeakGround, true,
                    new[] { Owner.Player, Owner.Enemy }, 1)));
            yield return new AttackPreviewParityTests.Vector("cliff", CliffFallTests.WithEnvironment(route,
                new StaticTerrainFact(71, new CellCoord(3, 1), StaticTerrainKind.Cliff, true,
                    new[] { Owner.Player, Owner.Enemy })));
            yield return new AttackPreviewParityTests.Vector("tornado", TraitTrailTestData.Environment(route,
                TornadoEnvironmentTests.Source(cell: new CellCoord(2, 1))), TornadoEnvironmentTests.Rules);
            yield return new AttackPreviewParityTests.Vector("invalid", MovementTestData.Route(route,
                new CellCoord(1, 1), new CellCoord(-1, 1)));
        }
    }
}
