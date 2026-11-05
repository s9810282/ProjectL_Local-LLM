using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects
{
    /// <summary>명령 안에서만 공유하는 불변 효과 조상 목록입니다.</summary>
    internal sealed class EffectCausalChain
    {
        private readonly EffectOriginKey[] _origins;
        public static EffectCausalChain Empty { get; } = new(Array.Empty<EffectOriginKey>());
        private EffectCausalChain(EffectOriginKey[] origins)
        {
            _origins = origins;
        }
        public bool Contains(EffectOriginKey origin) => Array.IndexOf(_origins, origin) >= 0;
        public EffectCausalChain Append(EffectOriginKey origin)
        {
            if (!origin.IsValid)
                throw new ArgumentException("유효한 효과 출처가 필요합니다.", nameof(origin));
            return new EffectCausalChain(_origins.Concat(new[] { origin }).ToArray());
        }
    }
}
