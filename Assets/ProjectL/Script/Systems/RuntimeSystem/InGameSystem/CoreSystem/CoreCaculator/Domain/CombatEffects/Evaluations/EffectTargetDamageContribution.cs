using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations
{
    internal readonly struct EffectTargetDamageContribution
    {
        public EffectOriginKey Origin { get; }
        public StatusEffectKey? EffectKey => Numerator > Denominator ? StatusEffectKey.OutgoingDamageIncrease : (StatusEffectKey?)null;
        public bool BypassPhysicalDefense { get; }
        public int Numerator { get; }
        public int Denominator { get; }
        public EffectTargetDamageContribution(EffectOriginKey origin, bool bypassPhysicalDefense, int numerator, int denominator)
        {
            if (!origin.IsValid) throw new ArgumentException(nameof(origin));
            if (numerator < 1 || denominator < 1) throw new ArgumentOutOfRangeException(nameof(numerator));
            Origin = origin;
            BypassPhysicalDefense = bypassPhysicalDefense;
            Numerator = numerator;
            Denominator = denominator;
        }
    }
}
