using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>넥서스 증원으로 만든 개체의 주변 효과 적용 전 사실이다.</summary>
    public sealed class CombatReinforcementEvent : CombatEvent
    {
        public EntityId NexusId { get; }
        public NexusReinforcementRecord Record { get; }
        public CharacterCombatFacts Created { get; }

        public CombatReinforcementEvent(EntityId nexusId, NexusReinforcementRecord record, CharacterCombatFacts created)
            : base(CombatEventKind.Reinforced)
        {
            if (nexusId.IsEmpty)
                throw new ArgumentException("증원 넥서스 ID가 필요합니다.", nameof(nexusId));
            Record = record ?? throw new ArgumentNullException(nameof(record));
            Created = created ?? throw new ArgumentNullException(nameof(created));
            if (Created.EntityId != Record.EntityId)
                throw new ArgumentException("증원 사건과 생성 이력의 개체 ID가 다릅니다.", nameof(created));

            NexusId = nexusId;
        }
    }
}
