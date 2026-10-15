using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact
{

    /// <summary>사망 사실과 영혼 정의를 받아 최종 필드 변경만 계산한다.</summary>
    internal sealed class StageSoulService
    {
        internal static SoulFieldChange PlanDeaths(SoulFieldSnapshot field,
            IReadOnlyList<SoulDeathFacts> deaths, bool isNight, CompiledArtifactLoadout loadout)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            if (deaths == null) throw new ArgumentNullException(nameof(deaths));
            var entries = new List<SoulFieldFacts>(field.Entries);
            var seen = new HashSet<EntityId>();
            foreach (SoulDeathFacts death in deaths)
            {
                if (death == null || !seen.Add(death.Transition.Expected.EntityId))
                    throw new ArgumentException("Null or duplicate death fact.", nameof(deaths));
            }
            var ordered = new List<SoulDeathFacts>(deaths);
            ordered.Sort((left, right) => left.Transition.Expected.EntityId.Value.CompareTo(
                right.Transition.Expected.EntityId.Value));
            long nextId = field.NextSoulId;
            foreach (SoulDeathFacts death in ordered)
            {
                CharacterCombatFacts before = death.Transition.Expected;
                foreach (CompiledArtifactRule compiled in loadout.Rules.Span)
                {
                    if (compiled.Rule is not SoulAltarRule rule ||
                        !rule.CanSpawn(isNight, before.Owner == Owner.Enemy)) continue;
                    long id = nextId;
                    nextId = checked(nextId + 1);
                    entries.Add(new SoulFieldFacts(id, compiled.Origin, death.Anchor,
                        rule.DurationUnitTurns, rule.Amounts.HealAmount));
                }
            }
            return new SoulFieldChange(field, new SoulFieldSnapshot(nextId, entries));
        }
    }
}
