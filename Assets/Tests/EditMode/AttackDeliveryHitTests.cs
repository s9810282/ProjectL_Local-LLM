using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Tests.Core
{
    /// <summary>일반 공격과 관통이 같은 모양의 전달 피격 목록을 내보내는지 확인한다(설계 2026-10-12 D1).</summary>
    public sealed class AttackDeliveryHitTests
    {
        private static readonly CellCoord ActorCell = new CellCoord(1, 3);

        [Test]
        public void OrdinarySplash_ExposesRolesPercentsOriginsAndAffectedCellsInSelectionOrder()
        {
            var delivery = Resolve(CombatCommandTestData.Input());
            var hits = delivery.Hits.ToArray();
            Assert.That(hits.Select(h => h.TargetId.Value), Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(hits.Select(h => h.Role), Is.EqualTo(new[] { AttackTargetRole.Primary, AttackTargetRole.Additional, AttackTargetRole.Additional }));
            Assert.That(hits.Select(h => h.BaseDamage), Is.EqualTo(new[] { 20, 20, 20 }));
            Assert.That(hits.Select(h => h.AppliedPercent), Is.EqualTo(new[] { 100, 70, 70 }));
            Assert.That(hits.Select(h => h.ImpactOrigin), Is.EqualTo(new CellCoord?[] { ActorCell, new CellCoord(4, 3), new CellCoord(4, 3) }));
            Assert.That(hits.All(h => h.AppliesDamage), Is.True);
            Assert.That(delivery.AffectedCells, Is.EqualTo(
                Enumerable.Range(2, 3).SelectMany(y => Enumerable.Range(3, 3).Select(x => new CellCoord(x, y)))));
            Assert.That(delivery.PiercingShot, Is.Null);
        }

        [Test]
        public void OrdinaryZeroPercentSplash_KeepsTheHitButAppliesNoDamage()
        {
            var method = new TargetingMethodSpec(TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.Radial, 0);
            var hits = Resolve(CombatCommandTestData.Input(method)).Hits.ToArray();
            Assert.That(hits.Select(h => h.TargetId.Value), Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(hits.Select(h => h.AppliesDamage), Is.EqualTo(new[] { true, false, false }));
        }

        [Test]
        public void Piercing_ExposesEncounterOrderFalloffBaseDamageAndTraveledCells()
        {
            var delivery = Resolve(PiercingShotTestData.Input());
            var hits = delivery.Hits.ToArray();
            Assert.That(hits.Select(h => h.TargetId.Value), Is.EqualTo(new[] { 9, 2, 7 }));
            Assert.That(hits.Select(h => h.Role), Is.EqualTo(new[] { AttackTargetRole.Effect, AttackTargetRole.Primary, AttackTargetRole.Effect }));
            Assert.That(hits.Select(h => h.BaseDamage), Is.EqualTo(new[] { 10, 7, 4 }));
            Assert.That(hits.All(h => h.AppliedPercent == 100 && h.ImpactOrigin == ActorCell), Is.True);
            Assert.That(delivery.AffectedCells, Is.EqualTo(delivery.PiercingShot.TraveledCells.ToArray()));
        }

        [Test]
        public void PiercingWithZeroBudget_HasNoHitsAndNoAffectedCells()
        {
            var delivery = Resolve(PiercingShotTestData.Input(cap: 0));
            Assert.That(delivery.PiercingShot, Is.Not.Null);
            Assert.That(delivery.Hits, Is.Empty);
            Assert.That(delivery.AffectedCells, Is.Empty);
        }

        [Test]
        public void Hit_RejectsInvalidFacts()
        {
            Assert.Throws<ArgumentException>(() => new DeliveredAttackHit(default, AttackTargetRole.Primary, 1, 100, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeliveredAttackHit(new EntityId(2), AttackTargetRole.None, 1, 100, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeliveredAttackHit(new EntityId(2), AttackTargetRole.Primary, -1, 100, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeliveredAttackHit(new EntityId(2), AttackTargetRole.Primary, 1, -1, null));
        }

        private static ResolvedAttackDelivery Resolve(CombatCommandInput input)
        {
            var actor = input.Characters[0];
            var profile = input.Profiles[0];
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight, input.State.Environment,
                input.EnvironmentBindings, input.State.WallBlocking);
            var evaluation = CombatAttackQueries.Selection(input, actor, profile, walls: walls);
            Assert.That(evaluation.FailureReason, Is.EqualTo(AttackFailureReason.None));
            return CombatAttackDelivery.Resolve(input, actor, profile, evaluation.Selection,
                CombatAttackCalculator.CommonAttackDamage(input, actor, profile), walls, AttackElevation.From(input.State.Environment, input.EnvironmentBindings));
        }
    }
}
