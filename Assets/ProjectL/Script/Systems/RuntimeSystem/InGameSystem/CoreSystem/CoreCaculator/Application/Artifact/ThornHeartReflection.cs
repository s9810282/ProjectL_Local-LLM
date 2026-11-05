using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact
{
    /// <summary>호출자가 같은 공격 투영에 반영할 반사 체력 변경과 공개 근거입니다.</summary>
    internal sealed class ThornHeartReflection
    {
        internal CharacterStateChange TargetChange { get; }
        internal CombatArtifactEvent Activation { get; }
        internal CombatDamageOrigin DamageOrigin { get; }

        internal ThornHeartReflection(CharacterStateChange targetChange, CombatArtifactEvent activation,
            CombatDamageOrigin damageOrigin)
        {
            TargetChange = targetChange ?? throw new ArgumentNullException(nameof(targetChange));
            Activation = activation ?? throw new ArgumentNullException(nameof(activation));
            DamageOrigin = damageOrigin ?? throw new ArgumentNullException(nameof(damageOrigin));
        }
    }
}
