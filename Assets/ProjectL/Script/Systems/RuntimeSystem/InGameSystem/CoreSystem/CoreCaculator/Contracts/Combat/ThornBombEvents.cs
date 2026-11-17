using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum ThornBombTrigger
    {
        AttackHit = 1,
        ChainExplosion = 2,
        DueControllerTurn = 3,
        TargetDeath = 4,
        Replacement = 5
    }

    public sealed class CombatThornBombAttachedEvent : CombatEvent
    {
        public ThornBombSnapshot Bomb { get; }

        public CombatThornBombAttachedEvent(ThornBombSnapshot bomb) : base(CombatEventKind.ThornBombAttached)
        {
            Bomb = bomb ?? throw new ArgumentNullException(nameof(bomb));
        }
    }

    public sealed class CombatThornBombDetonatedEvent : CombatEvent
    {
        public ThornBombSnapshot Bomb { get; }
        public CellCoord Center { get; }
        public ThornBombTrigger Trigger { get; }
        public long AttackGroup { get; }

        public CombatThornBombDetonatedEvent(ThornBombSnapshot bomb, CellCoord center, ThornBombTrigger trigger, long attackGroup)
            : base(CombatEventKind.ThornBombDetonated)
        {
            if (!Enum.IsDefined(typeof(ThornBombTrigger), trigger)) throw new ArgumentOutOfRangeException(nameof(trigger));
            if (attackGroup < 1) throw new ArgumentOutOfRangeException(nameof(attackGroup));
            Bomb = bomb ?? throw new ArgumentNullException(nameof(bomb));
            Center = center;
            Trigger = trigger;
            AttackGroup = attackGroup;
        }
    }

    /// <summary>
    /// 부착 대상이 낙사해 터지지 않고 사라진 폭탄이다(설계 R15). 기폭과 달리 피해·넉백·연쇄가 없다.
    /// 같은 전이에 그 대상의 낙사 사건(<see cref="CombatFallEvent"/>)이 있어야 한다.
    /// </summary>
    public sealed class CombatThornBombDiscardedEvent : CombatEvent
    {
        public ThornBombSnapshot Bomb { get; }

        public CombatThornBombDiscardedEvent(ThornBombSnapshot bomb) : base(CombatEventKind.ThornBombDiscarded)
        {
            Bomb = bomb ?? throw new ArgumentNullException(nameof(bomb));
        }
    }
}
