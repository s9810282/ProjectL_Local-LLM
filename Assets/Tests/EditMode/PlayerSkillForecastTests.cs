using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;

namespace ProjectL.Tests.Core
{
    public sealed class PlayerSkillForecastTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public void TornadoVisibilityControlsFullSimulationWithoutLeakingHiddenPath(bool visible)
        {
            var plain = JudgmentInteractionTests.Input();
            var member = new TornadoMemberSpec(31, new[] { visible ? Owner.Player : Owner.Enemy }, new CellCoord(5, 5), 1);
            var group = new TornadoGimmickState(new StageGimmickId(1), new[] { new TornadoMemberState(31, new CellCoord(5, 5), 1,
                TornadoGimmickPhase.Cooldown, 1, null) });
            var battle = new CombatBattleFacts(4, 1, 2, 0, Array.Empty<CharacterInstanceId>(), 0,
                gimmicks: new[] { new TornadoGimmickSpec(new[] { member }) });
            var input = MagicBulletTestData.Copy(plain, state: plain.State.With(gimmicks: new StageGimmickStateSet(new[] { group })), battle: battle);
            var forecast = Query(input, TornadoEnvironmentTests.Rules);
            Assert.That(forecast.CanUse, Is.True);
            var actual = CombatCommandCalculator.Calculate(input, TornadoEnvironmentTests.Rules);
            Assert.That(actual.Events.OfType<CombatTornadoEntryEvent>().Any(), Is.True);
            CombatHealthEvidenceValidator.Validate(actual);
            if (visible) Assert.That(Encode(forecast.Prediction.Events), Is.EqualTo(Encode(actual.Events)));
            else Assert.That(Encode(forecast), Is.EqualTo(Encode(Query(plain, TornadoEnvironmentTests.Rules))));
        }

        [Test]
        public void PrivateRandomProgressCannotSelectADifferentPublicBranch()
        {
            var input = PowderBlossomFixture.Input(enemies: new[] { PowderBlossomFixture.Enemy(2, 4, 3, 5),
                PowderBlossomFixture.Enemy(3, 6, 6, 100), PowderBlossomFixture.Enemy(4, 7, 6, 100) }, trait: new KillChainAttackTraitSpec());
            var changed = input.WithWorld(input.State.With(itemRandomState: 182,
                environmentProgress: new EnvironmentProgressFacts(99, 999, 89291)), input.Characters,
                input.Main, input.SoulCounts, input.Souls, input.EnvironmentBindings);
            Assert.That(Query(input).CanUse, Is.True);
            Assert.That(Encode(Query(changed)), Is.EqualTo(Encode(Query(input))));
        }

