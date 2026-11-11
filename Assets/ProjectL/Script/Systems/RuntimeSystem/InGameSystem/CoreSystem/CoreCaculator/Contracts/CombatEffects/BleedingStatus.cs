using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects
{
    public readonly struct BleedingStack
    {
        public EffectOriginKey Origin { get; }
        public int Damage { get; }

        public BleedingStack(EffectOriginKey origin, int damage)
        {
            if (!origin.IsValid) throw new ArgumentException("유효한 출처가 필요합니다.", nameof(origin));
            if (damage < 1) throw new ArgumentOutOfRangeException(nameof(damage));
            Origin = origin;
            Damage = damage;
        }
    }

    public sealed class BleedingStatus : AppliedStatus
    {
        public IReadOnlyList<BleedingStack> Stacks { get; }
        public int RemainingAffectedSideStarts { get; }
        public override StatusEffectLifetimeKind Lifetime => StatusEffectLifetimeKind.AffectedSideTurnStarts;

        public BleedingStatus(IReadOnlyList<BleedingStack> stacks, int remainingAffectedSideStarts)
            : base(StatusEffectKey.Bleeding, stacks != null && stacks.Count > 0 ? stacks[0].Origin : (EffectOriginKey?)null)
        {
            if (stacks == null) throw new ArgumentNullException(nameof(stacks));
            if (stacks.Count < 1 || stacks.Count > 5) throw new ArgumentException("출혈은 1~5중첩이어야 합니다.", nameof(stacks));
            if (remainingAffectedSideStarts < 1) throw new ArgumentOutOfRangeException(nameof(remainingAffectedSideStarts));
            var copy = new BleedingStack[stacks.Count];
            for (int i = 0; i < copy.Length; i++)
            {
                if (!stacks[i].Origin.IsValid || stacks[i].Damage < 1)
                    throw new ArgumentException("유효한 출혈 기여분이 필요합니다.", nameof(stacks));
                copy[i] = stacks[i];
            }
            Stacks = Array.AsReadOnly(copy);
            RemainingAffectedSideStarts = remainingAffectedSideStarts;
        }

        public override bool SameValues(AppliedStatus other)
        {
            if (other is not BleedingStatus status || RemainingAffectedSideStarts != status.RemainingAffectedSideStarts ||
                Stacks.Count != status.Stacks.Count) return false;
            for (int i = 0; i < Stacks.Count; i++)
                if (Stacks[i].Origin != status.Stacks[i].Origin || Stacks[i].Damage != status.Stacks[i].Damage) return false;
            return true;
        }
    }
}
