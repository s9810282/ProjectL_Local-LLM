using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>소속이 확인된 선행 발동의 위치입니다. 표시 유무·시작 시각·예약 상태는 보관하지 않습니다.</summary>
    internal readonly struct CombatPreAttackTraitBinding
    {
        internal CombatPreAttackTraitBinding(CombatPreAttackSourceKey attack, TraitActivationSnapshot trait,
            int eventIndex, int activationIndex, int? pullEventIndex)
        {
            Attack = attack;
            Trait = trait;
            EventIndex = eventIndex;
            ActivationIndex = activationIndex;
            PullEventIndex = pullEventIndex;
        }

        internal CombatPreAttackSourceKey Attack { get; }
        internal TraitActivationSnapshot Trait { get; }
        internal int EventIndex { get; }
        internal int ActivationIndex { get; }

        /// <summary>당김 이동 사건의 위치입니다. 버프 발동은 null이며 특성 표시 실행 시작을 사용합니다.</summary>
        internal int? PullEventIndex { get; }
    }
}
