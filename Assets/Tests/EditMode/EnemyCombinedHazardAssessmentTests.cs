using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Tests.Core
{
    public sealed class EnemyCombinedHazardAssessmentTests
    {
        private static readonly CellCoord Center = new(1, 1);
        private static readonly CellCoord Intermediate = new(2, 2);
        private static readonly CellCoord End = new(4, 4);
        private static readonly CombatAssessmentGap ThrowGaps = CombatAssessmentGap.UnknownPosition | CombatAssessmentGap.RandomOutcome;

        [Test]
        public void WarnedSweepIncludesIntermediateCenters()
        {
            var forecast = Forecast(Input(Tornado()));
            Assert.That(forecast.Facts.Where(f => f.BoundaryCount == 5 && f.Key == StageGimmickKey.Tornado)
                .Select(f => f.Cell), Is.EquivalentTo(new CellCoord?[] { Center, Intermediate, new CellCoord(3, 3), End }));
        }

        [Test]
        public void CurrentTornadoEntryIsRiskEvenBeforeNextSweep()
        {
            var input = WithStorm(Input(Tornado()), TornadoGimmickPhase.Warning, 8);
            var actor = Actor(input);
            var forecast = Forecast(input);
            var endpoint = new CellCoord(2, 1);
            var contact = forecast.Evaluate(actor, new[] { Center, endpoint }, endpoint);
            var clear = forecast.Evaluate(actor, Array.Empty<CellCoord>(), endpoint);
            Assert.That(contact.Gaps & ThrowGaps, Is.EqualTo(ThrowGaps));
            Assert.That(clear.Gaps & ThrowGaps, Is.EqualTo(CombatAssessmentGap.None));
            var accumulated = forecast.Evaluate(actor, Array.Empty<CellCoord>(), endpoint, forecast.EntryGaps(actor, Center));
            Assert.That(accumulated.Gaps, Is.EqualTo(contact.Gaps));
            Assert.That(accumulated.PossibleHpLoss, Is.EqualTo(contact.PossibleHpLoss));
            var stationary = CombatMovementCalculator.MoveTo(actor, Center);
            Assert.That(forecast.Evaluate(stationary, new[] { Center }, Center).Gaps, Is.EqualTo(CombatAssessmentGap.None));
        }

        [Test]
        public void HiddenTornadoDoesNotRevealEntryOrSweepCells()
        {
            var input = Input(new TornadoGimmickSpec(new[] {
                new TornadoMemberSpec(71, new[] { Owner.Player }, Center, 1, sideLength: 1) }));
            var forecast = Forecast(input);
            Assert.That(forecast.Facts, Is.Empty);
            Assert.That(forecast.Evaluate(Actor(input), new[] { Center, Intermediate }, Intermediate).Gaps,
                Is.EqualTo(CombatAssessmentGap.None));
        }

        [Test]
        public void ImmuneActorDoesNotReceiveThrowRisk()
        {
            var input = Input(Tornado(), new RailwayGimmickSpec(5, 2));
            var profiles = input.Profiles.Select(p =>
            {
                if (p.Definition.Key != EntityKey.Unit_Demon) return p;
                var d = p.Definition;
                return new CombatAttackProfile(new UnitSpec(d.Key, d.Role, d.FootprintWidth, d.FootprintHeight,
                    d.FootprintLocalCells.ToArray(), d.MaxHealth, d.MoveMethod, d.AttackRange, d.CategoryKey,
                    d.AttackPower, d.PhysicalDefense, d.Priority, d.Traits.ToArray(), d.OwnerEligibility,
                    behavior: new UnitBehaviorSpec(true, true, true)), p.Method);
            }).ToArray();
            input = CombatCommandTestData.With(input, profiles: profiles);
            var actor = Actor(input);
            var risk = Forecast(input).Evaluate(actor, new[] { Center, Intermediate }, Intermediate);
            Assert.That(risk.Gaps & ThrowGaps, Is.EqualTo(CombatAssessmentGap.None));
            Assert.That(risk.KnownHpLoss, Is.EqualTo(actor.Health.CurrentHealth));
        }

        [Test]
        public void RandomLandingIsNotKnownSafe()
        {
            var input = Input(Tornado());
            var actor = Actor(input);
            var risk = Forecast(input).Evaluate(actor, Array.Empty<CellCoord>(), Intermediate);
            Assert.That(risk.Gaps & ThrowGaps, Is.EqualTo(ThrowGaps));
            Assert.That(risk.KnownHpLoss, Is.Zero);
            Assert.That(risk.PossibleHpLoss, Is.EqualTo(actor.Health.CurrentHealth));
            Assert.That(risk.Evidence.Any(f => f.Cell == Intermediate), Is.True);
        }

        [Test]
        public void CooldownDoesNotPeekNextDirection()
        {
            var input = WithStorm(Input(Tornado()), TornadoGimmickPhase.Cooldown, 5);
            var forecast = Forecast(input);
            var warning = forecast.Facts.Single(f => f.Key == StageGimmickKey.Tornado && f.BoundaryCount == 5);
            Assert.That(warning.Cell, Is.Null);
            Assert.That(warning.Gaps, Is.EqualTo(CombatAssessmentGap.FutureWarning));
            var risk = forecast.Evaluate(Actor(input), Array.Empty<CellCoord>(), Intermediate);
            Assert.That(risk.Evidence.Where(f => f.BoundaryCount == 6).All(f => f.Cell == null), Is.True);
        }

        [Test]
        public void NoSourceRandomMutation()
        {
            var input = Input(Tornado(), Haboob());
            string before = EnemyActionAssessmentTests.Encode(input);
            var alternate = CombatCommandTestData.With(input, state: input.State.With(environmentProgress:
                new EnvironmentProgressFacts(input.State.EnvironmentProgress.NextObjectEntryOrdinal,
                    input.State.EnvironmentProgress.NextSourceId, 9191)));
            Assert.That(EnemyHazardForecastTests.EncodeFacts(Forecast(alternate)),
                Is.EqualTo(EnemyHazardForecastTests.EncodeFacts(Forecast(input))));
            Assert.That(EnemyActionAssessmentTests.Encode(input), Is.EqualTo(before));
        }

        [Test]
        public void TornadoBeforeRailMakesRailContactUncertain()
        {
            var input = Input(Tornado(), new RailwayGimmickSpec(5, 2));
            var actor = Actor(input);
            var risk = Forecast(input).Evaluate(actor, Array.Empty<CellCoord>(), End);
            Assert.That(risk.KnownHpLoss, Is.Zero);
            Assert.That(risk.PossibleHpLoss, Is.EqualTo(actor.Health.CurrentHealth));
            Assert.That(risk.Evidence.Select(f => f.Key).Distinct(),
                Is.EqualTo(new[] { StageGimmickKey.Tornado, StageGimmickKey.Railway }));
        }

        [Test]
        public void DeterministicEndpointThenRailUsesNewCell()
        {
            var input = Input(new RailwayGimmickSpec(5, 2));
            var actor = Actor(input);
            var forecast = Forecast(input);
            var projected = CombatMovementCalculator.MoveTo(actor, Intermediate);
            Assert.That(forecast.Evaluate(projected, Array.Empty<CellCoord>(), projected.Anchor.Value).KnownHpLoss,
                Is.EqualTo(projected.Health.CurrentHealth));
            Assert.That(forecast.Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value).KnownHpLoss, Is.Zero);
        }

        [Test]
        public void HaboobCrossBoundaryDesignationMatchesLiveQuery()
        {
            var input = Input(Haboob());
            var forecast = Forecast(input);
            var region = ((HaboobGimmickState)input.State.Gimmicks.Get(new StageGimmickId(1))).Cells;
            var warned = forecast.Facts.Where(f => f.Key == StageGimmickKey.Haboob && f.BoundaryCount == 5 && f.Cell.HasValue)
                .Select(f => f.Cell.Value).ToArray();
            Assert.That(warned, Is.EquivalentTo(region));
            Assert.That(HaboobTargetingRules.CanDesignate(warned, Center, Intermediate),
                Is.EqualTo(HaboobTargetingRules.CanDesignate(region, Center, Intermediate)));
            Assert.That(HaboobTargetingRules.CanDesignate(warned, Center, Intermediate), Is.False);
            var actor = CombatMovementCalculator.MoveTo(Actor(input), Center);
            var profile = CombatCommandCalculator.RequiredProfile(input, actor);
            var live = CombatAttackQueries.Selection(input, actor, profile, AttackAim.ForCell(Intermediate));
            Assert.That(live.FailureReason, Is.EqualTo(AttackFailureReason.HaboobDesignationBlocked));
            Assert.That(forecast.CanDesignateAt(Center, Intermediate, 4), Is.False);
            Assert.That(forecast.CanDesignateAt(Center, Intermediate, 5), Is.False);
            Assert.That(forecast.CanDesignateAt(Center, Intermediate, 6), Is.Null);
            Assert.That(forecast.Evaluate(Actor(input), Array.Empty<CellCoord>(), Center).PossibleHpLoss, Is.Zero);
        }

        [Test]
        public void HaboobWithoutAlternativeAnchorRetainsKnownRegion()
        {
            var region = new[] { CellCoord.Zero, new CellCoord(7, 7) };
            var input = Input(new HaboobGimmickSpec(footprintOffsets: region, initialAnchor: CellCoord.Zero));
            var forecast = Forecast(input);
            var relocation = forecast.Facts.Where(f => f.Key == StageGimmickKey.Haboob && f.BoundaryCount == 6).ToArray();
            Assert.That(relocation.Select(f => f.Cell), Is.EquivalentTo(region.Select(cell => (CellCoord?)cell)));
            Assert.That(relocation.Select(f => f.Gaps), Is.All.EqualTo(CombatAssessmentGap.None));
            Assert.That(forecast.CanDesignateAt(CellCoord.Zero, Intermediate, 6), Is.False);
        }

        [Test]
        public void TwoHazardsCannotOvercountLife()
        {
            var input = Input(Tornado(), new RailwayGimmickSpec(5, 2), new RockfallGimmickSpec(maximumHealthLossPercent: 100));
            var actor = Actor(input);
            var risk = Forecast(input).Evaluate(actor, Array.Empty<CellCoord>(), Intermediate);
            Assert.That(risk.KnownHpLoss, Is.Zero);
            Assert.That(risk.PossibleHpLoss, Is.EqualTo(actor.Health.CurrentHealth));
            Assert.That(risk.Evidence.Select(f => f.Key).Distinct(),
                Is.EqualTo(new[] { StageGimmickKey.Tornado, StageGimmickKey.Railway, StageGimmickKey.Rockfall }));
            Assert.That(risk.PossibleBindEndCount, Is.Zero);
        }

        [Test]
        public void KnownTerminalStopsForecast()
        {
            var input = WithStorm(Input(Tornado(), new RailwayGimmickSpec(5, 2), new RockfallGimmickSpec()),
                TornadoGimmickPhase.Warning, 8);
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 10
                ? CombatCommandTestData.With(c, position: Intermediate) : c).ToArray());
            var actor = Actor(input);
            var risk = Forecast(input).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(risk.Evidence.Any(f => f.Key == StageGimmickKey.Rockfall), Is.False);
            Assert.That(risk.KnownHpLoss, Is.Zero);
        }

        [Test]
        public void UncertainKillDoesNotSuppressRemainingHazards()
        {
            var input = Input(Tornado(), new RailwayGimmickSpec(5, 2), new RockfallGimmickSpec());
            input = CombatCommandTestData.With(input, characters: input.Characters.Select(c => c.EntityId.Value == 10
                ? CombatCommandTestData.With(c, position: Intermediate) : c).ToArray());
            var actor = Actor(input);
            var risk = Forecast(input).Evaluate(actor, Array.Empty<CellCoord>(), actor.Anchor.Value);
            Assert.That(risk.Evidence.Any(f => f.Key == StageGimmickKey.Rockfall), Is.True);
            Assert.That(risk.KnownHpLoss, Is.Zero);
        }

        private static TornadoGimmickSpec Tornado() => new(new[] {
            new TornadoMemberSpec(71, new[] { Owner.Enemy, Owner.Player }, Center, 1, sideLength: 1) });

        private static HaboobGimmickSpec Haboob() => new(footprintOffsets: new[] { CellCoord.Zero }, initialAnchor: Center);

        private static CombatCommandInput Input(params StageGimmickSpec[] definitions)
        {
            var input = EnemyHazardForecastTests.Input(definitions, warning: true, rails: new[] { Intermediate }).WithActor(new EntityId(2));
            var rockfall = input.State.Gimmicks.Entries.OfType<RockfallGimmickState>().SingleOrDefault();
            if (rockfall != null)
                input = CombatCommandTestData.With(input, state: input.State.With(gimmicks: input.State.Gimmicks.Replace(
                    new RockfallGimmickState(rockfall.Id, RockfallGimmickPhase.Warning, 5, new[] { Actor(input).Anchor.Value }))));
            return definitions.Any(d => d is TornadoGimmickSpec) ? WithStorm(input, TornadoGimmickPhase.Warning, 5) : input;
        }

        private static CombatCommandInput WithStorm(CombatCommandInput input, TornadoGimmickPhase phase, long boundary)
        {
            var group = input.State.Gimmicks.Entries.OfType<TornadoGimmickState>().Single();
            var state = new TornadoGimmickState(group.Id, new[] { new TornadoMemberState(71, Center, 1, phase, boundary,
                phase == TornadoGimmickPhase.Warning ? new CellCoord(1, 1) : null) });
            return CombatCommandTestData.With(input, state: input.State.With(gimmicks: input.State.Gimmicks.Replace(state)));
        }

        private static CharacterCombatFacts Actor(CombatCommandInput input) => input.Characters.Single(c => c.EntityId == input.ActorId);

        private static StageHazardForecast Forecast(CombatCommandInput input)
        {
            using var builder = new StageHazardForecast.Builder(input);
            return EnemyHazardForecastTests.Finish(builder);
        }
    }
}
