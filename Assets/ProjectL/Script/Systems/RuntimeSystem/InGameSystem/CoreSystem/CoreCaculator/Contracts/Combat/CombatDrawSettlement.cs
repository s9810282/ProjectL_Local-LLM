using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public readonly struct CombatDrawAvailability
    {
        public CharacterDrawFailureReason FailureReason { get; }
        public bool CanDraw => FailureReason == CharacterDrawFailureReason.None;
        public int RemainingCount { get; }
        public int GoldCost { get; }
        public int? Slot { get; }
        public CombatDrawAvailability(CharacterDrawFailureReason reason, int remaining, int goldCost, int? slot = null)
        { FailureReason = reason; RemainingCount = remaining; GoldCost = goldCost; Slot = slot; }
    }

    /// <summary>신규 캐릭터·덱 커서·ID·스테이지 골드를 함께 게시하기 위한 구체적인 드로우 변경이다. 골드는 전투 상태의 스테이지 골드이며 Run 재화가 아니다.</summary>
    public sealed class CombatDrawSettlement
    {
        public EntityId EntityId { get; }
        public EntityKey CharacterKey { get; }
        public int Slot { get; }
        public int ExpectedCursor { get; }
        public int FinalCursor { get; }
        public long ExpectedNextEntityId { get; }
        public long FinalNextEntityId { get; }
        public int ExpectedGold { get; }
        public int FinalGold { get; }
        public CombatDrawSettlement(EntityId id, EntityKey key, int slot, int expectedCursor, long expectedNextEntityId,
            int expectedGold, int goldCost)
        {
            if (id.Value != expectedNextEntityId || slot < 0 || expectedCursor < 0 || expectedGold < goldCost || goldCost < 0)
                throw new ArgumentException("드로우 변경 값이 잘못되었습니다.");
            EntityId = id; CharacterKey = key; Slot = slot; ExpectedCursor = expectedCursor;
            FinalCursor = checked(expectedCursor + 1); ExpectedNextEntityId = expectedNextEntityId;
            FinalNextEntityId = checked(expectedNextEntityId + 1); ExpectedGold = expectedGold; FinalGold = expectedGold - goldCost;
        }
    }
}
