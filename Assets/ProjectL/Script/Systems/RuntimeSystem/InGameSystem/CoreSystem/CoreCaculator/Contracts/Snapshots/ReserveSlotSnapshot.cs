using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// View가 Reserve 슬롯 하나의 번호와 점유 Entity를 표시할 때 읽는 복사본이다.
    /// </summary>
    public readonly struct ReserveSlotSnapshot
    {
        public int SlotIndex { get; }
        public EntityId OccupantId { get; }
        public bool IsOccupied => OccupantId != EntityId.Empty;

        public ReserveSlotSnapshot(int slotIndex, EntityId occupantId)
        {
            if (slotIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(slotIndex));

            SlotIndex = slotIndex;
            OccupantId = occupantId;
        }
    }
}
