using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>
    /// 경험치 사건이다. ExperienceEarned는 처치 하나의 경험치가 받는 아군의 이번 전투 획득량에 쌓인 사실,
    /// ExperienceApplied는 승리 때 쌓인 경험치가 보유 기록(레벨·진행 경험치)에 반영된 사실이다.
    /// </summary>
    public sealed class CombatExperienceEvent : CombatEvent
    {
        /// <summary>경험치를 받은 아군 전투 개체다.</summary>
        public EntityId RecipientId { get; }
        /// <summary>처치된 적이다. ExperienceApplied에서는 null이다.</summary>
        public EntityId? VictimId { get; }
        /// <summary>쌓인 처치 경험치다. ExperienceApplied에서는 반영된 전투 획득량 합계다.</summary>
        public int Amount { get; }
        /// <summary>반영된 보유 기록 변경이다. ExperienceEarned에서는 null이다.</summary>
        public OwnedCharacterChange Owned { get; }
        /// <summary>반영으로 오른 레벨 수다.</summary>
        public int LevelsGained { get; }
        /// <summary>메인이 오른 레벨마다 고른 랜덤 성장이다(오른 순서). 서브와 ExperienceEarned에서는 비어 있다.</summary>
        public IReadOnlyList<MainGrowthKind> MainGrowth { get; }

        private CombatExperienceEvent(CombatEventKind kind, EntityId recipientId, EntityId? victimId, int amount,
            OwnedCharacterChange owned, int levelsGained, IReadOnlyList<MainGrowthKind> mainGrowth) : base(kind)
        {
            if (recipientId.IsEmpty) throw new ArgumentException("경험치를 받은 개체가 필요합니다.", nameof(recipientId));
            if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
            if (levelsGained < 0) throw new ArgumentOutOfRangeException(nameof(levelsGained));
            RecipientId = recipientId; VictimId = victimId; Amount = amount; Owned = owned; LevelsGained = levelsGained;
            MainGrowth = Array.AsReadOnly((mainGrowth ?? Array.Empty<MainGrowthKind>()).ToArray());
            if (MainGrowth.Count != 0 && (owned == null || owned.Final.Role != CharacterOwnershipRole.Main ||
                                          MainGrowth.Count != levelsGained || MainGrowth.Contains(MainGrowthKind.None)))
                throw new ArgumentException("메인 성장은 메인의 오른 레벨 수만큼 있어야 합니다.", nameof(mainGrowth));
        }

        public static CombatExperienceEvent Earned(EntityId victimId, EntityId recipientId, int amount)
        {
            if (victimId.IsEmpty) throw new ArgumentException("처치된 적이 필요합니다.", nameof(victimId));
            return new CombatExperienceEvent(CombatEventKind.ExperienceEarned, recipientId, victimId, amount, null, 0, null);
        }

        /// <summary>승리 반영 사실이다. 오른 레벨 수는 보유 기록 변경의 레벨 차와 같아야 한다.</summary>
        public static CombatExperienceEvent Applied(EntityId recipientId, OwnedCharacterChange owned, int amount,
            int levelsGained, IReadOnlyList<MainGrowthKind> mainGrowth = null)
        {
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            if (levelsGained != owned.Final.Level - owned.Expected.Level)
                throw new ArgumentException("오른 레벨 수가 보유 기록 변경과 다릅니다.", nameof(levelsGained));
            return new CombatExperienceEvent(CombatEventKind.ExperienceApplied, recipientId, null, amount, owned,
                levelsGained, mainGrowth);
        }
    }
}
