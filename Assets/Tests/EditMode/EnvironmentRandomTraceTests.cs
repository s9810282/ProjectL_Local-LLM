using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    /// <summary>기믹 검증기가 공유하는 환경 난수 추적(리뷰 D3)의 판단 규칙을 고정한다.</summary>
    public sealed class EnvironmentRandomTraceTests
    {
        [Test]
        public void StartValueIsKnownUntilAnEventChangesIt()
        {
            var trace = new EnvironmentRandomTrace(123);

            Assert.That(trace.Matches(123), Is.True);
            Assert.That(trace.Matches(124), Is.False);
            trace.Observe(new CombatEffectsEvent(Array.Empty<TraitActivationSnapshot>(), Array.Empty<CategoryActivationSnapshot>()));
            Assert.That(trace.Matches(124), Is.False, "난수와 무관한 사건은 알려진 값을 바꾸지 않는다.");
        }

        [Test]
        public void DeclaringEventsReplaceTheKnownValue()
        {
            var trace = new EnvironmentRandomTrace(5);
            var chain = ChainWarning(5, 9);

            trace.Observe(chain);
            Assert.That(trace.Matches(9), Is.True);
            Assert.That(trace.Matches(5), Is.False);

            var selected = Selected(9);
            trace.Observe(selected);
            Assert.That(trace.Matches(selected.FinalState), Is.True);
            Assert.That(trace.Matches(9), Is.False);
        }

        [Test]
        public void HunterReselectionMakesTheValueUnknownUntilTheNextDeclaration()
        {
            var trace = new EnvironmentRandomTrace(5);

            trace.Observe(HunterSelected());
            Assert.That(trace.Matches(5), Is.True);
            Assert.That(trace.Matches(77), Is.True, "값을 남기지 않은 난수 사용 뒤에는 시작값을 판단하지 않는다.");

            trace.Observe(ChainWarning(77, 81));
            Assert.That(trace.Matches(81), Is.True);
            Assert.That(trace.Matches(77), Is.False);
        }

        [Test]
        public void EndsWithChecksTheFinalValueOnlyWhileTheDeclarationIsTheLastRandomUse()
        {
            var trace = new EnvironmentRandomTrace(5);
            var first = ChainWarning(5, 9);
            trace.Observe(first);

            Assert.That(trace.EndsWith(first, 9), Is.True);
            Assert.That(trace.EndsWith(first, 10), Is.False);

            trace.Observe(Selected(9));
            Assert.That(trace.EndsWith(first, 10), Is.True, "뒤의 선언 사건이 최종 값을 정한다.");

            var later = new EnvironmentRandomTrace(5);
            later.Observe(first);
            later.Observe(HunterSelected());
            Assert.That(later.EndsWith(first, 10), Is.True, "뒤에 값을 모르는 난수 사용이 있으면 판단하지 않는다.");
        }

        private static CombatLavaChainEvent ChainWarning(uint expected, uint final)
        {
            var id = new StageGimmickId(1);
            var head = new CellCoord(0, 2);
            return new CombatLavaChainEvent(LavaChainGimmickStep.Warning,
                new StageGimmickBoundary(Owner.Player, 2, StageGimmickBoundaryPhase.Warning),
                new LavaChainGimmickState(id, head, 2), new LavaChainGimmickState(id, head, 3, true, CellCoord.Right),
                new[] { head }, expected, final);
        }

        private static CombatEnvironmentRandomEvent Selected(uint expected)
        {
            uint final = expected;
            int index = EnvironmentRandomSequence.NextIndex(ref final, 3);
            return new CombatEnvironmentRandomEvent(expected, final, 3, index);
        }

        private static CombatEffectsEvent HunterSelected() => new(new[]
        {
            new TraitActivationSnapshot(new EntityId(1), new EntityId(2), TraitKey.Hunter, TraitActivationKind.HunterTargetSelected, 0)
        }, Array.Empty<CategoryActivationSnapshot>());
    }
}
