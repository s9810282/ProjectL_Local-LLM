using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>후속 지형·공격 사건보다 먼저 재생할 실제 끌어오기 이동입니다.</summary>
    public sealed class CombatPullEvent : CombatEvent
    {
        public AttackPullSnapshot Movement { get; }

        public CombatPullEvent(AttackPullSnapshot movement) : base(CombatEventKind.Pulled)
            => Movement = movement ?? throw new ArgumentNullException(nameof(movement));
    }
}
