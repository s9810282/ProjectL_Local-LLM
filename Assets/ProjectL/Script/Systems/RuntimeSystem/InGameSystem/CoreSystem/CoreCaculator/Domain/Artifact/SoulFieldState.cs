using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact
{
    internal sealed class SoulFieldState
    {
        internal static SoulFieldChange PlanCountdown(SoulFieldSnapshot field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            var remaining = new List<SoulFieldFacts>();
            foreach (SoulFieldFacts soul in field.Entries)
                if (soul.RemainingUnitTurns > 1)
                    remaining.Add(new SoulFieldFacts(soul.Id, soul.Origin, soul.Cell,
                        soul.RemainingUnitTurns - 1, soul.HealAmount));
            return new SoulFieldChange(field, new SoulFieldSnapshot(field.NextSoulId, remaining));
        }
    }
}
