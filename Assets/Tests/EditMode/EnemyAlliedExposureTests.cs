using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyAlliedExposureTests
    {
        [TestCase("A8")]
        [TestCase("X8")]
        [TestCase("H8")]
        public void AdmittedUsefulActionBeatsHoldAcrossFrozenSeeds(string fixture)
        {
            foreach (int seed in EnemyAiBenchmarkFixtures.Seeds)
            {
                var input = EnemyAiBenchmarkFixtures.Build(fixture, seed);
                string before = EnemyAiBenchmarkFixtures.Describe(input, true);
                using var session = new EnemyDecisionSession(input, EnemyAiBenchmarkFixtures.Rules, EnemyDecisionSettings.Default);
                EnemyUtilityDecisionTests.Drain(session);
                Assert.That(session.Decision.Kind, Is.Not.EqualTo(EnemyDecisionKind.EndEnemyPhase), $"{fixture}/{seed}");
                foreach (int slice in new[] { 1, 2 })
                {
                    using var sliced = new EnemyDecisionSession(input, EnemyAiBenchmarkFixtures.Rules, EnemyDecisionSettings.Default);
                    EnemyUtilityDecisionTests.Drain(sliced, slice);
                    Assert.That(sliced.Decision, Is.EqualTo(session.Decision), $"{fixture}/{seed} slice {slice}");
                }
                var progress = input.State.EnvironmentProgress;
                var differentRng = CombatCommandTestData.With(input, state: input.State.With(itemRandomState: 9751,
                    environmentProgress: new EnvironmentProgressFacts(progress.NextObjectEntryOrdinal, progress.NextSourceId, 9187)));
                using var variant = new EnemyDecisionSession(differentRng, EnemyAiBenchmarkFixtures.Rules, EnemyDecisionSettings.Default);
                EnemyUtilityDecisionTests.Drain(variant);
                Assert.That(variant.Decision, Is.EqualTo(session.Decision), $"{fixture}/{seed} RNG");
                var actual = CombatCommandCalculator.AdvancePrepared(input, session.Decision, EnemyAiBenchmarkFixtures.Rules);
                Assert.That(actual.Accepted, Is.True, $"{fixture}/{seed}: {actual.Reason}");
                Assert.That(actual.Characters.Characters.Where(change => change.Expected.Owner == Owner.Enemy)
                    .Any(change => change.Final.Health.CurrentHealth < change.Expected.Health.CurrentHealth), Is.False, $"{fixture}/{seed}");
                Assert.That(EnemyAiBenchmarkFixtures.Describe(input, true), Is.EqualTo(before));
            }
        }

        [TestCase(CombatCommandKind.Attack)]
        [TestCase(CombatCommandKind.Move)]
        public void RemoteRegenerationLeapAndKnockbackDoNotExpandCandidateHarm(CombatCommandKind kind)
        {
            var input = CombatCommandTestData.Input();
            var remote = input.Characters.Single(c => c.CharacterKey == EntityKey.Unit_MainCharacter);
            var traits = new TraitSpec[] { new RegenerationTraitSpec(2), new LeapTraitSpec(), new KnockbackTraitSpec(1) };
            var statuses = new TraitStatusSnapshot[]
            {
                new RegenerationTraitStatusSnapshot(2),
                new LeapTraitStatusSnapshot(),
                new KnockbackTraitStatusSnapshot(1),
            };
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == remote.EntityId
                    ? CombatCommandTestData.With(c, traits: statuses) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == remote.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(remote.CharacterKey, 1, CategoryKey.Main, traits), p.Method)
                    : p).ToArray());
            if (kind == CombatCommandKind.Move)
                input = MovementTestData.Route(input, new CellCoord(1, 3), new CellCoord(2, 3));

            var facts = EnemyActionAssessmentTests.Assess(input, Array.Empty<IMovementTileRule>(), out _);
            // 원격 재생·도약·넉백은 이 후보에 피해가 없어 적 평가에서 공백도 내지 않는다(R-1).
            Assert.That(facts.Gaps.HasFlag(CombatAssessmentGap.UnsupportedEffect), Is.False);
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.Zero);
        }

        [Test]
        public void MineOutsideMoveRouteDoesNotBlockPreciseAssessment()
        {
            var mine = MineEnvironmentTests.Mine(hidden: false, cell: new CellCoord(4, 4));
            var input = MineEnvironmentTests.Input(mine);
            var facts = EnemyActionAssessmentTests.Assess(input, MineEnvironmentTests.Rules, out var moved);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment));
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.Zero);
            Assert.That(moved, Is.Not.Null);
        }

        [TestCase(TraitKey.HuntersMark)]
        [TestCase(TraitKey.Vanguard)]
        [TestCase(TraitKey.Acceleration)]
        [TestCase(TraitKey.Pull)]
        [TestCase(TraitKey.StationaryFire)]
        public void RemoteLoadoutTraitDoesNotExpandMoveHarm(TraitKey trait)
        {
            var input = CombatCommandTestData.Input();
            var remote = input.Characters.Single(c => c.CharacterKey == EntityKey.Unit_MainCharacter);
            (TraitSpec Spec, TraitStatusSnapshot Status) effect = trait switch
            {
                TraitKey.HuntersMark => (new HuntersMarkTraitSpec(20, 1), new HuntersMarkTraitStatusSnapshot(20, 1)),
                TraitKey.Vanguard => (new VanguardTraitSpec(1, 1), new VanguardTraitStatusSnapshot()),
                TraitKey.Acceleration => (new AccelerationTraitSpec(1), new AccelerationTraitStatusSnapshot()),
                TraitKey.Pull => (new PullTraitSpec(1), new PullTraitStatusSnapshot(1)),
                TraitKey.StationaryFire => (new StationaryFireTraitSpec(20), new StationaryFireTraitStatusSnapshot()),
                _ => throw new ArgumentOutOfRangeException(nameof(trait)),
            };
            var method = new TargetingMethodSpec(TargetingShape.CardinalLine, TargetingChoiceMode.Single, TargetingSplash.None, 0);
            input = CombatCommandTestData.With(input,
                characters: input.Characters.Select(c => c.EntityId == remote.EntityId
                    ? CombatCommandTestData.With(c, traits: new[] { effect.Status }) : c).ToArray(),
                profiles: input.Profiles.Select(p => p.Definition.Key == remote.CharacterKey
                    ? new CombatAttackProfile(CombatCommandTestData.Spec(remote.CharacterKey, 1, CategoryKey.Main, effect.Spec), method)
                    : p).ToArray());
            input = MovementTestData.Route(input, new CellCoord(1, 3), new CellCoord(2, 3));

            var facts = EnemyActionAssessmentTests.Assess(input, Array.Empty<IMovementTileRule>(), out _);
            // 원격 소지 특성은 공백을 선언하지 않았거나 행위자 공격이 아니라 적 평가에서 공백을 내지 않는다(R-1).
            Assert.That(facts.Gaps.HasFlag(CombatAssessmentGap.UnsupportedEffect), Is.False);
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.Zero);
        }

        [Test]
        public void EnteredMineBoundsTheBlastWithoutIncludingRemoteAllies()
        {
            var input = MineEnvironmentTests.Input(MineEnvironmentTests.Mine(hidden: false));
            var near = CombatCommandTestData.Character(20, EntityKey.Unit_Akimbo, new CellCoord(2, 3), Owner.Player);
            var far = CombatCommandTestData.Character(21, EntityKey.Unit_Akimbo, new CellCoord(4, 4), Owner.Player);
            input = MineEnvironmentTests.Copy(input, characters: input.Characters.Concat(new[] { near, far }).ToArray());
            var facts = EnemyActionAssessmentTests.Assess(input, MineEnvironmentTests.Rules, out var moved);
            Assert.That(facts.IsPrecise, Is.False);
            Assert.That(facts.Gaps.HasFlag(CombatAssessmentGap.UnsupportedEffect), Is.True);
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.EqualTo(2000));
            Assert.That(moved, Is.Null);
        }

        [TestCase("X8", 11)]
        [TestCase("H8", 13)]
        public void StationaryAttackDoesNotInheritArrivalUncertainty(string fixture, int actorId)
        {
            var input = EnemyAiBenchmarkFixtures.Build(fixture, 1);
            var actor = input.Characters.Single(unit => unit.EntityId.Value == actorId);
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            using var candidates = new EnemyActionCandidates(context, actor, EnemyActionCandidateTests.MakeForecast(input));
            while (!candidates.IsComplete) candidates.Step();
            Assert.That(candidates.Attacks, Is.Not.Empty);
            Assert.That(CombatActionAssessment.TryAssess(input, candidates.Attacks[0], EnemyAiBenchmarkFixtures.Rules,
                context, out var facts, out _), Is.True);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment));
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.EqualTo(facts.AlliedHpLossPermille));
        }

        [Test]
        public void ReachedTornadoRetainsRandomLandingWithOnlyMoverExposed()
        {
            var input = EnemyAiBenchmarkFixtures.Build("X8", 1);
            var movedAside = input.Characters.Select(unit => unit.EntityId.Value == 11
                ? CombatCommandTestData.With(unit, position: new CellCoord(10, 10)) : unit).ToArray();
            input = CombatCommandTestData.With(input, characters: movedAside);
            var actor = input.Characters.Single(unit => unit.EntityId.Value == 12);
            var route = new MovementPath(new[] { actor.Anchor.Value, new CellCoord(5, 1) });
            var context = new EnemyDecisionContext(input, EnemyDecisionSettings.Default);
            Assert.That(CombatActionAssessment.TryAssess(input, EnemyDecision.Move(actor.EntityId, route, default),
                EnemyAiBenchmarkFixtures.Rules, context, out var facts, out var moved), Is.True);
            Assert.That(facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment | CombatAssessmentGap.RandomOutcome | CombatAssessmentGap.UnknownPosition));
            Assert.That(facts.IsPrecise, Is.False);
            Assert.That(facts.PossibleAlliedHpLossPermille, Is.EqualTo(1000));
            Assert.That(context.PreviewCount, Is.Zero);
            Assert.That(moved, Is.Null);
        }

        [Test]
        public void PiercingFalloffHasDeterministicObservedConsequences()
        {
            var input = PiercingShotTestData.Input(cap: 3);
            var facts = EnemyActionAssessmentTests.Assess(input, Array.Empty<IMovementTileRule>(), out _);
            var actual = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            long opponentLoss = actual.Characters.Characters.Where(change => change.Expected.Owner != Owner.Player)
                .Sum(change => Math.Max(0, change.Expected.Health.CurrentHealth - change.Final.Health.CurrentHealth) *
                    1000L / change.Expected.Health.MaxHealth);
            Assert.That(actual.Accepted, Is.True, actual.Reason);
            Assert.That(facts.IsPrecise, Is.True);
            Assert.That(facts.Gaps, Is.EqualTo(CombatAssessmentGap.HiddenEnvironment));
            Assert.That(facts.OpponentHpLossPermille, Is.EqualTo(opponentLoss));
        }
    }
}
