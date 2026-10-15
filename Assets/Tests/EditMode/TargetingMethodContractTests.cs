using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    public sealed class TargetingMethodContractTests
    {
        [Test]
        public void SerializedMethods_KeepStableValues()
        {
            Assert.That((int)TargetingShape.None, Is.Zero);
            Assert.That((int)TargetingShape.CardinalLine, Is.EqualTo(1));
            Assert.That((int)TargetingShape.DiagonalLine, Is.EqualTo(2));
            Assert.That((int)TargetingShape.EightDirectionLine, Is.EqualTo(3));
            Assert.That((int)TargetingShape.Fan, Is.EqualTo(4));
            Assert.That((int)TargetingShape.RadialSquare, Is.EqualTo(5));
            Assert.That((int)TargetingChoiceMode.None, Is.Zero);
            Assert.That((int)TargetingChoiceMode.Single, Is.EqualTo(1));
            Assert.That((int)TargetingChoiceMode.Direction, Is.EqualTo(2));
            Assert.That((int)TargetingChoiceMode.All, Is.EqualTo(3));
            Assert.That((int)TargetingSplash.None, Is.Zero);
            Assert.That((int)TargetingSplash.Cross, Is.EqualTo(1));
            Assert.That((int)TargetingSplash.Diagonal, Is.EqualTo(2));
            Assert.That((int)TargetingSplash.Radial, Is.EqualTo(3));
            Assert.That((int)AttackTargetRole.None, Is.Zero);
            Assert.That((int)AttackTargetRole.Primary, Is.EqualTo(1));
            Assert.That((int)AttackTargetRole.Additional, Is.EqualTo(2));
            Assert.That((int)AttackTargetRole.Designated, Is.EqualTo(3));
        }

        [Test]
        public void SupportedAimKinds_KeepNumericValues()
        {
            Assert.That(Enum.GetValues(typeof(AttackAimKind)), Is.EqualTo(new[]
            {
                AttackAimKind.None, AttackAimKind.Cell, AttackAimKind.Direction, AttackAimKind.All
            }));
            Assert.That((int)AttackAimKind.None, Is.Zero);
            Assert.That((int)AttackAimKind.Cell, Is.EqualTo(2));
            Assert.That((int)AttackAimKind.Direction, Is.EqualTo(3));
            Assert.That((int)AttackAimKind.All, Is.EqualTo(4));
        }

        [TestCase(TargetingChoiceMode.Single, AttackAimKind.Cell)]
        [TestCase(TargetingChoiceMode.Direction, AttackAimKind.Direction)]
        [TestCase(TargetingChoiceMode.All, AttackAimKind.All)]
        public void EveryShape_UsesExplicitChoiceWithoutInferringFaction(
            TargetingChoiceMode choice, AttackAimKind expectedAim)
        {
            foreach (TargetingShape shape in new[]
                     {
                         TargetingShape.CardinalLine, TargetingShape.DiagonalLine,
                         TargetingShape.EightDirectionLine, TargetingShape.Fan,
                         TargetingShape.RadialSquare
                     })
            {
                var method = new TargetingMethodSpec(shape, choice, TargetingSplash.None, 0);

                Assert.That(method.Shape, Is.EqualTo(shape));
                Assert.That(method.ChoiceMode, Is.EqualTo(choice));
                Assert.That(AttackProfileCompatibility.GetSupportedAimKind(method), Is.EqualTo(expectedAim));
            }
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(6)]
        [TestCase(8)]
        public void Method_RejectsNoneUnknownAndLegacyShapeNumbers(int shape)
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TargetingMethodSpec((TargetingShape)shape, TargetingChoiceMode.Single,
                    TargetingSplash.None, 0)).ParamName, Is.EqualTo("shape"));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(4)]
        public void Method_RejectsNoneOrUnknownChoice(int choice)
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TargetingMethodSpec(TargetingShape.RadialSquare, (TargetingChoiceMode)choice,
                    TargetingSplash.None, 0)).ParamName, Is.EqualTo("choiceMode"));
        }

        [TestCase(-1)]
        [TestCase(4)]
        public void Method_RejectsUnknownSplash(int splash)
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single,
                    (TargetingSplash)splash, 0)).ParamName, Is.EqualTo("splash"));
        }

        [TestCase(TargetingSplash.Cross)]
        [TestCase(TargetingSplash.Diagonal)]
        [TestCase(TargetingSplash.Radial)]
        public void Single_PreservesIndependentSplashAndPercentage(TargetingSplash splash)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, splash, 30);

            Assert.That(method.Splash, Is.EqualTo(splash));
            Assert.That(method.SplashPercent, Is.EqualTo(30));
            Assert.That(AttackProfileCompatibility.GetSupportedAimKind(method), Is.EqualTo(AttackAimKind.Cell));
        }

        [TestCase(TargetingChoiceMode.Direction, TargetingSplash.Cross)]
        [TestCase(TargetingChoiceMode.Direction, TargetingSplash.Diagonal)]
        [TestCase(TargetingChoiceMode.Direction, TargetingSplash.Radial)]
        [TestCase(TargetingChoiceMode.All, TargetingSplash.Cross)]
        [TestCase(TargetingChoiceMode.All, TargetingSplash.Diagonal)]
        [TestCase(TargetingChoiceMode.All, TargetingSplash.Radial)]
        public void NonSingle_RejectsSplashEvenAtZeroPercent(
            TargetingChoiceMode choice, TargetingSplash splash)
        {
            Assert.That(Assert.Throws<ArgumentException>(() =>
                new TargetingMethodSpec(TargetingShape.RadialSquare, choice, splash, 0)).ParamName,
                Is.EqualTo("splash"));
        }

        [TestCase(TargetingSplash.None)]
        [TestCase(TargetingSplash.Cross)]
        public void Method_RejectsNegativePercentage(TargetingSplash splash)
        {
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single,
                    splash, -1)).ParamName, Is.EqualTo("splashPercent"));
        }

        [TestCase(0)]
        [TestCase(101)]
        [TestCase(int.MaxValue)]
        public void Method_DoesNotInventProductionPercentageCap(int percentage)
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare,
                TargetingChoiceMode.Single, TargetingSplash.Cross, percentage);

            Assert.That(method.SplashPercent, Is.EqualTo(percentage));
        }

        [Test]
        public void NoSplash_DoesNotBecomeActiveFromPercentage()
        {
            var method = new TargetingMethodSpec(TargetingShape.Fan,
                TargetingChoiceMode.All, TargetingSplash.None, 50);

            Assert.That(method.Splash, Is.EqualTo(TargetingSplash.None));
            Assert.That(AttackProfileCompatibility.GetSupportedAimKind(method), Is.EqualTo(AttackAimKind.All));
        }

        [Test]
        public void MethodCompatibility_RejectsMissingDefinition()
        {
            Assert.Throws<ArgumentNullException>(() =>
                AttackProfileCompatibility.GetSupportedAimKind(null));
        }

        [TestCase(AttackDirection.Up)]
        [TestCase(AttackDirection.UpRight)]
        [TestCase(AttackDirection.Right)]
        [TestCase(AttackDirection.DownRight)]
        [TestCase(AttackDirection.Down)]
        [TestCase(AttackDirection.DownLeft)]
        [TestCase(AttackDirection.Left)]
        [TestCase(AttackDirection.UpLeft)]
        public void DirectionIntent_HasOnlyDirectionAndDefersShapeLegality(AttackDirection direction)
        {
            AttackAim aim = AttackAim.ForDirection(direction);

            Assert.That(aim.Kind, Is.EqualTo(AttackAimKind.Direction));
            Assert.That(aim.Direction, Is.EqualTo(direction));
            Assert.That(aim.Cell, Is.Null);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(9)]
        public void DirectionIntent_RejectsNoneAndUnknownDirections(int direction)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AttackAim.ForDirection((AttackDirection)direction));
        }

        [Test]
        public void AllIntent_RequiresNoDummyCellEntityOrDirection()
        {
            AttackAim aim = AttackAim.ForAll();

            Assert.That(aim.Kind, Is.EqualTo(AttackAimKind.All));
            Assert.That(aim.Cell, Is.Null);
            Assert.That(aim.Direction, Is.EqualTo(AttackDirection.None));
            Assert.That(default(AttackAim).Kind, Is.EqualTo(AttackAimKind.None));
        }

        [Test]
        public void CellAim_HasNoHeading()
        {
            AttackAim cell = AttackAim.ForCell(new CellCoord(-1, 2));

            Assert.That(cell.Kind, Is.EqualTo(AttackAimKind.Cell));
            Assert.That(cell.Cell, Is.EqualTo(new CellCoord(-1, 2)));
            Assert.That(cell.Direction, Is.EqualTo(AttackDirection.None));
            Assert.That(typeof(AttackAim).GetMethod("ForCell", new[] { typeof(CellCoord), typeof(AttackDirection) }),
                Is.Null);
        }

        [Test]
        public void AimContract_HasNoEntityPayloadOrFactory()
        {
            Assert.That(typeof(AttackAim).GetProperty("EntityId"), Is.Null);
            Assert.That(Array.Exists(typeof(AttackAim).GetMethods(), method => method.Name == "ForEntity"), Is.False);
        }
    }
}
