using System;
using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Tests.Core
{
    public sealed class WeakGroundStateTests
    {
        [Test]
        public void TerrainAndEventNumericValues_PreserveExistingCompatibility()
        {
            var terrainKinds = new[] { StaticTerrainKind.HighGround, StaticTerrainKind.Sandstorm,
                StaticTerrainKind.Wall, StaticTerrainKind.WeakGround, StaticTerrainKind.Cliff };
            Assert.That(terrainKinds.Select(kind => (int)kind), Is.EqualTo(Enumerable.Range(1, 5)));
            var eventKinds = new[] { CombatEventKind.Moved, CombatEventKind.Placed, CombatEventKind.Returned,
                CombatEventKind.HealthChanged, CombatEventKind.Died, CombatEventKind.Attacked,
                CombatEventKind.EffectsActivated, CombatEventKind.ArtifactActivated, CombatEventKind.SoulsCollected,
                CombatEventKind.SoulFieldSettled, CombatEventKind.CombatEntered,
                CombatEventKind.GimmickActivated,
                CombatEventKind.StageEnded, CombatEventKind.HealthNormalized, CombatEventKind.MovementSourceChanged,
                CombatEventKind.ObstacleReacted, CombatEventKind.HeldEnvironmentChanged,
                CombatEventKind.ObjectActivated, CombatEventKind.ObjectCreated, CombatEventKind.Relocated,
                CombatEventKind.TornadoActivated, CombatEventKind.Fell };
            Assert.That(eventKinds.Select(kind => (int)kind), Is.EqualTo(Enumerable.Range(1, 27).Where(value => value != 4 && value != 13 && value != 14 && value != 15 && value != 20)));
        }

        [Test]
        public void Entries_AccumulateWithoutMutatingOrCollapsingTheOriginal()
        {
            var original = Ground();
            var first = Register(original);
            var second = Register(first);
            var third = Register(second);

            Assert.That(Count(original), Is.Zero);
            Assert.That(Count(first), Is.EqualTo(1));
            Assert.That(Count(second), Is.EqualTo(2));
            Assert.That(Count(third), Is.EqualTo(3));
            Assert.That(Remaining(original), Is.EqualTo(2));
            Assert.That(Remaining(first), Is.EqualTo(1));
            Assert.That(Remaining(second), Is.Zero);
            Assert.That(Remaining(third), Is.Zero);
            Assert.That(Pending(first), Is.False);
            Assert.That(Pending(second), Is.True);
            Assert.That(third.Kind, Is.EqualTo(StaticTerrainKind.WeakGround));
        }

        [Test]
        public void Collapse_RequiresThresholdAndKeepsSourceIdentityAndVisibility()
        {
            var ground = Ground();
            Throws<InvalidOperationException>(() => Collapse(ground));
            var pending = Register(Register(ground));
            var cliff = Collapse(pending);

            Assert.That(cliff.Id, Is.EqualTo(71));
            Assert.That(cliff.Cell, Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(cliff.Active, Is.True);
            Assert.That(cliff.KnownTo, Is.EqualTo(new[] { Owner.Player, Owner.Enemy }));
            Assert.That(cliff.Kind, Is.EqualTo(StaticTerrainKind.Cliff));
            Assert.That(Threshold(cliff), Is.Zero);
            Assert.That(Count(cliff), Is.Zero);
            Assert.That(Remaining(cliff), Is.Zero);
            Assert.That(Pending(cliff), Is.False);
            Assert.That(pending.Kind, Is.EqualTo(StaticTerrainKind.WeakGround));
            Assert.That(Count(pending), Is.EqualTo(2));
            Throws<InvalidOperationException>(() => Register(cliff));
            Throws<InvalidOperationException>(() => Collapse(cliff));
        }

        [Test]
        public void InactiveGround_DoesNotBecomePendingOrAcceptEntries()
        {
            var ground = Ground(count: 2, active: false);
            Assert.That(Pending(ground), Is.False);
            Throws<InvalidOperationException>(() => Register(ground));
            Throws<InvalidOperationException>(() => Collapse(ground));
        }

        [Test]
        public void VisibilityProjection_PreservesProgressAndIndependentValues()
        {
            var ground = Ground(count: 1);
            var visible = (StaticTerrainFact)new MovementEnvironmentFacts(new[] { ground })
                .VisibleTo(Owner.Player).Tiles[0];
            Assert.That(Count(visible), Is.EqualTo(1));
            Assert.That(Threshold(visible), Is.EqualTo(2));
            Assert.That(Remaining(visible), Is.EqualTo(1));
            Assert.That(visible.KnownTo, Is.EqualTo(new[] { Owner.Player }));
            Assert.That(ground.KnownTo, Has.Count.EqualTo(2));
            Assert.That(ground.SameValues(Ground(count: 1)), Is.True);
            Assert.That(ground.SameValues(Ground(count: 0)), Is.False);
            Assert.That(ground.SameValues(Ground(threshold: 3, count: 1)), Is.False);
            Assert.That(ground.SameValues(visible), Is.False);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void WeakGround_RejectsNonPositiveThreshold(int threshold)
        {
            Throws<ArgumentOutOfRangeException>(() => Ground(threshold));
        }

        [Test]
        public void WeakGround_RejectsNegativeCount()
        {
            Throws<ArgumentOutOfRangeException>(() => Ground(count: -1));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        public void NonWeakGround_RejectsProgressInsteadOfSilentlyIgnoringIt(int kind)
        {
            Throws<ArgumentException>(() => Create((StaticTerrainKind)kind, 2, 0));
            Throws<ArgumentException>(() => Create((StaticTerrainKind)kind, 0, 1));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void LegacyTerrain_RemainsReadableAndCannotRegisterEntries(int serializedKind)
        {
            var terrain = new StaticTerrainFact(71, new CellCoord(2, 3),
                (StaticTerrainKind)serializedKind, true, new[] { Owner.Player });
            Assert.That(terrain.SameValues(terrain.WithKnownTo(new[] { Owner.Player })), Is.True);
            Assert.That(terrain.TriggersArrivalRule, Is.False);
            Throws<InvalidOperationException>(() => Register(terrain));
            Throws<InvalidOperationException>(() => Collapse(terrain));
        }

        [Test]
        public void LargeCount_RemainingDoesNotUnderflowAndIncrementDoesNotWrap()
        {
            var ground = Ground(threshold: int.MaxValue, count: long.MaxValue);
            Assert.That(Remaining(ground), Is.Zero);
            Assert.That(Pending(ground), Is.True);
            Throws<OverflowException>(() => Register(ground));
            Assert.That(Count(ground), Is.EqualTo(long.MaxValue));
        }

        [Test]
        public void FallEvent_RecordsEntitySourceAndCell()
        {
            var fall = Fall(new EntityId(7), 71);
            Assert.That(fall.EntityId, Is.EqualTo(new EntityId(7)));
            Assert.That(fall.TerrainSourceId, Is.EqualTo(71));
            Assert.That(fall.Cell, Is.EqualTo(new CellCoord(2, 3)));
            Assert.That(fall.Kind, Is.EqualTo(CombatEventKind.Fell));
        }

        [Test]
        public void FallEvent_RejectsEmptyEntity()
        {
            Throws<ArgumentOutOfRangeException>(() => Fall(EntityId.Empty, 71));
        }

        [TestCase(0L)]
        [TestCase(-1L)]
        public void FallEvent_RejectsInvalidTerrainSource(long sourceId)
        {
            Throws<ArgumentOutOfRangeException>(() => Fall(new EntityId(7), sourceId));
        }

        private static StaticTerrainFact Ground(int threshold = 2, long count = 0, bool active = true) =>
            Create(StaticTerrainKind.WeakGround, threshold, count, active);

        private static StaticTerrainFact Create(StaticTerrainKind kind, int threshold, long count, bool active = true) =>
            new StaticTerrainFact(71, new CellCoord(2, 3), kind, active,
                new[] { Owner.Player, Owner.Enemy }, threshold, count);

        private static StaticTerrainFact Register(StaticTerrainFact ground) => ground.RegisterEntry();
        private static StaticTerrainFact Collapse(StaticTerrainFact ground) => ground.Collapse();
        private static long Count(StaticTerrainFact ground) => ground.EntryCount;
        private static int Threshold(StaticTerrainFact ground) => ground.EntryThreshold;
        private static int Remaining(StaticTerrainFact ground) => ground.RemainingEntries;
        private static bool Pending(StaticTerrainFact ground) => ground.PendingCollapse;
        private static CombatFallEvent Fall(EntityId entityId, long sourceId) =>
            new CombatFallEvent(entityId, sourceId, new CellCoord(2, 3));
        private static void Throws<T>(TestDelegate action) where T : Exception => Assert.Throws<T>(action);
    }
}
