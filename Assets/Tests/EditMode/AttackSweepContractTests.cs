using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    public sealed class AttackSweepContractTests
    {
        [Test]
        public void SideDefaultsOnlyAtResolution()
        {
            Assert.That(AttackSweepGeometry.ResolveSide(null), Is.EqualTo(AttackSweepSide.Right));
            Assert.That(AttackSweepGeometry.ResolveSide(AttackSweepSide.Right), Is.EqualTo(AttackSweepSide.Right));
            Assert.That(AttackSweepGeometry.ResolveSide(AttackSweepSide.Left), Is.EqualTo(AttackSweepSide.Left));
            Assert.Throws<ArgumentOutOfRangeException>(() => AttackSweepGeometry.ResolveSide((AttackSweepSide)0));
            Assert.Throws<ArgumentOutOfRangeException>(() => AttackSweepGeometry.ResolveSide((AttackSweepSide)3));
        }

        [TestCase(AttackDirection.Up, Rotation4.R0)]
        [TestCase(AttackDirection.Right, Rotation4.R90)]
        [TestCase(AttackDirection.Down, Rotation4.R180)]
        [TestCase(AttackDirection.Left, Rotation4.R270)]
        public void FacingUsesAimedDirectionOrRetainsExistingFacingForAll(AttackDirection direction, Rotation4 expected)
        {
            Assert.That(AttackSweepGeometry.ResolveFacing(AttackAim.ForDirection(direction), CellCoord.Zero, Rotation4.R270),
                Is.EqualTo(expected));
            Assert.That(AttackSweepGeometry.ResolveFacing(AttackAim.ForAll(), CellCoord.Zero, expected), Is.EqualTo(expected));
            var aimed = AttackAim.ForCell(new CellCoord(1, 1));
            Assert.That(AttackSweepGeometry.ResolveFacing(aimed, CellCoord.Zero, expected), Is.EqualTo(Rotation4.R90));
            Assert.That(CombatCommandCalculator.AttackRotation(aimed, CellCoord.Zero), Is.EqualTo(Rotation4.R90));
        }

        [Test]
        public void SameIntentCopiesPreserveSideAndNewIntentCanClearIt()
        {
            var basis = CombatCommandTestData.Input();
            var left = basis.WithIntent(basis.Kind, basis.Aim, basis.TargetCell, null, null, AttackSweepSide.Left);
            var copied = left.WithActor(left.ActorId).WithWorld(left.State, left.Characters, left.Main, left.SoulCounts,
                left.Souls, left.EnvironmentBindings).WithIntent(left.Kind, left.Aim, left.TargetCell, null, null);
            Assert.That(basis.SweepSide, Is.Null);
            Assert.That(copied.SweepSide, Is.EqualTo(AttackSweepSide.Left));
            Assert.That(copied.WithIntent(copied.Kind, copied.Aim, copied.TargetCell, null, null, null).SweepSide, Is.Null);
            Assert.That(CombatCommandComposition.Intent(copied, CombatCommandKind.Attack, copied.Aim).SweepSide, Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => copied.WithIntent(copied.Kind, copied.Aim, copied.TargetCell, null, null, (AttackSweepSide)0));
        }

        [Test]
        public void MovementOnlyDeathIsPreservedInReadOnlyResultAndEvent()
        {
            var actor = new EntityId(1);
            var target = new EntityId(2);
            var start = new CellCoord(0, 1);
            var end = new CellCoord(2, 1);
            var first = new CellCoord(1, 1);
            var path = new[] { first };
            var movement = new AttackSweepTargetSnapshot(target, start, end, path, null, AttackSweepStop.Interrupted);
            var targets = new[] { movement };
            var ids = new[] { target };
            var area = new[] { start, first };
            var sweep = new AttackSweepSnapshot(new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.DeadHand, "test"),
                actor, Rotation4.R0, AttackSweepSide.Right, area, ids, targets, false);

            path[0] = default;
            targets[0] = null;
            ids[0] = default;
            area[0] = default;

            Assert.That(sweep.AreaCells[0], Is.EqualTo(start));
            Assert.That(sweep.CapturedTargetIds[0], Is.EqualTo(target));
            Assert.That(sweep.Targets[0].EnteredCells[0], Is.EqualTo(first));
            Assert.Throws<NotSupportedException>(() => ((IList<CellCoord>)movement.EnteredCells)[0] = default);
            var result = new CombatSweepEvent(sweep, 1);
            Assert.That(result.Kind, Is.EqualTo(CombatEventKind.Swept));
            Assert.That(result.Sweep, Is.SameAs(sweep));

            var attack = AttackResult.FromFacts(actor, EntityId.Empty, Rotation4.R0,
                new UnitActionUseSettlementSnapshot(actor, UnitActionUse.Available, UnitActionUse.Attacked),
                new[] { start, first }, Array.Empty<AttackHitResult>(),
                new AttackCellSelection(null, new[] { start, first }, Array.Empty<CellCoord>()), sweep: sweep);
            var preview = AttackPreview.FromFacts(attack, Array.Empty<AttackTargetPreview>(), Array.Empty<TraitStatusSnapshot>(),
                CategorySnapshot.CreateStateless(CategoryKey.Main));
            Assert.That(preview.Sweep, Is.SameAs(sweep));
            Assert.That(preview.Targets.Length, Is.Zero);

            var input = CombatCommandTestData.Input();
            var transition = CombatCommandCalculator.Calculate(input, Array.Empty<IMovementTileRule>());
            Assert.That(transition.Accepted, Is.True, transition.Reason);
            Assert.That(transition.Attack.Value.Sweep, Is.Null);
            Assert.That(CombatCommandCalculator.Preview(input, Array.Empty<IMovementTileRule>()).Sweep, Is.Null);
        }

        [Test]
        public void SweepRequiresExactlyOneOutcomePerCapturedTargetInSpatialOrder()
        {
            var actor = new EntityId(1);
            var near = new EntityId(2);
            var far = new EntityId(3);
            var nearCell = new CellCoord(0, 1);
            var farCell = new CellCoord(1, 1);
            var endpoint = new CellCoord(2, 1);
            var origin = new EffectOriginKey(EffectOwnerKind.Trait, 1, (int)TraitKey.DeadHand, "main");
            var nearOutcome = new AttackSweepTargetSnapshot(near, nearCell, endpoint, Array.Empty<CellCoord>(),
                nearCell, AttackSweepStop.Blocked);
            var farOutcome = new AttackSweepTargetSnapshot(far, farCell, endpoint, Array.Empty<CellCoord>(),
                farCell, AttackSweepStop.Immune);
            AttackSweepSnapshot Build(params AttackSweepTargetSnapshot[] targets) =>
                new AttackSweepSnapshot(origin, actor, Rotation4.R0, AttackSweepSide.Right, new[] { nearCell, farCell },
                    new[] { near, far }, targets, false);

            Assert.That(Build(farOutcome, nearOutcome).Targets, Is.EqualTo(new[] { farOutcome, nearOutcome }));
            Assert.Throws<ArgumentException>(() => Build(nearOutcome, farOutcome));
            Assert.Throws<ArgumentException>(() => Build(farOutcome));
            Assert.Throws<ArgumentException>(() => Build(farOutcome, farOutcome));
            var beyondEdge = new AttackSweepTargetSnapshot(near, nearCell, new CellCoord(3, 1), Array.Empty<CellCoord>(),
                nearCell, AttackSweepStop.Blocked);
            Assert.Throws<ArgumentException>(() => Build(farOutcome, beyondEdge));
        }
    }
}
