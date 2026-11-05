using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>스테이지 동안 받은 방어구 파괴의 출처와 잔존 비율 원본입니다.</summary>
    public sealed class ArmorBreakState
    {
        public IReadOnlyList<ArmorBreakStack> Stacks { get; }
        public ArmorBreakState(IReadOnlyList<ArmorBreakStack> stacks)
        {
            if (stacks == null) throw new ArgumentNullException(nameof(stacks));
            if (stacks.Any(s => s == null)) throw new ArgumentException("중첩 원본이 필요합니다.", nameof(stacks));
            Stacks = Array.AsReadOnly(stacks.ToArray());
        }
        public bool SameValues(ArmorBreakState other) => other != null && Stacks.Count == other.Stacks.Count &&
            Stacks.Select((s, i) => s.SameValues(other.Stacks[i])).All(same => same);
    }

    public sealed class ArmorBreakStack
    {
        private readonly DefenseDecreaseStatus _status;
        public EffectOriginKey Origin => _status.Origin.Value;
        public int ReductionPercent => _status.ReductionPercent;
        public ArmorBreakStack(EffectOriginKey origin, int reductionPercent)
        {
            if (!origin.IsValid || origin.Kind != EffectOwnerKind.Trait || origin.OwnerInstanceId < 1 || origin.DefinitionKey != (int)TraitKey.ArmorBreaker)
                throw new ArgumentException("방어구 파괴의 특성 출처가 필요합니다.", nameof(origin));
            if (reductionPercent < 0 || reductionPercent > 100) throw new ArgumentOutOfRangeException(nameof(reductionPercent));
            _status = new DefenseDecreaseStatus(origin, reductionPercent);
        }
        internal ArmorBreakStack(DefenseDecreaseStatus status) => _status = status ?? throw new ArgumentNullException(nameof(status));
        public DefenseDecreaseStatus ToStatus() => _status;
        public bool SameValues(ArmorBreakStack other) => other != null && Origin == other.Origin && ReductionPercent == other.ReductionPercent;
    }
}
