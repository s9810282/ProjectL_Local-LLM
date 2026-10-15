using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.GameFlow
{
    internal sealed class StageStartCatalog
    {
        private readonly Dictionary<StageKey, StageStartSpec> _specs;

        internal StageStartCatalog(IReadOnlyList<StageStartSpec> specs)
        {
            if (specs == null)
                throw new ArgumentNullException(nameof(specs));

            TraitRegistry traitRegistry = TraitRegistry.CreateDefault();
            _specs = new Dictionary<StageKey, StageStartSpec>(specs.Count);

            for (int i = 0; i < specs.Count; i++)
            {
                StageStartSpec spec = specs[i];
                if (spec == null)
                    throw new ArgumentException("spec는 null일 수 없습니다.", nameof(specs));
                if (_specs.ContainsKey(spec.StageKey))
                    throw new ArgumentException($"중복된 StageKey입니다: {spec.StageKey}", nameof(specs));

                _ = new UnitCatalog(spec.Units, traitRegistry);
                _ = new StructureCatalog(spec.Structures);
                _ = new CategoryMatchupResolver(spec.CategoryMatchups);
                _specs.Add(spec.StageKey, spec);
            }
        }

        internal bool Contains(StageKey key)
        {
            return _specs.ContainsKey(key);
        }

        internal bool TryGet(StageKey key, out StageStartSpec spec)
        {
            return _specs.TryGetValue(key, out spec);
        }

        internal void ValidateRequiredKeys(
            IReadOnlyList<StageKey> regularKeys,
            StageKey bossKey)
        {
            if (regularKeys == null)
                throw new ArgumentNullException(nameof(regularKeys));
            if (bossKey.IsEmpty)
                throw new ArgumentException("Boss StageKey는 비어 있을 수 없습니다.", nameof(bossKey));

            for (int i = 0; i < regularKeys.Count; i++)
            {
                StageKey key = regularKeys[i];
                if (key.IsEmpty)
                    throw new ArgumentException("일반 StageKey는 비어 있을 수 없습니다.", nameof(regularKeys));
                if (Contains(key) == false)
                    throw new ArgumentException($"StageStartSpec이 없습니다: {key}", nameof(regularKeys));
            }

            if (Contains(bossKey) == false)
                throw new ArgumentException($"StageStartSpec이 없습니다: {bossKey}", nameof(bossKey));
        }
    }
}
