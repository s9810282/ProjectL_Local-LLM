using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Tests.Core
{
    /// <summary>Buffer 계약 지식 이전 계획 B-4: Buffer에서 옮긴 환경 원본 전이 판정이 옛 Buffer 규칙과 같은지 확인한다.</summary>
    public sealed class TileSuccessorTests
    {
        private const string IdentityMessage = "기존 환경 원본의 기능·식별 내용 변경 또는 재활성화는 허용하지 않습니다.";
        private const string TornadoMessage = "전투 중 토네이도 반복 간격을 변경할 수 없습니다.";
        private static readonly CellCoord Cell = new(2, 2);
        private static readonly Owner[] Known = { Owner.Player };

        [Test]
        public void SingleEntry_AcceptsExactlyOneMoreEntry()
        {
            Assert.That(Weak(3, 0).IsGroundSuccessor(Weak(3, 1), singleEntry: true), Is.True);
            Assert.That(Weak(3, 0).IsGroundSuccessor(Weak(3, 2), singleEntry: true), Is.False);
            Assert.That(Weak(3, 1).IsGroundSuccessor(Weak(3, 1), singleEntry: true), Is.False);
            Assert.That(Weak(3, 1).IsGroundSuccessor(Weak(3, 0), singleEntry: true), Is.False);
            Assert.That(Weak(3, 0).IsGroundSuccessor(Weak(4, 1), singleEntry: true), Is.False);
            Assert.That(Weak(3, 0, active: false).IsGroundSuccessor(Weak(3, 1, active: false), singleEntry: true), Is.False);
        }

        [Test]
        public void AccumulatedEntry_AcceptsAnyIncrease()
        {
            Assert.That(Weak(3, 0).IsGroundSuccessor(Weak(3, 1), singleEntry: false), Is.True);
            Assert.That(Weak(3, 0).IsGroundSuccessor(Weak(3, 2), singleEntry: false), Is.True);
            Assert.That(Weak(3, 2).IsGroundSuccessor(Weak(3, 2), singleEntry: false), Is.False);
        }

        [Test]
        public void Collapse_RequiresPendingCollapse()
        {
            var pending = Weak(1, 1);
            var cliff = new StaticTerrainFact(1, Cell, StaticTerrainKind.Cliff, true, Known);
            Assert.That(pending.IsGroundSuccessor(pending.Collapse(), singleEntry: true), Is.True);
            Assert.That(pending.IsGroundSuccessor(pending.Collapse(), singleEntry: false), Is.True);
            Assert.That(Weak(3, 0).IsGroundSuccessor(cliff, singleEntry: true), Is.False);
            Assert.That(Weak(3, 0).IsGroundSuccessor(cliff, singleEntry: false), Is.False);
        }

        [Test]
        public void MaxEntryCount_DoesNotOverflowAndStillCollapses()
        {
            var full = Weak(1, long.MaxValue);
            Assert.That(full.IsGroundSuccessor(Weak(1, long.MaxValue), singleEntry: true), Is.False);
            Assert.That(full.IsGroundSuccessor(full.Collapse(), singleEntry: true), Is.True);
        }

        [Test]
        public void SuccessorFailure_KeepsBufferRules()
        {
            var tornado = new TornadoSourceFact(5, Cell, Known, 3, true, 3);
            Assert.That(tornado.SuccessorFailure(tornado.AfterActivation(new CellCoord(3, 3))), Is.Null);
            Assert.That(tornado.SuccessorFailure(new TornadoSourceFact(5, Cell, Known, 3, true, 4)), Is.EqualTo(TornadoMessage));
            Assert.That(tornado.SuccessorFailure(new StaticTerrainFact(5, Cell, StaticTerrainKind.HighGround, true, Known)),
                Is.EqualTo(IdentityMessage));

            var mine = new MineSourceFact(6, Cell, 1, 10, true, Known);
            Assert.That(mine.SuccessorFailure(mine.WithActive(false)), Is.Null);
            Assert.That(mine.WithActive(false).SuccessorFailure(mine), Is.EqualTo(IdentityMessage));
            Assert.That(mine.SuccessorFailure(new MineSourceFact(6, Cell, 1, 11, true, Known)), Is.EqualTo(IdentityMessage));

            // 지형은 지금 Buffer처럼 형식만 본다(지반은 사건 사슬 검사가, 벽은 벽 검사가 따로 본다).
            var high = new StaticTerrainFact(7, Cell, StaticTerrainKind.HighGround, true, Known);
            Assert.That(high.SuccessorFailure(new StaticTerrainFact(7, Cell, StaticTerrainKind.Sandstorm, false, Known)), Is.Null);
        }

        [Test]
        public void ChangesOnlyActive_IsTheSharedObjectRule()
        {
            var mine = new MineSourceFact(6, Cell, 1, 10, true, Known);
            var inactive = mine.WithActive(false);
            Assert.That(mine.ChangesOnlyActive(inactive), Is.True);
            // Core 판정은 재활성화를 막지 않는다. 재활성화 금지는 Buffer 판정(SuccessorFailure)에만 있다.
            Assert.That(inactive.ChangesOnlyActive(mine), Is.True);
            Assert.That(mine.ChangesOnlyActive(new MineSourceFact(6, Cell, 1, 11, true, Known)), Is.False);
        }

        private static StaticTerrainFact Weak(int threshold, long count, bool active = true) =>
            new(1, Cell, StaticTerrainKind.WeakGround, active, Known, threshold, count);
    }
}
