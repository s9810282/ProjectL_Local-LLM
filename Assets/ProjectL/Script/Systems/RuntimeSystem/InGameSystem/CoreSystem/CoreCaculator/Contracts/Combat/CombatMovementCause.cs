using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    public enum CombatMovementAgency
    {
        Voluntary = 1,
        Forced = 2
    }

    /// <summary>실제 이동을 결정한 원인이다. 이동이 만든 특성 효과와는 구분한다.</summary>
    public sealed class CombatMovementCause
    {
        public CombatMovementAgency Agency { get; }
        public TraitKey? TraitKey { get; }

        public CombatMovementCause(CombatMovementAgency agency, TraitKey? traitKey = null)
        {
            if (!Enum.IsDefined(typeof(CombatMovementAgency), agency))
                throw new ArgumentOutOfRangeException(nameof(agency));
            if (traitKey.HasValue && (traitKey.Value == default(TraitKey) || !Enum.IsDefined(typeof(TraitKey), traitKey.Value)))
                throw new ArgumentOutOfRangeException(nameof(traitKey));
            Agency = agency;
            TraitKey = traitKey;
        }
    }
}
