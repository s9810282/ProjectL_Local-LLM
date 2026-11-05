using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>넉백 요약 값과 결과·미리보기의 대응 검사를 확인한다(설계 2026-10-27 §4, 계획 T1.4).</summary>
    public sealed class AttackKnockbackSnapshotTests
    {
        private static readonly EntityId Actor = new EntityId(1);
        private static readonly EntityId Target = new EntityId(2);
        private static readonly EntityId Other = new EntityId(3);
        private static readonly CellCoord Start = new CellCoord(2, 3);
        private static readonly UnitActionUseSettlementSnapshot Cost = new UnitActionUseSettlementSnapshot(Actor,UnitActionUse.Available,UnitActionUse.Attacked);
        private static readonly CategorySnapshot Category = CategorySnapshot.CreateStateless(CategoryKey.Main);

        private static AttackKnockbackSnapshot Record(AttackKnockbackStop stop, int distance, params CellCoord[] path) =>
            Record(Target, CellCoord.Right, stop, distance, path);

        private static AttackKnockbackSnapshot Record(EntityId id, CellCoord step, AttackKnockbackStop stop, int distance,
            params CellCoord[] path) =>
            new AttackKnockbackSnapshot(id, AttackKnockbackTargetKind.Character, Start, step, distance, path, stop);

        private static CellCoord At(int x, int y) => new CellCoord(x, y);

        [Test]
        public void Completed_FollowsStepAndEndsAtLastCell()
        {
            var record = Record(AttackKnockbackStop.Completed, 2, At(3, 3), At(4, 3));

            Assert.That(record.Path, Is.EqualTo(new[] { At(3, 3), At(4, 3) }));
            Assert.That(record.FinalAnchor, Is.EqualTo(At(4, 3)));
            Assert.That(record.RequestedDistance, Is.EqualTo(2));
        }

        [Test]
        public void Diagonal_IsAllowed()
        {
            var record = Record(Target, new CellCoord(-1, 1), AttackKnockbackStop.Completed, 2, At(1, 4), At(0, 5));

            Assert.That(record.FinalAnchor, Is.EqualTo(At(0, 5)));
        }

        [Test]
        public void BlockedAtFirstCell_HasEmptyPathAndStaysAtStart()
        {
            var record = Record(AttackKnockbackStop.Blocked, 3);

            Assert.That(record.Path, Is.Empty);
            Assert.That(record.FinalAnchor, Is.EqualTo(Start));
        }

        [Test]
        public void BlockedMidway_EndsBeforeRequestedDistance()
        {
            var record = Record(AttackKnockbackStop.Blocked, 3, At(3, 3));

            Assert.That(record.FinalAnchor, Is.EqualTo(At(3, 3)));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void Interrupted_KeepsEnteredCells(int entered)
        {
            var path = new CellCoord[entered];
            for (int i = 0; i < entered; i++) path[i] = At(Start.X + i + 1, Start.Y);

            var record = Record(AttackKnockbackStop.Interrupted, 3, path);

            Assert.That(record.FinalAnchor, Is.EqualTo(path[entered - 1]));
        }

        [Test]
        public void Immune_HasEmptyPathAndKeepsDirection()
        {
            var record = Record(AttackKnockbackStop.Immune, 2);

            Assert.That(record.Path, Is.Empty);
            Assert.That(record.Step, Is.EqualTo(CellCoord.Right));
            Assert.That(record.FinalAnchor, Is.EqualTo(Start));
        }

        [Test]
        public void Path_IsCopied()
        {
            var path = new[] { At(3, 3) };
            var record = Record(AttackKnockbackStop.Completed, 1, path);
            path[0] = At(7, 7);

            Assert.That(record.Path[0], Is.EqualTo(At(3, 3)));
        }

        [Test]
        public void InvalidValues_AreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Record(EntityId.Empty, CellCoord.Right, AttackKnockbackStop.Blocked, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Record(Target, CellCoord.Zero, AttackKnockbackStop.Blocked, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Record(Target, new CellCoord(2, 0), AttackKnockbackStop.Blocked, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => Record(AttackKnockbackStop.Blocked, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackKnockbackSnapshot(Target, (AttackKnockbackTargetKind)0,
                Start, CellCoord.Right, 1, Array.Empty<CellCoord>(), AttackKnockbackStop.Blocked));
            Assert.Throws<ArgumentOutOfRangeException>(() => Record((AttackKnockbackStop)0, 1));
            Assert.Throws<ArgumentNullException>(() => new AttackKnockbackSnapshot(Target, AttackKnockbackTargetKind.Character,
                Start, CellCoord.Right, 1, null, AttackKnockbackStop.Blocked));
        }

        [Test]
        public void Path_MustFollowStepFromStart()
        {
            Assert.Throws<ArgumentException>(() => Record(AttackKnockbackStop.Completed, 1, At(2, 4)));
            Assert.Throws<ArgumentException>(() => Record(AttackKnockbackStop.Completed, 2, At(3, 3), At(5, 3)));
            Assert.Throws<ArgumentException>(() => Record(AttackKnockbackStop.Completed, 1, At(4, 3)));
        }

        [Test]
        public void CellCount_MustMatchStop()
        {
            Assert.Throws<ArgumentException>(() => Record(AttackKnockbackStop.Completed, 2, At(3, 3)));
            Assert.Throws<ArgumentException>(() => Record(AttackKnockbackStop.Completed, 1, At(3, 3), At(4, 3)));
            Assert.Throws<ArgumentException>(() => Record(AttackKnockbackStop.Blocked, 1, At(3, 3)));
            Assert.Throws<ArgumentException>(() => Record(AttackKnockbackStop.Interrupted, 2));
            Assert.Throws<ArgumentException>(() => Record(AttackKnockbackStop.Immune, 2, At(3, 3)));
        }

        private static AttackHitResult Hit(EntityId id) => new AttackHitResult(id, 20, 10, AttackTargetRole.Designated);

        private static AttackTargetPreview TargetPreview(EntityId id) =>
            new AttackTargetPreview(id, 10, 20, new[] { Start }, 10, 1, 1, 0, AttackTargetRole.Designated);

        private static AttackResult Result(AttackHitResult[] hits, params AttackKnockbackSnapshot[] knockbacks) =>
            AttackResult.FromFacts(Actor, EntityId.Empty, Rotation4.R0, Cost, new[] { Start }, hits,
                new AttackCellSelection(null, new[] { Start }, Array.Empty<CellCoord>()),
                knockbacks: knockbacks.Length == 0 ? null : knockbacks);

        [Test]
        public void Result_WithoutKnockbacks_IsEmpty()
        {
            Assert.That(Result(new[] { Hit(Target) }).Knockbacks, Is.Empty);
            Assert.That(default(AttackResult).Knockbacks, Is.Empty);
        }

        [Test]
        public void Result_KeepsKnockbacksInOrder()
        {
            var first = Record(AttackKnockbackStop.Blocked, 1);
            var second = Record(Other, CellCoord.Up, AttackKnockbackStop.Immune, 1);

            var result = Result(new[] { Hit(Target), Hit(Other) }, first, second);

            Assert.That(result.Knockbacks, Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void Result_RejectsKnockbackOutsideHitsDuplicateAndNull()
        {
            var record = Record(AttackKnockbackStop.Blocked, 1);

            Assert.Throws<ArgumentException>(() => Result(new[] { Hit(Other) }, record));
            Assert.Throws<ArgumentException>(() => Result(new[] { Hit(Target) }, record, Record(AttackKnockbackStop.Immune, 1)));
            Assert.Throws<ArgumentException>(() => Result(new[] { Hit(Target) }, new AttackKnockbackSnapshot[] { null }));
        }

        [Test]
        public void Preview_CarriesResultKnockbacks()
        {
            var record = Record(AttackKnockbackStop.Completed, 1, At(3, 3));
            var result = Result(new[] { Hit(Target) }, record);

            var preview = AttackPreview.FromFacts(result, new[] { TargetPreview(Target) },
                Array.Empty<TraitStatusSnapshot>(), Category);

            Assert.That(preview.Knockbacks, Is.EqualTo(new[] { record }));
        }

        [Test]
        public void Preview_RejectsKnockbackTargetMissingFromPreviewTargets()
        {
            var result = Result(new[] { Hit(Target), Hit(Other) }, Record(AttackKnockbackStop.Completed, 1, At(3, 3)));

            Assert.Throws<ArgumentException>(() => AttackPreview.FromFacts(result, new[] { TargetPreview(Other) },
                Array.Empty<TraitStatusSnapshot>(), Category));
        }

        [Test]
        public void OtherPreviewFactories_HaveNoKnockbacks()
        {
            Assert.That(AttackPreview.Failure(AttackFailureReason.NoEligibleTargets, Actor, EntityId.Empty).Knockbacks, Is.Empty);
            Assert.That(default(AttackPreview).Knockbacks, Is.Empty);
        }
    }
}
