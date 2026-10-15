using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results
{
    public sealed class CharacterDrawResult
    {
        public bool Succeeded { get; }
        public CharacterDrawFailureReason FailureReason { get; }
        public EntityId DrawnEntityId { get; }
        public EntityKey DrawnKey { get; }
        public int? ReserveSlotIndex { get; }
        public StageSnapshot CurrentStage { get; }

        private CharacterDrawResult(bool succeeded, CharacterDrawFailureReason reason,
            EntityId entityId, EntityKey key, int? slotIndex, StageSnapshot currentStage)
        {
            CurrentStage = currentStage ?? throw new ArgumentNullException(nameof(currentStage));
            if (!Enum.IsDefined(typeof(CharacterDrawFailureReason), reason))
                throw new ArgumentOutOfRangeException(nameof(reason));
            if (succeeded != (reason == CharacterDrawFailureReason.None))
                throw new ArgumentException("성공 여부와 실패 이유가 일치하지 않습니다.", nameof(reason));
            if (succeeded)
            {
                if (entityId.IsEmpty || key == EntityKey.None || !Enum.IsDefined(typeof(EntityKey), key))
                    throw new ArgumentException("성공한 뽑기에는 유효한 유닛 식별자가 필요합니다.");
                if (!slotIndex.HasValue || slotIndex.Value < 0 || slotIndex.Value >= currentStage.ReserveSlots.Length)
                    throw new ArgumentOutOfRangeException(nameof(slotIndex));
            }
            else if (!entityId.IsEmpty || key != EntityKey.None || slotIndex.HasValue)
                throw new ArgumentException("실패한 뽑기에는 생성 정보가 없어야 합니다.");

            Succeeded = succeeded;
            FailureReason = reason;
            DrawnEntityId = entityId;
            DrawnKey = key;
            ReserveSlotIndex = slotIndex;
        }

        public static CharacterDrawResult Success(EntityId entityId, EntityKey key,
            int slotIndex, StageSnapshot currentStage) =>
            new CharacterDrawResult(true, CharacterDrawFailureReason.None, entityId, key, slotIndex, currentStage);

        public static CharacterDrawResult Failure(CharacterDrawFailureReason reason, StageSnapshot currentStage) =>
            new CharacterDrawResult(false, reason, EntityId.Empty, EntityKey.None, null, currentStage);
    }
}
