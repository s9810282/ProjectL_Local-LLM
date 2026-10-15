using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions
{
    /// <summary>
    /// 이번 행동에서 해당 Effect가 실제로 발동했다
    /// </summary>
    internal readonly struct EffectActivation
    {
        public EffectOriginKey Origin { get; }
        /// <summary>
        /// 코드는 Trait일 땐 해당 Kind로 해석, Category일 땐 해당 Kind로 해석한다.
        /// </summary>
        public int Code { get; }
        public int Magnitude { get; }
        public EntityId TargetId { get; }

        internal EffectActivation(EffectOriginKey origin, int code, int magnitude, EntityId targetId)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            if (code <= 0) throw new ArgumentOutOfRangeException(nameof(code));
            if (magnitude < 0) throw new ArgumentOutOfRangeException(nameof(magnitude));

            Origin = origin;
            Code = code;
            Magnitude = magnitude;
            TargetId = targetId;
        }
    }
}
