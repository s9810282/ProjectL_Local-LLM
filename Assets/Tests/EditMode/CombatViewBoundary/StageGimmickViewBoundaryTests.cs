using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Tests.CombatViewBoundary
{
    public sealed class StageGimmickViewBoundaryTests
    {
        [Test]
        public void ConsumedObject_DisplayRetainsSourceIdentityCellPayloadAndVisibility()
        {
            var known = new[] { Owner.Player };
            var source = new MineSourceFact(31, CellCoord.Up, 4, 7, true, known);
            var activation = new CombatObjectActivationEvent(source, new EntityId(3));
            var consumed = new CombatMovementSourceEvent(source, source.WithActive(false));
            known[0] = Owner.Enemy;
            Assert.That(activation.Source.Id, Is.EqualTo(31));
            Assert.That(activation.Source.Cell, Is.EqualTo(CellCoord.Up));
            Assert.That(((MineSourceFact)activation.Source).Damage, Is.EqualTo(7));
            Assert.That(activation.Source.KnownTo, Is.EqualTo(new[] { Owner.Player }));
            Assert.That(activation.Source.Active, Is.True);
            Assert.That(consumed.Expected, Is.SameAs(activation.Source));
            Assert.That(consumed.Final.Active, Is.False);
        }

        [Test]
        public void DestroyedObstacle_DisplayRetainsCreditAndPhysicalOriginSeparately()
        {
            var origin = new CombatDamageOrigin(CellCoord.Up, new EntityId(3), 7);
            var e = new CombatObstacleReactionEvent(new EntityId(9), ObstacleContentKind.PowderBarrel,
                ObstacleReactionKind.Destroyed, origin, 8);
            Assert.That(e.ObstacleId, Is.EqualTo(new EntityId(9)));
            Assert.That(e.Origin.PhysicalCell, Is.EqualTo(CellCoord.Up));
            Assert.That(e.Origin.CreditedCharacterId, Is.EqualTo(new EntityId(3)));
            Assert.That(e.Origin.AttackGroup, Is.EqualTo(7));
            Assert.That(e.AttackGroup, Is.EqualTo(8));
        }

        [Test]
        public void TornadoDisplayFacts_RetainOriginalAndFinalSourceAfterSubsequentRelocation()
        {
            var source = new TornadoSourceFact(31, CellCoord.Zero, new[] { Owner.Player });
            var final = source.AfterActivation(CellCoord.Right);
            var e = new CombatTornadoEvent(source, final);
            var later = final.AfterActivation(CellCoord.Up);
            Assert.That(e.Kind, Is.EqualTo(CombatEventKind.TornadoActivated));
            Assert.That(e.Expected.Cell, Is.EqualTo(CellCoord.Zero));
            Assert.That(e.Final.Cell, Is.EqualTo(CellCoord.Right));
            Assert.That(e.Final.NextDueCompletedTurn, Is.EqualTo(6));
            Assert.That(later.NextDueCompletedTurn, Is.EqualTo(9));
            Assert.Throws<ArgumentException>(() => new CombatTornadoEvent(source,
                new TornadoSourceFact(32, CellCoord.Right, new[] { Owner.Player })));
        }

        [Test]
        public void RailwayDisplayFacts_CopyCellsAndCasualties()
        {
            CellCoord[] cells = { new CellCoord(1, 2) };
            EntityId[] killed = { new EntityId(7) };
            var result = new RailwayGimmickActivationSnapshot(new StageGimmickId(1), cells, killed);
            cells[0] = new CellCoord(9, 9);
            killed[0] = new EntityId(8);
            Assert.That(result.AffectedRailCells.Span[0], Is.EqualTo(new CellCoord(1, 2)));
            Assert.That(result.KilledUnitIds.Span[0], Is.EqualTo(new EntityId(7)));
        }

        [Test]
        public void RailwayDisplayFacts_RejectNullArrays()
        {
            Assert.Throws<ArgumentNullException>(() => new RailwayGimmickActivationSnapshot(new StageGimmickId(1), null, Array.Empty<EntityId>()));
            Assert.Throws<ArgumentNullException>(() => new RailwayGimmickActivationSnapshot(new StageGimmickId(1), Array.Empty<CellCoord>(), null));
        }
    }
}
