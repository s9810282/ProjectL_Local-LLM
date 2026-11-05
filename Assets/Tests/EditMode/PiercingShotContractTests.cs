using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class PiercingShotContractTests
    {
        private static readonly EntityId Actor = new EntityId(1);
        private static readonly EntityId Selected = new EntityId(2);
        private static readonly EntityId Interceptor = new EntityId(9);
        private static readonly CellCoord Origin = new CellCoord(1, 3);
        private static readonly CellCoord Aim = new CellCoord(5, 3);
        private static readonly CellCoord Contact = new CellCoord(3, 3);
        private static readonly CellCoord[] Route = { new CellCoord(2, 3), Contact };
        private static readonly UnitActionUseSettlementSnapshot Cost = new UnitActionUseSettlementSnapshot(new EntityId(1),UnitActionUse.Available,UnitActionUse.Attacked);
        private static readonly CategorySnapshot Category = CategorySnapshot.CreateStateless(CategoryKey.Main);
        private static readonly StageSnapshot Stage = new StageSnapshot(8, 8, StagePhase.Combat,
            Array.Empty<EntitySnapshot>(), Array.Empty<ReserveSlotSnapshot>());

        private static EffectOriginKey Source(int owner = 1) =>
            new EffectOriginKey(EffectOwnerKind.Trait, owner, (int)TraitKey.PiercingFalloff, "main");

        private static PiercingShotEncounter Encounter(EntityId? id = null, int index = 0,
            CellCoord? cell = null, int percent = 100) =>
            new PiercingShotEncounter(id ?? Interceptor, index, cell ?? Contact, percent);

        private static PiercingShotDelivery Delivery(CellCoord[] route = null,
            PiercingShotEncounter[] encounters = null, CellCoord? endpoint = null,
            PiercingShotStopReason stop = PiercingShotStopReason.TargetBudget, CellCoord? step = null,
            CellCoord? aim = null, EffectOriginKey? source = null) =>
            new PiercingShotDelivery(source ?? Source(), Origin, aim ?? Aim, step ?? CellCoord.Right,
                route ?? Route, encounters ?? new[] { Encounter() }, endpoint ?? Contact, stop);

        private static AttackCellSelection Selection(CellCoord? aim = null) =>
            new AttackCellSelection(aim ?? Aim, new[] { aim ?? Aim }, Array.Empty<CellCoord>());

        private static AttackHitResult Hit(EntityId? id = null, AttackTargetRole role = AttackTargetRole.Effect,
            int splash = 100) => new AttackHitResult(id ?? Interceptor, 20, 10, role, splash);

        private static AttackTargetPreview Target(EntityId? id = null,
            AttackTargetRole role = AttackTargetRole.Effect, int splash = 100) =>
            new AttackTargetPreview(id ?? Interceptor, 10, 20, new[] { Contact }, 10, 1, 1, 0,
                role, splash);

        private static AttackResult Result(string factory, EntityId selected, PiercingShotDelivery delivery,
            AttackHitResult[] hits = null, AttackCellSelection selection = null, CellCoord[] affected = null)
        {
            hits ??= new[] { Hit() };
            selection ??= Selection();
            affected ??= Route;
            if (factory == "Success")
                return AttackResult.Success(Actor, selected, Rotation4.R0, Cost, affected, hits, Stage,
                    Array.Empty<TraitActivationSnapshot>(), Array.Empty<CategoryActivationSnapshot>(), selection,
                    piercingShot: delivery);
            if (factory == "SuccessForCell")
                return AttackResult.SuccessForCell(Actor, selection.PrimaryCell.Value, Rotation4.R0, Cost,
                    affected, hits, Stage, Array.Empty<TraitActivationSnapshot>(),
                    Array.Empty<CategoryActivationSnapshot>(), piercingShot: delivery);
            return AttackResult.FromFacts(Actor, selected, Rotation4.R0, Cost, affected, hits, selection,
                piercingShot: delivery);
        }

        private static AttackPreview Preview(string factory, EntityId selected, PiercingShotDelivery delivery,
            AttackTargetPreview[] targets = null, AttackCellSelection selection = null, CellCoord[] affected = null)
        {
            targets ??= new[] { Target() };
            selection ??= Selection();
            affected ??= Route;
            if (factory == "Success")
                return AttackPreview.Success(Actor, selected, Rotation4.R0, Cost, affected, targets,
                    Array.Empty<TraitStatusSnapshot>(), Category, selection, piercingShot: delivery);
            if (factory == "SuccessForCell")
                return AttackPreview.SuccessForCell(Actor, selection.PrimaryCell.Value, Rotation4.R0, Cost,
                    affected, targets, Array.Empty<TraitStatusSnapshot>(), Category, piercingShot: delivery);
            return AttackPreview.FromFacts(Result("FromFacts", selected, delivery), targets,
                Array.Empty<TraitStatusSnapshot>(), Category);
        }

        [TestCase("Success")]
        [TestCase("FromFacts")]
        public void SelectedBWithoutHit_RetainsPrimaryIdentity(string factory)
        {
            var delivery = Delivery();
            var result = Result(factory, Selected, delivery);
            var preview = Preview(factory, Selected, delivery);
            Assert.That(result.PrimaryTargetId, Is.EqualTo(Selected));
            Assert.That(preview.PrimaryTargetId, Is.EqualTo(Selected));
            Assert.That(result.PrimaryCell, Is.EqualTo(Aim));
            Assert.That(result.DesignatedCells.ToArray(), Is.EqualTo(new[] { Aim }));
            Assert.That(result.AdditionalCells.IsEmpty, Is.True);
            Assert.That(result.Hits.Length, Is.EqualTo(1));
            Assert.That(result.Hits.Span[0].TargetId, Is.EqualTo(Interceptor));
            Assert.That(result.PiercingShot, Is.SameAs(delivery));
            Assert.That(preview.PiercingShot, Is.SameAs(delivery));
        }

        [TestCase("Success")]
        [TestCase("SuccessForCell")]
        [TestCase("FromFacts")]
        public void EmptyAim_AllHitsAreEffect(string factory)
        {
            var delivery = Delivery();
            var result = Result(factory, EntityId.Empty, delivery);
            var preview = Preview(factory, EntityId.Empty, delivery);
            Assert.That(result.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(preview.PrimaryTargetId.IsEmpty, Is.True);
            Assert.That(result.PrimaryCell, Is.EqualTo(Aim));
            Assert.That(preview.PrimaryCell, Is.EqualTo(Aim));
            Assert.That(result.PiercingShot, Is.SameAs(delivery));
            Assert.That(preview.PiercingShot, Is.SameAs(delivery));
            Assert.That(result.Hits.Span[0].Role, Is.EqualTo(AttackTargetRole.Effect));
            Assert.That(preview.Targets.Span[0].Role, Is.EqualTo(AttackTargetRole.Effect));
        }

        [Test]
        public void EffectHit_DoesNotUseSplashPercent()
        {
            var method = new TargetingMethodSpec(TargetingShape.EightDirectionLine,
                TargetingChoiceMode.Single, TargetingSplash.None, 0);
            Assert.That(Hit().AppliedSplashPercent, Is.EqualTo(100));
            Assert.That(Target().AppliedSplashPercent, Is.EqualTo(100));
            Assert.Throws<ArgumentException>(() => Hit(splash: method.SplashPercent));
            Assert.Throws<ArgumentException>(() => Target(splash: method.SplashPercent));
        }

        [Test]
        public void Constructor_CopiesArraysBeforePublishingToResultAndPreview()
        {
            var route = (CellCoord[])Route.Clone();
            var encounters = new[] { Encounter() };
            var delivery = Delivery(route, encounters);
            var result = Result("FromFacts", Selected, delivery);
            var preview = AttackPreview.FromFacts(result, new[] { Target() }, Array.Empty<TraitStatusSnapshot>(), Category);
            route[0] = Aim;
            encounters[0] = Encounter(Selected);
            Assert.That(result.PiercingShot.TraveledCells.ToArray(), Is.EqualTo(Route));
            Assert.That(preview.PiercingShot.Encounters.Span[0].TargetId, Is.EqualTo(Interceptor));
        }

        [TestCase(1, 0)] [TestCase(-1, 0)] [TestCase(0, 1)] [TestCase(0, -1)]
        [TestCase(1, 1)] [TestCase(1, -1)] [TestCase(-1, 1)] [TestCase(-1, -1)]
        public void Delivery_AcceptsAllEightSignedUnitSteps(int x, int y)
        {
            var origin = new CellCoord(4, 4);
            var next = new CellCoord(4 + x, 4 + y);
            var delivery = new PiercingShotDelivery(Source(), origin, new CellCoord(4 + 3*x, 4 + 3*y),
                new CellCoord(x, y), new[] { next }, Array.Empty<PiercingShotEncounter>(), next,
                PiercingShotStopReason.BoardEdge);
            Assert.That(delivery.Step, Is.EqualTo(new CellCoord(x, y)));
            Assert.That(delivery.OriginCell, Is.EqualTo(origin));
        }

        [TestCase(0, 0)] [TestCase(2, 0)] [TestCase(1, 2)] [TestCase(int.MinValue, 0)]
        public void Delivery_RejectsInvalidSteps(int x, int y) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => Delivery(step: new CellCoord(x, y)));

        [TestCase(1, 3)] [TestCase(0, 3)] [TestCase(5, 4)] [TestCase(1, 5)]
        public void Delivery_RejectsAimOffTheForwardRay(int x, int y) =>
            Assert.Throws<ArgumentException>(() => Delivery(aim: new CellCoord(x, y)));

        [Test]
        public void Delivery_RejectsSkippedReversedAndOffRayTravel()
        {
            Assert.Throws<ArgumentException>(() => Delivery(route: new[] { Contact }));
            Assert.Throws<ArgumentException>(() => Delivery(route: new[] { Contact, Route[0] }));
            Assert.Throws<ArgumentException>(() => Delivery(route: new[] { Route[0], new CellCoord(3, 4) }));
        }

        [TestCase(0)] [TestCase(101)] [TestCase(-1)]
        public void Encounter_RejectsNonPositiveOrExcessPercentage(int percent) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => Encounter(percent: percent));

        [Test]
        public void Encounter_RejectsEmptyIdentityAndNegativeIndex()
        {
            Assert.Throws<ArgumentException>(() => Encounter(EntityId.Empty));
            Assert.Throws<ArgumentOutOfRangeException>(() => Encounter(index: -1));
        }

        [Test]
        public void Delivery_RejectsInvalidEncounterSequence()
        {
            Assert.Throws<ArgumentException>(() => Delivery(encounters: new[] { Encounter(index: 1) }));
            Assert.Throws<ArgumentException>(() => Delivery(encounters: new[] { default(PiercingShotEncounter) }));
            Assert.Throws<ArgumentException>(() => Delivery(encounters: new[] { Encounter(cell: Aim) }));
            Assert.Throws<ArgumentException>(() => Delivery(encounters: new[]
                { Encounter(cell: Route[0]), Encounter(index: 1) }));
            Assert.Throws<ArgumentException>(() => Delivery(encounters: new[]
                { Encounter(), Encounter(Selected, 1, Route[0]) }));
        }

        [Test]
        public void Delivery_WallEndsAtExcludedNextCell_OtherStopsEndAtLastTravelCell()
        {
            var blocked = new CellCoord(4, 3);
            Assert.That(Delivery(endpoint: blocked, stop: PiercingShotStopReason.Wall).Endpoint, Is.EqualTo(blocked));
            Assert.Throws<ArgumentException>(() => Delivery(endpoint: Contact, stop: PiercingShotStopReason.Wall));
            Assert.Throws<ArgumentException>(() => Delivery(endpoint: Aim, stop: PiercingShotStopReason.Wall));
            foreach (var reason in new[] { PiercingShotStopReason.Structure,
                         PiercingShotStopReason.BoardEdge, PiercingShotStopReason.TargetBudget })
            {
                Assert.That(Delivery(stop: reason).Endpoint, Is.EqualTo(Contact));
                Assert.Throws<ArgumentException>(() => Delivery(endpoint: blocked, stop: reason));
            }
        }

        [Test]
        public void Delivery_EmptyRouteOnlyRepresentsZeroBudgetAtOrigin()
        {
            var delivery = Delivery(Array.Empty<CellCoord>(), Array.Empty<PiercingShotEncounter>(), Origin);
            var result = Result("FromFacts", Selected, delivery, Array.Empty<AttackHitResult>(), affected: Array.Empty<CellCoord>());
            Assert.That(result.Hits.IsEmpty, Is.True);
            Assert.That(result.PrimaryTargetId, Is.EqualTo(Selected));
            Assert.Throws<ArgumentException>(() => Delivery(Array.Empty<CellCoord>(), new[] { Encounter() }, Origin));
            Assert.Throws<ArgumentException>(() => Delivery(Array.Empty<CellCoord>(), Array.Empty<PiercingShotEncounter>(), Aim));
            foreach (var reason in new[] { PiercingShotStopReason.Wall, PiercingShotStopReason.Structure,
                         PiercingShotStopReason.BoardEdge })
                Assert.Throws<ArgumentException>(() => Delivery(Array.Empty<CellCoord>(), Array.Empty<PiercingShotEncounter>(), Origin, reason));
        }

        [TestCase("Success")]
        [TestCase("SuccessForCell")]
        [TestCase("FromFacts")]
        public void Factories_RejectUnattributedEffectOrMismatchedDelivery(string factory)
        {
            Assert.Throws<ArgumentException>(() => Result(factory, EntityId.Empty, null));
            Assert.Throws<ArgumentException>(() => Preview(factory, EntityId.Empty, null));
            foreach (var bad in new[] { Delivery(source: Source(4)), Delivery(aim: new CellCoord(6, 3)),
                         Delivery(encounters: new[] { Encounter(new EntityId(7)) }) })
            {
                Assert.Throws<ArgumentException>(() => Result(factory, EntityId.Empty, bad));
                Assert.Throws<ArgumentException>(() => Preview(factory, EntityId.Empty, bad));
            }
        }

        [TestCase("Success")]
        [TestCase("SuccessForCell")]
        [TestCase("FromFacts")]
        public void Factories_RejectPrimaryMismatchMissingAndDuplicateHits(string factory)
        {
            Assert.Throws<ArgumentException>(() => Result(factory, Selected, Delivery(), new[] { Hit(role: AttackTargetRole.Primary) }));
            Assert.Throws<ArgumentException>(() => Preview(factory, Selected, Delivery(), new[] { Target(role: AttackTargetRole.Primary) }));
            Assert.Throws<ArgumentException>(() => Result(factory, Selected, Delivery(), Array.Empty<AttackHitResult>()));
            Assert.Throws<ArgumentException>(() => Preview(factory, Selected, Delivery(), Array.Empty<AttackTargetPreview>()));
            Assert.Throws<ArgumentException>(() => Result(factory, Selected, Delivery(), new[] { Hit(), Hit() }));
            Assert.Throws<ArgumentException>(() => Preview(factory, Selected, Delivery(), new[] { Target(), Target() }));
        }

        [Test]
        public void Factories_RejectRouteMismatchSplashAndMissingAim()
        {
            Assert.Throws<ArgumentException>(() => Result("FromFacts", Selected, Delivery(), affected: new[] { Aim }));
            Assert.Throws<ArgumentException>(() => Preview("Success", Selected, Delivery(), affected: new[] { Aim }));
            var splash = new AttackCellSelection(Aim, new[] { Aim }, new[] { new CellCoord(5, 4) });
            Assert.Throws<ArgumentException>(() => Result("Success", Selected, Delivery(), selection: splash));
            Assert.Throws<ArgumentException>(() => Preview("Success", Selected, Delivery(), selection: splash));
            var direction = new AttackCellSelection(null, new[] { Aim }, Array.Empty<CellCoord>());
            Assert.Throws<ArgumentException>(() => Result("FromFacts", EntityId.Empty, Delivery(), selection: direction));
            Assert.Throws<ArgumentException>(() => Preview("Success", EntityId.Empty, Delivery(), selection: direction));
        }

        [Test]
        public void FailureAndCancellation_HaveNoDelivery()
        {
            Assert.That(AttackResult.Failure(AttackFailureReason.UnsupportedAttackDelivery, Actor, Selected, Stage).PiercingShot, Is.Null);
            Assert.That(AttackPreview.Failure(AttackFailureReason.UnsupportedAttackDelivery, Actor, Selected).PiercingShot, Is.Null);
            Assert.Throws<ArgumentException>(() => AttackResult.FromFacts(Actor, Selected, Rotation4.R0,
                new UnitActionUseSettlementSnapshot(new EntityId(1),UnitActionUse.Available,UnitActionUse.Attacked), Array.Empty<CellCoord>(), Array.Empty<AttackHitResult>(), Selection(),
                AttackFailureReason.Dead, piercingShot: Delivery()));
        }

        [Test]
        public void ActualSelectedHit_IsPrimaryWhileEncounterOrderIgnoresHitArrayOrder()
        {
            var route = new[] { new CellCoord(2, 3), Contact, new CellCoord(4, 3), Aim };
            var delivery = Delivery(route, new[] { Encounter(), Encounter(Selected, 1, Aim, 70) }, Aim);
            var result = Result("FromFacts", Selected, delivery,
                new[] { Hit(Selected, AttackTargetRole.Primary), Hit() }, affected: route);
            var preview = AttackPreview.FromFacts(result,
                new[] { Target(Selected, AttackTargetRole.Primary), Target() }, Array.Empty<TraitStatusSnapshot>(), Category);
            Assert.That(preview.PiercingShot.Encounters.Span[0].TargetId, Is.EqualTo(Interceptor));
            Assert.That(preview.PiercingShot.Encounters.Span[1].TargetId, Is.EqualTo(Selected));
            Assert.That(preview.PiercingShot.Encounters.Span[1].RemainingPercent, Is.EqualTo(70));
            Assert.Throws<ArgumentException>(() => Result("FromFacts", Selected, delivery,
                new[] { Hit(Selected), Hit() }, affected: route));
        }

        [Test]
        public void Delivery_RejectsMissingSourceCollectionsAndInvalidStopReason()
        {
            Assert.Throws<ArgumentException>(() => Delivery(source: default(EffectOriginKey)));
            Assert.Throws<ArgumentOutOfRangeException>(() => Delivery(stop: (PiercingShotStopReason)0));
            // 4는 지운 옛 Range(사거리 끝)다. 관통 진행은 사거리로 끝나지 않는다.
            Assert.Throws<ArgumentOutOfRangeException>(() => Delivery(stop: (PiercingShotStopReason)4));
            Assert.Throws<ArgumentNullException>(() => new PiercingShotDelivery(Source(), Origin, Aim,
                CellCoord.Right, null, Array.Empty<PiercingShotEncounter>(), Origin, PiercingShotStopReason.TargetBudget));
            Assert.Throws<ArgumentNullException>(() => new PiercingShotDelivery(Source(), Origin, Aim,
                CellCoord.Right, Route, null, Contact, PiercingShotStopReason.BoardEdge));
        }

        [Test]
        public void Delivery_WidenedCoordinateArithmeticDoesNotWrap()
        {
            Assert.Throws<ArgumentException>(() => new PiercingShotDelivery(Source(), new CellCoord(int.MaxValue, 3),
                new CellCoord(int.MinValue, 3), CellCoord.Right, new[] { new CellCoord(int.MinValue, 3) },
                Array.Empty<PiercingShotEncounter>(), new CellCoord(int.MinValue, 3), PiercingShotStopReason.BoardEdge));
        }

        [TestCase("Success")]
        [TestCase("SuccessForCell")]
        [TestCase("FromFacts")]
        public void NonPiercingEmptySingle_RemainsLegal(string factory)
        {
            Assert.That(Result(factory, EntityId.Empty, null, Array.Empty<AttackHitResult>()).Succeeded, Is.True);
            if (factory != "FromFacts")
                Assert.That(Preview(factory, EntityId.Empty, null, Array.Empty<AttackTargetPreview>()).CanAttack, Is.True);
            else
                Assert.That(AttackPreview.FromFacts(Result(factory, EntityId.Empty, null, Array.Empty<AttackHitResult>()),
                    Array.Empty<AttackTargetPreview>(), Array.Empty<TraitStatusSnapshot>(), Category).CanAttack, Is.True);
        }
    }
}
