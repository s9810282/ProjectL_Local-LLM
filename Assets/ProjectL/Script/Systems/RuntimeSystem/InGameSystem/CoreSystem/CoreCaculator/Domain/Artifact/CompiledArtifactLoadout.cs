using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.States;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact
{
    internal readonly struct CompiledArtifactRule
    {
        internal EffectOriginKey Origin { get; }
        internal ICombatEffectRule Rule { get; }

        internal CompiledArtifactRule(EffectOriginKey origin, ICombatEffectRule rule)
        {
            if (origin.IsValid == false) throw new ArgumentException(nameof(origin));
            Origin = origin;
            Rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }
    }

    internal sealed class CompiledArtifactLoadout
    {
        internal long Revision { get; }
        internal ReadOnlyMemory<CompiledArtifactRule> Rules { get; }

        internal CompiledArtifactLoadout(long revision, CompiledArtifactRule[] rules)
        {
            if (revision < 0) throw new ArgumentOutOfRangeException(nameof(revision));
            if (rules == null) throw new ArgumentNullException(nameof(rules));

            var copiedRules = new CompiledArtifactRule[rules.Length];
            Array.Copy(rules, copiedRules, rules.Length);
            for (int i = 1; i < copiedRules.Length; i++)
            {
                if (copiedRules[i - 1].Origin.CompareTo(copiedRules[i].Origin) >= 0)
                    throw new ArgumentException("Artifact rules must have unique, sorted origins.", nameof(rules));
            }

            Revision = revision;
            Rules = copiedRules;
        }

        internal EffectRuleBinding[] CaptureBindings()
        {
            ReadOnlySpan<CompiledArtifactRule> rules = Rules.Span;
            var bindings = new EffectRuleBinding[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                bindings[i] = new EffectRuleBinding(
                    rules[i].Origin,
                    rules[i].Rule,
                    StatelessEffectStateSnapshot.Instance);
            }

            return bindings;
        }
    }
}
