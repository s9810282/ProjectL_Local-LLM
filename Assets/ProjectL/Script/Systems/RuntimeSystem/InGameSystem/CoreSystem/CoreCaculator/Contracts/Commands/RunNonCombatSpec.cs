using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands
{
    public sealed class RunNonCombatSpec
    {
        public ReadOnlyMemory<NonCombatStageBinding> Bindings { get; }
        public int ShopBaseSeed { get; }

        public RunNonCombatSpec(IReadOnlyList<NonCombatStageBinding> bindings, int shopBaseSeed)
        {
            if (bindings == null) throw new ArgumentNullException(nameof(bindings));
            var copy = new NonCombatStageBinding[bindings.Count];
            var keys = new HashSet<StageKey>();
            for (int i = 0; i < copy.Length; i++)
            {
                var binding = bindings[i];
                if (binding == null || !keys.Add(binding.StageKey))
                    throw new ArgumentException("Bindings must be non-null and have unique stage keys.", nameof(bindings));
                copy[i] = binding;
            }
            Bindings = copy;
            ShopBaseSeed = shopBaseSeed;
        }
    }
}
