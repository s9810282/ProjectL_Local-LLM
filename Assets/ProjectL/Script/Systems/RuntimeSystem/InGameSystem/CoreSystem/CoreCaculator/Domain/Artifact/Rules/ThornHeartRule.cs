using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules
{
    internal sealed class ThornHeartRule : ICombatEffectRule
    {
        internal int ReflectionDamage { get; }

        internal ThornHeartRule(int reflectionDamage)
        {
            if (reflectionDamage < 0)
                throw new ArgumentOutOfRangeException(nameof(reflectionDamage));

            ReflectionDamage = reflectionDamage;
        }

        internal int EvaluateDamage(bool isNight, bool hasActualEnemyAttackDamageToAlly)
        {
            return isNight && hasActualEnemyAttackDamageToAlly
                ? ReflectionDamage
                : 0;
        }
    }
}
