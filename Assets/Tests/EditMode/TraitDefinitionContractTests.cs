using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Definitions;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class TraitDefinitionContractTests
    {
        private const string Root = "ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.";

        [Test]
        public void DefinitionConversion_UsesRegisteredFactory()
        {
            var original = TraitRegistry.CreateDefault().GetRequiredRegistration(TraitKey.Hunter);
            var factory = new CountingHunterFactory();
            var registration = new TraitRegistration(original.TraitKey, original.SpecType, original.DefinitionType,
                original.RuntimeStateType, factory, original.TraitRuntimeStateFactory, original.Rule, original.EffectRuleFactory);
            var registry = new TraitRegistry(new[] { registration });
            var spec = new HunterTraitSpec(2, 4);
            var definition = registry.BuildDefinition(spec);
            Assert.That(factory.Calls, Is.EqualTo(1));
            Assert.That(factory.Received, Is.SameAs(spec));
            Assert.That(definition, Is.SameAs(factory.Result));
        }

        [Test]
        public void DefinitionConversion_PreservesValidationBoundaries()
        {
            var registry = TraitRegistry.CreateDefault();
            Assert.Throws<ArgumentNullException>(() => registry.BuildDefinition(null));
            Assert.Throws<InvalidOperationException>(() => new TraitRegistry(Array.Empty<TraitRegistration>()).BuildDefinition(new HunterTraitSpec(2, 4)));
            var factory = registry.GetRequiredRegistration(TraitKey.Hunter).TraitDefinitionFactory;
            Assert.Throws<ArgumentNullException>(() => factory.BuildDefinition(null));
            Assert.Throws<ArgumentException>(() => factory.BuildDefinition(new CohesionTraitSpec(3, 7, 2, 2)));
        }

        private sealed class CountingHunterFactory : ITraitDefinitionFactory
        {
            public TraitKey TraitKey => TraitKey.Hunter;
            public Type SpecType => typeof(HunterTraitSpec);
            public Type DefinitionType => typeof(HunterTraitDefinition);
            internal int Calls { get; private set; }
            internal TraitSpec Received { get; private set; }
            internal HunterTraitDefinition Result { get; } = new HunterTraitDefinition(9, 5);

            public TraitDefinition BuildDefinition(TraitSpec spec)
            {
                Calls++;
                Received = spec;
                return Result;
            }
        }

        [TestCase("SquareNeighbors", 1, false, 8)]
        [TestCase("SquareNeighbors", 2, false, 24)]
        [TestCase("OrthogonalCross", 1, false, 4)]
        [TestCase("SquareNeighbors", 1, true, 9)]
        public void SquareNeighbors_DefaultsExcludeCenter(string footprint, int radius, bool center, int expected)
        {
            Type areaType = typeof(TraitSpec).Assembly.GetType(Root + "Contracts.Commands.Traits.TraitAreaSpec");
            Assert.That(areaType, Is.Not.Null, "The pure area contract is required.");
            var constructor = areaType.GetConstructors().Single();
            var kind = Enum.Parse(constructor.GetParameters()[0].ParameterType, footprint);
            object area = constructor.Invoke(new[] { kind, (object)radius, center });
            Type rules = typeof(TraitRegistry).Assembly.GetReferencedAssemblies()
                .Select(Assembly.Load).Select(a => a.GetType(Root + "Domain.Traits.Rules.TraitAreaRules"))
                .FirstOrDefault(t => t != null);
            Assert.That(rules, Is.Not.Null);
            var contains = rules.GetMethod("Contains", BindingFlags.Public | BindingFlags.Static);
            int count = 0;
            var origin = new CellCoord(-3, 7);
            for (int x = -3; x <= 3; x++)
                for (int y = -3; y <= 3; y++)
                    if ((bool)contains.Invoke(null, new[] { (object)origin, new CellCoord(origin.X + x, origin.Y + y), area })) count++;
            Assert.That(count, Is.EqualTo(expected));
        }

        [TestCase("Vanguard", "attackBonus,durationControllerTurnEnds", "3,2")]
        [TestCase("CleanSweep", "attackBonusPerAllyAttack,durationControllerTurnEnds", "4,2")]
        [TestCase("SoloAction", "shieldAmount,radiusCells,requiredOccupiedNeighborCount,resetEveryControllerTurnEnds", "13,2,3,2")]
        [TestCase("CallOfBlood", "enemyMaxHealthHealPercent", "20")]
        [TestCase("MoraleBoost", "defeatedEnemyBaseAttackPercent,durationControllerTurnEnds", "20,2")]
        [TestCase("Disarm", "durationAffectedSideTurnEnds", "2")]
        [TestCase("AnkleBreaker", "durationAffectedSideTurnEnds", "2")]
        [TestCase("Cohesion", "defensePerAdjacentAlly,shieldAmountPerAlly,radiusCells,resetEveryControllerTurnEnds", "3,7,2,2")]
        [TestCase("Sacrifice", "radiusCells,maxTriggersPerEnemyTurn", "3,2")]
        [TestCase("Taunt", "radiusCells", "3")]
        [TestCase("Entrenchment", "radiusCells,damageReductionPercent", "2,20")]
        [TestCase("SteadfastWill", "", "")]
        [TestCase("GroundCompaction", "movementRangeBonus,opposingTurnEndsToExpire", "3,2")]
        [TestCase("DustScreen", "attackRangeReduction,attackDamageReductionPercent,opposingTurnEndsToExpire", "3,20,2")]
        [TestCase("Leap", "", "")]
        [TestCase("Acceleration", "attackPerMovedCell", "3")]
        [TestCase("StationaryFire", "extraDamagePercent", "120")]
        [TestCase("ArmorBreaker", "defenseReductionPercentPerHit", "20")]
        [TestCase("FinishingShot", "attackThresholdPercent", "25")]
        [TestCase("TrackingShot", "targetHealthThresholdPercent,extraDamagePercent", "60,20")]
        [TestCase("Hunter", "attackPerTargetKill,maxRewardsPerStage", "2,4")]
        [TestCase("FocusedBombardment", "requiredEnemyCount,extraDamagePercent", "2,70")]
        public void Definitions_RoundTripEveryTunable(string stem, string fields, string values)
        {
            int[] numbers = values.Length == 0 ? Array.Empty<int>() : values.Split(',').Select(int.Parse).ToArray();
            string[] names = fields.Length == 0 ? Array.Empty<string>() : fields.Split(',');
            Type specType = typeof(TraitSpec).Assembly.GetType(typeof(TraitSpec).Namespace + "." + stem + "TraitSpec");
            Assert.That(specType, Is.Not.Null);
            var arguments = numbers.Cast<object>().ToArray();
            var spec = (TraitSpec)Activator.CreateInstance(specType, arguments);
            var definition = TraitRegistry.CreateDefault().BuildDefinition(spec);
            var unit = new UnitSpec(EntityKey.Unit_Akimbo, UnitRole.Regular, 1, 1, new[] { CellCoord.Zero }, 100,
                MoveMethod.LiftAndPlace, 3, CategoryKey.Main, 10, 0, 1, new[] { spec });
            var runtime = new UnitCatalog(new[] { unit }, TraitRegistry.CreateDefault()).GetRequired(unit.Key)
                .TraitDefinitions.ToArray().Single();
            Assert.That(definition.TraitKey, Is.EqualTo(spec.TraitKey));
            Assert.That(runtime.TraitKey, Is.EqualTo(spec.TraitKey));
            for (int i = 0; i < names.Length; i++)
            {
                string property = char.ToUpperInvariant(names[i][0]) + names[i].Substring(1);
                Assert.That(definition.GetType().GetProperty(property).GetValue(definition), Is.EqualTo(numbers[i]));
                Assert.That(runtime.GetType().GetProperty(property).GetValue(runtime), Is.EqualTo(numbers[i]));
                object[] invalid = arguments.ToArray();
                invalid[i] = -1;
                var error = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(specType, invalid));
                Assert.That(error.InnerException, Is.InstanceOf<ArgumentOutOfRangeException>());
                Assert.That(((ArgumentException)error.InnerException).ParamName, Is.EqualTo(names[i]));
            }
        }

        [TestCase("Vanguard", 220)] [TestCase("CleanSweep", 230)] [TestCase("SoloAction", 240)]
        [TestCase("CallOfBlood", 250)] [TestCase("MoraleBoost", 260)] [TestCase("Disarm", 270)]
        [TestCase("AnkleBreaker", 280)] [TestCase("Cohesion", 290)] [TestCase("Sacrifice", 300)]
        [TestCase("Taunt", 310)] [TestCase("Entrenchment", 320)] [TestCase("SteadfastWill", 330)]
        [TestCase("GroundCompaction", 340)] [TestCase("DustScreen", 350)] [TestCase("Leap", 360)]
        [TestCase("Acceleration", 370)] [TestCase("StationaryFire", 380)] [TestCase("ArmorBreaker", 390)]
        [TestCase("FinishingShot", 400)] [TestCase("TrackingShot", 410)] [TestCase("Hunter", 420)]
        [TestCase("FocusedBombardment", 430)]
        public void Definitions_AreStableAndAvailabilityMatchesExecution(string name, int value)
        {
            Assert.That(Enum.TryParse(name, out TraitKey key), Is.True, "A stable serialized identity is required.");
            Assert.That((int)key, Is.EqualTo(value));
            if (key == TraitKey.Vanguard || key == TraitKey.CleanSweep || key == TraitKey.CallOfBlood || key == TraitKey.MoraleBoost || key == TraitKey.Disarm || key == TraitKey.AnkleBreaker || key == TraitKey.SoloAction || key == TraitKey.Cohesion || key == TraitKey.Entrenchment || key == TraitKey.Sacrifice || key == TraitKey.Taunt || key == TraitKey.SteadfastWill || key == TraitKey.GroundCompaction || key == TraitKey.DustScreen || key == TraitKey.Leap || key == TraitKey.Acceleration || key == TraitKey.StationaryFire || key == TraitKey.FinishingShot || key == TraitKey.TrackingShot || key == TraitKey.FocusedBombardment || key == TraitKey.Hunter || key == TraitKey.ArmorBreaker)
            {
                Assert.That(DeferredCombatFeatures.IsTraitAvailable(key), Is.True);
                Assert.That(TraitRegistry.CreateDefault().GetRequiredRegistration(key).TraitKey, Is.EqualTo(key));
                return;
            }
            Assert.That(DeferredCombatFeatures.IsTraitAvailable(key), Is.False);
            Assert.That(DeferredCombatFeatures.GetTraitUnavailableReason(key), Is.EqualTo("ImplementationPending"));
            var failure = Assert.Throws<InvalidOperationException>(() => TraitRegistry.CreateDefault().GetRequiredRegistration(key));
            Assert.That(failure.Message, Does.Contain(name).And.Contain("ImplementationPending"));
        }

        [Test]
        public void ExistingIdentityAndAvailability_ArePreserved()
        {
            Assert.That((int)TraitKey.HuntersMark, Is.EqualTo(190));
            Assert.That(DeferredCombatFeatures.IsTraitAvailable(TraitKey.HuntersMark), Is.True);
            foreach (var key in new[] { TraitKey.Focus, TraitKey.ThornField, TraitKey.Necromancy })
                Assert.That(DeferredCombatFeatures.GetTraitUnavailableReason(key), Is.EqualTo("DeferredUnitTurnTiming"));
        }
    }
}
