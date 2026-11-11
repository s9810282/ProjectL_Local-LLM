using System;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyPreviewPreparationTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void RepeatedEffectCaptureDoesNotRebuildRegistrationMetadata(bool withTrait)
        {
            var input = withTrait ? StationaryInput(30, false) : CombatCommandTestData.Input();
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            CombatCommandEffects.Capture(actor, profile);
            var counter = new BindingCounter();
            CoreProfiling.Install(counter);
            try
            {
                for (int i = 0; i < 32; i++)
                    CombatCommandEffects.Capture(actor, profile);
            }
            finally
            {
                CoreProfiling.Install(null);
            }
            TestContext.Out.WriteLine($"trait={withTrait}, captures={counter.Captures}, registryBuilds={counter.Builds}, " +
                $"captureMs={counter.CaptureTicks * 1000.0 / Stopwatch.Frequency:F3}, registryMs={counter.BuildTicks * 1000.0 / Stopwatch.Frequency:F3}");
            Assert.That(counter.Captures, Is.EqualTo(32));
            Assert.That(counter.Builds, Is.Zero, "미리보기마다 고정 등록표를 재구성하면 후보 수에 비례해 반복 비용이 발생합니다.");
        }

        [Test]
        public void EffectCaptureRetainsIndependentCurrentStateAndDefinition()
        {
            var first = CaptureStationary(StationaryInput(30, false));
            var changedState = CaptureStationary(StationaryInput(30, true));
            var changedDefinition = CaptureStationary(StationaryInput(70, false));
            var engine = new CombatEffectEngine();
            var damage = new TargetDamageEffectContext(20, 10, 1, 0);
            Assert.That(engine.EvaluateTargetDamage(first, damage).Contributions.Single().Numerator, Is.EqualTo(130));
            Assert.That(engine.EvaluateTargetDamage(changedState, damage).Contributions.Single().Numerator, Is.EqualTo(100));
            Assert.That(engine.EvaluateTargetDamage(changedDefinition, damage).Contributions.Single().Numerator, Is.EqualTo(170));
            Assert.That(engine.EvaluateTargetDamage(first, damage).Contributions.Single().Numerator, Is.EqualTo(130));
            Assert.That(first[0].ExpectedState, Is.Not.SameAs(changedState[0].ExpectedState));
        }

        [Test]
        public void RepeatedPreviewLeavesInputAndActualSettlementUnchanged()
        {
            var input = StationaryInput(30, false);
            var before = CombatCommandCalculator.Calculate(input, EnemyAiBenchmarkFixtures.Rules);
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            for (int i = 0; i < 8; i++)
            {
                var preview = CombatCommandCalculator.Preview(input, EnemyAiBenchmarkFixtures.Rules, out var projection);
                Assert.That(preview.CanAttack, Is.True);
                Assert.That(projection.Accepted, Is.True);
            }
            var after = CombatCommandCalculator.Calculate(input, EnemyAiBenchmarkFixtures.Rules);
            Assert.That(EnemyAiBaselineTests.Trace(after), Is.EqualTo(EnemyAiBaselineTests.Trace(before)));
            Assert.That(input.Characters.Single(c => c.EntityId == input.ActorId), Is.SameAs(actor));
            Assert.That(((StationaryFireTraitStatusSnapshot)actor.Stats.TraitStatuses.ToArray().Single()).MovedThisOwnTurn, Is.False);
            Assert.That(after.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt),
                Is.EqualTo(before.Attack.Value.Hits.ToArray().Select(h => h.DamageDealt)));
        }

        private static CombatCommandInput StationaryInput(int extraPercent, bool moved) =>
            CombatCommandEffectTests.ActorEffect(new StationaryFireTraitSpec(extraPercent),
                new StationaryFireTraitStatusSnapshot(moved));

        private static EffectRuleBinding[] CaptureStationary(CombatCommandInput input)
        {
            var actor = input.Characters.Single(c => c.EntityId == input.ActorId);
            return CombatCommandEffects.Capture(actor, CombatCommandCalculator.RequiredProfile(input, actor));
        }

        private sealed class BindingCounter : ICoreProfiler
        {
            private long _captureStart;
            private long _buildStart;
            internal int Captures { get; private set; }
            internal int Builds { get; private set; }
            internal long CaptureTicks { get; private set; }
            internal long BuildTicks { get; private set; }

            public void Begin(CoreProfileSection section)
            {
                if (section == CoreProfileSection.AttackEffectBinding)
                {
                    Captures++;
                    _captureStart = Stopwatch.GetTimestamp();
                }
                if (section == CoreProfileSection.TraitRegistryBuild)
                {
                    Builds++;
                    _buildStart = Stopwatch.GetTimestamp();
                }
            }

            public void End(CoreProfileSection section)
            {
                if (section == CoreProfileSection.AttackEffectBinding)
                    CaptureTicks += Stopwatch.GetTimestamp() - _captureStart;
                if (section == CoreProfileSection.TraitRegistryBuild)
                    BuildTicks += Stopwatch.GetTimestamp() - _buildStart;
            }
        }
    }
}
