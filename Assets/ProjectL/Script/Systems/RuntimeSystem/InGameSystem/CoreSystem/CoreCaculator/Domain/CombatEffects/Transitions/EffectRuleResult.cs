using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Transitions
{
    /// <summary>
    /// Rule 하나를 실행한 결과
    /// </summary>
    internal sealed class EffectRuleResult
    {
        public EffectStateChange? Change { get; }
        public IReadOnlyList<EffectActivation> Activations { get; }

        internal EffectRuleResult(EffectStateChange? change, EffectActivation[] activations)
        {
            if (activations == null) throw new ArgumentNullException(nameof(activations));
            var copied = (EffectActivation[])activations.Clone();

            Change = change;
            Activations = Array.AsReadOnly(copied);
        }
    }
}