        [TestCase(PlayerSkillUpgradeKey.None)]
        [TestCase(PlayerSkillUpgradeKey.Bloom)]
        [TestCase(PlayerSkillUpgradeKey.Condemnation)]
        public void CompleteKnownPowderCalculationIncludesChildrenAndMatchesExecution(PlayerSkillUpgradeKey upgrade)
        {
            var input = PowderBlossomFixture.Input(upgrade, enemies: new[] { PowderBlossomFixture.Enemy(2, 4, 3, 5),
                PowderBlossomFixture.Enemy(3, 6, 6, 10), PowderBlossomFixture.Enemy(4, 7, 6, 100) }, trait: new KillChainAttackTraitSpec());
            var before = Encode(new object[] { input.State, input.Characters });
            var forecast = Query(input);
            Assert.That(forecast.CanUse, Is.True, forecast.FailureReason.ToString());
            Assert.That(forecast.Coverage, Is.EqualTo(PlayerSkillForecastCoverage.KnownInformationConditional));
            Assert.That(Encode(forecast.Prediction.Events), Is.EqualTo(Encode(PowderBlossomFixture.Calculate(input).Events)));
            Assert.That(Encode(Query(input)), Is.EqualTo(Encode(forecast)));
            Assert.That(Encode(new object[] { input.State, input.Characters }), Is.EqualTo(before));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void HiddenMineCannotChangePublicHpPathOutcomeOrCoverage(bool lethal)
        {
            var plain = JudgmentInteractionTests.Input();
            var hidden = WithMine(plain, lethal ? 2000 : 7, true);
            Assert.That(Query(hidden, MineEnvironmentTests.Rules).CanUse, Is.True);
            Assert.That(Encode(Query(hidden, MineEnvironmentTests.Rules)), Is.EqualTo(Encode(Query(plain, MineEnvironmentTests.Rules))));
            var actual = CombatCommandCalculator.Calculate(hidden, MineEnvironmentTests.Rules);
            Assert.That(actual.Accepted, Is.True);
            Assert.That(actual.Characters.Characters.Single(c => c.Final.EntityId.Value == 2).Final.Health.CurrentHealth,
                Is.LessThan(850));
        }

        [Test]
        public void KnownMineIsFullySimulatedAndRepeatedForecastDoesNotConsumeIt()
        {
            var input = WithMine(JudgmentInteractionTests.Input(), 7, false);
            var forecast = Query(input, MineEnvironmentTests.Rules);
            Assert.That(forecast.CanUse, Is.True);
            Assert.That(Encode(forecast.Prediction.Events), Is.EqualTo(Encode(CombatCommandCalculator.Calculate(input, MineEnvironmentTests.Rules).Events)));
            Assert.That(input.State.Environment.Tiles.Single().Active, Is.True);
            Assert.That(input.State.PlayerSkill.RemainingUses, Is.EqualTo(2));
        }

        [Test]
        public void HiddenRangeTerrainCannotChangePublicSelectionOrRejection()
        {
            var plain = MagicBulletTestData.Input(new[] { new CellCoord(7, 3), new CellCoord(7, 4) });
            var terrain = new StaticTerrainFact(71, new CellCoord(3, 3), StaticTerrainKind.Sandstorm, true, new[] { Owner.Enemy });
            var hidden = MagicBulletTestData.Input(plain.PlayerSkillRequest.Points.ToArray(), terrain: new[] { terrain });
            Assert.That(Query(hidden).CanUse, Is.True);
            Assert.That(Encode(PlayerSkillQueries.Selection(hidden, hidden.PlayerSkillSource.Value)),
                Is.EqualTo(Encode(PlayerSkillQueries.Selection(plain, plain.PlayerSkillSource.Value))));
            Assert.That(Encode(Query(hidden)), Is.EqualTo(Encode(Query(plain))));
            Assert.That(CombatCommandCalculator.Calculate(hidden, Array.Empty<IMovementTileRule>()).Reason,
                Is.EqualTo(nameof(PlayerSkillFailureReason.PointOutOfRange)));
        }

        [Test]
        public void IncompleteSelectionHasNoPredictionAndEmptyValidCastPredictsOneUse()
        {
            var incomplete = MagicBulletTestData.Input(new[] { new CellCoord(5, 3) });
            var rejected = Query(incomplete);
            Assert.That(rejected.FailureReason, Is.EqualTo(PlayerSkillFailureReason.WrongPointCount));
            Assert.That(rejected.Prediction, Is.Null);
            var empty = Query(PowderBlossomFixture.Input());
            Assert.That(empty.CanUse, Is.True);
            Assert.That(empty.Prediction.UseSettlement.FinalRemainingUses, Is.EqualTo(1));
        }

        internal static CombatCommandInput WithMine(CombatCommandInput input, int damage, bool hidden)
        {
            var environment = new MovementEnvironmentFacts(new MovementTileFact[] {
                MineEnvironmentTests.Mine(71, damage: damage, hidden: hidden, cell: new CellCoord(5, 5)) });
            return input.WithWorld(input.State.With(environment: environment, environmentProgress: EnvironmentProgressFacts.Initial(environment)),
                input.Characters, input.Main, input.SoulCounts, input.Souls,
                new EnvironmentSemanticBindings(Array.Empty<long>(), new long[] { 71 }, Array.Empty<EntityId>()));
        }

        internal static PlayerSkillForecast Query(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules = null) =>
            PlayerSkillQueries.Forecast(input, rules ?? Array.Empty<IMovementTileRule>(), input.PlayerSkillSource.Value);
        internal static string Encode(object value) => AttackPreviewParityTests.Serialize(value, new HashSet<string>());
    }
}
