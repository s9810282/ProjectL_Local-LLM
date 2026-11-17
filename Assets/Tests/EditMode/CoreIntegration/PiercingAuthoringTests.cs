using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Unit.Trait;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Enhancements;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.CoreIntegration
{
    public sealed class PiercingAuthoringTests
    {
        [Test]
        public void SniperUsesDedicatedCrossAndOneEffectivePiercingDefinition()
        {
            var data = DataBaseAssetTests.ProductionInGame();
            var sniper = data.UnitTableQuery.GetUnit(EntityKey.Unit_Sniper);
            Assert.That(sniper.TargetingMethodKey, Is.EqualTo(10));
            var effective = OwnedUnitSpecResolver.ResolveBase(UnitSpecAdapter.ReadUnit(sniper), DataBaseAssetTests.ActiveGrowthCatalog());
            Assert.That(effective.AttackRange, Is.EqualTo(sniper.AttackRange));
            var method = UnitSpecAdapter.ReadProfile(data, effective).Method;
            Assert.That(method.Shape, Is.EqualTo(TargetingShape.CardinalLine));
            Assert.That(method.ChoiceMode, Is.EqualTo(TargetingChoiceMode.Single));
            Assert.That(method.Splash, Is.EqualTo(TargetingSplash.None));
            Assert.That(method.ChoiceAngle, Is.EqualTo(TargetingChoiceAngle.Unrestricted));
            var trait = effective.Traits.ToArray().OfType<PiercingFalloffTraitSpec>().Single();
            var authored = sniper.Traits.OfType<PiercingFalloffTraitData>().Single();
            Assert.That((trait.ReductionPercentPerTarget, trait.MaxTargetsPerRay),
                Is.EqualTo((authored.ReductionPercentPerTarget, authored.MaxTargetsPerRay)));
        }

        [Test]
        public void SharedMethodOneKeepsItsOriginalValues()
        {
            var method = DataBaseAssetTests.ProductionInGame().TargetingMethodTableQuery.GetTargetingMethod(1);
            Assert.That(method.Shape, Is.EqualTo(TargetingShape.RadialSquare));
            Assert.That(method.ChoiceMode, Is.EqualTo(TargetingChoiceMode.Single));
            Assert.That(method.Splash, Is.EqualTo(TargetingSplash.None));
            Assert.That(method.SplashPercent, Is.Zero);
            Assert.That(method.ChoiceAngle, Is.EqualTo(TargetingChoiceAngle.Unrestricted));
        }
    }
}
