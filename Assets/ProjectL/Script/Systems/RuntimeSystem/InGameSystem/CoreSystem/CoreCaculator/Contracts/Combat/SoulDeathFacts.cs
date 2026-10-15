using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>Records the actual death location before exclusion removes the final field position.</summary>
    public sealed class SoulDeathFacts
    {
        public CharacterStateChange Transition { get; }
        public CellCoord Anchor { get; }

        public SoulDeathFacts(CharacterStateChange transition, CellCoord anchor)
        {
            Transition = transition ?? throw new ArgumentNullException(nameof(transition));
            CharacterCombatFacts before = transition.Expected;
            CharacterCombatFacts after = transition.Final;
            if (before.Health.IsDead || !after.Health.IsDead ||
                before.Area != CharacterRuntimeArea.Field ||
                before.Owner != after.Owner || before.CharacterKey != after.CharacterKey ||
                before.OwnedId != after.OwnedId || before.OwnershipRole != after.OwnershipRole)
                throw new ArgumentException("사망 사실은 같은 필드 캐릭터의 생존→사망 변경이어야 합니다.");
            if (after.Anchor.HasValue && after.Anchor.Value != anchor)
                throw new ArgumentException("남아 있는 사망 위치와 전달된 위치가 다릅니다.");
            Anchor = anchor;
        }
    }
}
