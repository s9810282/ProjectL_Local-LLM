using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Rewards
{
    /// <summary>수락될 첫 전투 시작의 진입 효과 전 배치 인원을 한 번만 확정합니다.</summary>
    internal static class CombatParticipationCalculator
    {
        internal static CombatRewardFacts Capture(CombatRewardFacts current, IReadOnlyList<CharacterCombatFacts> characters)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            if (current.ParticipantsCaptured) return current;
            var participants = characters.Where(c => c.Owner == Owner.Player && c.OwnedId.HasValue &&
                c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead)
                .OrderBy(c => c.OwnershipRole == CharacterOwnershipRole.Main ? 0 : 1)
                .ThenBy(c => c.OwnedId.Value).Select(c => c.OwnedId.Value).Distinct().ToArray();
            return new CombatRewardFacts(true, participants, current.KillGoldEarned);
        }
    }
}
