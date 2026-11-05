using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>계산 순서에 따른 대상 표식의 변경 사실입니다. 소비는 Final이 null이며 별도 상태를 소유하지 않습니다.</summary>
    public sealed class CombatHuntersMarkEvent : CombatEvent
    {
        public EntityId TargetId { get; }
        public HuntersMarkState Expected { get; }
        public HuntersMarkState Final { get; }

        public CombatHuntersMarkEvent(EntityId targetId, HuntersMarkState expected, HuntersMarkState final)
            : base(CombatEventKind.HuntersMarkChanged)
        {
            if (targetId.IsEmpty) throw new ArgumentOutOfRangeException(nameof(targetId));
            if (expected == null ? final == null : expected.SameValues(final))
                throw new ArgumentException("표식 사건에는 실제 변경이 필요합니다.");
            TargetId = targetId;
            Expected = expected;
            Final = final;
        }
    }
}
